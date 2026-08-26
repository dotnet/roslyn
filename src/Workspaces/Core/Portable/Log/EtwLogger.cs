// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// A sink that publishes events to ETW using an EventSource. Opt-in: enabled per-<see cref="FunctionId"/>
/// by a predicate that the host can swap at runtime (Tools -> Options -> Performance Loggers). It stays
/// registered for the lifetime of the process; "disabled" means the predicate rejects everything, which
/// is what keeps a second instance from ever being composed alongside this one and double-posting.
/// </summary>
internal sealed class EtwLogger : IEventSink
{
    /// <summary>
    /// A predicate that rejects every <see cref="FunctionId"/>. Used as the initial state for sinks
    /// that are off until a user turns them on.
    /// </summary>
    public static readonly Func<FunctionId, bool> DisabledPredicate = static _ => false;

    // Due to ETW specifics, RoslynEventSource.Instance needs to be initialized during EtwLogger construction 
    // so that we can enable the listeners synchronously before any events are logged.
    private readonly RoslynEventSource _source = RoslynEventSource.Instance;

    private Func<FunctionId, bool> _isEnabledPredicate;

    public EtwLogger(Func<FunctionId, bool> isEnabledPredicate)
        => _isEnabledPredicate = isEnabledPredicate;

    /// <summary>
    /// Replaces the enablement predicate in place. Callers must refresh the composed instance rather
    /// than constructing a competing one, or events would be posted twice.
    /// </summary>
    public void UpdatePredicate(Func<FunctionId, bool> isEnabledPredicate)
        => Volatile.Write(ref _isEnabledPredicate, isEnabledPredicate);

    public bool IsEnabled(FunctionId functionId)
        => _source.IsEnabled() && Volatile.Read(ref _isEnabledPredicate)(functionId);

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
