using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mvc.Data;
using mvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using mvc.Documents;
using QuestPDF.Fluent;
using Microsoft.AspNetCore.Hosting;
using ClosedXML.Excel;
using System.IO;
using mvc.Services; // <-- USING AGREGADO

namespace mvc.Controllers
{
    [Authorize(Roles = "Administrador, Empleado")]
    public class VentasController : Controller
    {
        private readonly ErpDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService; // <-- CAMPO AGREGADO PARA EL SERVICIO

        // --- CONSTRUCTOR MODIFICADO ---
        public VentasController(ErpDbContext context, UserManager<Usuario> userManager, IWebHostEnvironment env, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _emailService = emailService; // <-- ASIGNACIÓN DEL SERVICIO
        }

        // GET: Ventas
        public async Task<IActionResult> Index(string busqueda)
        {
            ViewData["BusquedaActual"] = busqueda;
            var ventasQuery = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Producto)
                .Include(v => v.Usuario)
                .AsQueryable();
            if (!String.IsNullOrEmpty(busqueda))
            {
                bool esEntero = int.TryParse(busqueda, out int numeroEntero);
                bool esDecimal = decimal.TryParse(busqueda, out decimal numeroDecimal);
                ventasQuery = ventasQuery.Where(v =>
                    v.Cliente.NombreCliente.Contains(busqueda) ||
                    v.Producto.NombreProducto.Contains(busqueda) ||
                    v.Usuario.UserName.Contains(busqueda) ||
                    (esEntero && (v.VentaID == numeroEntero || v.Cantidad == numeroEntero)) ||
                    (esDecimal && v.Total == numeroDecimal) ||
                    v.FechaVenta.ToString().Contains(busqueda)
                );
            }
            return View(await ventasQuery.AsNoTracking().ToListAsync());
        }

        // GET: Ventas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Producto)
                .Include(v => v.Usuario)
                .FirstOrDefaultAsync(m => m.VentaID == id);
            if (venta == null) return NotFound();
            return View(venta);
        }

        // GET: Ventas/Create
        public async Task<IActionResult> Create()
        {
            await CargarDatosParaVista(null);
            return View();
        }

        // --- MÉTODO CREATE [HttpPost] MODIFICADO (RÁPIDO) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("VentaID,ClienteID,ProductoID,Cantidad,FechaVenta,Total")] Ventas venta)
        {
            ModelState.Remove("Cliente");
            ModelState.Remove("Producto");
            ModelState.Remove("Usuario");
            ModelState.Remove("UsuarioID");

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                venta.UsuarioID = user.Id;
            }
            else
            {
                ModelState.AddModelError("UsuarioID", "Debe iniciar sesión un usuario válido.");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    if (venta.ProductoID > 0 && venta.Cantidad > 0)
                    {
                        var producto = await _context.Productos.FindAsync(venta.ProductoID);
                        if (producto != null)
                        {
                            if (producto.StockActual < venta.Cantidad)
                            {
                                ModelState.AddModelError("Cantidad", $"No hay suficiente stock para '{producto.NombreProducto}'. Stock actual: {producto.StockActual}.");
                            }
                            else
                            {
                                producto.StockActual -= venta.Cantidad;
                                _context.Update(producto);
                                venta.Total = producto.PrecioUnitario * venta.Cantidad;
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("ProductoID", "El producto seleccionado no es válido.");
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        await transaction.RollbackAsync();
                        await CargarDatosParaVista(venta);
                        return View(venta);
                    }

                    _context.Add(venta);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // --- INICIO DE LA LÓGICA DE ENVÍO DE CORREO (ASÍNCRONO) ---
                    var ventaCompleta = await _context.Ventas
                        .Include(v => v.Cliente)
                        .Include(v => v.Producto)
                        .FirstOrDefaultAsync(v => v.VentaID == venta.VentaID);

                    if (ventaCompleta != null && !string.IsNullOrEmpty(ventaCompleta.Cliente.Correo))
                    {
                        var logoPath = Path.Combine(_env.WebRootPath, "images", "LOGO UMG.jpg");
                        var documentoPdf = new VentaDocument(ventaCompleta, logoPath);
                        byte[] pdfBytes = documentoPdf.GeneratePdf();

                        // ¡ESTE ES EL CAMBIO!
                        // No esperamos (await) al email. Lo "disparamos y olvidamos" en 
                        // un hilo separado para que el usuario no tenga que esperar.
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.EnviarFacturaPorCorreoAsync(ventaCompleta, pdfBytes);
                            }
                            catch (Exception ex)
                            {
                                // Si el email falla, no bloqueamos al usuario.
                                // Solo lo registramos en los logs del contenedor.
                                Console.WriteLine($"Error de fondo al enviar email: {ex.Message}");
                            }
                        });
                    }
                    // --- FIN DE LA LÓGICA DE ENVÍO ---

                    // El usuario recibe la respuesta INMEDIATAMENTE
                    TempData["Success"] = "Venta creada exitosamente. La factura se está enviando al cliente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Exception"] = "Error inesperado al crear la venta: " + ex.Message;
                    await CargarDatosParaVista(venta);
                    return View(venta);
                }
            }
        }


        // GET: Ventas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var venta = await _context.Ventas.FindAsync(id);
            if (venta == null)
            {
                return NotFound();
            }

            await CargarDatosParaVista(venta);
            return View(venta);
        }

        // POST: Ventas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VentaID,ClienteID,ProductoID,Cantidad,FechaVenta,Total,UsuarioID")] Ventas venta)
        {
            if (id != venta.VentaID) return NotFound();

            ModelState.Remove("Cliente");
            ModelState.Remove("Producto");
            ModelState.Remove("Usuario");

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var ventaOriginal = await _context.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.VentaID == id);
                    if (ventaOriginal == null) return NotFound();

                    var producto = await _context.Productos.FindAsync(venta.ProductoID);
                    if (producto == null)
                    {
                        ModelState.AddModelError("ProductoID", "El producto seleccionado no es válido.");
                    }
                    else
                    {
                        int diferencia = venta.Cantidad - ventaOriginal.Cantidad;
                        if (producto.StockActual < diferencia)
                        {
                            ModelState.AddModelError("Cantidad", $"No hay suficiente stock para aumentar la cantidad. Stock actual: {producto.StockActual}.");
                        }
                        else
                        {
                            producto.StockActual -= diferencia;
                            _context.Update(producto);
                            venta.Total = producto.PrecioUnitario * venta.Cantidad;
                        }
                    }

                    if (!ModelState.IsValid)
                    {
                        await CargarDatosParaVista(venta);
                        return View(venta);
                    }

                    _context.Update(venta);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Venta actualizada exitosamente. El stock ha sido ajustado.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["Exception"] = "Error inesperado al actualizar la venta: " + ex.Message;
                    await CargarDatosParaVista(venta);
                    return View(venta);
                }
            }
        }

        // GET: Ventas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Producto)
                .Include(v => v.Usuario)
                .FirstOrDefaultAsync(m => m.VentaID == id);
            if (venta == null) return NotFound();
            return View(venta);
        }

        // POST: Ventas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venta = await _context.Ventas.FindAsync(id);
            if (venta != null)
            {
                var producto = await _context.Productos.FindAsync(venta.ProductoID);
                if (producto != null)
                {
                    producto.StockActual += venta.Cantidad;
                    _context.Update(producto);
                }
                _context.Ventas.Remove(venta);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Venta eliminada y stock restaurado.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GenerarPdfVenta(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Producto)
                .Include(v => v.Usuario)
                .FirstOrDefaultAsync(m => m.VentaID == id);
            if (venta == null) return NotFound();

            var logoPath = Path.Combine(_env.WebRootPath, "images", "LOGO UMG.jpg");
            var documentoPdf = new VentaDocument(venta, logoPath);
            byte[] pdfBytes = documentoPdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Venta-{venta.VentaID}.pdf");
        }

        private bool VentaExists(int id)
        {
            return _context.Ventas.Any(e => e.VentaID == id);
        }

        public async Task<IActionResult> GenerarExcelVentas()
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Producto)
                .Include(v => v.Usuario)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte de Ventas");
                // ... (tu código para el Excel grupal)
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte_Ventas.xlsx");
                }
            }
        }

        public async Task<IActionResult> GenerarExcelVentaIndividual(int id)
        {
            // ... (tu código para el Excel individual)
            return Ok(); // Placeholder
        }

        private async Task CargarDatosParaVista(Ventas venta)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UsuarioNombre = user?.UserName ?? "Desconocido";
            ViewBag.FechaVenta = venta?.FechaVenta != default ? venta.FechaVenta : DateTime.Now;

            ViewBag.ClienteID = new SelectList(
                _context.Clientes.Where(c => c.Estado == "Activo").Select(c => new {
                    ClienteID = c.ClienteID,
                    Nombre = c.Nit + " - " + c.NombreCliente
                }), "ClienteID", "Nombre", venta?.ClienteID);

            ViewBag.ProductoID = new SelectList(
                _context.Productos.Where(p => p.EstadoProducto).Select(p => new {
                    ProductoID = p.ProductoID,
                    Nombre = p.CodigoProducto + " - " + p.NombreProducto + " - (Q" + p.PrecioUnitario + ")"
                }), "ProductoID", "Nombre", venta?.ProductoID);

            ViewBag.Clientes = await _context.Clientes.Where(c => c.Estado == "Activo").ToListAsync();
            ViewBag.Productos = await _context.Productos.Where(p => p.EstadoProducto).ToListAsync();
        }
    }
}