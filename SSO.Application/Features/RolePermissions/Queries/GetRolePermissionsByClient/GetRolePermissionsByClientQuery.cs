using Microsoft.EntityFrameworkCore;
using SSO.Application.Interfaces.Repos;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SSO.Application.Features.RolePermissions.Queries.GetRolePermissionsByClient
{
    public class GetRolePermissionsByClientQuery : IRequest<Result<List<Guid>>>
    {
        public Guid RoleId { get; set; }
        public Guid ClientId { get; set; }
    }

    internal class GetRolePermissionsByClientQueryHandler : IRequestHandler<GetRolePermissionsByClientQuery, Result<List<Guid>>>
    {
        private readonly IUnitOfWork<Guid> _unitOfWork;

        public GetRolePermissionsByClientQueryHandler(IUnitOfWork<Guid> unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<List<Guid>>> Handle(GetRolePermissionsByClientQuery request, CancellationToken cancellationToken)
        {
            var permissionIds = await _unitOfWork.Repository<Domain.Entities.RolePermission>().Entities
                .Where(x => x.RoleId == request.RoleId && x.ApplicationClientId == request.ClientId)
                .Select(x => x.PermissionId)
                .ToListAsync(cancellationToken);

            return await Result<List<Guid>>.SuccessAsync(permissionIds);
        }
    }
}
