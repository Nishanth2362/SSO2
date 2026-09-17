using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Shared.Wrapper.Mediator
{
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

    public interface IPipelineBehavior<TRequest, TResponse>
    {
        Task<TResponse> Handle(TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct);
    }
}
