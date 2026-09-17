using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    public interface IJobProgressService
    {
        Task ReportProgressAsync(string jobId, int progress, string message);
    }
}
