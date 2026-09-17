using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Shared.Wrapper.Mediator
{
    internal abstract class RequestHandlerWrapper<TResponse>
    {
        public abstract Task<TResponse> Handle(
            IRequest<TResponse> request,
            IServiceProvider provider,
            CancellationToken ct);
    }

    internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse>
    : RequestHandlerWrapper<TResponse>
    where TRequest : IRequest<TResponse>
    {
        public override async Task<TResponse> Handle(
            IRequest<TResponse> request,
            IServiceProvider provider,
            CancellationToken ct)
        {
            var handler = provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();

            var behaviors = provider
                .GetServices<IPipelineBehavior<TRequest, TResponse>>()
                .Reverse()
                .ToList();

            RequestHandlerDelegate<TResponse> handlerDelegate =
                () => handler.Handle((TRequest)request, ct);

            foreach (var behavior in behaviors)
            {
                var next = handlerDelegate;
                handlerDelegate = () => behavior.Handle((TRequest)request, next, ct);
            }

            return await handlerDelegate();
        }
    }

    internal sealed class VoidRequestHandlerWrapperImpl<TRequest>
    where TRequest : IRequest
    {
        public async Task Handle(IRequest request,
            IServiceProvider provider,
            CancellationToken ct)
        {
            var handler = provider.GetRequiredService<IRequestHandler<TRequest>>();
            await handler.Handle((TRequest)request, ct);
        }
    }

    internal abstract class StreamHandlerWrapper<TResponse>
    {
        public abstract IAsyncEnumerable<TResponse> Handle(
            IStreamRequest<TResponse> request,
            IServiceProvider provider,
            CancellationToken ct);
    }

    internal sealed class StreamHandlerWrapperImpl<TRequest, TResponse>
        : StreamHandlerWrapper<TResponse>
        where TRequest : IStreamRequest<TResponse>
    {
        public override IAsyncEnumerable<TResponse> Handle(
            IStreamRequest<TResponse> request,
            IServiceProvider provider,
            CancellationToken ct)
        {
            var handler = provider.GetRequiredService<IStreamRequestHandler<TRequest, TResponse>>();
            return handler.Handle((TRequest)request, ct);
        }
    }



}
