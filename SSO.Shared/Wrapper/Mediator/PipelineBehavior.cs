using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SSO.Common.Wrapper;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;

namespace SSO.Shared.Wrapper.Mediator
{
    public sealed class ExceptionBehavior<TRequest, TResponse>
     : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<ExceptionBehavior<TRequest, TResponse>> _logger;

        public ExceptionBehavior(ILogger<ExceptionBehavior<TRequest, TResponse>> logger)
            => _logger = logger;

        public async Task<TResponse> Handle(TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            try { return await next(); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {Request}", typeof(TRequest).Name);
                throw;
            }
        }
    }


    public sealed class PerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

        public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
            => _logger = logger;

        public async Task<TResponse> Handle(TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            var response = await next();
            sw.Stop();

            _logger.LogInformation("{Request} took {Time}ms",
                typeof(TRequest).Name, sw.ElapsedMilliseconds);

            return response;
        }
    }

    public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
            => _logger = logger;

        public async Task<TResponse> Handle(TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            _logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
            var response = await next();
            _logger.LogInformation("Handled {Request}", typeof(TRequest).Name);
            return response;
        }
    }

    public sealed class ValidationError
    {
        public string PropertyName { get; init; } = default!;
        public string ErrorMessage { get; init; } = default!;
    }

    public interface IRequestValidator<TRequest>
    {
        Task<IEnumerable<ValidationError>> ValidateAsync(
            TRequest request,
            CancellationToken cancellationToken);
    }


    internal sealed class ValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IRequestValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IRequestValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var failures = new List<ValidationError>();

            foreach (var validator in _validators)
            {
                var result = await validator.ValidateAsync(request, cancellationToken);
                failures.AddRange(result);
            }

            if (failures.Count == 0)
                return await next();

            // Convert validation errors to Result<T>
            var responseType = typeof(TResponse);

            if (responseType.IsGenericType &&
                responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = responseType.GetGenericArguments()[0];
                var failMethod = typeof(Result<>)
                    .MakeGenericType(resultType)
                    .GetMethod("ValidationFail")!;

                return (TResponse)failMethod.Invoke(null, new object[] { failures })!;
            }

            throw new ValidationException(Newtonsoft.Json.JsonConvert.SerializeObject( failures));
        }
    }


    public interface ICacheableRequest
    {
        string CacheKey { get; }
        TimeSpan? Expiration { get; }
        bool BypassCache { get; }
    }

    public interface IInvalidateCacheRequest
    {
        IEnumerable<string> CacheKeys { get; }
    }

    internal sealed class CachingBehavior<TRequest, TResponse>
     : IPipelineBehavior<TRequest, TResponse>
     where TRequest : IRequest<TResponse>
    {
        private readonly IAppCache _cache;

        public CachingBehavior(IAppCache cache)
        {
            _cache = cache;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // 1️⃣ Handle cache invalidation (commands)
            if (request is IInvalidateCacheRequest invalidate)
            {
                foreach (var key in invalidate.CacheKeys)
                    _cache.Remove(key);

                return await next();
            }

            // 2️⃣ Handle caching (queries)
            if (request is not ICacheableRequest cacheable || cacheable.BypassCache)
                return await next();

            var cacheKey = cacheable.CacheKey;
            var expiration = cacheable.Expiration ?? TimeSpan.FromMinutes(5);

            return await _cache.GetOrAddAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = expiration;
                return await next();
            });
        }
    }


}
