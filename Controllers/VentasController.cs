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
using mvc.Documents; // <-- NECESARIO PARA EL EXCEL
using QuestPDF.Fluent;
using Microsoft.AspNetCore.Hosting;
using ClosedXML.Excel; // <-- NECESARIO PARA EL EXCEL
using System.IO; // <-- NECESARIO PARA EL EXCEL
using mvc.Services;

namespace mvc.Controllers
{
    [Authorize(Roles = "Administrador, Empleado")]
    public class VentasController : Controller
    {
        private readonly ErpDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;

        public VentasController(ErpDbContext context, UserManager<Usuario> userManager, IWebHostEnvironment env, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
            _emailService = emailService;
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

        // POST: Ventas/Create
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

                    var ventaCompleta = await _context.Ventas
                        .Include(v => v.Cliente)
                        .Include(v => v.Producto)
                        .FirstOrDefaultAsync(v => v.VentaID == venta.VentaID);

                    if (ventaCompleta != null && !string.IsNullOrEmpty(ventaCompleta.Cliente.Correo))
                    {
                        var logoPath = Path.Combine(_env.WebRootPath, "images", "LOGO UMG.jpg");
                        var documentoPdf = new VentaDocument(ventaCompleta, logoPath);
                        byte[] pdfBytes = documentoPdf.GeneratePdf();

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.EnviarFacturaPorCorreoAsync(ventaCompleta, pdfBytes);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error de fondo al enviar email: {ex.Message}");
                            }
                        });
                    }

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
            if (id == null) return NotFound();
            var venta = await _context.Ventas.FindAsync(id);
            if (venta == null) return NotFound();
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

        // --- EXPORTAR PDF ---
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

        // --- EXCEL GRUPAL (ARREGLADO) ---
        public async Task<IActionResult> GenerarExcelVentas()
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Producto)
                .Include(v => v.Usuario)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte de Ventas");

                var currentRow = 1;

                // Escribir Cabeceras
                worksheet.Cell(currentRow, 1).Value = "Venta ID";
                worksheet.Cell(currentRow, 2).Value = "Fecha";
                worksheet.Cell(currentRow, 3).Value = "Cliente";
                worksheet.Cell(currentRow, 4).Value = "Producto";
                worksheet.Cell(currentRow, 5).Value = "Cantidad";
                worksheet.Cell(currentRow, 6).Value = "Total";
                worksheet.Cell(currentRow, 7).Value = "Vendedor";

                // Estilo para cabeceras
                var headerRange = worksheet.Range($"A{currentRow}:G{currentRow}");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD");

                // Escribir Datos
                foreach (var venta in ventas)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = venta.VentaID;
                    worksheet.Cell(currentRow, 2).Value = venta.FechaVenta;
                    worksheet.Cell(currentRow, 3).Value = venta.Cliente?.NombreCliente ?? "N/A";
                    worksheet.Cell(currentRow, 4).Value = venta.Producto?.NombreProducto ?? "N/A";
                    worksheet.Cell(currentRow, 5).Value = venta.Cantidad;
                    worksheet.Cell(currentRow, 6).Value = venta.Total;
                    worksheet.Cell(currentRow, 7).Value = venta.Usuario?.UserName ?? "N/A";
                }

                // Formato de Columnas
                worksheet.Column(2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                worksheet.Column(6).Style.NumberFormat.Format = "\"Q\"#,##0.00";
                worksheet.Columns().AdjustToContents();

                // Guardar y enviar
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Reporte_Ventas.xlsx");
                }
            }
        }

        // --- EXCEL INDIVIDUAL (ARREGLADO) ---
        public async Task<IActionResult> GenerarExcelVentaIndividual(int id)
        {
            // 1. Buscar la venta completa (igual que en PDF)
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Producto)
                .Include(v => v.Usuario)
                .FirstOrDefaultAsync(m => m.VentaID == id);

            if (venta == null) return NotFound();

            // 2. Usar tu clase 'VentaExcelDocument'
            var documentoExcel = new VentaExcelDocument(venta);
            byte[] excelBytes = documentoExcel.GenerateExcel();

            // 3. Devolver el archivo
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Factura-{venta.VentaID}.xlsx");
        }

        // --- MÉTODO PRIVADO (SIN CAMBIOS) ---
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