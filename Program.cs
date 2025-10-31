using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; // <-- AÑADIDO
using mvc.Data;
using mvc.Models;
using QuestPDF.Infrastructure; // Para PDF
using mvc.Services;
using OfficeOpenXml; // Para importar Excel
using System.Globalization;
using Microsoft.Extensions.Logging; // <-- AÑADIDO

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

// --- INICIO: CÓDIGO PARA APLICAR MIGRACIONES ---
// Este bloque crea la base de datos "pruebainv" y sus tablas si no existen.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Pide el DbContext (tu conexión a la BD)
        var context = services.GetRequiredService<ErpDbContext>();

        // Aplica las migraciones. Esto crea la BD y las tablas.
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Si algo falla, lo mostrará en los logs de Docker
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Un error ocurrió al migrar la base de datos.");
    }
}
// --- FIN: CÓDIGO PARA APLICAR MIGRACIONES ---


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