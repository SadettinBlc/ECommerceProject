namespace ECommerce.API.Models
{
    public class Basket : BaseEntity
    {
        // Sepetin sahibi olan kullanıcı
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        // Sepetin içindeki ürünler
        public ICollection<BasketItem> BasketItems { get; set; }
    }
}