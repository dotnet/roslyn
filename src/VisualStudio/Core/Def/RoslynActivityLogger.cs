// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices;

/// <summary>
/// Let people to inject <see cref="TraceSource"/> to monitor Roslyn activity
/// 
/// Here, we don't technically use TraceSource as it is meant to be used. but just as an easy 
/// way to log data to listeners.
/// 
/// this also involves creating string, boxing and etc. so, perf wise, it will impact VS quite a bit.
/// this also won't collect trace from Roslyn OOP for now. only in proc activity
/// </summary>
internal static class RoslynActivityLogger
{
    private static readonly object s_gate = new();

    public static void SetLogger(TraceSource traceSource)
    {
        Contract.ThrowIfNull(traceSource);

        lock (s_gate)
        {
            // internally, it just uses our existing ILogger
            Logger.SetLogger(AggregateLogger.AddOrReplace(new TraceSourceLogger(traceSource), Logger.GetLogger(), l => (l as TraceSourceLogger)?.TraceSource == traceSource));
        }
    }

    public static void RemoveLogger(TraceSource traceSource)
    {
        Contract.ThrowIfNull(traceSource);

        lock (s_gate)
        {
            // internally, it just uses our existing ILogger
            Logger.SetLogger(AggregateLogger.Remove(Logger.GetLogger(), l => (l as TraceSourceLogger)?.TraceSource == traceSource));
        }
    }

    private sealed class TraceSourceLogger : ILogger
    {
        private const int LogEventId = 0;
        private const int StartEventId = 1;
        private const int EndEventId = 2;

        public readonly TraceSource TraceSource;

        public TraceSourceLogger(TraceSource traceSource)
            => TraceSource = traceSource;

        public bool IsEnabled(FunctionId functionId)
        {
            // we log every roslyn activity
            return true;
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
            => TraceSource.TraceData(TraceEventType.Verbose, LogEventId, functionId.Convert(), logMessage.GetMessage());

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
            => TraceSource.TraceData(TraceEventType.Verbose, StartEventId, functionId.Convert(), uniquePairId);

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
            => TraceSource.TraceData(TraceEventType.Verbose, EndEventId, functionId.Convert(), uniquePairId, cancellationToken.IsCancellationRequested, delta, logMessage.GetMessage());
    }
}
