using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
namespace uyg.UI.Controllers

{

    public class HomeController : Controller

    {

        private readonly ILogger<HomeController> _logger;

        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)

        {

            _logger = logger;

            _configuration = configuration;

        }



        public IActionResult Index()

        {

            return View();

        }



        [Route("Categories")]

        public IActionResult Categories()

        {

            var ApiBaseURL = _configuration["ApiBaseURL"];

            ViewBag.ApiBaseURL = ApiBaseURL;

            return View();

        }



        [Route("Products/{id}")]

        [Route("Products")]

        public IActionResult Products(int id = 0)

        {

            var ApiBaseURL = _configuration["ApiBaseURL"];

            ViewBag.ApiBaseURL = ApiBaseURL;

            ViewBag.CatId = id;

            return View();

        }

        


        [Route("Login")]

        public IActionResult Login()

        {

            var ApiBaseURL = _configuration["ApiBaseURL"];

            ViewBag.ApiBaseURL = ApiBaseURL;

            return View();

        }

           // Tarayıcıya direkt /Register yazılınca çalışsın
        [Route("Home/Register")]    // Tarayıcıya /Home/Register yazılınca da çalışsın
        public IActionResult Register()
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        public IActionResult Roles()
        {
            // Arayüzün API ile konuşabilmesi için adresi yolluyoruz
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        public IActionResult Profile()
        {
            // Arayüzün API'yi bulabilmesi için gereken o sihirli köprü
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        public IActionResult MainPage()
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        [Route("Home/ProductDetails/{id}")]
        public IActionResult ProductDetails(int id)
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            ViewBag.ProductId = id; // Hangi ürüne tıklandığını arayüze (JavaScript'e) iletiyoruz
            return View();
        }

        [Route("Home/Favorites")]
        public IActionResult Favorites()
        {
            // API adresini View'a taşıyoruz
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        [Route("Home/Basket")]
        public IActionResult Basket()
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        [Route("Home/Checkout")]
        public IActionResult Checkout()
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        [Route("Home/MyOrders")]
        public IActionResult MyOrders()
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        [Route("Home/AdminOrders")]
        public IActionResult AdminOrders()
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }
        // BURAYI MyProfile OLARAK DEĞİŞTİRDİK
        [Route("Home/MyProfile")]
        public IActionResult MyProfile()
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        [Route("Home/MyReviews")]
        public IActionResult MyReviews()
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        [Route("Home/AdminReviews")]
        public IActionResult AdminReviews()
        {
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

        [Route("Home/Contact")]
        public IActionResult Contact()
        {
            return View();
        }
    }
}