using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult List()
        {
            var categories = _categoryRepository.Where(c => true)
                .Include(c => c.Products)
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.Name,
                    IsActive = c.IsActive,
                    Created = c.Created,
                    Updated = c.Updated,
                    ProductCount = c.Products != null ? c.Products.Count : 0
                }).ToList();
            return Ok(categories);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var category = _categoryRepository.Where(c => c.Id == id).FirstOrDefault();
            if (category == null) return NotFound();
            return Ok(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // KİLİT EKLENDİ
        public async Task<ResultDto> Add(CategoryAddDto dto)
        {
            var newCategory = new Category
            {
                Name = dto.Name,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true
            };

            await _categoryRepository.AddAsync(newCategory);

            result.Status = true;
            result.Message = "Kategori başarıyla eklendi.";
            return result;
        }

        [HttpPut]
        [Authorize(Roles = "Admin")] // KİLİT EKLENDİ
        public async Task<ResultDto> Update(CategoryUpdateDto dto)
        {
            var category = _categoryRepository.Where(c => c.Id == dto.Id).FirstOrDefault();
            if (category == null)
            {
                result.Status = false;
                result.Message = "Güncellenecek kategori bulunamadı.";
                return result;
            }

            category.Name = dto.Name;
            category.IsActive = dto.IsActive;
            category.Updated = DateTime.Now;

            await _categoryRepository.UpdateAsync(category);

            result.Status = true;
            result.Message = "Kategori başarıyla güncellendi.";
            return result;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // KİLİT EKLENDİ
        public async Task<ResultDto> Delete(int id)
        {
            await _categoryRepository.DeleteAsync(id);
            result.Status = true;
            result.Message = "Kategori sistemden silindi.";
            return result;
        }
    }
}