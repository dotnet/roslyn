// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// A sink that publishes events to ETW using an EventSource. Opt-in per <see cref="FunctionId"/>, via
/// Tools -&gt; Options -&gt; Performance Loggers.
/// </summary>
internal sealed class EtwEventSink(Func<FunctionId, bool> isEnabledPredicate) : IEventSink
{
    // Due to ETW specifics, RoslynEventSource.Instance needs to be initialized during EtwEventSink construction
    // so that we can enable the listeners synchronously before any events are logged.
    private readonly RoslynEventSource _source = RoslynEventSource.Instance;

    public bool IsEnabled(FunctionId functionId)
        => _source.IsEnabled() && isEnabledPredicate(functionId);

    public void Log(FunctionId functionId, LogMessage logMessage)
        => _source.Log(GetMessage(logMessage), functionId);

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        => _source.BlockStart(GetMessage(logMessage), functionId, uniquePairId);

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            _source.BlockCanceled(functionId, delta, uniquePairId);
        }
        else
        {
            _source.BlockStop(functionId, delta, uniquePairId);
        }
    }

    private bool IsVerbose()
    {
        // "-1" makes this to work with any keyword
        return _source.IsEnabled(EventLevel.Verbose, (EventKeywords)(-1));
    }

    private string GetMessage(LogMessage logMessage)
        => IsVerbose() ? logMessage.GetMessage() : string.Empty;
}
