// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {

    // --- 🍔 LÓGICA PARA OCULTAR/MOSTRAR EL MENÚ LATERAL ---
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('hidden');
        });
    }

    // --- 🌙 LÓGICA PARA EL MODO OSCURO (CON MEMORIA) ---
    const themeSwitcher = document.getElementById('themeSwitcher');
    const body = document.body;

    // Función para aplicar el tema y guardar la preferencia
    const applyTheme = (theme) => {
        if (theme === 'dark') {
            body.classList.add('dark-mode');
            themeSwitcher.textContent = '☀️'; // Cambia el ícono a un sol
            localStorage.setItem('theme', 'dark'); // Guarda en memoria
        } else {
            body.classList.remove('dark-mode');
            themeSwitcher.textContent = '🌙'; // Cambia el ícono a una luna
            localStorage.setItem('theme', 'light'); // Guarda en memoria
        }
    };

    // Al cargar la página, revisa si hay un tema guardado
    const savedTheme = localStorage.getItem('theme') || 'light'; // Por defecto es claro
    applyTheme(savedTheme);

    // Evento de clic para el botón de tema
    if (themeSwitcher) {
        themeSwitcher.addEventListener('click', function () {
            // Revisa si el cuerpo YA tiene la clase dark-mode
            const isDarkMode = body.classList.contains('dark-mode');
            // Si es modo oscuro, cambia a claro. Si no, cambia a oscuro.
            applyTheme(isDarkMode ? 'light' : 'dark');
        });
    }

});