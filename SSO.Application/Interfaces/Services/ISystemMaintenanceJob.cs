using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    public interface ISystemMaintenanceJob
    {
        Task CleanTemporaryFilesAsync();
        Task PerformSystemBackupAsync();
    }
}
