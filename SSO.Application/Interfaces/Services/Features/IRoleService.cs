using SSO.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Interfaces.Services.Features
{
    public interface IRoleService
    {
        public Task AddUserRoles(ApplicationUser user,List<string> roles);

        public Task<List<string>>  GetUserRoles(ApplicationUser user);

        public Task RemoveUserRoles(ApplicationUser user, List<string> roles);

        public Task RemoveAllUserRoles(ApplicationUser user);
    }
}
