using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests.DataTable;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Domain.Entities;
using SSO.Shared.Wrapper.Mediator;

namespace SSO.Application.Features.EmailTemplates.Queries.GetPaged
{
    public class GetPagedEmailTemplatesQuery : DataTableRequest, IRequest<DataTableResponse<EmailTemplateResponse>>
    {
        public GetPagedEmailTemplatesQuery(DataTableRequest request)
        {
            this.SearchValue = request.SearchValue;
            this.Start = request.Start;
            this.Length = request.Length;
            this.Draw = request.Draw;
            this.SortColumn = request.SortColumn;
            this.SortDirection = request.SortDirection;
        }
    }

    internal class GetPagedEmailTemplatesQueryHandler : IRequestHandler<GetPagedEmailTemplatesQuery, DataTableResponse<EmailTemplateResponse>>
    {
        private readonly IDataTableService _dataTableService;
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ILogger<GetPagedEmailTemplatesQueryHandler> _logger;

        public GetPagedEmailTemplatesQueryHandler(IDataTableService dataTableService, IUnitOfWork<Guid> unitOfWork, ILogger<GetPagedEmailTemplatesQueryHandler> logger)
        {
            _dataTableService = dataTableService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DataTableResponse<EmailTemplateResponse>> Handle(GetPagedEmailTemplatesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var query = _unitOfWork.Repository<EmailTemplate>().Entities.AsNoTracking();

                return await _dataTableService.BuildAsync(
                    query,
                    request,
                    e => new EmailTemplateResponse
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Subject = e.Subject,
                        TemplateType = e.TemplateType,
                        TriggerEvent = e.TriggerEvent,
                        Body = e.Body,
                        IsActive = e.IsActive,
                        CreatedOn = e.CreatedOn
                    },
                    e => true,
                    new List<string>
                    {
                        nameof(EmailTemplate.Name),
                        nameof(EmailTemplate.Subject)
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting paged email templates.");
                return new DataTableResponse<EmailTemplateResponse>
                {
                    Data = new List<EmailTemplateResponse>(),
                    RecordsFiltered = 0,
                    RecordsTotal = 0,
                    Draw = request.Draw
                };
            }
        }
    }
}
