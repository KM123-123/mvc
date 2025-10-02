// Ubicaciones.cs
using System.ComponentModel.DataAnnotations;

namespace mvc.Models
{
    public class Ubicaciones
    {
        [Key]
        public int UbicacionID { get; set; }

        [Required(ErrorMessage = "El nombre de la ubicación es requerido")]
        [StringLength(100)]
        [Display(Name = "Nombre de Ubicación")]
        public string NombreUbicacion { get; set; }

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }
    }
}
