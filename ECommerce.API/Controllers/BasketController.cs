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
    [Authorize] // Sepet işlemleri için üye girişi zorunludur
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
            // Sisteme giriş yapmış kullanıcının ID'sini yakalıyoruz
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kullanıcının sepetini, sepetteki ürünleri (BasketItems) ve o ürünlerin detaylarını (Product) Include ile tek seferde çekiyoruz
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

            // 1. Ürün ve Stok Kontrolü
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

            // 2. Kullanıcının aktif bir sepeti var mı kontrol et, yoksa anında oluştur
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
                // Repository içindeki SaveChangesAsync çalıştığı için sepetin Id'si anında oluştu
            }

            // 3. Eklenmek istenen ürün zaten sepette var mı kontrol et
            var basketItem = _basketItemRepository.Where(bi => bi.BasketId == basket.Id && bi.ProductId == productId).FirstOrDefault();

            if (basketItem != null)
            {
                // Ürün zaten sepetteyse sadece miktarını artır
                basketItem.Quantity += quantity;
                basketItem.Updated = DateTime.Now;
                await _basketItemRepository.UpdateAsync(basketItem);
            }
            else
            {
                // Ürün sepette yoksa yeni bir satır olarak ekle
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