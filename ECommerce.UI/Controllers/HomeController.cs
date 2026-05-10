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



        public IActionResult Profile()
        {
            // Arayüzün API'yi bulabilmesi için gereken o sihirli köprü
            ViewBag.ApiBaseURL = _configuration["ApiBaseURL"];
            return View();
        }

    }

}