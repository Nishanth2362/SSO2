using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.Templates.Queries.GetPaged
{
    public class GetPagedTemplateQuery : DataTableRequest, IRequest<DataTableResponse<TemplateResponse>>
    {
        public GetPagedTemplateQuery(DataTableRequest request)
        {
            this.SearchValue = request.SearchValue;
            this.Start = request.Start;
            this.Length = request.Length;
            this.Draw = request.Draw;
            this.SortColumn = request.SortColumn;
            this.SortDirection = request.SortDirection;
        }
    }

    internal class GetPagedTemplateQueryHandler : IRequestHandler<GetPagedTemplateQuery, DataTableResponse<TemplateResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedTemplateQueryHandler> _logger;

        public GetPagedTemplateQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, ILogger<GetPagedTemplateQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DataTableResponse<TemplateResponse>> Handle(GetPagedTemplateQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<Domain.Entities.Template>().Entities.AsNoTracking();

                return await _dataTableService.BuildAsync(
                            query,
                            request,
                            e => new TemplateResponse
                            {
                                Id = e.Id,
                                Key = e.Key,
                                Name = e.Name,
                                Type = e.Type,
                                Version = e.Version,
                                Content = e.Content
                            },
                            e => true,
                            new List<string>
                            {
                                nameof(Domain.Entities.Template.Name),
                                nameof(Domain.Entities.Template.Key)
                            },
                            cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting paged templates.");
                return new DataTableResponse<TemplateResponse>
                {
                    Data = new List<TemplateResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
