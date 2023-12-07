// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.LogHub;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    internal sealed class LogHubLspLogger : AbstractLspLogger, ILspServiceLogger
    {
        private readonly TraceConfiguration _configuration;
        private readonly TraceSource _traceSource;
        private bool _disposed;

        public LogHubLspLogger(TraceConfiguration configuration, TraceSource traceSource)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _traceSource = traceSource ?? throw new ArgumentNullException(nameof(traceSource));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                Contract.Fail($"{GetType().FullName} was double disposed");
                return;
            }

            _disposed = true;
            _traceSource.Flush();
            _traceSource.Close();
            _configuration.Dispose();
        }

        public override void LogInformation(string message, params object[] @params)
        {
            // Explicitly call TraceEvent here instead of TraceInformation.
            // TraceInformation indirectly calls string.Format which throws if the message
            // has unescaped curlies in it (can be a part of a URI for example).
            // Since we have no need to call string.Format here, we don't.
            _traceSource.TraceEvent(TraceEventType.Information, id: 0, message);
        }

        public override void LogWarning(string message, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Warning, id: 0, message);
        }

        public override void LogError(string message, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Error, id: 0, message);
        }

        public override void LogException(Exception exception, string? message = null, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Error, id: 0, "Exception: {0}", exception);
        }

        public override void LogStartContext(string message, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Start, id: 0, message);
        }

        public override void LogEndContext(string message, params object[] @params)
        {
            _traceSource.TraceEvent(TraceEventType.Stop, id: 0, message);
        }

        public override AbstractLspRequestScope TrackLspRequest(string message, ILspServices lspServices)
        {
            var telemetryLogger = lspServices.GetRequiredService<RequestTelemetryLogger>();

            return new LogHubLspRequestScope(message, this, telemetryLogger);
        }

        internal sealed class LogHubLspRequestScope : AbstractLspRequestScope
        {
            private readonly ILspServiceLogger _hostLogger;
            private readonly RequestTelemetryScope _telemetryScope;

            public LogHubLspRequestScope(string name, ILspServiceLogger hostLogger, RequestTelemetryLogger requestTelemetryLogger)
                : base(name)
            {
                _telemetryScope = new RequestTelemetryScope(name, requestTelemetryLogger);

                _hostLogger = hostLogger;
                _hostLogger.LogStartContext(name);
            }

            public override void RecordCancellation()
            {
                _hostLogger.LogInformation($"{Name} - Canceled");
                _telemetryScope.RecordCancellation();
            }

            public override void RecordException(Exception exception)
            {
                _hostLogger.LogException(exception);
                _telemetryScope.RecordException(exception);
            }

            public override void RecordWarning(string message)
            {
                _hostLogger.LogWarning(message);
                _telemetryScope.RecordWarning(message);
            }

            public override void Dispose()
            {
                _hostLogger.LogEndContext(Name);
                _telemetryScope.Dispose();
            }
        }
    }
}
