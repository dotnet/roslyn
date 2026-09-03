// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Logs metadata on LSP requests (duration, success / failure metrics)
/// for this particular LSP server instance.
/// </summary>
internal class RequestTelemetryLogger : IDisposable, ILspService
{
    protected readonly string ServerTypeName;

    public RequestTelemetryLogger(string serverTypeName)
    {
        ServerTypeName = serverTypeName;
    }

    public void UpdateFindDocumentTelemetryData(bool success, string? workspaceKind)
    {
        var workspaceKindTelemetryProperty = success ? workspaceKind : "Failed";
        if (workspaceKindTelemetryProperty != null)
        {
            IncreaseFindDocumentCount(workspaceKindTelemetryProperty);
        }
    }

    protected virtual void IncreaseFindDocumentCount(string workspaceCounterMetricName)
    {
        RoslynTelemetry.Count(FunctionId.LSP_FindDocumentInWorkspace, workspaceCounterMetricName, 1,
            new("server", ServerTypeName),
            new("workspace", workspaceCounterMetricName));
    }

    public void UpdateUsedForkedSolutionCounter(bool usedForkedSolution)
    {
        var metricName = usedForkedSolution ? "ForkedCount" : "NonForkedCount";
        RoslynTelemetry.Count(FunctionId.LSP_UsedForkedSolution, metricName, 1,
            new("server", ServerTypeName),
            new("usedForkedSolution", usedForkedSolution));
    }

    public void UpdateTelemetryData(
        string methodName,
        string? language,
        TimeSpan queuedDuration,
        TimeSpan requestDuration,
        Result result)
    {
        // Store the request time metrics per LSP method.
        RoslynTelemetry.Record(FunctionId.LSP_TimeInQueue, "TimeInQueue", (long)queuedDuration.TotalMilliseconds,
            new("server", ServerTypeName));

        RoslynTelemetry.Record(FunctionId.LSP_RequestDuration, "RequestDuration", (long)requestDuration.TotalMilliseconds,
            new("server", ServerTypeName),
            new("method", methodName),
            new("language", language));

        var metricName = result switch
        {
            Result.Succeeded => "SucceededCount",
            Result.Failed => "FailedCount",
            Result.Cancelled => "CancelledCount",
            _ => throw ExceptionUtilities.UnexpectedValue(result)
        };

        RoslynTelemetry.Count(FunctionId.LSP_RequestCounter, metricName, 1,
            new("server", ServerTypeName),
            new("method", methodName),
            new("language", language));
    }

    public void Dispose()
    {
        // Ensure that telemetry logged for this server instance is flushed before potentially creating a new instance.
        // This is also called on disposal of the telemetry session, but will no-op if already flushed.
        RoslynTelemetry.Flush();
    }

    internal enum Result
    {
        Succeeded,
        Failed,
        Cancelled
    }
}
