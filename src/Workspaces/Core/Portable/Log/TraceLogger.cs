// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Implementation of <see cref="IEventSink"/> that produces timing debug output. Opt-in, and controlled
/// the same way as <see cref="EtwLogger"/>: it stays registered and its predicate decides whether
/// anything is written.
/// </summary>
internal sealed class TraceLogger : IEventSink{
    private Func<FunctionId, bool> _isEnabledPredicate;

    public TraceLogger(Func<FunctionId, bool> isEnabledPredicate)
        => _isEnabledPredicate = isEnabledPredicate;

    /// <inheritdoc cref="EtwLogger.UpdatePredicate"/>
    public void UpdatePredicate(Func<FunctionId, bool> isEnabledPredicate)
        => Volatile.Write(ref _isEnabledPredicate, isEnabledPredicate);

    public bool IsEnabled(FunctionId functionId)
        => Volatile.Read(ref _isEnabledPredicate)(functionId);

    public void Log(FunctionId functionId, LogMessage logMessage)
        => Trace.WriteLine(string.Format("[{0}] {1} - {2}", Environment.CurrentManagedThreadId, functionId.ToString(), logMessage.GetMessage()));

    public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        => Trace.WriteLine(string.Format("[{0}] Start({1}) : {2} - {3}", Environment.CurrentManagedThreadId, uniquePairId, functionId.ToString(), logMessage.GetMessage()));

    public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
    {
        var functionString = functionId.ToString() + (cancellationToken.IsCancellationRequested ? " Canceled" : string.Empty);
        Trace.WriteLine(string.Format("[{0}] End({1}) : [{2}ms] {3}", Environment.CurrentManagedThreadId, uniquePairId, delta, functionString));
    }
}
