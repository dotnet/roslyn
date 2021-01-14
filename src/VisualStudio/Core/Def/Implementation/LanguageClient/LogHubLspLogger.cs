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
    internal partial class InProcLanguageServer
    {
        private class LogHubLspLogger : ILspLogger
        {
            private TraceConfiguration? _configuration;
            private TraceSource? _traceSource;

            public LogHubLspLogger(TraceConfiguration configuration, TraceSource traceSource)
            {
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                _traceSource = traceSource ?? throw new ArgumentNullException(nameof(traceSource));
            }

            public void Dispose()
            {
                if (_traceSource == null || _configuration == null)
                {
                    Contract.Fail($"{GetType().FullName} was double disposed");
                    return;
                }

                _traceSource.Flush();
                _traceSource.Close();
                _configuration.Dispose();

                _traceSource = null;
                _configuration = null;
            }

            public void TraceInformation(string message)
                => _traceSource?.TraceInformation(message);
        }
    }
}
