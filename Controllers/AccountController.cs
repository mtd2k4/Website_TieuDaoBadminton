using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.ViewModel.Auth;

namespace tieudaobadminton.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signIn;
        private readonly UserManager<IdentityUser> _users;
        private readonly ApplicationDbContext _db;

        public AccountController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> users, ApplicationDbContext db)
        {
            _signIn = signIn;
            _users = users;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
            => View(new LoginVM { ReturnUrl = returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = await _users.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(vm);
            }

            var result = await _signIn.PasswordSignInAsync(user, vm.Password, vm.RememberMe, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(vm);
            }

            // Redirect theo role: Admin vào Dashboard
            if (await _users.IsInRoleAsync(user, "Admin") || await _users.IsInRoleAsync(user, "Staff"))
                return RedirectToAction("Index", "Home", new { area = "Admin" });

            // Customer: về returnUrl nếu có, không thì về Home
            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
            => View(new RegisterVM { ReturnUrl = returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var user = new IdentityUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                EmailConfirmed = true
            };

            var created = await _users.CreateAsync(user, vm.Password);
            if (!created.Succeeded)
            {
                foreach (var e in created.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // Mặc định khách hàng
            await _users.AddToRoleAsync(user, "Customer");

            await _signIn.SignInAsync(user, isPersistent: true);

            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            var user = await _users.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var userId = user.Id;
            var userName = user.UserName ?? "";
            var phone = user.PhoneNumber ?? "";

            var orders = await _db.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId || o.UserId == userName || (!string.IsNullOrEmpty(phone) && o.Phone == phone))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }
    }
}
