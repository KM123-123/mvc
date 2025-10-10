using System;
using System.Collections.Generic;

namespace mvc.ViewModels
{
    // Clases internas para estructurar las listas
    public class VentaRecienteViewModel
    {
        public int VentaID { get; set; }
        public string ClienteNombre { get; set; }
        public string ProductoNombre { get; set; }
        public string VendedorNombre { get; set; }
        public decimal Total { get; set; }
        public DateTime FechaVenta { get; set; }
    }

    public class ProductoBajoStockViewModel
    {
        public string NombreProducto { get; set; }
        public int StockActual { get; set; }
        public int StockMinimo { get; set; }
    }

    public class RankingVendedorViewModel
    {
        public string NombreVendedor { get; set; }
        public decimal TotalVendido { get; set; }
        public int CantidadVentas { get; set; }
    }

    // ViewModel principal
    public class DashboardOperativoViewModel
    {
        // KPIs
        public decimal VentasHoy { get; set; }
        public int NuevosClientesHoy { get; set; }
        public int ProductosBajoStock { get; set; }

        // Listas y Rankings
        public List<VentaRecienteViewModel> UltimasVentas { get; set; }
        public List<ProductoBajoStockViewModel> ProductosParaReabastecer { get; set; }
        public List<RankingVendedorViewModel> RankingVendedoresMes { get; set; }
    }
}