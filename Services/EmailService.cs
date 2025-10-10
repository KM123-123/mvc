using mvc.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;

namespace mvc.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarFacturaPorCorreoAsync(Ventas venta, byte[] pdfFactura)
        {
            try
            {
                var emailSettings = _config.GetSection("EmailSettings");
                var mensaje = new MimeMessage();
                mensaje.From.Add(new MailboxAddress(emailSettings["FromName"], emailSettings["FromAddress"]));
                mensaje.To.Add(new MailboxAddress(venta.Cliente.NombreCliente, venta.Cliente.Correo));
                mensaje.Subject = $"Factura de tu compra - Venta #{venta.VentaID}";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $@"
                    <h1>¡Hola, {venta.Cliente.NombreCliente}!</h1>
                    <p>Gracias por tu compra. Adjuntamos la factura de tu pedido #{venta.VentaID} en formato PDF.</p>
                    <p>¡Esperamos verte pronto!</p>
                    <p>Atentamente,<br>El equipo de {emailSettings["FromName"]}</p>";

                // Se agrega el PDF como un archivo adjunto.
                bodyBuilder.Attachments.Add($"Factura-{venta.VentaID}.pdf", pdfFactura, ContentType.Parse("application/pdf"));

                mensaje.Body = bodyBuilder.ToMessageBody();

                using (var clienteSmtp = new SmtpClient())
                {
                    await clienteSmtp.ConnectAsync(emailSettings["SmtpServer"], int.Parse(emailSettings["Port"]), SecureSocketOptions.StartTls);
                    await clienteSmtp.AuthenticateAsync(emailSettings["FromAddress"], emailSettings["SmtpPassword"]);
                    await clienteSmtp.SendAsync(mensaje);
                    await clienteSmtp.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                // En un proyecto real, aquí registrarías el error en un log.
                Console.WriteLine($"Error al enviar correo con factura: {ex.Message}");
            }
        }
    }
}