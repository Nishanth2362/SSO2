using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Services.Features;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using System;
using System.Collections.Generic;
using System.Text;
using static SSO.Common.Constants.Permission.Permissions;

namespace SSO.Infrastructure.Services.Features
{
    public class RoleService : IRoleService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<RoleService> _logger;
        private readonly RoleManager<ApplicationRole> _roleManager;
        public RoleService(ApplicationDbContext dbContext, ILogger<RoleService> logger, RoleManager<ApplicationRole> roleManager)
        {
            _dbContext = dbContext;
            _logger = logger;
            _roleManager = roleManager;
        }

        public async Task AddUserRoles(ApplicationUser user, List<string> roles)
        {
            foreach (var roleName in roles)
            {
                if (!_roleManager.RoleExistsAsync(roleName).Result)
                {
                    _logger.LogWarning($"Role '{roleName}' does not exist.");
                    continue;
                }
                var role = await _roleManager.FindByNameAsync(roleName);
                if(role == null)
                {
                    _logger.LogWarning($"Role '{roleName}' does not exist.");
                    continue;
                }
                await _dbContext.UserRoles.AddAsync(new ApplicationUserRole()
                {
                    RoleId = role.Id,
                    UserId = user.Id,
                    TenantId = user.TenantId
                });
            }
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }

        public Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            throw new NotImplementedException();
        }

        public async Task RemoveAllUserRoles(ApplicationUser user)
        {
           var userRoles = await _dbContext.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync();
            foreach (var userRole in userRoles)
            {
                _dbContext.UserRoles.Remove(userRole);
            }
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }

        public async Task RemoveUserRoles(ApplicationUser user, List<string> roles)
        {
            foreach (var roleName in roles)
            {
               var role = await _roleManager.FindByNameAsync(roleName);
                if(role == null)
                {
                    _logger.LogWarning($"Role '{roleName}' does not exist.");
                    continue;
                }
                var userRole = _dbContext.UserRoles.FirstOrDefault(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
                if (userRole != null)
                {
                    _dbContext.UserRoles.Remove(userRole);
                }
            }
            await _dbContext.SaveChangesAsync(CancellationToken.None);
        }
    }
}
