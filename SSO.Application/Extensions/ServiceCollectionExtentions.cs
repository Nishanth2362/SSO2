using DocumentFormat.OpenXml.Bibliography;
using Microsoft.Extensions.DependencyInjection;
using SSO.Shared.Wrapper.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace SSO.Application.Extensions
{
    public static class ServiceCollectionExtentions
    {
        public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
        {
            // Register application services here
            // For example:
            // services.AddTransient<IExampleService, ExampleService>();
            // Register Mediator and its behaviors

            services.AddMediator(typeof(ServiceCollectionExtentions).Assembly);
            return services;
        }
    }
}
