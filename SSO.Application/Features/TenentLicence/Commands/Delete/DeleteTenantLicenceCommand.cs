using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.TenentLicence.Commands.Delete
{
    public class DeleteTenantLicenceCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
    }

    internal class DeleteTenantLicenceCommandHandler : IRequestHandler<DeleteTenantLicenceCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<DeleteTenantLicenceCommandHandler> _logger;

        public DeleteTenantLicenceCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<DeleteTenantLicenceCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(DeleteTenantLicenceCommand command, CancellationToken ct)
        {
            try
            {
                var license = await _unitOfWork.Repository<Domain.Entities.TenantLicense>().GetByIdAsync(command.Id);
                if (license == null)
                    return await Result<Guid>.FailAsync("Tenant license not found.");

                await _unitOfWork.Repository<Domain.Entities.TenantLicense>().DeleteAsync(license);
                await _unitOfWork.Commit(ct);
                return await Result<Guid>.SuccessAsync(command.Id, "Tenant license deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting tenant license with Id {Id}", command.Id);
                return await Result<Guid>.FailAsync("An error occurred while deleting the tenant license.");
            }
        }
    }
}
