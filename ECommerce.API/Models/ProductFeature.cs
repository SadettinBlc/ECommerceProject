namespace ECommerce.API.Models
{
    public class ProductFeature : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string FeatureName { get; set; } 
        public string FeatureValue { get; set; } 
    }
}