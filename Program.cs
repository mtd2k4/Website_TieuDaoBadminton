using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;

var builder = WebApplication.CreateBuilder(args);

// =======================
// 1. DbContext (EF Core)
// =======================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// =======================
// 2. Identity (NET 8)
// =======================
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// =======================
// 3. MVC + Razor + Services
// =======================
builder.Services.AddHttpClient();
builder.Services.AddScoped<tieudaobadminton.Helpers.OrderNotificationEmailService>();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // ⚠ BẮT BUỘC cho Identity UI

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

// 🔐 BẮT BUỘC CÓ 2 DÒNG NÀY
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
// MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Identity Razor Pages
app.MapRazorPages();
await DbSeeder.SeedAsync(app.Services);

await tieudaobadminton.Data.IdentitySeeder.SeedAsync(app.Services);
app.Run();
