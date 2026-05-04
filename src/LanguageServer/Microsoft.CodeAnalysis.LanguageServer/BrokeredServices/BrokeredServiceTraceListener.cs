// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

[Export, Shared]
internal sealed class BrokeredServiceTraceListener : TraceListener
{
    private readonly ILogger _logger;

    public TraceSource Source { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public BrokeredServiceTraceListener(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(nameof(BrokeredServiceTraceListener));
        Source = new TraceSource("ServiceBroker", SourceLevels.All);
        Source.Listeners.Add(this);
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
