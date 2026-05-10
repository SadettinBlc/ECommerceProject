namespace ECommerce.API.DTOs
{
    public class UpdateProfileDto
    {
        public string FullName { get; set; }
        public string UserName { get; set; } // EKLENDİ
        public string Email { get; set; }    // EKLENDİ
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string PhotoUrl { get; set; }
    }
}