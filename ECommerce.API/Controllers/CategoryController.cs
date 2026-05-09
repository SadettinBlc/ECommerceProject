using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IRepository<Category> _categoryRepository;
        ResultDto result = new ResultDto();

        public CategoryController(IRepository<Category> categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return Ok(categories);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // Sadece admin kategori ekleyebilir
        public async Task<ResultDto> Add(Category category)
        {
            category.Created = DateTime.Now;
            category.Updated = DateTime.Now;
            category.IsActive = true;

            await _categoryRepository.AddAsync(category);
            result.Status = true;
            result.Message = "Kategori başarıyla eklendi.";
            return result;
        }
    }
}