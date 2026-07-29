using System;
using Microsoft.Extensions.Logging;
using MSBuildWorkspaceTester.Services;

namespace MSBuildWorkspaceTester.Logging
{
    internal class OutputLogger : ILogger
    {
        private readonly OutputService _outputService;

        public OutputLogger(OutputService outputService)
        {
            _outputService = outputService ?? throw new ArgumentNullException(nameof(outputService));
        }

        private class NullDisposable : IDisposable
        {
            public void Dispose() { }
        }

        public IDisposable BeginScope<TState>(TState state) => new NullDisposable();

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string messageText = formatter(state, exception);
            _outputService.WriteLine(messageText);
        }
    }
}
