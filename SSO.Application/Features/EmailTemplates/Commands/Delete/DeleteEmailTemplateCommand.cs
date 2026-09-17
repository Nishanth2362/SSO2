using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;

namespace SSO.Application.Features.EmailTemplates.Commands.Delete
{
    public class DeleteEmailTemplateCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteEmailTemplateValidator : IRequestValidator<DeleteEmailTemplateCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(DeleteEmailTemplateCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();
            if (request.Id == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.Id), ErrorMessage = "A valid email template identifier is required." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }

    internal class DeleteEmailTemplateCommandHandler : IRequestHandler<DeleteEmailTemplateCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<DeleteEmailTemplateCommandHandler> _logger;

        public DeleteEmailTemplateCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<DeleteEmailTemplateCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(DeleteEmailTemplateCommand command, CancellationToken ct)
        {
            try
            {
                var template = await _unitOfWork.Repository<EmailTemplate>().GetByIdAsync(command.Id);
                if (template == null)
                    return await Result<Guid>.FailAsync("Email template not found.");

                await _unitOfWork.Repository<EmailTemplate>().DeleteAsync(template);
                await _unitOfWork.Commit(ct);
                return await Result<Guid>.SuccessAsync(command.Id, "Email template deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting email template with Id {Id}", command.Id);
                return await Result<Guid>.FailAsync("An error occurred while deleting the email template.");
            }
        }
    }
}
