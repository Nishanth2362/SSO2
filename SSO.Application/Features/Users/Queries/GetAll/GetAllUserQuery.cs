using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Users.Queries.GetAll
{
    public class GetAllUserQuery : IRequest<Result<List<UserResponse>>>
    {
    }

    internal class GetAllUserQueryHandler : IRequestHandler<GetAllUserQuery, Result<List<UserResponse>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetAllUserQueryHandler> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public GetAllUserQueryHandler(IUnitOfWork<Guid> unitOfWork,UserManager<ApplicationUser> userManager, ILogger<GetAllUserQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<Result<List<UserResponse>>> Handle(GetAllUserQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var users =await _userManager.Users
                    .Select(x => new UserResponse
                    {
                        Id = x.Id,
                        UserName = x.UserName,
                        Email = x.Email,
                        FirstName = x.Name, // Assuming Name is FirstName or FullName
                        LastName = string.Empty, // ApplicationUser only has Name
                        TenantId = x.TenantId,
                        CreatedAt = x.CreatedOn ?? DateTime.MinValue,
                        UpdatedAt = x.LastModifiedOn ?? DateTime.MinValue,
                        IsActive = x.IsActive
                    }).ToListAsync(cancellationToken);

                return await Result<List<UserResponse>>.SuccessAsync(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all users.");
                return await Result<List<UserResponse>>.FailAsync("An error occurred while retrieving users.");
            }
        }
    }
}
