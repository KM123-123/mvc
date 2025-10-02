using System.ComponentModel.DataAnnotations;

namespace mvc.Models
{
    public class Clientes
    {
        [Key]
        public int ClienteID { get; set; }

        [Required(ErrorMessage = "El NIT es requerido")]
        [StringLength(20)]
        [Display(Name = "NIT")]
        public string Nit { get; set; }

        [Required(ErrorMessage = "El nombre del cliente es requerido")]
        [StringLength(200)]
        [Display(Name = "Nombre del Cliente")]
        public string NombreCliente { get; set; }

        [StringLength(300)]
        [Display(Name = "Dirección")]
        public string? Direccion { get; set; }

        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [Display(Name = "Correo Electrónico")]
        public string? Correo { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo"; // Por defecto "Activo"

        /*
        // Relación con ventas (un cliente puede tener varias ventas)
        public virtual ICollection<Venta>? Ventas { get; set; }*/
    }
}
