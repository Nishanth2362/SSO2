using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.ManagementTransactions.Commands.RecordChanges
{
    public record RecordManagementTransactionChangesCommand : IRequest<Result<Guid>>
    {
        public Guid TransactionId { get; init; }
        public List<string> ChangedFields { get; init; } = new();
    }

    internal class RecordManagementTransactionChangesCommandHandler : IRequestHandler<RecordManagementTransactionChangesCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;

        public RecordManagementTransactionChangesCommandHandler(IUnitOfWork<Guid> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<Guid>> Handle(RecordManagementTransactionChangesCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.TransactionId == Guid.Empty || request.ChangedFields == null || request.ChangedFields.Count == 0)
                {
                    return await Result<Guid>.SuccessAsync(request.TransactionId);
                }

                var transaction = await _unitOfWork.Repository<ManagementTransaction>().Entities
                    .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

                if (transaction == null)
                {
                    return await Result<Guid>.FailAsync("Transaction not found.");
                }

                var existingChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(transaction.ChangedFieldsJson))
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<List<string>>(transaction.ChangedFieldsJson);
                        if (parsed != null)
                        {
                            foreach (var item in parsed) existingChanges.Add(item);
                        }
                    }
                    catch
                    {
                        // Ignore parse error
                    }
                }

                foreach (var field in request.ChangedFields)
                {
                    if (!string.IsNullOrWhiteSpace(field))
                    {
                        existingChanges.Add(field.Trim().ToLowerInvariant());
                    }
                }

                transaction.ChangedFieldsJson = JsonSerializer.Serialize(existingChanges.ToList());

                await _unitOfWork.Repository<ManagementTransaction>().UpdateAsync(transaction);
                await _unitOfWork.Commit(cancellationToken);

                return await Result<Guid>.SuccessAsync(transaction.Id);
            }
            catch (Exception ex)
            {
                return await Result<Guid>.FailAsync($"Error recording changes: {ex.Message}");
            }
        }
    }
}
