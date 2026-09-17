using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Templates.Commands.Delete
{
    public class DeleteTemplateCommand : IRequest<Result<Guid>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteTemplateValidator : IRequestValidator<DeleteTemplateCommand>
    {
        public Task<IEnumerable<ValidationError>> ValidateAsync(DeleteTemplateCommand request, CancellationToken cancellationToken)
        {
            var errors = new List<ValidationError>();
            if (request.Id == Guid.Empty)
                errors.Add(new ValidationError { PropertyName = nameof(request.Id), ErrorMessage = "A valid template identifier is required." });

            return Task.FromResult(errors.AsEnumerable());
        }
    }

    internal class DeleteTemplateCommandHandler : IRequestHandler<DeleteTemplateCommand, Result<Guid>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<DeleteTemplateCommandHandler> _logger;

        public DeleteTemplateCommandHandler(IUnitOfWork<Guid> unitOfWork, ILogger<DeleteTemplateCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Guid>> Handle(DeleteTemplateCommand command, CancellationToken ct)
        {
            try
            {
                var template = await _unitOfWork.Repository<Domain.Entities.Template>().GetByIdAsync(command.Id);
                if (template == null)
                    return await Result<Guid>.FailAsync("Template not found.");

                await _unitOfWork.Repository<Domain.Entities.Template>().DeleteAsync(template);
                await _unitOfWork.Commit(ct);
                return await Result<Guid>.SuccessAsync(command.Id, "Template deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting template with Id {Id}", command.Id);
                return await Result<Guid>.FailAsync("An error occurred while deleting the template.");
            }
        }
    }
}
