using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Templates.Queries.GetById
{
    public class GetByIDTemplateQuery : IRequest<Result<TemplateResponse>>
    {
        public Guid Id { get; set; }

        public GetByIDTemplateQuery(Guid id)
        {
            Id = id;
        }
    }

    internal class GetByIDTemplateQueryHandler : IRequestHandler<GetByIDTemplateQuery, Result<TemplateResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetByIDTemplateQueryHandler> _logger;

        public GetByIDTemplateQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetByIDTemplateQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<TemplateResponse>> Handle(GetByIDTemplateQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var template = await _unitOfWork.Repository<Domain.Entities.Template>().GetByIdAsync(request.Id);
                if (template == null)
                {
                    return await Result<TemplateResponse>.FailAsync("Template not found.");
                }

                var response = new TemplateResponse
                {
                    Id = template.Id,
                    Key = template.Key,
                    Name = template.Name,
                    Type = template.Type,
                    Version = template.Version,
                    Content = template.Content
                };

                return await Result<TemplateResponse>.SuccessAsync(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting template by id {Id}", request.Id);
                return await Result<TemplateResponse>.FailAsync("An error occurred while retrieving the template.");
            }
        }
    }
}
