using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        ResultDto result = new ResultDto();

        public ProductController(IRepository<Product> productRepository)
        {
            _productRepository = productRepository;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var products = await _productRepository.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Güvenlik: Sadece Adminler ürün ekleyebilir
        public async Task<ResultDto> Add(Product product)
        {
            product.Created = DateTime.Now;
            product.Updated = DateTime.Now;
            product.IsActive = true;

            await _productRepository.AddAsync(product);

            result.Status = true;
            result.Message = "Ürün başarıyla eklendi.";
            return result;
        }
    }
}