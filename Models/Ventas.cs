using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvc.Models
{
    public class Ventas
    {
        [Key]
        public int VentaID { get; set; }

        [Required(ErrorMessage = "El cliente es requerido")]
        [ForeignKey("Cliente")]
        [Display(Name = "Cliente")]
        public int ClienteID { get; set; }
        public virtual Clientes Cliente { get; set; }

        [Required(ErrorMessage = "El producto es requerido")]
        [ForeignKey("Producto")]
        [Display(Name = "Producto")]
        public int ProductoID { get; set; }
        public virtual Productos Producto { get; set; }

        [Required]
        [Display(Name = "Usuario (Vendedor)")]
        public string UsuarioID { get; set; }
        public virtual Usuario Usuario { get; set; }

        [Required(ErrorMessage = "La cantidad es requerida")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Total")]
        public decimal Total { get; set; }

        [Required]
        [Display(Name = "Fecha de la Venta")]
        public DateTime FechaVenta { get; set; } = DateTime.Now;
    }
}