namespace ECommerce.API.DTOs
{
    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } // O sayfadaki ürünlerin listesi
        public int TotalCount { get; set; } // Veritabanındaki toplam ürün sayısı
        public int TotalPages { get; set; } // Toplam sayfa sayısı
        public int CurrentPage { get; set; } // Şu an bulunulan sayfa
        public int PageSize { get; set; } // Bir sayfada gösterilecek ürün sayısı
    }
}