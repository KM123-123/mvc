// --- ASEGÚRATE DE TENER TODOS ESTOS USINGS AL PRINCIPIO ---
using mvc.Models;
using QuestPDF.Fluent; // <-- ESTE ES EL USING MÁS IMPORTANTE PARA EL ERROR
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
// ---------------------------------------------------------

namespace mvc.Documents
{
    public class VentaDocument : IDocument
    {
        private readonly Ventas _venta;
        private readonly string _logoPath;

        public VentaDocument(Ventas venta, string logoPath)
        {
            _venta = venta;
            _logoPath = logoPath;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(50);

                // Sección de la Marca de Agua usando Image simple
                if (File.Exists(_logoPath))
                {
                    page.Background()
                        .AlignCenter()
                        .AlignMiddle()
                        .Width(500)
                        .Height(500)
                        .Image(_logoPath);
                }

                // El resto de la página
                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }

        void ComposeHeader(IContainer container)
        {
            var azulTitulos = "#00529B";
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Universidad Mariano Galvez").Bold().FontSize(14);
                    col.Item().Text("Cuilapa Santa Rosa");
                    col.Item().Text("NIT: 57463-5");
                });
                row.RelativeItem().Column(col =>
                {
                    col.Item().AlignRight().Text("FACTURA").Bold().FontSize(24).FontColor(azulTitulos);
                    col.Item().AlignRight().Grid(grid =>
                    {
                        grid.Columns(2);
                        grid.Item(1).Text("Nº FACTURA").SemiBold();
                        grid.Item(1).AlignRight().Text(_venta.VentaID.ToString());
                        grid.Item(1).Text("FECHA").SemiBold();
                        grid.Item(1).AlignRight().Text($"{_venta.FechaVenta:d/M/yyyy}");
                        grid.Item(1).Text("VENCIMIENTO").SemiBold();
                        grid.Item(1).AlignRight().Text($"{_venta.FechaVenta.AddDays(90):d/M/yyyy}");
                    });
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            var azulTitulos = "#00529B";
            container.PaddingVertical(30).Column(col =>
            {
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(clientCol =>
                {
                    clientCol.Item().Text("CLIENTE").Bold().FontColor(azulTitulos);
                    clientCol.Item().PaddingTop(5).Grid(grid =>
                    {
                        grid.Columns(12);
                        grid.Item(2).Text("NIT").SemiBold();
                        grid.Item(10).Text(_venta.Cliente?.Nit ?? "N/A");
                        grid.Item(2).Text("NOMBRE").SemiBold();
                        grid.Item(10).Text(_venta.Cliente?.NombreCliente ?? "N/A");
                        grid.Item(2).Text("DOMICILIO").SemiBold();
                        grid.Item(10).Text(_venta.Cliente?.Direccion ?? "N/A");
                    });
                });
                col.Item().PaddingTop(20).Element(ComposeTable);
                col.Item().AlignRight().PaddingTop(10).Element(ComposeTotals);
            });
        }

        void ComposeTable(IContainer container)
        {
            var azulHeader = "#E3F2FD";
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80);
                    columns.RelativeColumn(3);
                    columns.ConstantColumn(80);
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(100);
                });
                table.Header(header =>
                {
                    header.Cell().Background(azulHeader).Padding(5).Text("CÓDIGO").Bold();
                    header.Cell().Background(azulHeader).Padding(5).Text("ARTÍCULO").Bold();
                    header.Cell().Background(azulHeader).Padding(5).AlignCenter().Text("CANTIDAD").Bold();
                    header.Cell().Background(azulHeader).Padding(5).AlignRight().Text("PRECIO UNITARIO").Bold();
                    header.Cell().Background(azulHeader).Padding(5).AlignRight().Text("TOTAL").Bold();
                });
                var precioUnitario = _venta.Cantidad > 0 ? _venta.Total / _venta.Cantidad : 0;
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(_venta.Producto?.CodigoProducto ?? "N/A");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(_venta.Producto?.NombreProducto ?? "N/A");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(_venta.Cantidad.ToString());
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"Q{precioUnitario:N2}");
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"Q{_venta.Total:N2}");
            });
        }

        void ComposeTotals(IContainer container)
        {
            var total = _venta.Total;
            var subtotal = total / 1.12m;
            var iva = total - subtotal;
            container.Grid(grid =>
            {
                grid.Columns(2);
                grid.Item(1).Text("SUBTOTAL").Bold();
                grid.Item(1).AlignRight().Text($"Q{subtotal:N2}");
                grid.Item(1).Text("IVA (12%)").Bold();
                grid.Item(1).AlignRight().Text($"Q{iva:N2}");
                grid.Item(2).Background(Colors.Grey.Lighten3).Padding(5).Column(col =>
                {
                    col.Item().Text("TOTAL FACTURA").Bold().FontSize(14);
                    col.Item().AlignRight().Text($"Q{total:N2}").Bold().FontSize(14);
                });
            });
        }
    }
}