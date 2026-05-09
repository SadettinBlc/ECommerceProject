namespace ECommerce.API.Models
{
    public class BasketItem : BaseEntity
    {
        // Hangi sepete ait olduğu
        public int BasketId { get; set; }
        public Basket Basket { get; set; }

        // Sepete atılan ürün
        public int ProductId { get; set; }
        public Product Product { get; set; }

        // Alınacak adet
        public int Quantity { get; set; }
    }
}