using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSO.Application.Interfaces.Services
{
    public interface ICurrentUserService
    {
        Guid UserId { get; }
        string UserName { get; }
        string IpAddress { get; }
        bool IsAdmin { get; }
        Guid TenantId { get; }
        bool IsMasterTenant { get; }
    }
}
