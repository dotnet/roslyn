// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.VisualStudio.LogHub;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    internal class LogHubLspLogger : ILspLogger
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

        public void TraceInformation(string message)
            => _traceSource.TraceInformation(message);

        public void TraceWarning(string message)
            => _traceSource.TraceEvent(TraceEventType.Warning, id: 0, message);

        public void TraceError(string message)
            => _traceSource.TraceEvent(TraceEventType.Error, id: 0, message);

        public void TraceException(Exception exception)
            => _traceSource.TraceEvent(TraceEventType.Error, id: 0, "Exception: {0}", exception);

        public void TraceStart(string message)
            => _traceSource.TraceEvent(TraceEventType.Start, id: 0, message);

        public void TraceStop(string message)
            => _traceSource.TraceEvent(TraceEventType.Stop, id: 0, message);
    }
}
