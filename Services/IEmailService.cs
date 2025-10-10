using mvc.Models;
using System.Threading.Tasks;

namespace mvc.Services
{
    public interface IEmailService
    {
        Task EnviarFacturaPorCorreoAsync(Ventas venta, byte[] pdfFactura);
    }
}