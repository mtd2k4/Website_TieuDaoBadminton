using Microsoft.AspNetCore.Identity;

namespace tieudaobadminton.Data
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // Roles
            if (!await roleMgr.RoleExistsAsync("Admin"))
                await roleMgr.CreateAsync(new IdentityRole("Admin"));

            if (!await roleMgr.RoleExistsAsync("Customer"))
                await roleMgr.CreateAsync(new IdentityRole("Customer"));

            // Admin user
            var adminEmail = "admin@shop.com";
            var admin = await userMgr.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userMgr.CreateAsync(admin, "Admin@12345");
                if (result.Succeeded)
                    await userMgr.AddToRoleAsync(admin, "Admin");
            }
            else
            {
                // Mở khóa nếu tài khoản đang bị khóa
                await userMgr.SetLockoutEndDateAsync(admin, null);

                // Khôi phục mật khẩu về Admin@12345
                var token = await userMgr.GeneratePasswordResetTokenAsync(admin);
                await userMgr.ResetPasswordAsync(admin, token, "Admin@12345");

                // Đảm bảo có Role Admin
                if (!await userMgr.IsInRoleAsync(admin, "Admin"))
                    await userMgr.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
