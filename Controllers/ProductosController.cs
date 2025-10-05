using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using mvc.Data;
using mvc.Models;
using Microsoft.AspNetCore.Authorization;

namespace mvc.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ProductosController : Controller
    {
        private readonly ErpDbContext _context;
        private readonly ILogger<ProductosController> _logger;

        public ProductosController(ErpDbContext context, ILogger<ProductosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Productos
        public async Task<IActionResult> Index(string busqueda)
        {
            // Mantiene el valor del buscador en la vista después de la búsqueda.
            ViewData["BusquedaActual"] = busqueda;

            // 1. ESTA ES TU BASE: Se mantiene la consulta original con los Includes.
            // Usamos IQueryable para poder añadirle más filtros después.
            var productosQuery = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .AsQueryable();

            // 2. SE AÑADE EL FILTRO: Si el usuario escribió algo, filtramos la consulta.
            if (!String.IsNullOrEmpty(busqueda))
            {
                int idBuscado = 0;
                bool esBusquedaDeId = int.TryParse(busqueda, out idBuscado);

                productosQuery = productosQuery.Where(p =>
                    (esBusquedaDeId && p.ProductoID == idBuscado) ||
                    p.CodigoProducto.Contains(busqueda) ||
                    p.NombreProducto.Contains(busqueda) ||
                    p.EstadoProducto.ToString().Contains(busqueda) ||
                    p.Categoria.Nombre.Contains(busqueda) ||
                    p.Proveedor.NombreProveedor.Contains(busqueda)
                );
            }

            // 3. SE EJECUTA LA CONSULTA FINAL: Se trae la lista (ya sea completa o filtrada).
            return View(await productosQuery.AsNoTracking().ToListAsync());
        }

        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(m => m.ProductoID == id);

            if (productos == null)
            {
                return NotFound();
            }

            return View(productos);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            CargarListasDesplegables();
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProductoID,CodigoProducto,NombreProducto,CategoriaID,ProveedorID,EstadoProducto,StockActual,StockMinimo,PrecioUnitario,ValorAdquisicion,FechaAdquisicion")] Productos productos)
        {
            // Remover validaciones de propiedades de navegación que pueden causar problemas
            ModelState.Remove("Categoria");
            ModelState.Remove("Proveedor");

            // DEBUG: Verificar valores recibidos
            _logger.LogInformation($"CategoriaID recibido: {productos.CategoriaID}");
            _logger.LogInformation($"ProveedorID recibido: {productos.ProveedorID}");

            // Validación personalizada para CategoriaID
            if (productos.CategoriaID == 0)
            {
                ModelState.AddModelError("CategoriaID", "Debe seleccionar una categoría válida");
            }

            // ---> INICIO: VALIDACIÓN DE CÓDIGO DUPLICADO <---
            // Verificar si el CodigoProducto ya existe (ignorando mayúsculas/minúsculas)
            bool codigoExiste = await _context.Productos
                .AnyAsync(p => p.CodigoProducto.ToLower() == productos.CodigoProducto.ToLower());

            if (codigoExiste)
            {
                ModelState.AddModelError("CodigoProducto", "Este código de producto ya existe.");
            }
            // ---> FIN: VALIDACIÓN DE CÓDIGO DUPLICADO <---

            // Si ModelState no es válido, recolecto mensajes y regreso la vista
            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(kvp => kvp.Value.Errors != null && kvp.Value.Errors.Count > 0)
                    .Select(kvp => new {
                        Campo = kvp.Key,
                        Mensajes = kvp.Value.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? (e.Exception?.Message ?? "Error de binding") : e.ErrorMessage)
                    })
                    .ToList();

                var lista = errores.SelectMany(e => e.Mensajes.Select(m => $"{e.Campo}: {m}")).ToList();
                var texto = lista.Any() ? string.Join(" || ", lista) : "ModelState inválido (sin detalles).";
                TempData["ModelErrors"] = texto;
                _logger.LogWarning("ModelState inválido al crear producto: {0}", texto);

                CargarListasDesplegables();
                return View(productos);
            }

            try
            {
                _context.Add(productos);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto creado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError(dbEx, "Error al guardar producto en BD.");
                ModelState.AddModelError(string.Empty, "Error al guardar en la base de datos: " + inner);
                TempData["Exception"] = inner;

                CargarListasDesplegables();
                return View(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al guardar producto.");
                ModelState.AddModelError(string.Empty, "Error inesperado: " + ex.Message);
                TempData["Exception"] = ex.ToString();

                CargarListasDesplegables();
                return View(productos);
            }
        }

        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productos = await _context.Productos.FindAsync(id);
            if (productos == null)
            {
                return NotFound();
            }

            CargarListasDesplegables();
            return View(productos);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProductoID,CodigoProducto,NombreProducto,CategoriaID,ProveedorID,EstadoProducto,StockActual,StockMinimo,PrecioUnitario,ValorAdquisicion,FechaAdquisicion")] Productos productos)
        {
            if (id != productos.ProductoID)
            {
                return NotFound();
            }

            // Remover validaciones de propiedades de navegación que pueden causar problemas
            ModelState.Remove("Categoria");
            ModelState.Remove("Proveedor");

            // DEBUG: Verificar valores recibidos
            _logger.LogInformation($"CategoriaID recibido: {productos.CategoriaID}");
            _logger.LogInformation($"ProveedorID recibido: {productos.ProveedorID}");

            // Validación personalizada para CategoriaID
            if (productos.CategoriaID == 0)
            {
                ModelState.AddModelError("CategoriaID", "Debe seleccionar una categoría válida");
            }

            // ---> INICIO: VALIDACIÓN DE CÓDIGO DUPLICADO AL EDITAR <---
            // Verificar si el CodigoProducto ya existe en OTRO producto
            bool codigoExiste = await _context.Productos
                .AnyAsync(p => p.CodigoProducto.ToLower() == productos.CodigoProducto.ToLower() && p.ProductoID != id);

            if (codigoExiste)
            {
                ModelState.AddModelError("CodigoProducto", "Este código de producto ya está en uso por otro producto.");
            }
            // ---> FIN: VALIDACIÓN DE CÓDIGO DUPLICADO AL EDITAR <---

            // Si ModelState no es válido, recolecto mensajes y regreso la vista
            if (!ModelState.IsValid)
            {
                var errores = ModelState
                    .Where(kvp => kvp.Value.Errors != null && kvp.Value.Errors.Count > 0)
                    .Select(kvp => new {
                        Campo = kvp.Key,
                        Mensajes = kvp.Value.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? (e.Exception?.Message ?? "Error de binding") : e.ErrorMessage)
                    })
                    .ToList();

                var lista = errores.SelectMany(e => e.Mensajes.Select(m => $"{e.Campo}: {m}")).ToList();
                var texto = lista.Any() ? string.Join(" || ", lista) : "ModelState inválido (sin detalles).";
                TempData["ModelErrors"] = texto;
                _logger.LogWarning("ModelState inválido al editar producto: {0}", texto);

                CargarListasDesplegables();
                return View(productos);
            }

            try
            {
                _context.Update(productos);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Producto actualizado exitosamente";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductosExists(productos.ProductoID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError(dbEx, "Error al actualizar producto en BD.");
                ModelState.AddModelError(string.Empty, "Error al actualizar en la base de datos: " + inner);
                TempData["Exception"] = inner;

                CargarListasDesplegables();
                return View(productos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al actualizar producto.");
                ModelState.AddModelError(string.Empty, "Error inesperado: " + ex.Message);
                TempData["Exception"] = ex.ToString();

                CargarListasDesplegables();
                return View(productos);
            }
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(m => m.ProductoID == id);

            if (productos == null)
            {
                return NotFound();
            }

            return View(productos);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var productos = await _context.Productos.FindAsync(id);
                if (productos != null)
                {
                    _context.Productos.Remove(productos);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Producto eliminado exitosamente";
                }
                else
                {
                    TempData["Exception"] = "El producto no fue encontrado";
                }
            }
            catch (DbUpdateException dbEx)
            {
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError(dbEx, "Error al eliminar producto de BD.");
                TempData["Exception"] = "Error al eliminar de la base de datos: " + inner;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar producto.");
                TempData["Exception"] = "Error inesperado: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductosExists(int id)
        {
            return _context.Productos.Any(e => e.ProductoID == id);
        }

        // Cargar listas desplegables con ID y Nombre concatenados
        private void CargarListasDesplegables()
        {
            try
            {
                // Categorías - Crear lista con ID y Nombre concatenados
                var categorias = _context.Categoria
                    .OrderBy(c => c.CategoriaID)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoriaID.ToString(), // Valor que se guarda (solo ID)
                        Text = $"{c.CategoriaID} - {c.Nombre}" // Texto que se muestra (ID - Nombre)
                    })
                    .ToList();

                ViewData["CategoriaID"] = categorias;

                // Proveedores - Crear lista con ID y Nombre concatenados
                var proveedores = _context.Proveedores
                    .Where(p => p.Estado == "Activo") // Solo proveedores activos
                    .OrderBy(p => p.NombreProveedor)
                    .Select(p => new SelectListItem
                    {
                        Value = p.ProveedorID.ToString(), // Valor que se guarda (solo ID)
                        Text = $"{p.ProveedorID} - {p.NombreProveedor}" // Texto que se muestra (ID - Nombre)
                    })
                    .ToList();

                ViewData["ProveedorID"] = proveedores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar listas desplegables");
                ViewData["CategoriaID"] = new List<SelectListItem>();
                ViewData["ProveedorID"] = new List<SelectListItem>();
            }
        }
    }
}