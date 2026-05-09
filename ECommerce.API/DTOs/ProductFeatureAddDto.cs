namespace ECommerce.API.DTOs
{
    public class ProductFeatureAddDto
    {
        public int ProductId { get; set; }
        public string FeatureName { get; set; }
        public string FeatureValue { get; set; }
    }
}