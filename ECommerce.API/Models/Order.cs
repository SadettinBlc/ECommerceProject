using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.API.Models
{
    public class Order : BaseEntity
    {
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }

        public string Address { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        public string OrderStatus { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; }
    }
}