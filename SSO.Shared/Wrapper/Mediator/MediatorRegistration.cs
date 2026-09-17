using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SSO.Shared.Wrapper.Mediator
{


    public static class MediatorServiceCollectionExtensions
    {
        public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddScoped<IMediator, Mediator>();
            services.AddLazyCache();

            var types = assemblies.SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (var type in types)
                foreach (var i in type.GetInterfaces())
                {
                    if (!i.IsGenericType) continue;

                    var def = i.GetGenericTypeDefinition();

                    if (def == typeof(IRequestHandler<,>) ||
                        def == typeof(IRequestHandler<>) ||
                        def == typeof(INotificationHandler<>) ||
                        def == typeof(IRequestValidator<>) ||
                        def == typeof(IStreamRequestHandler<,>))
                        services.AddScoped(i, type);
                }

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
            //services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

            return services;
        }
    }

}