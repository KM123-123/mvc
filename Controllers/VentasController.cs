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
using mvc.Documents; // El namespace entaDocument
using QuestPDF.Fluent; // El using principal de QuestPDF
using Microsoft.AspNetCore.Hosting; //Para la Imagen
using ClosedXML.Excel;
using System.IO;

namespace mvc.Controllers
{
    [Authorize(Roles = "Administrador, Empleado")]
    public class VentasController : Controller
    {
        private readonly ErpDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly IWebHostEnvironment _env;

        public VentasController(ErpDbContext context, UserManager<Usuario> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
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
                // Intenta convertir la búsqueda a diferentes tipos numéricos
                int numeroEntero;
                bool esEntero = int.TryParse(busqueda, out numeroEntero);

                decimal numeroDecimal;
                bool esDecimal = decimal.TryParse(busqueda, out numeroDecimal);

                ventasQuery = ventasQuery.Where(v =>
                    // Búsqueda en campos de texto (existente)
                    v.Cliente.NombreCliente.Contains(busqueda) ||
                    v.Producto.NombreProducto.Contains(busqueda) ||
                    v.Usuario.UserName.Contains(busqueda) ||

                    // Búsqueda por ID, Cantidad (si el texto es un número entero)
                    (esEntero && (v.VentaID == numeroEntero || v.Cantidad == numeroEntero)) ||

                    // Búsqueda por Total (si el texto es un número decimal)
                    (esDecimal && v.Total == numeroDecimal) ||

                    // Búsqueda en la fecha (convirtiendo la fecha a texto)
                    // Esto permite buscar por año, mes, día, etc. ej: "2024", "09-27"
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
                        await CargarDatosParaVista(venta);
                        return View(venta);
                    }

                    _context.Add(venta);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["Success"] = "Venta creada exitosamente. El stock ha sido actualizado.";
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
                    // Obtenemos la venta original ANTES de cualquier cambio
                    var ventaOriginal = await _context.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.VentaID == id);
                    if (ventaOriginal == null) return NotFound();

                    var producto = await _context.Productos.FindAsync(venta.ProductoID);
                    if (producto == null)
                    {
                        ModelState.AddModelError("ProductoID", "El producto seleccionado no es válido.");
                    }
                    else
                    {
                        int cantidadOriginal = ventaOriginal.Cantidad;
                        int cantidadNueva = venta.Cantidad;
                        int diferencia = cantidadNueva - cantidadOriginal;

                        // Si el stock actual menos la diferencia es negativo, no hay suficiente stock
                        if (producto.StockActual < diferencia)
                        {
                            ModelState.AddModelError("Cantidad", $"No hay suficiente stock para aumentar la cantidad. Stock actual: {producto.StockActual}.");
                        }
                        else
                        {
                            // Ajustamos el stock con la diferencia
                            producto.StockActual -= diferencia;
                            _context.Update(producto);

                            // Recalculamos el total
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
            try
            {
                var venta = await _context.Ventas.FindAsync(id);
                if (venta != null)
                {
                    _context.Ventas.Remove(venta);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Venta eliminada exitosamente";
                }
                else
                {
                    TempData["Exception"] = "La venta no existe";
                }
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                TempData["Exception"] = "Error al eliminar de la base de datos: " + inner;
            }
            catch (Exception ex)
            {
                TempData["Exception"] = "Error inesperado: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GenerarPdfVenta(int id)
        {
            // 1. Obtener los datos de la venta (igual que en el método Details)
            var venta = await _context.Ventas
                              .Include(v => v.Cliente)
                              .Include(v => v.Producto)
                              .Include(v => v.Usuario)
                              .FirstOrDefaultAsync(m => m.VentaID == id);

            if (venta == null)
            {
                return NotFound();
            }


            // 2. Crear una instancia del documento pasándole los datos de la venta
            var logoPath = Path.Combine(_env.WebRootPath, "images", "LOGO UMG.jpg");
            var documentoPdf = new VentaDocument(venta, logoPath);

            // 3. Generar el PDF en memoria
            byte[] pdfBytes = documentoPdf.GeneratePdf();

            // 4. Devolver el archivo PDF para que el navegador lo muestre o descargue
            return File(pdfBytes, "application/pdf", $"Venta-{venta.VentaID}.pdf");
        }

        private bool VentaExists(int id)
        {
            return _context.Ventas.Any(e => e.VentaID == id);
        }

        // Agrega este método a tu VentasController.cs

        public async Task<IActionResult> GenerarExcelVentas()
        {
            // 1. Obtener los datos de todas las ventas (esto no cambia)
            var ventas = await _context.Ventas
                                    .Include(v => v.Cliente)
                                    .Include(v => v.Producto)
                                    .Include(v => v.Usuario)
                                    .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reporte de Ventas");
                var currentRow = 1;

                // --- ENCABEZADOS DE LA TABLA (CON MÁS DETALLE) ---
                // Se agregaron las columnas para los detalles del cliente y producto
                worksheet.Cell(currentRow, 1).Value = "ID Venta";
                worksheet.Cell(currentRow, 2).Value = "NIT Cliente";
                worksheet.Cell(currentRow, 3).Value = "Nombre Cliente";
                worksheet.Cell(currentRow, 4).Value = "Dirección Cliente";
                worksheet.Cell(currentRow, 5).Value = "Código Producto";
                worksheet.Cell(currentRow, 6).Value = "Nombre Producto";
                worksheet.Cell(currentRow, 7).Value = "Cantidad";
                worksheet.Cell(currentRow, 8).Value = "Precio Unitario";
                worksheet.Cell(currentRow, 9).Value = "Total";
                worksheet.Cell(currentRow, 10).Value = "Fecha de Venta";
                worksheet.Cell(currentRow, 11).Value = "Vendedor";

                // Aplicar estilo al encabezado (ajustamos el rango a las nuevas columnas)
                var headerRange = worksheet.Range("A1:K1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;


                // --- CUERPO DE LA TABLA (DATOS) ---
                foreach (var venta in ventas)
                {
                    currentRow++;
                    // Llenamos las nuevas celdas con la información detallada
                    worksheet.Cell(currentRow, 1).Value = venta.VentaID;
                    worksheet.Cell(currentRow, 2).Value = venta.Cliente?.Nit ?? "N/A";
                    worksheet.Cell(currentRow, 3).Value = venta.Cliente?.NombreCliente ?? "N/A";
                    worksheet.Cell(currentRow, 4).Value = venta.Cliente?.Direccion ?? "N/A";
                    worksheet.Cell(currentRow, 5).Value = venta.Producto?.CodigoProducto ?? "N/A";
                    worksheet.Cell(currentRow, 6).Value = venta.Producto?.NombreProducto ?? "N/A";
                    worksheet.Cell(currentRow, 7).Value = venta.Cantidad;
                    worksheet.Cell(currentRow, 8).Value = venta.Producto?.PrecioUnitario ?? 0;
                    worksheet.Cell(currentRow, 9).Value = venta.Total;
                    worksheet.Cell(currentRow, 10).Value = venta.FechaVenta;
                    worksheet.Cell(currentRow, 11).Value = venta.Usuario?.UserName ?? "N/A";
                }

                // --- AJUSTAR FORMATOS Y ANCHOS ---
                // Ajustamos los índices de las columnas de moneda y fecha
                worksheet.Column(8).Style.NumberFormat.Format = "\"Q\"#,##0.00"; // Formato Precio Unitario
                worksheet.Column(9).Style.NumberFormat.Format = "\"Q\"#,##0.00"; // Formato Total
                worksheet.Column(10).Style.DateFormat.Format = "dd/MM/yyyy hh:mm tt"; // Formato de fecha

                worksheet.Columns().AdjustToContents(); // Ajustar ancho de todas las columnas al contenido


                // --- GENERAR Y DEVOLVER EL ARCHIVO (esto no cambia) ---
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    string nombreArchivo = $"Reporte_Ventas_Detallado_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    return File(content, contentType, nombreArchivo);
                }
            }
        }

        public async Task<IActionResult> GenerarExcelVentaIndividual(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Producto)
                .Include(v => v.Usuario)
                .FirstOrDefaultAsync(m => m.VentaID == id);

            if (venta == null) return NotFound();

            var documentoExcel = new VentaExcelDocument(venta);
            byte[] excelBytes = documentoExcel.GenerateExcel();

            string nombreArchivo = $"Venta_{venta.VentaID}.xlsx";
            string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return File(excelBytes, contentType, nombreArchivo);
        }

        private async Task CargarDatosParaVista(Ventas venta)
        {
            var user = await _userManager.GetUserAsync(User);
            ViewBag.UsuarioNombre = user?.UserName ?? "Desconocido";

            ViewBag.FechaVenta = venta?.FechaVenta != default ? venta.FechaVenta : DateTime.Now;

            ViewBag.ClienteID = new SelectList(
                _context.Clientes
                        .Where(c => c.Estado == "Activo") // <-- CORREGIDO
                        .Select(c => new {
                            ClienteID = c.ClienteID,
                            Nombre = c.Nit + " - " + c.NombreCliente
                        }),
                "ClienteID", "Nombre", venta?.ClienteID);

            ViewBag.ProductoID = new SelectList(
                _context.Productos
                        .Where(p => p.EstadoProducto) // <-- CORREGIDO
                        .Select(p => new {
                            ProductoID = p.ProductoID,
                            Nombre = p.CodigoProducto + " - " + p.NombreProducto + " - (Q" + p.PrecioUnitario + ")"
                        }),
                "ProductoID", "Nombre", venta?.ProductoID);

            // También filtramos los objetos completos que van al JavaScript
            ViewBag.Clientes = await _context.Clientes.Where(c => c.Estado == "Activo").ToListAsync(); // <-- CORREGIDO
            ViewBag.Productos = await _context.Productos.Where(p => p.EstadoProducto).ToListAsync(); // <-- CORREGIDO
        }
    }
}