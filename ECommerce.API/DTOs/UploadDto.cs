using Microsoft.AspNetCore.Http; 

namespace ECommerce.API.DTOs
{
    public class UploadDto
    {
        public IFormFile File { get; set; }
    }
}