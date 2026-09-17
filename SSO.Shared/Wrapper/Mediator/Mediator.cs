using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SSO.Shared.Wrapper.Mediator
{
        public sealed class Mediator : IMediator
    {
        private readonly IServiceScopeFactory _scopeFactory;

        private static readonly ConcurrentDictionary<Type, object> _requestHandlers = new();
        private static readonly ConcurrentDictionary<Type, object> _streamHandlers = new();

        public Mediator(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;

            var wrapper = (RequestHandlerWrapper<TResponse>)_requestHandlers.GetOrAdd(
                request.GetType(),
                type =>
                {
                    var wrapperType = typeof(RequestHandlerWrapperImpl<,>)
                        .MakeGenericType(type, typeof(TResponse));
                    return Activator.CreateInstance(wrapperType)!;
                });

            return await wrapper.Handle(request, provider, ct);
        }

        public async Task Send(IRequest request, CancellationToken ct = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;

            var wrapperType = typeof(VoidRequestHandlerWrapperImpl<>)
                .MakeGenericType(request.GetType());

            dynamic wrapper = Activator.CreateInstance(wrapperType)!;
            await wrapper.Handle(request, provider, ct);
        }

        public async Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : INotification
        {
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;

            var handlers = provider.GetServices<INotificationHandler<TNotification>>();
            var tasks = handlers.Select(h => h.Handle(notification, ct));

            await Task.WhenAll(tasks);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default)
        {
            var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;

            var wrapper = (StreamHandlerWrapper<TResponse>)_streamHandlers.GetOrAdd(
                request.GetType(),
                type =>
                {
                    var wrapperType = typeof(StreamHandlerWrapperImpl<,>)
                        .MakeGenericType(type, typeof(TResponse));
                    return Activator.CreateInstance(wrapperType)!;
                });

            return wrapper.Handle(request, provider, ct);
        }
    }


}
