using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Basket> _basketRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly UserManager<AppUser> _userManager;
        ResultDto result = new ResultDto();

        public OrderController(
            IRepository<Order> orderRepository,
            IRepository<Basket> basketRepository,
            IRepository<Product> productRepository,
            UserManager<AppUser> userManager)
        {
            _orderRepository = orderRepository;
            _basketRepository = basketRepository;
            _productRepository = productRepository;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<ResultDto> CreateOrderFromBasket(string shippingAddress)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Kullanıcının aktif sepetini ve içindeki ürünleri çekiyoruz
            var basket = _basketRepository.Where(b => b.AppUserId == userId && b.IsActive)
                .Include(b => b.BasketItems)
                .ThenInclude(bi => bi.Product)
                .FirstOrDefault();

            if (basket == null || !basket.BasketItems.Any())
            {
                result.Status = false;
                result.Message = "Sepetiniz boş! Sipariş oluşturulamaz.";
                return result;
            }

            // 2. KRİTİK ADIM: Stok Kontrolü
            // Siparişi onaylamadan önce her bir ürünün stoğunu tek tek kontrol ediyoruz
            foreach (var item in basket.BasketItems)
            {
                if (item.Product.Stock < item.Quantity)
                {
                    result.Status = false;
                    result.Message = $"Yetersiz Stok! '{item.Product.Name}' ürününden stokta sadece {item.Product.Stock} adet var. Lütfen sepetinizi güncelleyin.";
                    return result;
                }
            }

            // 3. Sipariş Başlığını Oluşturma
            var newOrder = new Order
            {
                AppUserId = userId,
                Address = shippingAddress,
                OrderStatus = "Hazırlanıyor",
                TotalPrice = basket.BasketItems.Sum(x => x.Product.Price * x.Quantity),
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true,
                OrderItems = new List<OrderItem>()
            };

            // 4. Sepetteki Ürünleri Sipariş Kalemine Dönüştürme ve STOKTAN DÜŞME
            foreach (var item in basket.BasketItems)
            {
                // Sipariş detayı ekleniyor
                newOrder.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price, // Satın alma anındaki fiyat sabitleniyor
                    Created = DateTime.Now,
                    Updated = DateTime.Now,
                    IsActive = true
                });

                // STOK OTOMATİK DÜŞÜRÜLÜYOR
                item.Product.Stock -= item.Quantity;
                await _productRepository.UpdateAsync(item.Product);
            }

            // 5. Siparişi Kaydet
            await _orderRepository.AddAsync(newOrder);

            // 6. Sepeti Kapat (IsActive = false yaparak sepeti 'siparişe dönüşmüş' sayıyoruz)
            basket.IsActive = false;
            basket.Updated = DateTime.Now;
            await _basketRepository.UpdateAsync(basket);

            result.Status = true;
            result.Message = "Ödeme başarılı! Siparişiniz alındı, stoklar güncellendi ve sepetiniz boşaltıldı.";
            return result;
        }

        [HttpGet]
        public IActionResult GetMyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orders = _orderRepository.Where(o => o.AppUserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.Created)
                .ToList();

            return Ok(orders);
        }
        // 1. Tüm Siparişleri Listele (Sadece Admin görebilmeli)
        [HttpGet]
        //[Authorize(Roles = "Admin")] // Test aşamasında yorum satırı yapabilirsin
        public IActionResult GetAllOrders()
        {
            var orders = _orderRepository.Where(o => o.IsActive)
                .Include(o => o.AppUser) // Siparişi kimin verdiğini görmek için
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.Created)
                .ToList();

            return Ok(orders);
        }

        // 2. Sipariş Durumunu Güncelle (Hazırlanıyor -> Kargoya Verildi vb.)
        [HttpPost]
        public async Task<ResultDto> UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                result.Status = false;
                result.Message = "Sipariş bulunamadı.";
                return result;
            }

            order.OrderStatus = newStatus;
            order.Updated = DateTime.Now;
            await _orderRepository.UpdateAsync(order);

            result.Status = true;
            result.Message = $"Sipariş durumu '{newStatus}' olarak güncellendi.";
            return result;
        }
        [HttpPost("{orderId}")]
        [Authorize]
        public async Task<ResultDto> CancelOrder(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = _orderRepository.Where(o => o.Id == orderId && o.AppUserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault();

            if (order == null)
            {
                result.Status = false;
                result.Message = "Sipariş bulunamadı veya size ait değil.";
                return result;
            }

            // Sadece henüz kargolanmamış siparişler iptal edilebilir
            if (order.OrderStatus != "Hazırlanıyor" && order.OrderStatus != "Sipariş Alındı")
            {
                result.Status = false;
                result.Message = "Bu sipariş kargoya verildiği için iptal edilemez.";
                return result;
            }

            // 1. Sipariş durumunu güncelle
            order.OrderStatus = "İptal Edildi";
            order.Updated = DateTime.Now;
            order.IsActive = false;
            await _orderRepository.UpdateAsync(order);

            // 2. STOKLARI GERİ İADE ET (Kritik İşlem)
            foreach (var item in order.OrderItems)
            {
                item.Product.Stock += item.Quantity;
                await _productRepository.UpdateAsync(item.Product);
            }

            result.Status = true;
            result.Message = "Siparişiniz iptal edildi ve tutar/stok iadesi sağlandı.";
            return result;
        }

        [HttpGet("{orderId}")]
        [Authorize]
        public IActionResult GetOrderDetails(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kullanıcının sadece KENDİ siparişinin detayını görebilmesi için güvenlik kontrolü (userId)
            var order = _orderRepository.Where(o => o.Id == orderId && o.AppUserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault();

            if (order == null)
            {
                return NotFound("Sipariş bulunamadı veya bu siparişi görüntüleme yetkiniz yok.");
            }

            return Ok(order);
        }
    }
}