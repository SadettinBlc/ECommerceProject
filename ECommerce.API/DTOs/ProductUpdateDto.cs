namespace ECommerce.API.DTOs
{
    public class ProductUpdateDto
    {
        public int Id { get; set; } // Hangi ürünü güncelleyeceğimizi bilmek için ID şart!
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public string PhotoUrl { get; set; }
        public bool IsActive { get; set; } // Ürünü silmek yerine satışa kapatmak (gizlemek) için
    }
}