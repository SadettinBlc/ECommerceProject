using ECommerce.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize] // Sadece giriş yapanlar (veya yetkisi olanlar) dosya yükleyebilsin
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        // IWebHostEnvironment, sunucunun klasör yollarını (wwwroot gibi) bulmamızı sağlar
        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage([FromForm] UploadDto dto) // Artık doğrudan dosyayı değil, DTO'yu alıyoruz
        {
            try
            {
                var file = dto.File; // DTO'nun içindeki dosyayı değişkene atadık

                // 1. Dosya geldi mi kontrolü
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Lütfen bir dosya seçiniz.");
                }

                // 2. Güvenlik: Sadece resim dosyaları
                var extension = Path.GetExtension(file.FileName).ToLower();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Sadece .jpg, .jpeg, .png ve .webp uzantılı resimler yüklenebilir.");
                }

                // 3. Dosya ismini eşsiz yapma
                string newFileName = Guid.NewGuid().ToString() + extension;

                // 4. Kaydedilecek klasörün yolunu güvene alma (Null hatasına karşı)
                string webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
                    ? Path.Combine(_env.ContentRootPath, "wwwroot")
                    : _env.WebRootPath;

                string folderPath = Path.Combine(webRoot, "images", "products");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                string fullPath = Path.Combine(folderPath, newFileName);

                // 5. Dosyayı fiziksel olarak kaydetme
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