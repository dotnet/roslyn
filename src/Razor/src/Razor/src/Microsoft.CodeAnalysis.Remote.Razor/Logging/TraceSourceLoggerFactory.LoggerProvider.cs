// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Razor.Logging;

namespace Microsoft.CodeAnalysis.Remote.Razor.Logging;

internal sealed partial class TraceSourceLoggerFactory
{
    private sealed class LoggerProvider(TraceSource traceSource) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
            => new Logger(traceSource, categoryName);
    }
}
