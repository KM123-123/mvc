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
    public class ClientesController : Controller
    {
        private readonly ErpDbContext _context;

        public ClientesController(ErpDbContext context)
        {
            _context = context;
        }

        // GET: Clientes
        public async Task<IActionResult> Index(string busqueda)
        {
            // Mantiene el valor del buscador en la vista.
            ViewData["BusquedaActual"] = busqueda;

            // Crea la consulta base para los clientes.
            var clientes = from c in _context.Clientes select c;

            // Si hay un término de búsqueda, aplica el filtro.
            if (!String.IsNullOrEmpty(busqueda))
            {
                // Intenta convertir la búsqueda a un número para el ID.
                int idBuscado = 0;
                bool esBusquedaDeId = int.TryParse(busqueda, out idBuscado);

                // Filtra por cualquier campo que contenga el texto de búsqueda.
                clientes = clientes.Where(c =>
                    // Busca por ID si el término es un número
                    (esBusquedaDeId && c.ClienteID == idBuscado) ||

                    // Busca en los campos de tipo texto (string)
                    c.Nit.Contains(busqueda) ||
                    c.NombreCliente.Contains(busqueda) ||
                    c.Direccion.Contains(busqueda) ||
                    c.Telefono.Contains(busqueda) ||
                    c.Correo.Contains(busqueda) ||

                    // Busca en el campo Estado
                    c.Estado.ToString().Contains(busqueda)
                );
            }

            // Ejecuta la consulta y la pasa a la vista.
            return View(await clientes.AsNoTracking().ToListAsync());
        }

        // GET: Clientes/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clientes = await _context.Clientes
                .FirstOrDefaultAsync(m => m.ClienteID == id);
            if (clientes == null)
            {
                return NotFound();
            }

            return View(clientes);
        }

        // GET: Clientes/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Clientes/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ClienteID,Nit,NombreCliente,Direccion,Telefono,Correo,Estado")] Clientes clientes)
        {
            if (ModelState.IsValid)
            {
                _context.Add(clientes);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(clientes);
        }

        // GET: Clientes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clientes = await _context.Clientes.FindAsync(id);
            if (clientes == null)
            {
                return NotFound();
            }
            return View(clientes);
        }

        // POST: Clientes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ClienteID,Nit,NombreCliente,Direccion,Telefono,Correo,Estado")] Clientes clientes)
        {
            if (id != clientes.ClienteID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(clientes);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClientesExists(clientes.ClienteID))
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
            return View(clientes);
        }

        // GET: Clientes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var clientes = await _context.Clientes
                .FirstOrDefaultAsync(m => m.ClienteID == id);
            if (clientes == null)
            {
                return NotFound();
            }

            return View(clientes);
        }

        // POST: Clientes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var clientes = await _context.Clientes.FindAsync(id);
            if (clientes != null)
            {
                _context.Clientes.Remove(clientes);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClientesExists(int id)
        {
            return _context.Clientes.Any(e => e.ClienteID == id);
        }
    }
}
