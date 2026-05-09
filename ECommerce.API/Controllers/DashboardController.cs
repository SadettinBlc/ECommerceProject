using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    // [Authorize(Roles = "Admin")] // Test aşamasında kapalı kalabilir
    public class DashboardController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(
            IRepository<Product> productRepository,
            IRepository<Order> orderRepository,
            UserManager<AppUser> userManager)
        {
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult GetSummaryStats()
        {
            var totalUsers = _userManager.Users.Count();
            var totalProducts = _productRepository.Where(p => p.IsActive).Count();

            // Tüm aktif siparişlerin toplam tutarını hesaplar
            var totalRevenue = _orderRepository.Where(o => o.IsActive).Sum(o => o.TotalPrice);
            var totalOrders = _orderRepository.Where(o => o.IsActive).Count();

            // Sadece sayıları dönen isimsiz (anonymous) bir obje gönderiyoruz
            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue
            });
        }
    }
}