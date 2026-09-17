using Microsoft.AspNetCore.SignalR;
using SSO.Application.Interfaces.Services;
using SSO.WebApplication.Hubs;
using System.Threading.Tasks;

namespace SSO.WebApplication.Services
{
    public class JobProgressService : IJobProgressService
    {
        private readonly IHubContext<SignalRHub> _hubContext;

        public JobProgressService(IHubContext<SignalRHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task ReportProgressAsync(string jobId, int progress, string message)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveJobProgress", jobId, progress, message);
        }
    }
}
