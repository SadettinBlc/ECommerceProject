using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize] // Sadece yetkililer erişebilir
    public class DashboardController : ControllerBase
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Review> _reviewRepository;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(
            IRepository<Order> orderRepository,
            IRepository<Product> productRepository,
            IRepository<Review> reviewRepository,
            UserManager<AppUser> userManager)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            _reviewRepository = reviewRepository;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult GetStats()
        {
            // 1. Toplam Ciro (İptal edilen siparişler hariç tutulur)
            var totalRevenue = _orderRepository.Where(o => o.OrderStatus != "İptal Edildi").Sum(o => o.TotalPrice);

            // 2. Toplam Sipariş Sayısı
            var totalOrders = _orderRepository.Where(o => o.Id > 0).Count();

            // 3. Teslim Edilen Siparişler
            var deliveredOrders = _orderRepository.Where(o => o.OrderStatus == "Teslim Edildi").Count();

            // 4. Aktif Ürünler
            var activeProducts = _productRepository.Where(p => p.IsActive).Count();

            // 5. Kayıtlı Üyeler
            var totalUsers = _userManager.Users.Count();

            // 6. Toplam Ürün Değerlendirmesi (Yorumlar)
            var totalReviews = _reviewRepository.Where(r => r.Id > 0).Count();

            return Ok(new
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                DeliveredOrders = deliveredOrders,
                ActiveProducts = activeProducts,
                TotalUsers = totalUsers,
                TotalReviews = totalReviews
            });
        }
    }
}