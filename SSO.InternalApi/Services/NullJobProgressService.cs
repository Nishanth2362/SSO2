using SSO.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace SSO.InternalApi.Services;

public class NullJobProgressService : IJobProgressService
{
    public Task ReportProgressAsync(string jobId, int progress, string message)
    {
        return Task.CompletedTask;
    }
}
