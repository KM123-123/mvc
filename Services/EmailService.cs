using mvc.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using SendGrid; // <-- NUEVO USING
using SendGrid.Helpers.Mail; // <-- NUEVO USING

namespace mvc.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ISendGridClient _sendGridClient; // <-- NUEVO CLIENTE

        public EmailService(IConfiguration config)
        {
            _config = config;

            // Lee la API Key que pondremos en el comando de Docker
            var apiKey = _config["SendGrid_ApiKey"];
            _sendGridClient = new SendGridClient(apiKey);
        }

        public async Task EnviarFacturaPorCorreoAsync(Ventas venta, byte[] pdfFactura)
        {
            try
            {
                var emailSettings = _config.GetSection("EmailSettings");

                // 1. Crear remitente y destinatario
                var from = new EmailAddress(emailSettings["FromAddress"], emailSettings["FromName"]);
                var to = new EmailAddress(venta.Cliente.Correo, venta.Cliente.NombreCliente);

                // 2. Crear contenido del correo
                var subject = $"Factura de tu compra - Venta #{venta.VentaID}";
                var htmlContent = $@"
                    <h1>¡Hola, {venta.Cliente.NombreCliente}!</h1>
                    <p>Gracias por tu compra. Adjuntamos la factura de tu pedido #{venta.VentaID} en formato PDF.</p>
                    <p>¡Esperamos verte pronto!</p>
                    <p>Atentamente,<br>El equipo de {emailSettings["FromName"]}</p>";
                // Fallback por si el cliente no puede ver HTML
                var plainTextContent = "Gracias por tu compra. Adjuntamos tu factura.";

                // 3. Crear el mensaje de SendGrid
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                // 4. Adjuntar el PDF
                // SendGrid necesita el archivo en Base64
                var pdfBase64 = Convert.ToBase64String(pdfFactura);
                msg.AddAttachment($"Factura-{venta.VentaID}.pdf", pdfBase64, "application/pdf");

                // 5. Enviar el correo usando la API de SendGrid
                var response = await _sendGridClient.SendEmailAsync(msg);

                // (Opcional) Registrar si SendGrid dio un error
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error al enviar con SendGrid: {response.StatusCode}");
                    string responseBody = await response.Body.ReadAsStringAsync();
                    Console.WriteLine($"Cuerpo del error de SendGrid: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo con factura (SendGrid): {ex.Message}");
            }
        }
    }
}