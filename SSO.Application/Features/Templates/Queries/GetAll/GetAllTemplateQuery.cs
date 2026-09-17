using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Templates.Queries.GetAll
{
    public class GetAllTemplateQuery : IRequest<Result<List<TemplateResponse>>>
    {
    }

    internal class GetAllTemplateQueryHandler : IRequestHandler<GetAllTemplateQuery, Result<List<TemplateResponse>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetAllTemplateQueryHandler> _logger;

        public GetAllTemplateQueryHandler(IUnitOfWork<Guid> unitOfWork, ILogger<GetAllTemplateQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<List<TemplateResponse>>> Handle(GetAllTemplateQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var templates = await _unitOfWork.Repository<Domain.Entities.Template>().Entities
                    .Select(x => new TemplateResponse
                    {
                        Id = x.Id,
                        Key = x.Key,
                        Name = x.Name,
                        Type = x.Type,
                        Version = x.Version,
                        Content = x.Content
                    }).ToListAsync(cancellationToken);

                return await Result<List<TemplateResponse>>.SuccessAsync(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all templates.");
                return await Result<List<TemplateResponse>>.FailAsync("An error occurred while retrieving templates.");
            }
        }
    }
}
