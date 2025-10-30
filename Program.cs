using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mvc.Data;
using mvc.Models;
using QuestPDF.Infrastructure; //Para Pdf
using mvc.Services;
using OfficeOpenXml; //Para el Importar el Excel
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// 🔹 Agregar configuración para soportar entornos (Docker, Development, etc.)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// 🔹 Configurar la base de datos
builder.Services.AddDbContext<ErpDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Conexion")));

// 🔹 Identity (roles, usuarios)
builder.Services.AddIdentity<Usuario, IdentityRole>()
    .AddEntityFrameworkStores<ErpDbContext>()
    .AddDefaultTokenProviders();

// 🔹 Otros servicios
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<IEmailService, EmailService>();

// 🔹 Para PDF
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// 🔹 Crear roles automáticamente al iniciar
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Administrador", "Empleado" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// 🔹 Configuración del pipeline HTTP
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
