// --- ASEGÚRATE DE TENER ESTOS USINGS AL PRINCIPIO ---
using mvc.Models;
using ClosedXML.Excel;
using System.IO;
// ---------------------------------------------------

namespace mvc.Documents
{
    public class VentaExcelDocument
    {
        private readonly Ventas _venta;

        public VentaExcelDocument(Ventas venta)
        {
            _venta = venta;
        }

        public byte[] GenerateExcel()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add($"Factura-{_venta.VentaID}");

                // --- 1. ENCABEZADO SUPERIOR (Información de la empresa y factura) ---
                // Datos de la empresa (izquierda)
                worksheet.Cell("A1").Value = "Universidad Mariano Galvez";
                worksheet.Cell("A1").Style.Font.Bold = true;
                worksheet.Cell("A2").Value = "Cuilapa Santa Rosa";
                worksheet.Cell("A3").Value = "NIT: 57463-5";

                // Datos de la factura (derecha)
                var facturaCell = worksheet.Cell("E1");
                facturaCell.Value = "FACTURA";
                facturaCell.Style.Font.Bold = true;
                facturaCell.Style.Font.FontSize = 20;
                facturaCell.Style.Font.FontColor = XLColor.FromHtml("#00529B"); // Color azul

                worksheet.Cell("D3").Value = "N° FACTURA";
                worksheet.Cell("E3").Value = _venta.VentaID;
                worksheet.Cell("D4").Value = "FECHA";
                worksheet.Cell("E4").Value = _venta.FechaVenta.ToString("dd/M/yyyy");
                worksheet.Cell("D5").Value = "VENCIMIENTO";
                worksheet.Cell("E5").Value = _venta.FechaVenta.AddDays(90).ToString("dd/M/yyyy");
                worksheet.Range("D3:D5").Style.Font.Bold = true;
                worksheet.Range("E3:E5").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;


                // --- 2. DATOS DEL CLIENTE ---
                var clientBox = worksheet.Range("A7:E10");
                clientBox.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                clientBox.Style.Border.OutsideBorderColor = XLColor.LightGray;

                worksheet.Cell("B8").Value = "CLIENTE";
                worksheet.Cell("B8").Style.Font.Bold = true;
                worksheet.Cell("B8").Style.Font.FontColor = XLColor.FromHtml("#00529B");

                worksheet.Cell("B9").Value = "NIT";
                worksheet.Cell("C9").Value = _venta.Cliente?.Nit ?? "N/A";
                worksheet.Cell("B10").Value = "NOMBRE";
                worksheet.Cell("C10").Value = _venta.Cliente?.NombreCliente ?? "N/A";
                worksheet.Cell("B11").Value = "DOMICILIO";
                worksheet.Cell("C11").Value = _venta.Cliente?.Direccion ?? "N/A";
                worksheet.Range("B9:B11").Style.Font.Bold = true;


                // --- 3. TABLA DE ARTÍCULOS ---
                var currentRow = 13;
                var headerTable = worksheet.Range($"A{currentRow}:E{currentRow}");
                headerTable.Style.Fill.BackgroundColor = XLColor.FromHtml("#E3F2FD"); // Azul claro
                headerTable.Style.Font.Bold = true;

                worksheet.Cell(currentRow, 1).Value = "CÓDIGO";
                worksheet.Cell(currentRow, 2).Value = "ARTÍCULO";
                worksheet.Cell(currentRow, 3).Value = "CANTIDAD";
                worksheet.Cell(currentRow, 4).Value = "PRECIO UNITARIO";
                worksheet.Cell(currentRow, 5).Value = "TOTAL";

                // Fila del producto vendido
                currentRow++;
                worksheet.Cell(currentRow, 1).Value = _venta.Producto?.CodigoProducto ?? "N/A";
                worksheet.Cell(currentRow, 2).Value = _venta.Producto?.NombreProducto ?? "N/A";
                worksheet.Cell(currentRow, 3).Value = _venta.Cantidad;
                worksheet.Cell(currentRow, 4).Value = _venta.Producto?.PrecioUnitario ?? 0;
                worksheet.Cell(currentRow, 5).Value = _venta.Total;
                worksheet.Range($"A{currentRow}:E{currentRow}").Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                worksheet.Range($"A{currentRow}:E{currentRow}").Style.Border.BottomBorderColor = XLColor.LightGray;

                // --- 4. CÁLCULO Y DESGLOSE DE TOTALES ---
                currentRow += 2; // Dejar un espacio
                var total = _venta.Total;
                var subtotal = total / 1.12m;
                var iva = total - subtotal;

                worksheet.Cell(currentRow, 4).Value = "SUBTOTAL";
                worksheet.Cell(currentRow, 5).Value = subtotal;
                currentRow++;
                worksheet.Cell(currentRow, 4).Value = "IVA (12%)";
                worksheet.Cell(currentRow, 5).Value = iva;

                worksheet.Range($"D{currentRow - 1}:D{currentRow}").Style.Font.Bold = true;

                // Bloque final de TOTAL FACTURA
                currentRow++;
                var totalBox = worksheet.Range($"D{currentRow}:E{currentRow}");
                totalBox.Style.Fill.BackgroundColor = XLColor.LightGray;
                totalBox.Style.Font.Bold = true;
                totalBox.Style.Font.FontSize = 12;

                worksheet.Cell(currentRow, 4).Value = "TOTAL FACTURA";
                worksheet.Cell(currentRow, 5).Value = total;


                // --- Formatos y Ajustes Finales ---
                worksheet.Column(2).Width = 30; // Ancho para el nombre del artículo
                worksheet.Column(4).Style.NumberFormat.Format = "\"Q\"#,##0.00";
                worksheet.Column(5).Style.NumberFormat.Format = "\"Q\"#,##0.00";
                worksheet.Columns("A,C,D,E").AdjustToContents(); // Ajustar el resto

                // --- Generar y Devolver ---
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}