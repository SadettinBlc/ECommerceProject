namespace ECommerce.API.Models
{
    public class Review : BaseEntity
    {
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string Comment { get; set; }
        public int Rating { get; set; }
    }
}