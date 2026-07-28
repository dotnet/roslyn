using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MSBuildWorkspaceTester.Services
{
    internal abstract class BaseService
    {
        protected readonly IServiceProvider ServiceProvider;
        protected readonly ILogger Logger;

        protected BaseService(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;

            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            Logger = loggerFactory.CreateLogger(GetType());
        }
    }
}
