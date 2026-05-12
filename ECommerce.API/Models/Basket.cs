namespace ECommerce.API.Models
{
    public class Basket : BaseEntity
    {
        
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        
        public ICollection<BasketItem> BasketItems { get; set; }
    }
}