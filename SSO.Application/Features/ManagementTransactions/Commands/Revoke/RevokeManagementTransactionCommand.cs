using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Domain.Enums;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.ManagementTransactions.Commands.Revoke
{
    public record RevokeManagementTransactionCommand : IRequest<Result<Guid>>
    {
        public Guid TransactionId { get; init; }
        public string? Reason { get; init; }
    }

    internal class RevokeManagementTransactionCommandHandler : IRequestHandler<RevokeManagementTransactionCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeService _dateTimeService;

        public RevokeManagementTransactionCommandHandler(
            IUnitOfWork<Guid> unitOfWork,
            ICurrentUserService currentUserService,
            IDateTimeService dateTimeService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<Guid>> Handle(RevokeManagementTransactionCommand request, CancellationToken cancellationToken)
        {
            var transaction = await _unitOfWork.Repository<ManagementTransaction>().Entities
                .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

            if (transaction == null)
            {
                return await Result<Guid>.FailAsync("Transaction not found.");
            }

            // Tenant boundary check for non-master admins
            if (!_currentUserService.IsMasterTenant && transaction.TenantId != _currentUserService.TenantId)
            {
                return await Result<Guid>.FailAsync("Access Denied. You cannot revoke sessions from another tenant.");
            }

            var now = _dateTimeService.NowUtc;
            transaction.Status = ManagementTransactionStatus.Revoked;
            transaction.RevokedReason = string.IsNullOrWhiteSpace(request.Reason) ? "Revoked by Administrator" : request.Reason.Trim();
            transaction.RevokedOn = now;
            transaction.RevokedBy = _currentUserService.UserName ?? "Admin";

            await _unitOfWork.Repository<ManagementTransaction>().UpdateAsync(transaction);
            await _unitOfWork.Commit(cancellationToken);

            return await Result<Guid>.SuccessAsync(transaction.Id, "Restricted Page Access session revoked successfully.");
        }
    }
}
