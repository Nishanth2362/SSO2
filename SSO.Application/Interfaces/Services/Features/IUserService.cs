using SSO.Application.Requests.Features;
using SSO.Application.Responses.Features;
using SSO.Common.Wrapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Interfaces.Services.Features
{
    public interface IUserService
    {
        Task<Result<Guid>> CreateUserAsync(UserRequest request);
        Task<Result<Guid>> UpdateUserAsync(UserRequest request);
        Task<Result<Guid>> DeleteUserAsync(Guid userId);
        Task<Result<UserResponse>> GetUser(Guid userId);
        Task<PaginatedResult<UserResponse>> GetUsersAsync(int pageNumber, int pageSize);
    }
}
