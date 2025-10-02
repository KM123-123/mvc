using System.ComponentModel.DataAnnotations;

namespace mvc.Models
{
    public class Proveedores
    {
        [Key]
        public int ProveedorID { get; set; }

        [Required(ErrorMessage = "El nombre del proveedor es requerido")]
        [StringLength(200)]
        [Display(Name = "Nombre del Proveedor")]
        public string NombreProveedor { get; set; }

        [StringLength(50)]
        [Display(Name = "Código Interno")]
        public string CodigoInterno { get; set; }

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [StringLength(300)]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [StringLength(20)]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Required]
        [Display(Name = "Estado")]
        public string Estado { get; set; } = "Activo";
    }
}