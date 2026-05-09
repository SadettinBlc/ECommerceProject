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

        [HttpPut]
        //[Authorize(Roles = "Admin")]
        public async Task<ResultDto> Update(Category category)
        {
            category.Updated = DateTime.Now;
            await _categoryRepository.UpdateAsync(category);

            result.Status = true;
            result.Message = "Ürün başarıyla güncellendi.";
            return result;
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<ResultDto> Delete(int id)
        {
            await _categoryRepository.DeleteAsync(id);
            result.Status = true;
            result.Message = "Ürün sistemden silindi.";
            return result;
        }
    }
}