using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.API.Models
{
    public class Product : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public int Stock { get; set; } // Satış yapılınca düşecek
        public int CategoryId { get; set; }
        public string UserId { get; set; }
        public string PhotoUrl { get; set; }

        public Category Category { get; set; }
        public AppUser User { get; set; }
    }
}