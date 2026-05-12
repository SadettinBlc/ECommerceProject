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

            
            try
            {
                
                if (string.IsNullOrWhiteSpace(dto.CardNumber) || dto.CardNumber.Length != 16)
                {
                    result.Status = false;
                    result.Message = "Ödeme Reddedildi: Kart numarası 16 hane olmalıdır.";
                    return result;
                }

                
                var basket = _basketRepository.Where(b => b.AppUserId == userId && b.IsActive)
                    .Include(b => b.BasketItems).ThenInclude(bi => bi.Product).FirstOrDefault();

                if (basket == null || !basket.BasketItems.Any())
                {
                    result.Status = false;
                    result.Message = "Sepet boş!";
                    return result;
                }

                
                decimal totalAmount = basket.BasketItems.Sum(x => x.Product.Price * x.Quantity);

                if (!string.IsNullOrWhiteSpace(dto.CouponCode) && dto.CouponCode.Trim().ToUpper() == "SADO10")
                {
                    totalAmount *= 0.90m; 
                }

                
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

                    
                    item.Product.Stock -= item.Quantity;
                    await _productRepository.UpdateAsync(item.Product);
                }

                await _orderRepository.AddAsync(newOrder);

                
                basket.IsActive = false;
                await _basketRepository.UpdateAsync(basket);

                result.Status = true;
                result.Message = $"Ödeme Başarılı! {totalAmount} TL çekildi. Siparişiniz oluşturuldu.";
                return result;
            }
            catch (Exception ex)
            {
                
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
        
        [HttpGet]
        
        public IActionResult GetAllOrders()
        {
            var orders = _orderRepository.Where(o => o.IsActive)
                .Include(o => o.AppUser) 
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.Created)
                .ToList();

            return Ok(orders);
        }

        
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

            
            if (order.OrderStatus != "Hazırlanıyor" && order.OrderStatus != "Sipariş Alındı")
            {
                result.Status = false;
                result.Message = "Bu sipariş kargoya verildiği için iptal edilemez.";
                return result;
            }

            
            order.OrderStatus = "İptal Edildi";
            order.Updated = DateTime.Now;
            order.IsActive = false;
            await _orderRepository.UpdateAsync(order);

            
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

        [HttpDelete("{orderId}")]
        [Authorize] 
        public async Task<ResultDto> DeleteOrder(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                result.Status = false;
                result.Message = "Sipariş bulunamadı.";
                return result;
            }

            await _orderRepository.DeleteAsync(orderId);
            result.Status = true;
            result.Message = "Sipariş sistemden tamamen silindi.";
            return result;
        }

    }
}