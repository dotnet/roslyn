using Microsoft.Extensions.Logging;
using MSBuildWorkspaceTester.Services;

namespace MSBuildWorkspaceTester.Logging
{
    internal static class Extensions
    {
        public static ILoggerFactory AddOutput(this ILoggerFactory loggerFactory, OutputService outputService)
        {
            loggerFactory.AddProvider(new OutputLoggerProvider(outputService));
            return loggerFactory;
        }
    }
}
