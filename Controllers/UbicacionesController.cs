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
    public class UbicacionesController : Controller
    {
        private readonly ErpDbContext _context;

        public UbicacionesController(ErpDbContext context)
        {
            _context = context;
        }

        // GET: Ubicaciones
        public async Task<IActionResult> Index()
        {
            return View(await _context.Ubicaciones.ToListAsync());
        }

        // GET: Ubicaciones/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ubicaciones = await _context.Ubicaciones
                .FirstOrDefaultAsync(m => m.UbicacionID == id);
            if (ubicaciones == null)
            {
                return NotFound();
            }

            return View(ubicaciones);
        }

        // GET: Ubicaciones/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Ubicaciones/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UbicacionID,NombreUbicacion,Descripcion")] Ubicaciones ubicaciones)
        {
            if (ModelState.IsValid)
            {
                _context.Add(ubicaciones);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ubicaciones);
        }

        // GET: Ubicaciones/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ubicaciones = await _context.Ubicaciones.FindAsync(id);
            if (ubicaciones == null)
            {
                return NotFound();
            }
            return View(ubicaciones);
        }

        // POST: Ubicaciones/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UbicacionID,NombreUbicacion,Descripcion")] Ubicaciones ubicaciones)
        {
            if (id != ubicaciones.UbicacionID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(ubicaciones);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UbicacionesExists(ubicaciones.UbicacionID))
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
            return View(ubicaciones);
        }

        // GET: Ubicaciones/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ubicaciones = await _context.Ubicaciones
                .FirstOrDefaultAsync(m => m.UbicacionID == id);
            if (ubicaciones == null)
            {
                return NotFound();
            }

            return View(ubicaciones);
        }

        // POST: Ubicaciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ubicaciones = await _context.Ubicaciones.FindAsync(id);
            if (ubicaciones != null)
            {
                _context.Ubicaciones.Remove(ubicaciones);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UbicacionesExists(int id)
        {
            return _context.Ubicaciones.Any(e => e.UbicacionID == id);
        }
    }
}
