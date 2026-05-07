using Microsoft.AspNetCore.Identity;

namespace ECommerce.API.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
        public string PhotoUrl { get; set; }
        public string Address { get; set; } // Siparişler için gerekli
    }
}