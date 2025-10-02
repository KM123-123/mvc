using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using mvc.Data;
using mvc.Models;
using QuestPDF.Infrastructure; //Para Pdf

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ErpDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Conexion")));

builder.Services.AddIdentity<Usuario, IdentityRole>()
    .AddEntityFrameworkStores<ErpDbContext>()
    .AddDefaultTokenProviders(); // 🔹 Tokens para recuperar contraseñas, etc.

// Add services to the container.
builder.Services.AddControllersWithViews();

//Para utilizar pdf
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// 🔹 Crear roles al iniciar la aplicación
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // ← Necesario para Identity
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
