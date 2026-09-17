using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;

namespace SSO.Application.Features.EmailTemplates.Queries.GetById
{
    public class GetByIdEmailTemplateQuery : IRequest<Result<EmailTemplateResponse>>
    {
        public Guid Id { get; set; }

        public GetByIdEmailTemplateQuery(Guid id)
        {
            Id = id;
        }
    }

    internal class GetByIdEmailTemplateQueryHandler : IRequestHandler<GetByIdEmailTemplateQuery, Result<EmailTemplateResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetByIdEmailTemplateQueryHandler> _logger;

        public GetByIdEmailTemplateQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetByIdEmailTemplateQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<EmailTemplateResponse>> Handle(GetByIdEmailTemplateQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var template = await _unitOfWork.Repository<EmailTemplate>().GetByIdAsync(request.Id);
                if (template == null)
                    return await Result<EmailTemplateResponse>.FailAsync("Email template not found.");

                return await Result<EmailTemplateResponse>.SuccessAsync(new EmailTemplateResponse
                {
                    Id = template.Id,
                    Name = template.Name,
                    Subject = template.Subject,
                    TemplateType = template.TemplateType,
                    TriggerEvent = template.TriggerEvent,
                    Body = template.Body,
                    IsActive = template.IsActive,
                    CreatedOn = template.CreatedOn
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting email template by Id {Id}", request.Id);
                return await Result<EmailTemplateResponse>.FailAsync("An error occurred while retrieving the email template.");
            }
        }
    }
}
