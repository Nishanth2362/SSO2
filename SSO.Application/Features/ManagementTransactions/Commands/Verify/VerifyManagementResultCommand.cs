using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
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

namespace SSO.Application.Features.ManagementTransactions.Commands.Verify
{
    public class VerifyManagementResultRequest
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class VerifyManagementResultResponse
    {
        public Guid TransactionId { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public string Scope { get; set; } = string.Empty;
        public ManagementTransactionStatus Status { get; set; }
        public string StatusName => Status.ToString();
        public DateTime? ConsumedOn { get; set; }
        public DateTime VerifiedAt { get; set; }
        public System.Collections.Generic.List<string> Changes { get; set; } = new();
        public bool HasChanges => Changes.Count > 0;
    }

    public record VerifyManagementResultCommand : IRequest<Result<VerifyManagementResultResponse>>
    {
        public string ClientId { get; init; } = string.Empty;
        public string ClientSecret { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
    }

    internal class VerifyManagementResultCommandHandler : IRequestHandler<VerifyManagementResultCommand, Result<VerifyManagementResultResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IDateTimeService _dateTimeService;

        public VerifyManagementResultCommandHandler(
            IUnitOfWork<Guid> unitOfWork,
            IOpenIddictApplicationManager applicationManager,
            IDateTimeService dateTimeService)
        {
            _unitOfWork = unitOfWork;
            _applicationManager = applicationManager;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<VerifyManagementResultResponse>> Handle(VerifyManagementResultCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
                {
                    return await Result<VerifyManagementResultResponse>.FailAsync("ClientId and ClientSecret are required.");
                }

                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return await Result<VerifyManagementResultResponse>.FailAsync("Result Code is required.");
                }

                // Authenticate client
                var app = await _applicationManager.FindByClientIdAsync(request.ClientId, cancellationToken);
                if (app == null || !await _applicationManager.ValidateClientSecretAsync(app, request.ClientSecret, cancellationToken))
                {
                    return await Result<VerifyManagementResultResponse>.FailAsync("Client authentication failed. Invalid credentials.");
                }

                var codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Code.Trim()))).ToLowerInvariant();

                var transaction = await _unitOfWork.Repository<ManagementTransaction>().Entities
                    .FirstOrDefaultAsync(t => t.ClientId == request.ClientId && t.ResultCodeHash == codeHash, cancellationToken);

                if (transaction == null)
                {
                    return await Result<VerifyManagementResultResponse>.FailAsync("Invalid result code.");
                }

                var now = _dateTimeService.NowUtc;

                if (transaction.IsResultCodeConsumed)
                {
                    return await Result<VerifyManagementResultResponse>.FailAsync("This result code has already been consumed.");
                }

                if (transaction.ResultCodeExpiresOn < now)
                {
                    return await Result<VerifyManagementResultResponse>.FailAsync("This result code has expired.");
                }

                transaction.IsResultCodeConsumed = true;
                transaction.ResultCodeConsumedOn = now;

                await _unitOfWork.Repository<ManagementTransaction>().UpdateAsync(transaction);
                await _unitOfWork.Commit(cancellationToken);

                var changesList = new System.Collections.Generic.List<string>();
                if (!string.IsNullOrWhiteSpace(transaction.ChangedFieldsJson))
                {
                    try
                    {
                        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(transaction.ChangedFieldsJson);
                        if (parsed != null) changesList = parsed;
                    }
                    catch
                    {
                        // Ignore deserialization error
                    }
                }

                return await Result<VerifyManagementResultResponse>.SuccessAsync(new VerifyManagementResultResponse
                {
                    TransactionId = transaction.Id,
                    ClientId = transaction.ClientId,
                    TenantId = transaction.TenantId,
                    UserId = transaction.UserId,
                    Scope = transaction.Scope,
                    Status = transaction.Status,
                    ConsumedOn = transaction.ConsumedOn,
                    VerifiedAt = now,
                    Changes = changesList
                }, "Restricted Page Access result verified successfully.");
            }
            catch (Exception ex)
            {
                return await Result<VerifyManagementResultResponse>.FailAsync($"Error verifying management result: {ex.Message}");
            }
        }
    }
}
