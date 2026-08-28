// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;

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
        TelemetryLogging.LogAggregatedCounter(FunctionId.LSP_FindDocumentInWorkspace, KeyValueLogMessage.Create(m =>
        {
            m[TelemetryLogging.KeyName] = ServerTypeName + "." + workspaceCounterMetricName;
            m[TelemetryLogging.KeyValue] = 1L;
            m[TelemetryLogging.KeyMetricName] = workspaceCounterMetricName;
            m["server"] = ServerTypeName;
            m["workspace"] = workspaceCounterMetricName;
        }));
    }

    public void UpdateUsedForkedSolutionCounter(bool usedForkedSolution)
    {
        var metricName = usedForkedSolution ? "ForkedCount" : "NonForkedCount";
        TelemetryLogging.LogAggregatedCounter(FunctionId.LSP_UsedForkedSolution, KeyValueLogMessage.Create(m =>
        {
            m[TelemetryLogging.KeyName] = ServerTypeName + "." + metricName;
            m[TelemetryLogging.KeyValue] = 1L;
            m[TelemetryLogging.KeyMetricName] = metricName;
            m["server"] = ServerTypeName;
            m["usedForkedSolution"] = usedForkedSolution;
        }));
    }

    public async Task ReportEmptySymbolResultAsync(
        string method,
        Document document,
        LinePosition linePosition,
        CancellationToken cancellationToken)
    {
        var logger = Logger.GetLogger();
        if (logger?.IsEnabled(FunctionId.LSP_SymbolRequest_EmptyResult) != true)
            return;

        await ReportEmptySymbolResultAsync(logger, ServerTypeName, method, document, linePosition, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task ReportEmptySymbolResultAsync(
        ILogger logger,
        string serverTypeName,
        string method,
        Document document,
        LinePosition linePosition,
        CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var line = text.Lines[linePosition.Line];
        var absolutePosition = text.Lines.GetPosition(linePosition);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var token = root.FindToken(absolutePosition, findInsideTrivia: true);

        var logMessage = KeyValueLogMessage.Create(static (properties, args) =>
        {
            properties["server"] = args.serverTypeName;
            properties["method"] = args.method;
            properties["line"] = args.linePosition.Line;
            properties["character"] = args.linePosition.Character;
            properties["lineCount"] = args.text.Lines.Count;
            properties["lineLength"] = args.line.Span.Length;
            properties["absolutePosition"] = args.absolutePosition;
            properties["language"] = args.document.Project.Language;
            properties["workspaceKind"] = args.document.Project.Solution.WorkspaceKind;
            properties["positionKind"] = RequestTelemetryLogger.GetPositionKind(args.text, args.line, args.absolutePosition, args.token);
            properties["tokenRawKind"] = args.token.RawKind;
        }, (serverTypeName, method, document, linePosition, text, line, absolutePosition, token), logLevel: LogLevel.Information);

        try
        {
            logger.Log(FunctionId.LSP_SymbolRequest_EmptyResult, logMessage);
        }
        finally
        {
            logMessage.Free();
        }
    }

    public void UpdateTelemetryData(
        string methodName,
        string? language,
        TimeSpan queuedDuration,
        TimeSpan requestDuration,
        Result result)
    {
        // Store the request time metrics per LSP method.
        TelemetryLogging.LogAggregatedHistogram(FunctionId.LSP_TimeInQueue, KeyValueLogMessage.Create(m =>
        {
            m[TelemetryLogging.KeyName] = ServerTypeName;
            m[TelemetryLogging.KeyValue] = (long)queuedDuration.TotalMilliseconds;
            m[TelemetryLogging.KeyMetricName] = "TimeInQueue";
            m["server"] = ServerTypeName;
        }));

        TelemetryLogging.LogAggregatedHistogram(FunctionId.LSP_RequestDuration, KeyValueLogMessage.Create(m =>
        {
            m[TelemetryLogging.KeyName] = ServerTypeName + "." + methodName + "." + language;
            m[TelemetryLogging.KeyValue] = (long)requestDuration.TotalMilliseconds;
            m[TelemetryLogging.KeyMetricName] = "RequestDuration";
            m["server"] = ServerTypeName;
            m["method"] = methodName;
            m["language"] = language;
        }));

        var metricName = result switch
        {
            Result.Succeeded => "SucceededCount",
            Result.Failed => "FailedCount",
            Result.Cancelled => "CancelledCount",
            _ => throw ExceptionUtilities.UnexpectedValue(result)
        };

        TelemetryLogging.LogAggregatedCounter(FunctionId.LSP_RequestCounter, KeyValueLogMessage.Create(m =>
        {
            m[TelemetryLogging.KeyName] = ServerTypeName + "." + methodName + "." + language + "." + metricName;
            m[TelemetryLogging.KeyValue] = 1L;
            m[TelemetryLogging.KeyMetricName] = metricName;
            m["server"] = ServerTypeName;
            m["method"] = methodName;
            m["language"] = language;
        }));
    }

    public void Dispose()
    {
        // Ensure that telemetry logged for this server instance is flushed before potentially creating a new instance.
        // This is also called on disposal of the telemetry session, but will no-op if already flushed.
        TelemetryLogging.Flush();
    }

    private static string GetPositionKind(SourceText text, TextLine line, int absolutePosition, SyntaxToken token)
    {
        if (absolutePosition == text.Length)
            return "EndOfFile";

        if (absolutePosition == line.End)
            return "EndOfLine";

        if (char.IsWhiteSpace(text[absolutePosition]))
            return "Whitespace";

        return token.Span.Contains(absolutePosition) ? "Token" : "Trivia";
    }

    internal enum Result
    {
        Succeeded,
        Failed,
        Cancelled
    }
}
