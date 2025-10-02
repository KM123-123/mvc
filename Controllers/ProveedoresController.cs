using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mvc.Data;
using mvc.Models;
using Microsoft.AspNetCore.Authorization;

namespace mvc.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class ProveedoresController : Controller
    {
        private readonly ErpDbContext _context;

        public ProveedoresController(ErpDbContext context)
        {
            _context = context;
        }

        // GET: Proveedores
        public async Task<IActionResult> Index(string busqueda)
        {
            // Mantiene el valor del buscador en la vista.
            ViewData["BusquedaActual"] = busqueda;

            // Crea la consulta base para los proveedores.
            var proveedores = from p in _context.Proveedores select p;

            // Si hay un término de búsqueda, aplica el filtro.
            if (!String.IsNullOrEmpty(busqueda))
            {
                // Intenta convertir la búsqueda a un número para el ID.
                int idBuscado = 0;
                bool esBusquedaDeId = int.TryParse(busqueda, out idBuscado);

                // Filtra por cualquier campo que contenga el texto de búsqueda.
                proveedores = proveedores.Where(p =>
                    // Busca por ID si el término es un número
                    (esBusquedaDeId && p.ProveedorID == idBuscado) ||

                    // Busca en los campos de tipo texto (string)
                    p.NombreProveedor.Contains(busqueda) ||
                    p.CodigoInterno.Contains(busqueda) ||
                    p.Descripcion.Contains(busqueda) ||
                    p.Direccion.Contains(busqueda) ||
                    p.Telefono.Contains(busqueda) ||

                    // Busca en el campo Estado (convirtiéndolo a texto para comparar)
                    p.Estado.ToString().Contains(busqueda)
                );
            }

            // Ejecuta la consulta y la pasa a la vista.
            return View(await proveedores.AsNoTracking().ToListAsync());
        }

        // GET: Proveedores/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedores = await _context.Proveedores
                .FirstOrDefaultAsync(m => m.ProveedorID == id);
            if (proveedores == null)
            {
                return NotFound();
            }

            return View(proveedores);
        }

        // GET: Proveedores/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Proveedores/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProveedorID,NombreProveedor,CodigoInterno,Descripcion,Direccion,Telefono,Estado")] Proveedores proveedores)
        {
            if (ModelState.IsValid)
            {
                _context.Add(proveedores);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(proveedores);
        }

        // GET: Proveedores/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedores = await _context.Proveedores.FindAsync(id);
            if (proveedores == null)
            {
                return NotFound();
            }
            return View(proveedores);
        }

        // POST: Proveedores/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProveedorID,NombreProveedor,CodigoInterno,Descripcion,Direccion,Telefono,Estado")] Proveedores proveedores)
        {
            if (id != proveedores.ProveedorID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proveedores);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProveedoresExists(proveedores.ProveedorID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(proveedores);
        }

        // GET: Proveedores/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proveedores = await _context.Proveedores
                .FirstOrDefaultAsync(m => m.ProveedorID == id);
            if (proveedores == null)
            {
                return NotFound();
            }

            return View(proveedores);
        }

        // POST: Proveedores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedores = await _context.Proveedores.FindAsync(id);
            if (proveedores != null)
            {
                _context.Proveedores.Remove(proveedores);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProveedoresExists(int id)
        {
            return _context.Proveedores.Any(e => e.ProveedorID == id);
        }
    }
}
