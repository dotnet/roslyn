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
/// Allows <see cref="TraceSource"/> instances to monitor in-process Roslyn activity.
///
/// This involves creating strings and boxing, so it is enabled only while a trace source is registered.
/// It does not collect activity from the out-of-process service.
/// </summary>
internal sealed class TraceSourceEventSink : IEventSink
{
    public static readonly TraceSourceEventSink Instance = new();

    private const int LogEventId = 0;
    private const int StartEventId = 1;
    private const int EndEventId = 2;

    private ImmutableArray<TraceSource> _traceSources = [];

    private TraceSourceEventSink()
    {
    }

    public void Add(TraceSource traceSource)
    {
        Contract.ThrowIfNull(traceSource);
        ImmutableInterlocked.Update(ref _traceSources, static (sources, source) => sources.Contains(source) ? sources : sources.Add(source), traceSource);
    }

    public void Remove(TraceSource traceSource)
    {
        Contract.ThrowIfNull(traceSource);
        ImmutableInterlocked.Update(ref _traceSources, static (sources, source) => sources.Remove(source), traceSource);
    }

    public bool IsEnabled(FunctionId functionId)
        => !_traceSources.IsEmpty;

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
