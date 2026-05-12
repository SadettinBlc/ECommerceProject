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
    [Authorize] 
    public class FavoriteController : ControllerBase
    {
        private readonly IRepository<Favorite> _favoriteRepository;
        ResultDto result = new ResultDto();

        public FavoriteController(IRepository<Favorite> favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }

        [HttpPost]
        public async Task<ResultDto> Add(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            var exist = _favoriteRepository.Where(f => f.AppUserId == userId && f.ProductId == productId).Any();
            if (exist)
            {
                result.Status = false;
                result.Message = "Bu ürün zaten favorilerinizde!";
                return result;
            }

            var newFavorite = new Favorite
            {
                ProductId = productId,
                AppUserId = userId,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true
            };

            await _favoriteRepository.AddAsync(newFavorite);
            result.Status = true;
            result.Message = "Ürün favorilere eklendi.";
            return result;
        }

        [HttpDelete("{productId}")]
        public async Task<ResultDto> Remove(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var favorite = _favoriteRepository.Where(f => f.AppUserId == userId && f.ProductId == productId).FirstOrDefault();

            if (favorite == null)
            {
                result.Status = false;
                result.Message = "Bu ürün zaten favorilerinizde değil.";
                return result;
            }

            await _favoriteRepository.DeleteAsync(favorite.Id);

            result.Status = true;
            result.Message = "Ürün favorilerden çıkarıldı.";
            return result;
        }

        [HttpGet]
        public IActionResult GetMyFavorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            var favorites = _favoriteRepository.Where(f => f.AppUserId == userId && f.IsActive)
                .Include(f => f.Product)
                .Select(f => f.Product) 
                .ToList();

            if (favorites.Count == 0)
            {
                return NotFound("Henüz favorilere eklediğiniz bir ürün bulunmuyor.");
            }

            return Ok(favorites);
        }
    }
}