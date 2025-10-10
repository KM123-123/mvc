using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using mvc.Data; // Asegúrate que este sea el namespace de tu DbContext
using mvc.ViewModels;
using System.Globalization; // Necesario para el dashboard operativo

namespace mvc.Controllers
{
    // Se actualizó para que ambos roles puedan acceder a los dashboards
    [Authorize(Roles = "Administrador, Empleado")]
    public class DashboardController : Controller
    {
        private readonly ErpDbContext _context;

        public DashboardController(ErpDbContext context)
        {
            _context = context;
        }

        //--- MÉTODO PARA EL DASHBOARD GERENCIAL (INDEX) ---
        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin)
        {
            // 1. Establecer el rango de fechas por defecto (últimos 30 días)
            var fin = fechaFin ?? DateTime.Now;
            var inicio = fechaInicio ?? fin.AddDays(-29);

            var viewModel = new DashboardViewModel
            {
                FechaInicio = inicio,
                FechaFin = fin
            };

            // 2. Obtener las ventas en el rango de fechas, incluyendo datos relacionados
            var ventasEnRango = _context.Ventas
                .Include(v => v.Producto)
                    .ThenInclude(p => p.Categoria)
                .Where(v => v.FechaVenta.Date >= inicio.Date && v.FechaVenta.Date <= fin.Date);

            var ventasList = await ventasEnRango.ToListAsync();

            if (!ventasList.Any())
            {
                // Si no hay ventas, devolver el modelo vacío para evitar errores
                return View(viewModel);
            }

            // 3. Calcular los KPIs
            viewModel.NumeroDeVentas = ventasList.Count;
            viewModel.IngresosTotales = ventasList.Sum(v => v.Total);
            viewModel.GananciaBruta = ventasList.Sum(v => v.Total - (v.Producto.ValorAdquisicion * v.Cantidad));
            viewModel.TicketPromedio = (viewModel.NumeroDeVentas > 0) ? viewModel.IngresosTotales / viewModel.NumeroDeVentas : 0;

            // 4. Preparar datos para los gráficos
            // Gráfico de Líneas: Ventas por Día
            viewModel.VentasPorDia = ventasList
                .GroupBy(v => v.FechaVenta.Date)
                .Select(g => new ChartData
                {
                    Dimension = g.Key.ToString("dd/MM"),
                    Total = g.Sum(v => v.Total)
                })
                .OrderBy(g => DateTime.ParseExact(g.Dimension, "dd/MM", CultureInfo.InvariantCulture))
                .ToList();

            // Gráfico de Barras: Top 5 Productos
            viewModel.TopProductos = ventasList
                .GroupBy(v => v.Producto.NombreProducto)
                .Select(g => new ChartData
                {
                    Dimension = g.Key,
                    Total = g.Sum(v => v.Total)
                })
                .OrderByDescending(g => g.Total)
                .Take(5)
                .ToList();

            // Gráfico de Pie: Ventas por Categoría
            viewModel.VentasPorCategoria = ventasList
                .Where(v => v.Producto.Categoria != null) // Evitar errores si una categoría es nula
                .GroupBy(v => v.Producto.Categoria.Nombre) // Asegúrate que la propiedad se llame 'NombreCategoria'
                .Select(g => new ChartData
                {
                    Dimension = g.Key,
                    Total = g.Sum(v => v.Total)
                })
                .OrderByDescending(g => g.Total)
                .ToList();

            return View(viewModel);
        }

        //--- MÉTODO PARA EL DASHBOARD OPERATIVO ---
        public async Task<IActionResult> Operativo()
        {
            var hoy = DateTime.Now.Date;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            var viewModel = new DashboardOperativoViewModel();

            // 1. Calcular los KPIs
            viewModel.VentasHoy = await _context.Ventas
                .Where(v => v.FechaVenta.Date == hoy)
                .SumAsync(v => v.Total);

            // Usa el campo FechaCreacion que agregaste al modelo Clientes
            viewModel.NuevosClientesHoy = await _context.Clientes
                .Where(c => c.FechaCreacion.Date == hoy)
                .CountAsync();

            viewModel.ProductosBajoStock = await _context.Productos
                .Where(p => p.EstadoProducto && p.StockActual <= p.StockMinimo)
                .CountAsync();

            // 2. Obtener listas y rankings
            viewModel.UltimasVentas = await _context.Ventas
                .OrderByDescending(v => v.FechaVenta)
                .Take(5)
                .Select(v => new VentaRecienteViewModel
                {
                    VentaID = v.VentaID,
                    ClienteNombre = v.Cliente.NombreCliente,
                    ProductoNombre = v.Producto.NombreProducto,
                    VendedorNombre = v.Usuario.UserName,
                    Total = v.Total,
                    FechaVenta = v.FechaVenta
                }).ToListAsync();

            viewModel.ProductosParaReabastecer = await _context.Productos
                .Where(p => p.EstadoProducto && p.StockActual <= p.StockMinimo)
                .OrderBy(p => p.StockActual)
                .Select(p => new ProductoBajoStockViewModel
                {
                    NombreProducto = p.NombreProducto,
                    StockActual = p.StockActual,
                    StockMinimo = p.StockMinimo
                }).ToListAsync();

            viewModel.RankingVendedoresMes = await _context.Ventas
                .Where(v => v.FechaVenta >= inicioMes)
                .GroupBy(v => v.Usuario.UserName)
                .Select(g => new RankingVendedorViewModel
                {
                    NombreVendedor = g.Key,
                    TotalVendido = g.Sum(v => v.Total),
                    CantidadVentas = g.Count()
                })
                .OrderByDescending(r => r.TotalVendido)
                .ToListAsync();

            return View("Operativo", viewModel);
        }
    }
}