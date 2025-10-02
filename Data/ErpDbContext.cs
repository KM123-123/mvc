using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using mvc.Models;
using mvc.Models;

namespace mvc.Data
{
    public class ErpDbContext : IdentityDbContext<Usuario>
    {
        public ErpDbContext(DbContextOptions<ErpDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<Categoria> Categoria { get; set; }
        public DbSet<Proveedores> Proveedores { get; set; }
        public DbSet<Ubicaciones> Ubicaciones { get; set; } = default!;
        public DbSet<Productos> Productos { get; set; } = default!;
        public DbSet<Clientes> Clientes { get; set; }
        public DbSet<Ventas> Ventas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de relaciones para Productos
            modelBuilder.Entity<Productos>()
                .HasOne(p => p.Categoria)
                .WithMany()
                .HasForeignKey(p => p.CategoriaID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Productos>()
                .HasOne(p => p.Proveedor)
                .WithMany()
                .HasForeignKey(p => p.ProveedorID)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuración de decimales para Productos
            modelBuilder.Entity<Productos>()
                .Property(p => p.PrecioUnitario)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Productos>()
                .Property(p => p.ValorAdquisicion)
                .HasColumnType("decimal(10,2)");

            // Configuraciones adicionales para otros modelos si es necesario

            // Ejemplo para Proveedores si tiene campos decimal
            // modelBuilder.Entity<Proveedores>()
            //     .Property(p => p.CampoDecimal)
            //     .HasColumnType("decimal(10,2)");
        }
    }
}