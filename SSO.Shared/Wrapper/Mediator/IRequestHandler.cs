using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Shared.Wrapper.Mediator
{
    public interface IRequestHandler<in TRequest, TResponse>
     where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken ct);
    }

    public interface IRequestHandler<in TRequest>
        where TRequest : IRequest
    {
        Task Handle(TRequest request, CancellationToken ct);
    }

    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        Task Handle(TNotification notification, CancellationToken ct);
    }

    public interface IStreamRequestHandler<in TRequest, TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        IAsyncEnumerable<TResponse> Handle(TRequest request, CancellationToken ct);
    }
}
