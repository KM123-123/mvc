using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using mvc.Models;
using Microsoft.AspNetCore.Authorization;

namespace mvc.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class UsuariosController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsuariosController(UserManager<Usuario> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index(string busqueda)
        {
            // Mantiene el valor del buscador en la vista.
            ViewData["BusquedaActual"] = busqueda;

            // 1. ESTA ES TU BASE: Se mantiene la carga de todos los usuarios.
            var usuarios = _userManager.Users.ToList();
            var usuariosConRoles = new List<dynamic>();
            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                usuariosConRoles.Add(new { Usuario = usuario, Roles = roles });
            }

            // 2. SE AÑADE EL FILTRO: Si el usuario escribió algo, filtramos la lista en memoria.
            if (!String.IsNullOrEmpty(busqueda))
            {
                // Se convierte la búsqueda a minúsculas para que no distinga mayúsculas/minúsculas.
                var busquedaLower = busqueda.ToLower();

                usuariosConRoles = usuariosConRoles.Where(item =>
                    // Busca en los campos del usuario
                    item.Usuario.UserName.ToLower().Contains(busquedaLower) ||
                    item.Usuario.FullName.ToLower().Contains(busquedaLower) ||
                    item.Usuario.Email.ToLower().Contains(busquedaLower) ||

                    // Busca en la lista de roles
                    ((IEnumerable<string>)item.Roles).Any(rol => rol.ToLower().Contains(busquedaLower)) ||

                    // Busca por estado "Sí" o "No"
                    (item.Usuario.IsActive ? "sí" : "no").Contains(busquedaLower)

                ).ToList();
            }

            // 3. SE ENVÍA LA LISTA FINAL: Se envía la lista completa o la filtrada a la vista.
            ViewBag.UsuariosConRoles = usuariosConRoles;
            return View(); // Ya no es necesario pasar 'usuarios' porque la vista usa el ViewBag
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(usuario);
            ViewBag.Roles = roles;

            return View(usuario);
        }

        // GET: Usuarios/Create
        public async Task<IActionResult> Create()
        {
            await CargarRolesEnViewBag();
            return View();
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Usuario model, string password, string rolSeleccionado)
        {
            if (ModelState.IsValid)
            {
                // Verificar que el rol seleccionado existe
                if (!string.IsNullOrEmpty(rolSeleccionado))
                {
                    var roleExists = await _roleManager.RoleExistsAsync(rolSeleccionado);
                    if (!roleExists)
                    {
                        ModelState.AddModelError("", "El rol seleccionado no es válido.");
                        await CargarRolesEnViewBag(rolSeleccionado);
                        return View(model);
                    }
                }

                var user = new Usuario
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = model.FullName,
                    Position = rolSeleccionado, // Guardamos el rol en Position para compatibilidad
                    IsActive = model.IsActive
                };

                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Asignar el rol al usuario si se seleccionó uno
                    if (!string.IsNullOrEmpty(rolSeleccionado))
                    {
                        await _userManager.AddToRoleAsync(user, rolSeleccionado);
                    }

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            await CargarRolesEnViewBag(rolSeleccionado);
            return View(model);
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            // Obtener el rol actual del usuario
            var rolesUsuario = await _userManager.GetRolesAsync(usuario);
            var rolActual = rolesUsuario.FirstOrDefault();

            await CargarRolesEnViewBag(rolActual);
            ViewBag.RolActual = rolActual;

            return View(usuario);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Usuario model, string rolSeleccionado)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var usuario = await _userManager.FindByIdAsync(id);
                if (usuario == null) return NotFound();

                // Verificar que el rol seleccionado existe
                if (!string.IsNullOrEmpty(rolSeleccionado))
                {
                    var roleExists = await _roleManager.RoleExistsAsync(rolSeleccionado);
                    if (!roleExists)
                    {
                        ModelState.AddModelError("", "El rol seleccionado no es válido.");
                        await CargarRolesEnViewBag(rolSeleccionado);
                        ViewBag.RolActual = rolSeleccionado;
                        return View(model);
                    }
                }

                // Actualizar propiedades del usuario
                usuario.FullName = model.FullName;
                usuario.Email = model.Email;
                usuario.Position = rolSeleccionado; // Mantener compatibilidad
                usuario.IsActive = model.IsActive;

                var result = await _userManager.UpdateAsync(usuario);

                if (result.Succeeded)
                {
                    // Actualizar roles del usuario
                    var rolesActuales = await _userManager.GetRolesAsync(usuario);

                    // Remover todos los roles actuales
                    if (rolesActuales.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(usuario, rolesActuales);
                    }

                    // Agregar el nuevo rol si se seleccionó uno
                    if (!string.IsNullOrEmpty(rolSeleccionado))
                    {
                        await _userManager.AddToRoleAsync(usuario, rolSeleccionado);
                    }

                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            await CargarRolesEnViewBag(rolSeleccionado);
            ViewBag.RolActual = rolSeleccionado;
            return View(model);
        }

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(usuario);
            ViewBag.Roles = roles;

            return View(usuario);
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario != null)
                await _userManager.DeleteAsync(usuario);

            return RedirectToAction(nameof(Index));
        }

        private async Task CargarRolesEnViewBag(string rolSeleccionado = null)
        {
            var roles = await _roleManager.Roles
                .Select(r => new { r.Name })
                .ToListAsync();

            ViewBag.Roles = new SelectList(roles, "Name", "Name", rolSeleccionado);
        }
    }
}