using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Features.Users.Commands.Delete
{
    public class DeleteUserCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteUserValidator : IRequestValidator<DeleteUserCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();
            if (request.Id == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.Id), ErrorMessage = "A valid user identifier is required." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }
    internal class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result<Guid>>
    {
        private readonly ILogger<DeleteUserCommandHandler> _logger;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;
        public DeleteUserCommandHandler(ILogger<DeleteUserCommandHandler> logger, IUnitOfWork<Guid> unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<Result<Guid>> Handle(DeleteUserCommand request, CancellationToken ct)
        {
            try
            {
                var existingUser = await _userManager.FindByIdAsync(request.Id.ToString());
                if (existingUser == null)
                {
                    return await Result<Guid>.FailAsync("User not found.");
                }
                var roles = await _userManager.GetRolesAsync(existingUser);
                if (roles != null)
                {
                    await _userManager.RemoveFromRolesAsync(existingUser, roles);
                }
                var result = await _userManager.DeleteAsync(existingUser);
                if (result.Succeeded)
                {
                    return await Result<Guid>.SuccessAsync(request.Id, "User deleted successfully.");
                }
                else
                {
                    return await Result<Guid>.FailAsync(string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with Id {UserId}", request.Id);
                return await Result<Guid>.FailAsync("An error occurred while deleting the user.");
            }
        }
    }
}
