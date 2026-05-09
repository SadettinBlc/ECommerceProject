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
        public async Task<ResultDto> Add(CategoryAddDto dto) // Artık Category değil, ufacık DTO'yu alıyoruz
        {
            // DTO'dan gelen ufak veriyi, asıl veritabanı modelimize (Category) çeviriyoruz
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
        // [Authorize(Roles = "Admin")]
        public async Task<ResultDto> Update(CategoryUpdateDto dto)
        {
            // Önce güncellenecek kategoriyi veritabanından buluyoruz
            var category = _categoryRepository.Where(c => c.Id == dto.Id).FirstOrDefault();

            if (category == null)
            {
                result.Status = false;
                result.Message = "Güncellenecek kategori bulunamadı.";
                return result;
            }

            // Sadece değişmesine izin verdiğimiz alanları güncelliyoruz
            category.Name = dto.Name;
            category.IsActive = dto.IsActive;
            category.Updated = DateTime.Now;

            await _categoryRepository.UpdateAsync(category);

            result.Status = true;
            result.Message = "Kategori başarıyla güncellendi.";
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