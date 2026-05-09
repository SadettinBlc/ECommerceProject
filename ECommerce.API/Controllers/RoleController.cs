using ECommerce.API.DTOs;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Sadece adminler rol dağıtabilir
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
            result.Message = "Bu rol zaten mevcut!";
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
                result.Message = "Böyle bir rol yok. Önce rolü oluşturun.";
                return result;
            }

            await _userManager.AddToRoleAsync(user, roleName);
            result.Status = true;
            result.Message = $"'{roleName}' yetkisi {userName} kullanıcısına başarıyla atandı.";
            return result;
        }
    }
}