using ECommerce.API.DTOs;
using ECommerce.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ECommerce.API.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IConfiguration _configuration;
        ResultDto result = new ResultDto();

        public UserController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        [HttpPost]
        [AllowAnonymous] // Herkes kayıt olabilir
        public async Task<ResultDto> Add(RegisterDto dto)
        {
            // Kullanıcı oluşturulurken PhotoUrl null kalmasın diye varsayılan bir değer atıyoruz
            var identityResult = await _userManager.CreateAsync(new AppUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                PhotoUrl = "profil.jpg",
                Address = "Belirtilmedi",
                Age = 0, // Varsayılan yaş
                Gender = "Belirtilmedi" // Varsayılan cinsiyet
            }, dto.Password);

            if (!identityResult.Succeeded)
            {
                result.Status = false;
                foreach (var item in identityResult.Errors)
                {
                    result.Message += item.Description + " ";
                }
                return result;
            }

            var user = await _userManager.FindByNameAsync(dto.UserName);
            var roleExist = await _roleManager.RoleExistsAsync("Uye");
            if (!roleExist)
            {
                var role = new AppRole { Name = "Uye" };
                await _roleManager.CreateAsync(role);
            }

            await _userManager.AddToRoleAsync(user, "Uye");
            result.Status = true;
            result.Message = "Üye başarıyla eklendi.";
            return result;
        }

        [HttpPost]
        [AllowAnonymous] // Herkes giriş yapmayı deneyebilir
        public async Task<ResultDto> SignIn(LoginDto dto)
        {
            var user = await _userManager.FindByNameAsync(dto.UserName);

            if (user is null)
            {
                result.Status = false;
                result.Message = "Üye Bulunamadı!";
                return result;
            }
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, dto.Password);

            if (!isPasswordCorrect)
            {
                result.Status = false;
                result.Message = "Kullanıcı Adı veya Parola Geçersiz!";
                return result;
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("JWTID", Guid.NewGuid().ToString()),
                new Claim("UserPhoto", user.PhotoUrl ?? "profil.jpg"),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = GenerateJWT(authClaims);

            result.Status = true;
            result.Message = token;
            return result;
        }

        private string GenerateJWT(List<Claim> claims)
        {
            // appsettings'ten veri çekme yolu düzeltildi (Jwt içinden alınıyor)
            var accessTokenExpiration = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessTokenExpiration"]));
            var authSecret = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var tokenObject = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    expires: accessTokenExpiration,
                    claims: claims,
                    signingCredentials: new SigningCredentials(authSecret, SecurityAlgorithms.HmacSha256)
                );

            string token = new JwtSecurityTokenHandler().WriteToken(tokenObject);
            return token;
        }

        [HttpGet]
        public IActionResult List()
        {
            // Şifreler gelmez, sadece genel kullanıcı bilgileri döner
            var users = _userManager.Users.Select(u => new {
                u.Id,
                u.FullName,
                u.UserName,
                u.Email,
                u.PhoneNumber,
                u.Address,
                u.Age,
                u.Gender,
                u.PhotoUrl
            }).ToList();

            return Ok(users);
        }

        [HttpPut]
        [Authorize]
        public async Task<ResultDto> UpdateProfile(UpdateProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                result.Status = false;
                result.Message = "Kullanıcı bulunamadı.";
                return result;
            }

            user.FullName = dto.FullName;
            user.PhoneNumber = dto.PhoneNumber;
            user.Address = dto.Address;
            user.Age = dto.Age;
            user.Gender = dto.Gender;

            var updateResult = await _userManager.UpdateAsync(user);

            if (updateResult.Succeeded)
            {
                result.Status = true;
                result.Message = "Profil bilgileriniz güncellendi.";
            }
            else
            {
                result.Status = false;
                result.Message = "Güncelleme başarısız oldu.";
            }

            return result;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound("Kullanıcı bulunamadı.");
            }

            // Sadece arayüzde formları doldurmak için gereken güvenli bilgileri dönüyoruz (Şifre vs. asla dönmez)
            return Ok(new
            {
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.Address,
                user.Age,
                user.Gender,
                user.PhotoUrl
            });
        }

        [HttpPost]
        [Authorize]
        public async Task<ResultDto> ChangePassword(ChangePasswordDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                result.Status = false;
                result.Message = "Kullanıcı bulunamadı.";
                return result;
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (changePasswordResult.Succeeded)
            {
                result.Status = true;
                result.Message = "Şifreniz başarıyla güncellendi.";
            }
            else
            {
                result.Status = false;
                result.Message = "Şifre değiştirme başarısız! Eski şifrenizi yanlış girmiş olabilirsiniz.";
            }

            return result;
        }
    }
}