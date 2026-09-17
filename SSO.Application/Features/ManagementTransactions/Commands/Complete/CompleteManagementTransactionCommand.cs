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

namespace SSO.Application.Features.ManagementTransactions.Commands.Complete
{
    public class CompleteManagementTransactionResult
    {
        public string CallbackUrl { get; set; } = string.Empty;
        public string ResultCode { get; set; } = string.Empty;
        public string? State { get; set; }
        public string RedirectUriWithCode { get; set; } = string.Empty;
        public ManagementTransactionStatus Status { get; set; }
    }

    public record CompleteManagementTransactionCommand : IRequest<Result<CompleteManagementTransactionResult>>
    {
        public Guid TransactionId { get; init; }
        public string? ActionSummary { get; init; }
    }

    internal class CompleteManagementTransactionCommandHandler : IRequestHandler<CompleteManagementTransactionCommand, Result<CompleteManagementTransactionResult>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly IDateTimeService _dateTimeService;

        public CompleteManagementTransactionCommandHandler(
            IUnitOfWork<Guid> unitOfWork,
            IDateTimeService dateTimeService)
        {
            _unitOfWork = unitOfWork;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<CompleteManagementTransactionResult>> Handle(CompleteManagementTransactionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var transaction = await _unitOfWork.Repository<ManagementTransaction>().Entities
                    .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

                if (transaction == null)
                {
                    return await Result<CompleteManagementTransactionResult>.FailAsync("Transaction not found.");
                }

                var now = _dateTimeService.NowUtc;

                // Generate one-time Result Code (valid for 2 minutes)
                var rawCodeBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(rawCodeBytes);
                }
                var resultCode = Convert.ToHexString(rawCodeBytes).ToLowerInvariant();
                var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(resultCode))).ToLowerInvariant();

                transaction.Status = ManagementTransactionStatus.Completed;
                transaction.ResultCodeHash = codeHash;
                transaction.ResultCodeExpiresOn = now.AddMinutes(2);
                transaction.IsResultCodeConsumed = false;

                await _unitOfWork.Repository<ManagementTransaction>().UpdateAsync(transaction);
                await _unitOfWork.Commit(cancellationToken);

                // Construct client return URL
                var separator = transaction.CallbackUrl.Contains('?') ? "&" : "?";
                var redirectUrl = $"{transaction.CallbackUrl}{separator}code={Uri.EscapeDataString(resultCode)}&status=completed";
                if (!string.IsNullOrEmpty(transaction.State))
                {
                    redirectUrl += $"&state={Uri.EscapeDataString(transaction.State)}";
                }

                return await Result<CompleteManagementTransactionResult>.SuccessAsync(new CompleteManagementTransactionResult
                {
                    CallbackUrl = transaction.CallbackUrl,
                    ResultCode = resultCode,
                    State = transaction.State,
                    RedirectUriWithCode = redirectUrl,
                    Status = transaction.Status
                }, "Restricted Page Access transaction completed.");
            }
            catch (Exception ex)
            {
                return await Result<CompleteManagementTransactionResult>.FailAsync($"Error completing management transaction: {ex.Message}");
            }
        }
    }
}
