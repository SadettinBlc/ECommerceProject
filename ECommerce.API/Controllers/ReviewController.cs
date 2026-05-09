using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("{productId}")]
        public IActionResult GetProductReviews(int productId)
        {
            var reviews = _reviewRepository.Where(r => r.ProductId == productId && r.IsActive).ToList();
            return Ok(reviews);
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
                Rating = rating,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true
            };

            await _reviewRepository.AddAsync(newReview);
            result.Status = true;
            result.Message = "Yorum başarıyla eklendi.";
            return result;
        }
    }
}