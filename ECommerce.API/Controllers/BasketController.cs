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
    public class BasketController : ControllerBase
    {
        private readonly IRepository<Basket> _basketRepository;
        private readonly IRepository<BasketItem> _basketItemRepository;
        private readonly IRepository<Product> _productRepository;
        ResultDto result = new ResultDto();

        public BasketController(IRepository<Basket> basketRepository, IRepository<BasketItem> basketItemRepository, IRepository<Product> productRepository)
        {
            _basketRepository = basketRepository;
            _basketItemRepository = basketItemRepository;
            _productRepository = productRepository;
        }

        [HttpGet]
        public IActionResult GetMyBasket()
        {
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            
            var basket = _basketRepository.Where(b => b.AppUserId == userId && b.IsActive)
                .Include(b => b.BasketItems)
                .ThenInclude(bi => bi.Product)
                .FirstOrDefault();

            if (basket == null || basket.BasketItems.Count == 0)
            {
                return NotFound("Sepetiniz şu an boş.");
            }

            return Ok(basket);
        }

        [HttpPost]
        public async Task<ResultDto> AddItem(int productId, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = await _productRepository.GetByIdAsync(productId);

            
            if (product == null)
            {
                result.Status = false;
                result.Message = "Ürün bulunamadı!";
                return result;
            }

            if (product.Stock < quantity)
            {
                result.Status = false;
                result.Message = $"Yetersiz stok! Sadece {product.Stock} adet ekleyebilirsiniz.";
                return result;
            }

           
            var basket = _basketRepository.Where(b => b.AppUserId == userId && b.IsActive).FirstOrDefault();
            if (basket == null)
            {
                basket = new Basket
                {
                    AppUserId = userId,
                    Created = DateTime.Now,
                    Updated = DateTime.Now,
                    IsActive = true
                };
                await _basketRepository.AddAsync(basket);
                
            }

            
            var basketItem = _basketItemRepository.Where(bi => bi.BasketId == basket.Id && bi.ProductId == productId).FirstOrDefault();

            if (basketItem != null)
            {
                
                basketItem.Quantity += quantity;
                basketItem.Updated = DateTime.Now;
                await _basketItemRepository.UpdateAsync(basketItem);
            }
            else
            {
                
                var newBasketItem = new BasketItem
                {
                    BasketId = basket.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    Created = DateTime.Now,
                    Updated = DateTime.Now,
                    IsActive = true
                };
                await _basketItemRepository.AddAsync(newBasketItem);
            }

            result.Status = true;
            result.Message = "Ürün sepete başarıyla eklendi.";
            return result;
        }

        [HttpDelete("{basketItemId}")]
        public async Task<ResultDto> RemoveItem(int basketItemId)
        {
            var basketItem = await _basketItemRepository.GetByIdAsync(basketItemId);
            if (basketItem == null)
            {
                result.Status = false;
                result.Message = "Sepette böyle bir ürün bulunamadı.";
                return result;
            }

            await _basketItemRepository.DeleteAsync(basketItemId);

            result.Status = true;
            result.Message = "Ürün sepetten çıkarıldı.";
            return result;
        }
    }
}