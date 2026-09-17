using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Domain.Enums;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Templates.Commands.AddEdit
{
    public class AddEditTemplateCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; } = Guid.Empty;
        public string Key { get; set; } = default!;
        public string Name { get; set; } = default!;
        public DocumentType Type { get; set; }
        public string Version { get; set; } = "1.0";
        public string Content { get; set; } = default!;

        /// <summary>Mandatory change description when editing an existing template.</summary>
        public string? Remarks { get; set; }
    }

    public class AddEditTemplateValidator : IRequestValidator<AddEditTemplateCommand>
    {
        private static readonly Regex TemplateKeyPattern = new("^[A-Z0-9_]+$", RegexOptions.Compiled);

        public Task<IEnumerable<ValidationError>> ValidateAsync(AddEditTemplateCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(request.Key))
                errors.Add(new ValidationError { PropertyName = nameof(request.Key), ErrorMessage = "Template key is required." });
            else
            {
                if (request.Key.Length > 50)
                    errors.Add(new ValidationError { PropertyName = nameof(request.Key), ErrorMessage = "Template key cannot exceed 50 characters." });
                if (!TemplateKeyPattern.IsMatch(request.Key))
                    errors.Add(new ValidationError { PropertyName = nameof(request.Key), ErrorMessage = "Template key must contain only uppercase letters, numbers, and underscores." });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Template name is required." });
            else if (request.Name.Length > 100)
                errors.Add(new ValidationError { PropertyName = nameof(request.Name), ErrorMessage = "Template name cannot exceed 100 characters." });

            if (!Enum.IsDefined(typeof(DocumentType), request.Type))
                errors.Add(new ValidationError { PropertyName = nameof(request.Type), ErrorMessage = "A valid document type is required." });

            if (string.IsNullOrWhiteSpace(request.Version))
                errors.Add(new ValidationError { PropertyName = nameof(request.Version), ErrorMessage = "Template version is required." });
            else if (request.Version.Length > 10)
                errors.Add(new ValidationError { PropertyName = nameof(request.Version), ErrorMessage = "Template version cannot exceed 10 characters." });

            if (string.IsNullOrWhiteSpace(request.Content))
                errors.Add(new ValidationError { PropertyName = nameof(request.Content), ErrorMessage = "Template content (file path) is required." });

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

    internal class AddEditTemplateCommandHandler : IRequestHandler<AddEditTemplateCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<AddEditTemplateCommandHandler> _logger;

        public AddEditTemplateCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<AddEditTemplateCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(AddEditTemplateCommand command, CancellationToken ct)
        {
            try
            {
                if (command.Id != Guid.Empty)
                {
                    var template = await _unitOfWork.Repository<Domain.Entities.Template>().GetByIdAsync(command.Id).ConfigureAwait(false);
                    if (template == null)
                        return await Result<Guid>.FailAsync("Template not found.");

                    // Since properties are 'init', we have to create a new one if we want to "update" it if the repo supports it, 
                    // or change them to 'set'. I'll change them to 'set' in the entity for simplicity.
                    
                    // Actually, I'll just implement the logic assuming they are 'set' and then fix the entity.
                    template.Name = command.Name;
                    template.Key = command.Key;
                    template.Type = command.Type;
                    template.Version = command.Version;
                    template.Content = command.Content;

                    await _unitOfWork.Repository<Domain.Entities.Template>().UpdateAsync(template).ConfigureAwait(false);
                    await _unitOfWork.Commit(ct, remarks: command.Remarks).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(template.Id, "Template updated successfully.").ConfigureAwait(false);
                }
                else
                {
                    var newTemplate = new Domain.Entities.Template
                    {
                        Id = Guid.NewGuid(),
                        Key = command.Key,
                        Name = command.Name,
                        Type = command.Type,
                        Version = command.Version,
                        Content = command.Content
                    };

                    await _unitOfWork.Repository<Domain.Entities.Template>().AddAsync(newTemplate).ConfigureAwait(false);
                    await _unitOfWork.Commit(ct).ConfigureAwait(false);
                    return await Result<Guid>.SuccessAsync(newTemplate.Id, "Template created successfully.").ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding/editing template {Name}", command.Name);
                return await Result<Guid>.FailAsync("An error occurred while processing your request. Please try again later.").ConfigureAwait(false);
            }
        }
    }
}
