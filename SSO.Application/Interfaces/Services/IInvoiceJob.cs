using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    public interface IInvoiceJob
    {
        Task MarkOverdueInvoicesAsync();
    }
}
