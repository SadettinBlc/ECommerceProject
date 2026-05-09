namespace ECommerce.API.DTOs
{
    public class CategoryUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; } // Belki kategoriyi silmek yerine gizlemek isteriz
    }
}