using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Domain.Enums;
using SSO.Shared.Wrapper.Mediator;

namespace SSO.Application.Features.EmailTemplates.Commands.AddEdit
{
    public class AddEditEmailTemplateCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Name { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public EmailTemplateType TemplateType { get; set; } = EmailTemplateType.HTML;
        public EmailTriggerEvent TriggerEvent { get; set; } = EmailTriggerEvent.Registration;
        public string Body { get; set; } = default!;
        public bool IsActive { get; set; } = true;

        /// <summary>Mandatory change description when editing an existing email template.</summary>
        public string? Remarks { get; set; }
    }

    public class AddEditEmailTemplateValidator : IRequestValidator<AddEditEmailTemplateCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(AddEditEmailTemplateCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Template name is required." });
            else if (request.Name.Length > 150)
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Template name cannot exceed 150 characters." });

            if (string.IsNullOrWhiteSpace(request.Subject))
                errors.Add(new ValidationError { PropertyName = nameof(request.Subject), ErrorMessage = "Subject line is required." });
            else if (request.Subject.Length > 500)
                errors.Add(new ValidationError { PropertyName = nameof(request.Subject), ErrorMessage = "Subject cannot exceed 500 characters." });

            if (string.IsNullOrWhiteSpace(request.Body))
                errors.Add(new ValidationError { PropertyName = nameof(request.Body), ErrorMessage = "Email body is required." });

            if (!Enum.IsDefined(typeof(EmailTemplateType), request.TemplateType))
                errors.Add(new ValidationError { PropertyName = nameof(request.TemplateType), ErrorMessage = "A valid template type is required." });

            if (!Enum.IsDefined(typeof(EmailTriggerEvent), request.TriggerEvent))
                errors.Add(new ValidationError { PropertyName = nameof(request.TriggerEvent), ErrorMessage = "A valid trigger event is required." });

            if (request.Id != Guid.Empty)
            {
                if (string.IsNullOrWhiteSpace(request.Remarks))
                    errors.Add(new ValidationError { PropertyName = nameof(request.Remarks), ErrorMessage = "A change description (remarks) is required when editing." });
                else if (request.Remarks.Trim().Length < 5)
                    errors.Add(new ValidationError { PropertyName = nameof(request.Remarks), ErrorMessage = "Remarks must be at least 5 characters." });
                else if (request.Remarks.Length > 500)
                    errors.Add(new ValidationError { PropertyName = nameof(request.Remarks), ErrorMessage = "Remarks cannot exceed 500 characters." });
            }

            return Task.FromResult(errors.AsEnumerable());
        }
    }

    internal class AddEditEmailTemplateCommandHandler : IRequestHandler<AddEditEmailTemplateCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<AddEditEmailTemplateCommandHandler> _logger;

        public AddEditEmailTemplateCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<AddEditEmailTemplateCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(AddEditEmailTemplateCommand command, CancellationToken ct)
        {
            try
            {
                if (command.Id != Guid.Empty)
                {
                    var template = await _unitOfWork.Repository<EmailTemplate>().GetByIdAsync(command.Id).ConfigureAwait(false);
                    if (template == null)
                        return await Result<Guid>.FailAsync("Email template not found.");

                    template.Name = command.Name;
                    template.Subject = command.Subject;
                    template.TemplateType = command.TemplateType;
                    template.TriggerEvent = command.TriggerEvent;
                    template.Body = command.Body;
                    template.IsActive = command.IsActive;

                    await _unitOfWork.Repository<EmailTemplate>().UpdateAsync(template).ConfigureAwait(false);
                    await _unitOfWork.Commit(ct, remarks: command.Remarks).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(template.Id, "Email template updated successfully.").ConfigureAwait(false);
                }
                else
                {
                    var newTemplate = new EmailTemplate
                    {
                        Id = Guid.NewGuid(),
                        Name = command.Name,
                        Subject = command.Subject,
                        TemplateType = command.TemplateType,
                        TriggerEvent = command.TriggerEvent,
                        Body = command.Body,
                        IsActive = command.IsActive
                    };

                    await _unitOfWork.Repository<EmailTemplate>().AddAsync(newTemplate).ConfigureAwait(false);
                    await _unitOfWork.Commit(ct).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(newTemplate.Id, "Email template created successfully.").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving email template {Name}", command.Name);
                return await Result<Guid>.FailAsync("An error occurred while saving the email template. Please try again.").ConfigureAwait(false);
            }
        }
    }
}
