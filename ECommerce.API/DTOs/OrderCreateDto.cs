namespace ECommerce.API.DTOs
{
    public class OrderCreateDto
    {
        public string ShippingAddress { get; set; }

        
        public string CardHolderName { get; set; }
        public string CardNumber { get; set; } 
        public string ExpirationDate { get; set; } 
        public string Cvv { get; set; } 

        
        public string? CouponCode { get; set; }
    }
}