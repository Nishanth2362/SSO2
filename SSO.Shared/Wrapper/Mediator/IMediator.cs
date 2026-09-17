namespace SSO.Shared.Wrapper.Mediator
{
    // ----------------------------
    // Custom Mediator Interfaces
    // ----------------------------
    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);
        Task Send(IRequest request, CancellationToken ct = default);
        Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
            where TNotification : INotification;
        IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken ct = default);
    }
}

