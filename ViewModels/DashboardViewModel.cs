using System;
using System.Collections.Generic;

namespace mvc.ViewModels
{
    // Clase simple para los datos de los gráficos
    public class ChartData
    {
        public string Dimension { get; set; }
        public decimal Total { get; set; }
    }

    public class DashboardViewModel
    {
        // KPIs
        public decimal IngresosTotales { get; set; }
        public decimal GananciaBruta { get; set; }
        public int NumeroDeVentas { get; set; }
        public decimal TicketPromedio { get; set; }

        // Datos para Gráficos
        public List<ChartData> VentasPorDia { get; set; }
        public List<ChartData> TopProductos { get; set; }
        public List<ChartData> VentasPorCategoria { get; set; }

        // Para el filtro de fecha
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }

        public DashboardViewModel()
        {
            VentasPorDia = new List<ChartData>();
            TopProductos = new List<ChartData>();
            VentasPorCategoria = new List<ChartData>();
        }
    }
}