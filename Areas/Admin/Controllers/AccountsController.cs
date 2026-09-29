using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using tieudaobadminton.ViewModels;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")] // chỉ Admin được quản lý tài khoản
    public class AccountsController : Controller
    {
        private readonly UserManager<IdentityUser> _users;
        private readonly RoleManager<IdentityRole> _roles;

        public AccountsController(UserManager<IdentityUser> users, RoleManager<IdentityRole> roles)
        {
            _users = users;
            _roles = roles;
        }

        // GET: /Admin/Accounts?q=...
        public IActionResult Index(string? q, string? role)
        {
            var query = _users.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(u => u.Email!.Contains(q) || u.UserName!.Contains(q));

            // role filter: lấy list rồi lọc (Identity role query không join trực tiếp đơn giản)
            // => làm ở view: show role + filter bằng dropdown chỉ để UX, không bắt buộc.
            ViewBag.Q = q;
            ViewBag.Role = role;
            return View(query.OrderByDescending(x => x.Id).Take(200).ToList());
        }

        // GET: /Admin/Accounts/Create
        public IActionResult Create()
        {
            ViewBag.Roles = _roles.Roles.Select(r => r.Name).ToList();
            return View(new AdminCreateUserVM());
        }

        // POST: /Admin/Accounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCreateUserVM vm)
        {
            ViewBag.Roles = _roles.Roles.Select(r => r.Name).ToList();

            if (!ModelState.IsValid) return View(vm);

            var exist = await _users.FindByEmailAsync(vm.Email);
            if (exist != null)
            {
                ModelState.AddModelError("", "Email đã tồn tại.");
                return View(vm);
            }

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

            // default Customer nếu không chọn
            var role = string.IsNullOrWhiteSpace(vm.Role) ? "Customer" : vm.Role!;
            if (await _roles.RoleExistsAsync(role))
                await _users.AddToRoleAsync(user, role);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Accounts/Edit/id
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _users.GetRolesAsync(user);

            ViewBag.Roles = _roles.Roles.Select(r => r.Name).ToList();
            return View(new AdminEditUserVM
            {
                Id = user.Id,
                Email = user.Email!,
                Role = roles.FirstOrDefault() ?? "Customer",
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.Now
            });
        }

        // POST: /Admin/Accounts/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminEditUserVM vm)
        {
            ViewBag.Roles = _roles.Roles.Select(r => r.Name).ToList();
            if (!ModelState.IsValid) return View(vm);

            var user = await _users.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            // đổi email
            if (!string.Equals(user.Email, vm.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = vm.Email;
                user.UserName = vm.Email;
            }

            // đổi role (1 role chính)
            var currentRoles = await _users.GetRolesAsync(user);
            if (currentRoles.Any())
                await _users.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrWhiteSpace(vm.Role) && await _roles.RoleExistsAsync(vm.Role))
                await _users.AddToRoleAsync(user, vm.Role);

            // khóa/mở khóa
            if (vm.IsLocked)
                await _users.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
            else
                await _users.SetLockoutEndDateAsync(user, null);

            await _users.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Accounts/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string newPassword)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var rs = await _users.ResetPasswordAsync(user, token, newPassword);

            if (!rs.Succeeded)
            {
                TempData["Err"] = string.Join(" | ", rs.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Ok"] = "Đã reset mật khẩu.";
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        // POST: /Admin/Accounts/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _users.FindByIdAsync(id);
            if (user == null) return NotFound();

            // không cho tự xóa chính mình
            if (User.Identity?.Name == user.UserName)
            {
                TempData["Err"] = "Không thể xóa tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            await _users.DeleteAsync(user);
            TempData["Ok"] = "Đã xóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }
    }
}
