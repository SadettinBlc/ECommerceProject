namespace ECommerce.API.DTOs
{
    public class OrderCreateDto
    {
        public string ShippingAddress { get; set; }

        // Sanal Pos (Mock Payment) Bilgileri
        public string CardHolderName { get; set; }
        public string CardNumber { get; set; } // 16 hane bekleyeceğiz
        public string ExpirationDate { get; set; } // Örn: 12/28
        public string Cvv { get; set; } // 3 hane bekleyeceğiz

        // Opsiyonel: Patron çıldırdı indirimi için
        public string? CouponCode { get; set; }
    }
}