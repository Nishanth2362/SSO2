using SSO.Application.Requests.DataTable;
using SSO.Application.Requests.Features;
using SSO.Application.Responses.DataTable;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Interfaces.Services.Features
{
    public interface IClientService
    {
        Task<Result<ClientResponse>> GetDefaultClientAsync(CancellationToken cancellationToken = default);
        Task<Result<Guid>> CreateAsync(ClientRequest request);
        Task<Result<List<ClientResponse>>> GetAllAsync(Guid tenantId);
        Task<PaginatedResult<ClientResponse>> GetPagedAsync(ClientPagedRequest request);
        Task<DataTableResponse<ClientResponse>> GetClientPaged(DataTableRequest request);
        Task<Result<string>> RotateSecretAsync(Guid clientId);
        Task<Result<ClientRequest>> GetByIdAsync(Guid clientId);
        Task<Result<Guid>> DeleteAsync(Guid clientId);
    }
}
