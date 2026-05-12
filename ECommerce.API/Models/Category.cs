using System.Collections;

namespace ECommerce.API.Models
{
    public class Category : BaseEntity
    {
        public string Name { get; set; }

        
        public ICollection<Product> Products { get; set; }
    }
}