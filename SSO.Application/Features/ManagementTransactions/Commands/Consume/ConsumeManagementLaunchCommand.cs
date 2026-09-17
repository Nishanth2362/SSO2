using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Domain.Enums;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.ManagementTransactions.Commands.Consume
{
    public class ConsumeManagementLaunchResult
    {
        public Guid TransactionId { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string Scope { get; set; } = string.Empty;
        public string TargetUrl { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string? State { get; set; }
    }

    public record ConsumeManagementLaunchCommand : IRequest<Result<ConsumeManagementLaunchResult>>
    {
        public string LaunchToken { get; init; } = string.Empty;
        public string? IpAddress { get; init; }
        public string? UserAgent { get; init; }
    }

    internal class ConsumeManagementLaunchCommandHandler : IRequestHandler<ConsumeManagementLaunchCommand, Result<ConsumeManagementLaunchResult>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly IDateTimeService _dateTimeService;

        public ConsumeManagementLaunchCommandHandler(
            IUnitOfWork<Guid> unitOfWork,
            IDateTimeService dateTimeService)
        {
            _unitOfWork = unitOfWork;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<ConsumeManagementLaunchResult>> Handle(ConsumeManagementLaunchCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.LaunchToken))
                {
                    return await Result<ConsumeManagementLaunchResult>.FailAsync("Launch token is required.");
                }

                var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.LaunchToken.Trim()))).ToLowerInvariant();

                var transaction = await _unitOfWork.Repository<ManagementTransaction>().Entities
                    .FirstOrDefaultAsync(t => t.LaunchTokenHash == tokenHash, cancellationToken);

                if (transaction == null)
                {
                    return await Result<ConsumeManagementLaunchResult>.FailAsync("Invalid launch token. Session not found.");
                }

                var now = _dateTimeService.NowUtc;

                if (transaction.IsConsumed || transaction.Status == ManagementTransactionStatus.Active)
                {
                    // If it's already active and within expiry, we can allow re-entry into the same session
                    if (transaction.ExpiresOn < now)
                    {
                        transaction.Status = ManagementTransactionStatus.Expired;
                        await _unitOfWork.Repository<ManagementTransaction>().UpdateAsync(transaction);
                        await _unitOfWork.Commit(cancellationToken);
                        return await Result<ConsumeManagementLaunchResult>.FailAsync("This Restricted Page Access session has expired.");
                    }

                    if (transaction.Status == ManagementTransactionStatus.Revoked)
                    {
                        return await Result<ConsumeManagementLaunchResult>.FailAsync("This Restricted Page Access session has been revoked by an administrator.");
                    }

                    return await Result<ConsumeManagementLaunchResult>.SuccessAsync(new ConsumeManagementLaunchResult
                    {
                        TransactionId = transaction.Id,
                        ClientId = transaction.ClientId,
                        TenantId = transaction.TenantId,
                        UserId = transaction.UserId,
                        Scope = transaction.Scope,
                        TargetUrl = transaction.TargetUrl,
                        CallbackUrl = transaction.CallbackUrl,
                        State = transaction.State
                    });
                }

                if (transaction.Status == ManagementTransactionStatus.Revoked)
                {
                    return await Result<ConsumeManagementLaunchResult>.FailAsync("This Restricted Page Access session was revoked.");
                }

                if (transaction.ExpiresOn < now)
                {
                    transaction.Status = ManagementTransactionStatus.Expired;
                    await _unitOfWork.Repository<ManagementTransaction>().UpdateAsync(transaction);
                    await _unitOfWork.Commit(cancellationToken);
                    return await Result<ConsumeManagementLaunchResult>.FailAsync("This launch token has expired. Please initiate a new session.");
                }

                // Atomically mark as consumed & active
                transaction.IsConsumed = true;
                transaction.ConsumedOn = now;
                transaction.ConsumedIpAddress = request.IpAddress;
                transaction.ConsumedUserAgent = request.UserAgent;
                transaction.Status = ManagementTransactionStatus.Active;

                await _unitOfWork.Repository<ManagementTransaction>().UpdateAsync(transaction);
                await _unitOfWork.Commit(cancellationToken);

                return await Result<ConsumeManagementLaunchResult>.SuccessAsync(new ConsumeManagementLaunchResult
                {
                    TransactionId = transaction.Id,
                    ClientId = transaction.ClientId,
                    TenantId = transaction.TenantId,
                    UserId = transaction.UserId,
                    Scope = transaction.Scope,
                    TargetUrl = transaction.TargetUrl,
                    CallbackUrl = transaction.CallbackUrl,
                    State = transaction.State
                }, "Restricted Page Access launch token consumed successfully.");
            }
            catch (Exception ex)
            {
                return await Result<ConsumeManagementLaunchResult>.FailAsync($"Error consuming launch token: {ex.Message}");
            }
        }
    }
}
