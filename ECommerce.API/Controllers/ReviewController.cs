using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly IRepository<Review> _reviewRepository;
        ResultDto result = new ResultDto();

        public ReviewController(IRepository<Review> reviewRepository)
        {
            _reviewRepository = reviewRepository;
        }

        [HttpPost]
        [Authorize] // Yorum yapmak için giriş zorunlu
        public async Task<ResultDto> Add(int productId, string comment, int rating)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newReview = new Review
            {
                ProductId = productId,
                AppUserId = userId,
                Comment = comment,
                Rating = rating, // Yıldız puanı doğrudan modelden geliyor
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true
            };

            await _reviewRepository.AddAsync(newReview);
            result.Status = true;
            result.Message = "Yorumunuz başarıyla eklendi.";
            return result;
        }

        [HttpGet("{productId}")]
        public IActionResult GetProductReviews(int productId)
        {
            // 1. Ürüne ait aktif yorumları çek
            var reviews = _reviewRepository.Where(r => r.ProductId == productId && r.IsActive)
                .Include(r => r.AppUser) // Yorumu yapanın adını göstermek için
                .ToList();

            if (reviews.Count == 0)
            {
                return Ok(new { AverageRating = 0, TotalReviews = 0, Reviews = reviews });
            }

            // 2. Matematiksel olarak yıldız ortalamasını hesapla
            double average = reviews.Average(r => r.Rating);

            // İsimsiz obje ile arayüze hem ortalamayı hem de yorum listesini dönüyoruz
            return Ok(new
            {
                AverageRating = Math.Round(average, 1), // 4.2343 yerine 4.2 döner
                TotalReviews = reviews.Count,
                Reviews = reviews.Select(r => new {
                    r.Id,
                    UserName = r.AppUser?.FullName ?? "İsimsiz Kullanıcı", // Null ihtimaline karşı ufak bir güvenlik önlemi
                    r.Comment,
                    r.Rating,
                    r.Created
                })
            });
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetMyReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var reviews = _reviewRepository.Where(r => r.AppUserId == userId && r.IsActive)
                .Include(r => r.Product)
                .OrderByDescending(r => r.Created)
                .Select(r => new {
                    r.Id,
                    r.Comment,
                    r.Rating,
                    r.Created,
                    ProductName = r.Product.Name,
                    ProductId = r.ProductId
                }).ToList();

            return Ok(reviews);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ResultDto> DeleteReview(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = _reviewRepository.Where(r => r.Id == id && r.AppUserId == userId).FirstOrDefault();

            if (review == null)
            {
                result.Status = false;
                result.Message = "Yorum bulunamadı veya silme yetkiniz yok.";
                return result;
            }

            await _reviewRepository.DeleteAsync(id);
            result.Status = true;
            result.Message = "Yorumunuz başarıyla silindi.";
            return result;
        }

        [HttpGet]
        [Authorize] // Güvenlik için Admin rolü eklenebilir: [Authorize(Roles = "Admin")]
        public IActionResult GetAllReviewsAdmin()
        {
            // Tüm yorumları ürünü ve yazan kişiyi içerecek şekilde çekiyoruz
            var reviews = _reviewRepository.Where(r => r.IsActive)
                .Include(r => r.Product)
                .Include(r => r.AppUser)
                .OrderByDescending(r => r.Created)
                .Select(r => new {
                    r.Id,
                    r.Comment,
                    r.Rating,
                    r.Created,
                    ProductName = r.Product.Name,
                    UserName = r.AppUser.FullName ?? r.AppUser.UserName
                }).ToList();

            return Ok(reviews);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ResultDto> AdminDeleteReview(int id)
        {
            ResultDto result = new ResultDto();
            var review = await _reviewRepository.GetByIdAsync(id);

            if (review == null)
            {
                result.Status = false;
                result.Message = "Yorum bulunamadı.";
                return result;
            }

            await _reviewRepository.DeleteAsync(id);
            result.Status = true;
            result.Message = "Yorum sistemden tamamen silindi.";
            return result;
        }

    }
}