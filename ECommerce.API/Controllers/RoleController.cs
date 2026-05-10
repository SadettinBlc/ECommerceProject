using ECommerce.API.DTOs;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // TAM KORUMA EKLENDİ! Artık sadece Adminler rol dağıtabilir.
    public class RoleController : ControllerBase
    {
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        ResultDto result = new ResultDto();

        public RoleController(RoleManager<AppRole> roleManager, UserManager<AppUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult List()
        {
            var roles = _roleManager.Roles.Select(r => new { r.Id, r.Name }).ToList();
            return Ok(roles);
        }

        [HttpPost]
        public async Task<ResultDto> CreateRole(string roleName)
        {
            var roleExist = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                var role = new AppRole { Name = roleName };
                await _roleManager.CreateAsync(role);
                result.Status = true;
                result.Message = "Rol başarıyla oluşturuldu.";
                return result;
            }
            result.Status = false;
            result.Message = "Bu rol zaten sistemde mevcut!";
            return result;
        }

        [HttpPost]
        public async Task<ResultDto> AssignRoleToUser(string userName, string roleName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                result.Status = false;
                result.Message = "Kullanıcı bulunamadı.";
                return result;
            }

            var roleExist = await _roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                result.Status = false;
                result.Message = "Böyle bir rol bulunamadı.";
                return result;
            }

            var addResult = await _userManager.AddToRoleAsync(user, roleName);
            if (addResult.Succeeded)
            {
                result.Status = true;
                result.Message = $"'{roleName}' yetkisi {userName} kullanıcısına başarıyla tanımlandı.";
            }
            else
            {
                result.Status = false;
                result.Message = "Rol atama sırasında bir hata oluştu.";
            }
            return result;
        }
        
        // 1. Rolü Tamamen Silme
        [HttpDelete("{roleName}")]
        public async Task<ResultDto> DeleteRole(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                result.Status = false;
                result.Message = "Silinecek rol bulunamadı.";
                return result;
            }

            var deleteResult = await _roleManager.DeleteAsync(role);
            if (deleteResult.Succeeded)
            {
                result.Status = true;
                result.Message = "Rol sistemden başarıyla silindi.";
            }
            else
            {
                result.Status = false;
                result.Message = "Rol silinirken bir hata oluştu (Kullanımda olabilir).";
            }
            return result;
        }

        // 2. Kullanıcıdan Rolü Geri Alma
        [HttpPost]
        public async Task<ResultDto> RemoveRoleFromUser(string userName, string roleName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                result.Status = false;
                result.Message = "Kullanıcı bulunamadı.";
                return result;
            }

            // Önce kullanıcıda bu rol var mı diye kontrol edelim
            var isInRole = await _userManager.IsInRoleAsync(user, roleName);
            if (!isInRole)
            {
                result.Status = false;
                result.Message = "Kullanıcı zaten bu yetkiye sahip değil.";
                return result;
            }

            var removeResult = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (removeResult.Succeeded)
            {
                result.Status = true;
                result.Message = $"'{roleName}' yetkisi {userName} kullanıcısından geri alındı.";
            }
            else
            {
                result.Status = false;
                result.Message = "Yetki geri alınırken bir hata oluştu.";
            }
            return result;

        }

        // Seçilen kullanıcının sahip olduğu rolleri liste halinde döner
        [HttpGet("{userName}")]
        public async Task<IActionResult> GetUserRoles(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return NotFound("Kullanıcı bulunamadı.");

            // Identity'nin kendi içindeki GetRolesAsync metodu o kullanıcının rollerini string listesi olarak verir
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);

        }
    }
}