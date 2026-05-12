using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize] 
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
            
            var totalRevenue = _orderRepository.Where(o => o.OrderStatus != "İptal Edildi").Sum(o => o.TotalPrice);

            
            var totalOrders = _orderRepository.Where(o => o.Id > 0).Count();

            
            var deliveredOrders = _orderRepository.Where(o => o.OrderStatus == "Teslim Edildi").Count();

            
            var activeProducts = _productRepository.Where(p => p.IsActive).Count();

            
            var totalUsers = _userManager.Users.Count();

            
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