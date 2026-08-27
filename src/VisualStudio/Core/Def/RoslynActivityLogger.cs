// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
    public static readonly TraceSourceSink Sink = new();

    public static void SetLogger(TraceSource traceSource)
    {
        Contract.ThrowIfNull(traceSource);
        Sink.Add(traceSource);
    }

    public static void RemoveLogger(TraceSource traceSource)
    {
        Contract.ThrowIfNull(traceSource);
        Sink.Remove(traceSource);
    }

    internal sealed class TraceSourceSink : IEventSink
    {
        private const int LogEventId = 0;
        private const int StartEventId = 1;
        private const int EndEventId = 2;

        private ImmutableArray<TraceSource> _traceSources = [];

        public void Add(TraceSource traceSource)
            => ImmutableInterlocked.Update(ref _traceSources, static (sources, source) => sources.Contains(source) ? sources : sources.Add(source), traceSource);

        public void Remove(TraceSource traceSource)
            => ImmutableInterlocked.Update(ref _traceSources, static (sources, source) => sources.Remove(source), traceSource);

        public bool IsEnabled(FunctionId functionId)
        {
            // we log every roslyn activity, but only while someone is listening
            return !_traceSources.IsEmpty;
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
            foreach (var traceSource in _traceSources)
                traceSource.TraceData(TraceEventType.Verbose, LogEventId, functionId.Convert(), logMessage.GetMessage());
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        {
            foreach (var traceSource in _traceSources)
                traceSource.TraceData(TraceEventType.Verbose, StartEventId, functionId.Convert(), uniquePairId);
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
            foreach (var traceSource in _traceSources)
                traceSource.TraceData(TraceEventType.Verbose, EndEventId, functionId.Convert(), uniquePairId, cancellationToken.IsCancellationRequested, delta, logMessage.GetMessage());
        }
    }
}
