using ECommerce.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize] 
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        
        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage([FromForm] UploadDto dto) 
        {
            try
            {
                var file = dto.File; 

                
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Lütfen bir dosya seçiniz.");
                }

                
                var extension = Path.GetExtension(file.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Sadece .jpg, .jpeg, .png ve .webp uzantılı resimler yüklenebilir.");
                }

                
                string newFileName = Guid.NewGuid().ToString() + extension;

                
                string webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
                    ? Path.Combine(_env.ContentRootPath, "wwwroot")
                    : _env.WebRootPath;

                string folderPath = Path.Combine(webRoot, "images", "products");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fullPath = Path.Combine(folderPath, newFileName);

                
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string savedUrl = $"/images/products/{newFileName}";
                return Ok(new { Url = savedUrl, Message = "Resim başarıyla yüklendi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Sunucu resim yüklenirken bir hata ile karşılaştı: {ex.Message}");
            }
        }
    }
}