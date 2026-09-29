using Microsoft.AspNetCore.Mvc;

namespace tieudaobadminton.Controllers
{
    [Route("Dich-Vu-Cang-Cuoc")]
    public class ServicesController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
