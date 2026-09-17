using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Payments.Commands.AddEdit
{
    public class RecordPaymentCommand : IRequest<Result<Guid>>
    {
        public Guid InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; } = DateTime.Now;
        public string Currency { get; set; } = "INR";
    }

    public class RecordPaymentValidator : IRequestValidator<RecordPaymentCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(RecordPaymentCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (request.InvoiceId == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.InvoiceId), ErrorMessage = "A valid invoice identifier is required." });

            if (request.Amount <= 0)
                errors.Add(new ValidationError { PropertyName = nameof(request.Amount), ErrorMessage = "Payment amount must be greater than zero." });

            if (!Enum.IsDefined(typeof(PaymentMethod), request.Method))
                errors.Add(new ValidationError { PropertyName = nameof(request.Method), ErrorMessage = "A valid payment method is required." });

            if (request.PaymentDate == default)
                errors.Add(new ValidationError { PropertyName = nameof(request.PaymentDate), ErrorMessage = "Payment date is required." });
            else if (request.PaymentDate.Date > DateTime.UtcNow.Date.AddDays(1))
                errors.Add(new ValidationError { PropertyName = nameof(request.PaymentDate), ErrorMessage = "Payment date cannot be set in the far future." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }

    internal class RecordPaymentCommandHandler : IRequestHandler<RecordPaymentCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<RecordPaymentCommandHandler> _logger;

        public RecordPaymentCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<RecordPaymentCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(RecordPaymentCommand request, CancellationToken ct)
        {
            try
            {
                var invoice = await _unitOfWork.Repository<Invoice>().GetByIdAsync(request.InvoiceId);
                if (invoice == null) return await Result<Guid>.FailAsync("Invoice not found.");

                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = request.InvoiceId,
                    Amount = request.Amount,
                    Method = request.Method,
                    TransactionId = string.IsNullOrEmpty(request.TransactionId) ? $"OFF-{DateTime.Now.Ticks}" : request.TransactionId,
                    PaymentDate = request.PaymentDate.ToUniversalTime(),
                    Status = PaymentStatus.Completed
                };

                await _unitOfWork.Repository<Payment>().AddAsync(payment);

                // Simple check: update invoice status if fully paid
                if (request.Amount >= invoice.TotalAmount)
                {
                    invoice.Status = InvoiceStatus.Paid;
                    await _unitOfWork.Repository<Invoice>().UpdateAsync(invoice);
                }

                await _unitOfWork.Commit(ct).ConfigureAwait(false);
                return await Result<Guid>.SuccessAsync(payment.Id, "Payment recorded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording payment");
                return await Result<Guid>.FailAsync("Error occurred during payment recording.");
            }
        }
    }
}
