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
    [Authorize] // Favori eklemek ve görmek için giriş zorunlu
    public class FavoriteController : ControllerBase
    {
        private readonly IRepository<Favorite> _favoriteRepository;
        ResultDto result = new ResultDto();

        public FavoriteController(IRepository<Favorite> favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }

        [HttpGet]
        public IActionResult MyFavorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favorites = _favoriteRepository.Where(f => f.AppUserId == userId && f.IsActive).ToList();
            return Ok(favorites);
        }

        [HttpPost]
        public async Task<ResultDto> Add(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Ürün daha önce favorilere eklenmiş mi kontrolü
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
    }
}