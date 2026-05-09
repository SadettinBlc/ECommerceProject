namespace ECommerce.API.Models
{
    public class ProductFeature : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string FeatureName { get; set; } // Filtre Başlığı (Örn: Marka, RAM, Tür)
        public string FeatureValue { get; set; } // Filtre Değeri (Örn: HP, 16 GB, Soulslike)
    }
}