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
            var query = _productRepository.Where(p => true)
                                          .Include(p => p.Category)
                                          .Include(p => p.ProductFeatures);

            int totalCount = query.Count();
            var products = query.OrderByDescending(p => p.Created)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize).ToList();

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

        [HttpGet("{categoryId}")]
        public IActionResult GetByCategory(int categoryId)
        {
            var products = _productRepository.Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .Include(p => p.ProductFeatures).ToList();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var product = _productRepository.Where(p => p.Id == id)
                .Include(p => p.ProductFeatures).FirstOrDefault();

            if (product == null) return NotFound();

            var returnData = new
            {
                Id = product.Id,
                Name = product.Name,
                Price = product.Price,
                Stock = product.Stock,
                Description = product.Description,
                CategoryId = product.CategoryId,
                IsActive = product.IsActive,
                PhotoUrl = product.PhotoUrl,
                ProductFeatures = product.ProductFeatures?.Select(f => new {
                    Id = f.Id,
                    Name = f.FeatureName,
                    Value = f.FeatureValue
                }).ToList()
            };
            return Ok(returnData);
        }

        [HttpPost]
        [Authorize]
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
                UserId = userId,
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true // DTO'da yok, yeni eklendiği için aktif yapıyoruz
            };
            await _productRepository.AddAsync(newProduct);
            result.Status = true;
            result.Message = "Ürün eklendi.";
            return result;
        }

        [HttpPut]
        public async Task<ResultDto> Update(ProductUpdateDto dto)
        {
            var product = _productRepository.Where(p => p.Id == dto.Id).FirstOrDefault();
            if (product == null)
            {
                result.Status = false;
                result.Message = "Ürün bulunamadı.";
                return result;
            }

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.CategoryId = dto.CategoryId;
            if (!string.IsNullOrEmpty(dto.PhotoUrl)) { product.PhotoUrl = dto.PhotoUrl; }
            product.IsActive = dto.IsActive; // Update DTO'sunda var
            product.Updated = DateTime.Now;

            await _productRepository.UpdateAsync(product);
            result.Status = true;
            result.Message = "Ürün güncellendi.";
            return result;
        }

        [HttpDelete("{id}")]
        public async Task<ResultDto> Delete(int id)
        {
            await _productRepository.DeleteAsync(id);
            result.Status = true;
            result.Message = "Ürün silindi.";
            return result;
        }

        [HttpPost]
        [Authorize]
        public async Task<ResultDto> AddFeature(ProductFeatureAddDto dto)
        {
            var newFeature = new ProductFeature
            {
                ProductId = dto.ProductId,
                FeatureName = dto.FeatureName,  // Senin DTO'ndaki isimlendirme
                FeatureValue = dto.FeatureValue, // Senin DTO'ndaki isimlendirme
                Created = DateTime.Now,
                Updated = DateTime.Now,
                IsActive = true
            };
            await _featureRepository.AddAsync(newFeature);
            result.Status = true;
            result.Message = "Özellik eklendi.";
            return result;
        }

        [HttpDelete("{featureId}")]
        [Authorize]
        public async Task<ResultDto> DeleteFeature(int featureId)
        {
            var feature = _featureRepository.Where(f => f.Id == featureId).FirstOrDefault();
            if (feature == null)
            {
                result.Status = false;
                result.Message = "Özellik bulunamadı.";
                return result;
            }
            await _featureRepository.DeleteAsync(featureId);
            result.Status = true;
            result.Message = "Özellik silindi.";
            return result;
        }
    }
}