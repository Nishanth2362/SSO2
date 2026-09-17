using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Repos;
using SSO.Application.Interfaces.Services;
using SSO.Application.Responses.Features;
using SSO.Common.Constants.Application;
using SSO.Common.Wrapper;
using SSO.Domain.Entities;
using SSO.Domain.Enums;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.ManagementTransactions.Queries.GetSummary
{
    public record GetManagementSummaryQuery : IRequest<Result<ManagementSummaryResponse>>;

    internal class GetManagementSummaryQueryHandler : IRequestHandler<GetManagementSummaryQuery, Result<ManagementSummaryResponse>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTimeService _dateTimeService;

        public GetManagementSummaryQueryHandler(
            IUnitOfWork<Guid> unitOfWork,
            ICurrentUserService currentUserService,
            IDateTimeService dateTimeService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _dateTimeService = dateTimeService;
        }

        public async Task<Result<ManagementSummaryResponse>> Handle(GetManagementSummaryQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<ManagementTransaction>().Entities.AsNoTracking();

            if (!_currentUserService.IsMasterTenant)
            {
                query = query.Where(x => x.TenantId == _currentUserService.TenantId);
            }

            var now = _dateTimeService.NowUtc;

            var transactions = await query.ToListAsync(cancellationToken);

            var total = transactions.Count;
            var active = transactions.Count(t => t.Status == ManagementTransactionStatus.Active && t.ExpiresOn >= now);
            var completed = transactions.Count(t => t.Status == ManagementTransactionStatus.Completed);
            var revoked = transactions.Count(t => t.Status == ManagementTransactionStatus.Revoked);
            var expired = transactions.Count(t => t.Status == ManagementTransactionStatus.Expired || (t.Status != ManagementTransactionStatus.Completed && t.Status != ManagementTransactionStatus.Revoked && t.ExpiresOn < now));

            var profileCount = transactions.Count(t => t.Scope == ApplicationConstants.RpapConfig.Scopes.ProfileManage);
            var usersCount = transactions.Count(t => t.Scope == ApplicationConstants.RpapConfig.Scopes.UsersManage);
            var usersRolesCount = transactions.Count(t => t.Scope == ApplicationConstants.RpapConfig.Scopes.UsersRolesManage);

            return await Result<ManagementSummaryResponse>.SuccessAsync(new ManagementSummaryResponse
            {
                TotalTransactions = total,
                ActiveSessions = active,
                CompletedSessions = completed,
                ExpiredTransactions = expired,
                RevokedSessions = revoked,
                ProfileScopeCount = profileCount,
                UsersScopeCount = usersCount,
                UsersRolesScopeCount = usersRolesCount
            });
        }
    }
}
