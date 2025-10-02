using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mvc.Models
{
    public class Productos
    {
        [Key]
        public int ProductoID { get; set; }

        [Required(ErrorMessage = "El código del producto es requerido")]
        [StringLength(50)]
        [Display(Name = "Código del Producto")]
        public string CodigoProducto { get; set; }

        [Required(ErrorMessage = "El nombre del producto es requerido")]
        [StringLength(200)]
        [Display(Name = "Nombre del Producto")]
        public string NombreProducto { get; set; }

        // SIN [Required] - La validación se hace en el controller
        [ForeignKey("Categoria")]
        [Display(Name = "Categoría")]
        public int CategoriaID { get; set; }

        // SIN [Required] porque es nullable (opcional)
        [ForeignKey("Proveedor")]
        [Display(Name = "Proveedor")]
        public int? ProveedorID { get; set; }

        [Required]
        [Display(Name = "Estado del Producto")]
        public bool EstadoProducto { get; set; } = true;

        [Required(ErrorMessage = "El stock actual es requerido")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock actual debe ser mayor o igual a 0")]
        [Display(Name = "Stock Actual")]
        public int StockActual { get; set; }

        [Required(ErrorMessage = "El stock mínimo es requerido")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo debe ser mayor o igual a 0")]
        [Display(Name = "Stock Mínimo")]
        public int StockMinimo { get; set; }

        [Required(ErrorMessage = "El precio unitario es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio unitario debe ser mayor a 0")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Precio Unitario")]
        public decimal PrecioUnitario { get; set; }

        [Required(ErrorMessage = "El valor de adquisición es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El valor de adquisición debe ser mayor a 0")]
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Valor de Adquisición")]
        public decimal ValorAdquisicion { get; set; }

        [Required(ErrorMessage = "La fecha de adquisición es requerida")]
        [Display(Name = "Fecha de Adquisición")]
        public DateTime FechaAdquisicion { get; set; }

        // Navegación
        public virtual Categoria Categoria { get; set; }
        public virtual Proveedores Proveedor { get; set; }
    }
}