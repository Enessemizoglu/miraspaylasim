using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using MirasPaylasim.Models;
using MirasPaylasim.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

// EF Core - SQL Server
builder.Services.AddDbContext<MirasContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity yapılandırması
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Şifre gereksinimleri
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    
    // Kullanıcı ayarları
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<MirasContext>()
.AddDefaultTokenProviders();

// Register inheritance calculator
builder.Services.AddScoped<MirasPaylasim.Services.IInheritanceCalculator, MirasPaylasim.Services.InheritanceCalculator>();

// Register Gemini News Filter Service
builder.Services.AddScoped<MirasPaylasim.Services.GeminiNewsFilterService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
