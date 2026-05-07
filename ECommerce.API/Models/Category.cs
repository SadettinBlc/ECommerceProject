using System.Collections;

namespace ECommerce.API.Models
{
    public class Category : BaseEntity
    {
        public string Name { get; set; }

        // Hata buradaydı: ICollection yanına <Product> eklenmeli
        public ICollection<Product> Products { get; set; }
    }
}