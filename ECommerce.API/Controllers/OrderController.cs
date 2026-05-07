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
    [Authorize] // Güvenlik: Sadece token'ı olan (giriş yapan) kullanıcılar sipariş verebilir
    public class OrderController : ControllerBase
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Product> _productRepository;
        ResultDto result = new ResultDto();

        public OrderController(IRepository<Order> orderRepository, IRepository<Product> productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        [HttpPost]
        public async Task<ResultDto> CreateOrder(string address, int productId, int quantity)
        {
            // 1. Ürünü ve stok durumunu kontrol et
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
                result.Message = $"Yetersiz stok! Sipariş verilemedi. Mevcut stok: {product.Stock}";
                return result;
            }

            // 2. JWT Token içerisinden siparişi veren kullanıcının ID'sini yakala
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 3. Siparişi ve sepet detayını (OrderItem) oluştur
            var newOrder = new Order
            {
                AppUserId = userId,
                Address = address,
                OrderStatus = "Sipariş Alındı",
                TotalPrice = product.Price * quantity,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        UnitPrice = product.Price, // O anki fiyattan sabitliyoruz
                        Created = DateTime.Now,
                        Updated = DateTime.Now,
                        IsActive = true
                    }
                }
            };

            await _orderRepository.AddAsync(newOrder);

            // 4. SATIŞ YAPILDI -> STOKTAN DÜŞ VE GÜNCELLE
            product.Stock -= quantity;
            await _productRepository.UpdateAsync(product);

            result.Status = true;
            result.Message = "Satın alma başarılı! Siparişiniz oluşturuldu ve stok güncellendi.";
            return result;
        }
    }
}