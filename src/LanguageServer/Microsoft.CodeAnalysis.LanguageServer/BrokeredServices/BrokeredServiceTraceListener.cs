// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

internal sealed class BrokeredServiceTraceListener : TraceListener
{
    private readonly ILogger _logger;

    private BrokeredServiceTraceListener(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(BrokeredServiceTraceListener));
    }

    public static TraceSource CreateTraceSource(ILoggerFactory loggerFactory)
    {
        var traceSource = new TraceSource("ServiceBroker", SourceLevels.All);
        traceSource.Listeners.Add(new BrokeredServiceTraceListener(loggerFactory));
        return traceSource;
    }

    public override void Write(string? message)
    {
        _logger.LogDebug(message);
    }

    public override void WriteLine(string? message)
    {
        _logger.LogDebug(message);
    }
}
