using Microsoft.AspNetCore.Mvc;

namespace tieudaobadminton.Controllers
{
    [Route("Huong-Dan-Size")]
    public class GuideController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
    }
}
