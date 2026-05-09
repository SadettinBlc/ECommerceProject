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
        public async Task<ResultDto> CreateOrderFromBasket(OrderCreateDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Hata yakalama bloğumuz devrede
            try
            {
                // 1. ÖDEME KONTROLÜ (Mock Payment)
                if (string.IsNullOrWhiteSpace(dto.CardNumber) || dto.CardNumber.Length != 16)
                {
                    result.Status = false;
                    result.Message = "Ödeme Reddedildi: Kart numarası 16 hane olmalıdır.";
                    return result;
                }

                // 2. SEPET VE STOK KONTROLÜ
                var basket = _basketRepository.Where(b => b.AppUserId == userId && b.IsActive)
                    .Include(b => b.BasketItems).ThenInclude(bi => bi.Product).FirstOrDefault();

                if (basket == null || !basket.BasketItems.Any())
                {
                    result.Status = false;
                    result.Message = "Sepet boş!";
                    return result;
                }

                // 3. TUTAR HESAPLAMA VE KUPON UYGULAMA
                decimal totalAmount = basket.BasketItems.Sum(x => x.Product.Price * x.Quantity);

                if (!string.IsNullOrWhiteSpace(dto.CouponCode) && dto.CouponCode.Trim().ToUpper() == "SADO10")
                {
                    totalAmount *= 0.90m; // %10 İndirim çakıyoruz
                }

                // 4. SİPARİŞ OLUŞTURMA
                var newOrder = new Order
                {
                    AppUserId = userId,
                    Address = dto.ShippingAddress,
                    OrderStatus = "Hazırlanıyor",
                    TotalPrice = totalAmount,
                    Created = DateTime.Now,
                    IsActive = true,
                    OrderItems = new List<OrderItem>()
                };

                foreach (var item in basket.BasketItems)
                {
                    // Stok Kontrolü
                    if (item.Product.Stock < item.Quantity)
                    {
                        result.Status = false;
                        result.Message = $"Yetersiz Stok: {item.Product.Name}";
                        return result;
                    }

                    newOrder.OrderItems.Add(new OrderItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Product.Price,
                        Created = DateTime.Now,
                        IsActive = true
                    });

                    // Stoktan Düşme
                    item.Product.Stock -= item.Quantity;
                    await _productRepository.UpdateAsync(item.Product);
                }

                await _orderRepository.AddAsync(newOrder);

                // Sepeti Kapat
                basket.IsActive = false;
                await _basketRepository.UpdateAsync(basket);

                result.Status = true;
                result.Message = $"Ödeme Başarılı! {totalAmount} TL çekildi. Siparişiniz oluşturuldu.";
                return result;
            }
            catch (Exception ex)
            {
                // Kod bir yerde patlarsa sistem çökmeden buraya düşecek ve bize hatayı söyleyecek
                result.Status = false;
                result.Message = "İşlem sırasında bir hata oluştu: " + ex.Message;
                return result;
            }
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