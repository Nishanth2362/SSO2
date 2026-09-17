using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using SSO.Application.Configuaration;
using SSO.Application.Interfaces.Repos;
using Microsoft.Extensions.Configuration;
using SSO.Application.Responses.Features;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Dashboard.Queries
{
    public class GetDashboardQuery : IRequest<DashboardResponse>
    {
    }

    internal class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardResponse>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly AppConfiguration _appConfig;
        private readonly IConfiguration _configuration;

        public GetDashboardQueryHandler(
            IUnitOfWork<Guid> unitOfWork,
            UserManager<ApplicationUser> userManager,
            IOpenIddictApplicationManager applicationManager,
            IOptions<AppConfiguration> appConfig,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _applicationManager = applicationManager;
            _appConfig = appConfig.Value;
            _configuration = configuration;
        }

        public async Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
        {
            var response = new DashboardResponse
            {
                TotalUsers = await _userManager.Users.CountAsync(cancellationToken),
                TotalTenants = await _unitOfWork.Repository<Domain.Entities.Tenants>().Entities.CountAsync(cancellationToken),
                TotalClients = await _applicationManager.CountAsync(cancellationToken),
                ActiveSubscriptions = await _unitOfWork.Repository<TenantSubscription>().Entities.Where(x => x.IsActive).CountAsync(cancellationToken),
                IssuerUri = _appConfig.Issuer,
                CertificateType = _appConfig.CertificateType,
                EncryptionAlgorithm = _appConfig.EncryptionAlgorithm,
                DatabaseProvider = GetDbProviderName()
            };

            // Simplified RecentActivity based on Users for demonstrating "Dashbord"
            var recentUsers = await _userManager.Users
                .OrderByDescending(x => x.CreatedOn)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach(var user in recentUsers)
            {
               response.RecentActivities.Add(new RecentActivityResponse
               {
                   Timestamp = user.CreatedOn?.ToString("HH:mm:ss") ?? "N/A",
                   Email = user.Email,
                   ClientApp = "Internal Portal",
                   GrantType = "Registration",
                   Status = "Success"
               });
            }

            return response;
        }

        private string GetDbProviderName()
        {
            var conn = _configuration.GetConnectionString("DefaultConnection")?.ToLowerInvariant() ?? "";
            if (conn.Contains("service_name=") || conn.Contains("sid=") || conn.Contains(":1521/")) return "Oracle";
            if (conn.Contains("host=") && conn.Contains("port=") && conn.Contains("database=")) return "PostgreSQL";
            if (conn.Contains("port=") && conn.Contains("server=")) return "MySQL";
            if (conn.Contains("server=") || conn.Contains("data source=")) return "SQL Server";
            return "Unknown";
        }
    }
}
