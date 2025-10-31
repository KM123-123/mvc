using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mvc.Data;
using mvc.Models;
using QuestPDF.Infrastructure; // Para PDF
using mvc.Services;
using OfficeOpenXml; // Para importar Excel
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Cargar configuración principal
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// 🔹 Configurar la conexión a la base de datos
builder.Services.AddDbContext<ErpDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Conexion")));

// 🔹 Configurar Identity (usuarios y roles)
builder.Services.AddIdentity<Usuario, IdentityRole>()
    .AddEntityFrameworkStores<ErpDbContext>()
    .AddDefaultTokenProviders();

// 🔹 Registrar servicios adicionales
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IEmailService, EmailService>();

// 🔹 Configuración para PDF
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// 🔹 Middleware de errores y seguridad
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 🔹 Rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
