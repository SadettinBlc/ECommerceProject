using Microsoft.AspNetCore.Authorization;
using ECommerce.API.DTOs;
using ECommerce.API.Models;
using ECommerce.API.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductFeature> _featureRepository; // Özellikler için depo eklendi
        ResultDto result = new ResultDto();

        // Kapıda hem ürün deposunu hem de özellik deposunu karşılıyoruz
        public ProductController(IRepository<Product> productRepository, IRepository<ProductFeature> featureRepository)
        {
            _productRepository = productRepository;
            _featureRepository = featureRepository;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            // Ürünleri çekerken özelliklerini de (Features) beraberinde getirmesi için IQueryable üzerinden Include yapabiliriz
            var products = await _productRepository.GetAllAsync();
            return Ok(products);
        }

        [HttpPost]
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

        [HttpPost]
        [Authorize]
        public async Task<ResultDto> AddFeature(ProductFeature feature)
        {
            feature.Created = DateTime.Now;
            feature.Updated = DateTime.Now;
            feature.IsActive = true;

            // Artık _featureRepository tanımlı olduğu için hata vermeyecek
            await _featureRepository.AddAsync(feature);

            result.Status = true;
            result.Message = "Ürün özelliği/filtresi başarıyla eklendi.";
            return result;
        }

        [HttpGet("{featureName}/{featureValue}")]
        public IActionResult FilterProducts(string featureName, string featureValue)
        {
            // Amazon usulü filtreleme: Özellikler tablosunda arama yapıp ilgili ürünleri getiriyoruz
            var products = _featureRepository.Where(f =>
                f.FeatureName == featureName &&
                f.FeatureValue == featureValue &&
                f.IsActive)
                .Select(f => f.Product) // Özellikten ürüne geçiş yapıyoruz
                .ToList();

            if (products == null || products.Count == 0)
            {
                return NotFound("Bu kriterlere uygun ürün bulunamadı.");
            }

            return Ok(products);
        }
        [HttpGet]
        public IActionResult List(int page = 1, int pageSize = 10)
        {
            // 1. Sadece aktif olan ürünleri sorgula (veritabanından henüz çekmedi, IQueryable bekliyor)
            var query = _productRepository.Where(p => p.IsActive);

            // 2. Toplam aktif ürün sayısını bul
            int totalCount = query.Count();

            // 3. İstenen sayfaya göre ürünleri atla (Skip) ve sadece o sayfanın ürünlerini al (Take)
            var products = query.OrderByDescending(p => p.Created) // En yeni ürünler en başta gelsin
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            // 4. Profesyonel DTO'muzu doldurup arayüze gönder
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
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            // Include kullanarak ürüne ait özellikleri (Features) de pakete dahil ediyoruz
            var product = _productRepository.Where(p => p.Id == id)
                .Include(p => p.ProductFeatures)
                .FirstOrDefault();

            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPut]
        //[Authorize(Roles = "Admin")]
        public async Task<ResultDto> Update(Product product)
        {
            product.Updated = DateTime.Now;
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
            // Ürün adında veya açıklamasında aranan kelime geçen aktif ürünleri getirir
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
            // Aynı kategorideki, ama şu an incelediğimiz ürün HARİÇ olan 4 ürünü getir
            var relatedProducts = _productRepository.Where(p =>
                p.CategoryId == categoryId &&
                p.Id != currentProductId &&
                p.IsActive)
                .OrderByDescending(p => p.Created)
                .Take(4)
                .ToList();

            return Ok(relatedProducts);
        }

        [HttpGet]
        public IActionResult GetLatestProducts()
        {
            // Veritabanına en son eklenen (tarihe göre azalan) 8 aktif ürünü vitrin için getirir
            var latestProducts = _productRepository.Where(p => p.IsActive)
                .OrderByDescending(p => p.Created)
                .Take(8)
                .ToList();

            return Ok(latestProducts);
        }
    }
}