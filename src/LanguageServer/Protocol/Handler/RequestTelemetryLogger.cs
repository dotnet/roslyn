// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Logs metadata on LSP requests (duration, success / failure metrics)
/// for this particular LSP server instance.
/// </summary>
internal sealed class RequestTelemetryLogger : IDisposable, ILspService
{
    private readonly string _serverTypeName;

    /// <summary>
    /// Store request counters in a concurrent dictionary as non-mutating LSP requests can
    /// run alongside other non-mutating requests.
    /// </summary>
    private readonly ConcurrentDictionary<(string Method, string? Language), Counter> _requestCounters;

    private readonly CountLogAggregator<string> _findDocumentResults;

    private readonly CountLogAggregator<bool> _usedForkedSolutionCounter;

    private int _disposed;

    public RequestTelemetryLogger(string serverTypeName)
    {
        _serverTypeName = serverTypeName;
        _requestCounters = new();
        _findDocumentResults = new();
        _usedForkedSolutionCounter = new();

        TelemetryLogging.Flushed += OnFlushed;
    }

    public void UpdateFindDocumentTelemetryData(bool success, string? workspaceKind)
    {
        var workspaceKindTelemetryProperty = success ? workspaceKind : "Failed";

        if (workspaceKindTelemetryProperty != null)
        {
            _findDocumentResults.IncreaseCount(workspaceKindTelemetryProperty);
        }
    }

    public void UpdateUsedForkedSolutionCounter(bool usedForkedSolution)
    {
        _usedForkedSolutionCounter.IncreaseCount(usedForkedSolution);
    }

    public void UpdateTelemetryData(
        string methodName,
        string? language,
        TimeSpan queuedDuration,
        TimeSpan requestDuration,
        Result result)
    {
        // Store the request time metrics per LSP method.
        TelemetryLogging.LogAggregated(FunctionId.LSP_TimeInQueue, KeyValueLogMessage.Create(m =>
        {
            m[TelemetryLogging.KeyName] = _serverTypeName;
            m[TelemetryLogging.KeyValue] = queuedDuration.Milliseconds;
            m[TelemetryLogging.KeyMetricName] = "TimeInQueue";
            m["server"] = _serverTypeName;
            m["method"] = methodName;
            m["language"] = language;
        }));

        TelemetryLogging.LogAggregated(FunctionId.LSP_RequestDuration, KeyValueLogMessage.Create(m =>
        {
            m[TelemetryLogging.KeyName] = _serverTypeName + "." + methodName;
            m[TelemetryLogging.KeyValue] = requestDuration.Milliseconds;
            m[TelemetryLogging.KeyMetricName] = "RequestDuration";
            m["server"] = _serverTypeName;
            m["method"] = methodName;
            m["language"] = language;
        }));

        _requestCounters.GetOrAdd((methodName, language), (_) => new Counter()).IncrementCount(result);
    }

    /// <summary>
    /// Only output aggregate telemetry to the vs logger when the server instance is disposed
    /// to avoid spamming the telemetry output with thousands of events
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Flush all telemetry logged through TelemetryLogging
        TelemetryLogging.Flush();

        TelemetryLogging.Flushed -= OnFlushed;
    }

    private void OnFlushed(object? sender, EventArgs e)
    {
        foreach (var kvp in _requestCounters)
        {
            TelemetryLogging.Log(FunctionId.LSP_RequestCounter, KeyValueLogMessage.Create(LogType.Trace, m =>
            {
                m["server"] = _serverTypeName;
                m["method"] = kvp.Key.Method;
                m["language"] = kvp.Key.Language;
                m["successful"] = kvp.Value.SucceededCount;
                m["failed"] = kvp.Value.FailedCount;
                m["cancelled"] = kvp.Value.CancelledCount;
            }));
        }

        if (!_findDocumentResults.IsEmpty)
        {
            TelemetryLogging.Log(FunctionId.LSP_FindDocumentInWorkspace, KeyValueLogMessage.Create(LogType.Trace, m =>
            {
                m["server"] = _serverTypeName;
                foreach (var kvp in _findDocumentResults)
                {
                    var info = kvp.Key.ToString()!;
                    m[info] = kvp.Value.GetCount();
                }
            }));
        }

        if (!_usedForkedSolutionCounter.IsEmpty)
        {
            TelemetryLogging.Log(FunctionId.LSP_UsedForkedSolution, KeyValueLogMessage.Create(LogType.Trace, m =>
            {
                m["server"] = _serverTypeName;
                foreach (var kvp in _usedForkedSolutionCounter)
                {
                    var info = kvp.Key.ToString()!;
                    m[info] = kvp.Value.GetCount();
                }
            }));
        }

        _requestCounters.Clear();
        _findDocumentResults.Clear();
        _usedForkedSolutionCounter.Clear();
    }

    private class Counter
    {
        private int _succeededCount;
        private int _failedCount;
        private int _cancelledCount;

        public int SucceededCount => _succeededCount;
        public int FailedCount => _failedCount;
        public int CancelledCount => _cancelledCount;

        public void IncrementCount(Result result)
        {
            switch (result)
            {
                case Result.Succeeded:
                    Interlocked.Increment(ref _succeededCount);
                    break;
                case Result.Failed:
                    Interlocked.Increment(ref _failedCount);
                    break;
                case Result.Cancelled:
                    Interlocked.Increment(ref _cancelledCount);
                    break;
            }
        }
    }

    internal enum Result
    {
        Succeeded,
        Failed,
        Cancelled
    }
}
