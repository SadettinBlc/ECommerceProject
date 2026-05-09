using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductFeature> _featureRepository;
        ResultDto result = new ResultDto();

        public ProductController(IRepository<Product> productRepository, IRepository<ProductFeature> featureRepository)
        {
            _productRepository = productRepository;
            _featureRepository = featureRepository;
        }

        [HttpGet]
        public IActionResult List(int page = 1, int pageSize = 10)
        {
            // Include ile Kategori ve Özellikleri de zorla getirtiyoruz
            var query = _productRepository.Where(p => p.IsActive)
                                          .Include(p => p.Category)
                                          .Include(p => p.ProductFeatures);

            int totalCount = query.Count();

            var products = query.OrderByDescending(p => p.Created)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            var result = new PagedResultDto<Product>
            {
                Items = products,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };

            return Ok(result);
        }

        // YENİ VE TEMİZ DTO'LU ADD METODU
        [HttpPost]
        [Authorize] // Ürünü kimin eklediğini bilmek için giriş zorunlu olmalı
        public async Task<ResultDto> Add(ProductAddDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var newProduct = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                CategoryId = dto.CategoryId,
                PhotoUrl = dto.PhotoUrl,
                UserId = userId, // JSON'da userId olarak geçen kısım arkada otomatik doluyor
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true
            };

            await _productRepository.AddAsync(newProduct);

            result.Status = true;
            result.Message = "Ürün başarıyla eklendi.";
            return result;
        }

        // YANLIŞLIKLA SİLİNEN ADDFEATURE METODU GERİ GELDİ
        [HttpPost]
        [Authorize]
        public async Task<ResultDto> AddFeature(ProductFeatureAddDto dto)
        {
            // DTO'dan gelen o 3 küçük veriyi, veritabanının istediği asıl modele çeviriyoruz
            var newFeature = new ProductFeature
            {
                ProductId = dto.ProductId,
                FeatureName = dto.FeatureName,
                FeatureValue = dto.FeatureValue,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true
            };

            await _featureRepository.AddAsync(newFeature);

            result.Status = true;
            result.Message = "Ürün özelliği/filtresi başarıyla eklendi.";
            return result;
        }

        [HttpGet("{featureName}/{featureValue}")]
        public IActionResult FilterProducts(string featureName, string featureValue)
        {
            // Veritabanında özelliği ararken küçük/büyük harf veya tam eşleşme aradığını unutma!
            var products = _featureRepository.Where(f =>
                f.FeatureName == featureName &&
                f.FeatureValue == featureValue &&
                f.IsActive)
                .Include(f => f.Product) // Özelliğe bağlı olan Ürünü de pakete dahil et
                .Select(f => f.Product)
                .ToList();

            if (products == null || products.Count == 0)
            {
                return NotFound("Bu kriterlere uygun ürün bulunamadı. (Örn: '16' yerine '16GB' yazmış olabilir misiniz?)");
            }

            return Ok(products);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var product = _productRepository.Where(p => p.Id == id)
                .Include(p => p.ProductFeatures)
                .FirstOrDefault();

            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPut]
        // [Authorize(Roles = "Admin")] // Gerekirse kilidi açabilirsin
        public async Task<ResultDto> Update(ProductUpdateDto dto)
        {
            // 1. Önce veritabanından güncellenecek o ürünü buluyoruz
            var product = _productRepository.Where(p => p.Id == dto.Id).FirstOrDefault();

            if (product == null)
            {
                result.Status = false;
                result.Message = "Güncellenecek ürün bulunamadı.";
                return result;
            }

            // 2. Ürünün sadece izin verdiğimiz alanlarını DTO'dan gelen yeni verilerle değiştiriyoruz
            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;
            product.PhotoUrl = dto.PhotoUrl;
            product.IsActive = dto.IsActive;

            product.Updated = DateTime.Now; // Güncellenme tarihini otomatik atıyoruz

            // 3. Değişiklikleri veritabanına kaydediyoruz
            await _productRepository.UpdateAsync(product);

            result.Status = true;
            result.Message = "Ürün başarıyla güncellendi.";
            return result;
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<ResultDto> Delete(int id)
        {
            await _productRepository.DeleteAsync(id);
            result.Status = true;
            result.Message = "Ürün sistemden silindi.";
            return result;
        }

        [HttpGet("{keyword}")]
        public IActionResult Search(string keyword)
        {
            var products = _productRepository.Where(p =>
                (p.Name.Contains(keyword) || p.Description.Contains(keyword)) && p.IsActive)
                .ToList();

            if (products.Count == 0)
            {
                return NotFound("Aradığınız kritere uygun ürün bulunamadı.");
            }

            return Ok(products);
        }

        [HttpGet("{categoryId}")]
        public IActionResult GetByCategory(int categoryId)
        {
            var products = _productRepository.Where(p => p.CategoryId == categoryId && p.IsActive).ToList();

            if (products.Count == 0)
            {
                return NotFound("Bu kategoride henüz ürün bulunmamaktadır.");
            }

            return Ok(products);
        }

        [HttpGet("{categoryId}/{currentProductId}")]
        public IActionResult GetRelatedProducts(int categoryId, int currentProductId)
        {
            var relatedProducts = _productRepository.Where(p =>
                p.CategoryId == categoryId &&
                p.Id != currentProductId &&
                p.IsActive)
                .OrderByDescending(p => p.Created)
                .Take(4)
                .ToList();

            return Ok(relatedProducts);
        }

        [HttpDelete("{featureId}")]
        [Authorize] // Güvenlik kilidi: Sadece yetkili/giriş yapmış kişiler silebilir
        public async Task<ResultDto> DeleteFeature(int featureId)
        {
            // 1. Önce silinmek istenen özelliğin veritabanında gerçekten olup olmadığını buluyoruz
            var feature = _featureRepository.Where(f => f.Id == featureId).FirstOrDefault();

            if (feature == null)
            {
                result.Status = false;
                result.Message = "Silinmek istenen özellik bulunamadı.";
                return result;
            }

            // 2. Özelliği Repository üzerinden siliyoruz
            await _featureRepository.DeleteAsync(featureId);

            result.Status = true;
            result.Message = "Ürün özelliği sistemden başarıyla silindi.";
            return result;
        }

        [HttpGet]
        public IActionResult GetLatestProducts()
        {
            var latestProducts = _productRepository.Where(p => p.IsActive)
                .OrderByDescending(p => p.Created)
                .Take(8)
                .ToList();

            return Ok(latestProducts);
        }
    }
}