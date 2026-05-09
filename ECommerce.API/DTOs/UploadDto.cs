using Microsoft.AspNetCore.Http; // IFormFile kullanabilmek için bu şart

namespace ECommerce.API.DTOs
{
    public class UploadDto
    {
        public IFormFile File { get; set; }
    }
}