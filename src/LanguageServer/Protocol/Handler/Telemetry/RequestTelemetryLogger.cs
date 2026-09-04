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
using LSP = Roslyn.LanguageServer.Protocol;

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

    public async Task ReportEmptySymbolResultAsync(
        string method,
        Document document,
        LSP.Position position,
        CancellationToken cancellationToken)
    {
        var logger = Logger.GetLogger();
        if (logger?.IsEnabled(FunctionId.LSP_SymbolRequest_EmptyResult) != true)
            return;

        await ReportEmptySymbolResultAsync(logger, ServerTypeName, method, document, position, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task ReportEmptySymbolResultAsync(
        ILogger logger,
        string serverTypeName,
        string method,
        Document document,
        LSP.Position position,
        CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        var line = default(TextLine);
        var token = default(SyntaxToken);
        var positionKind = GetInvalidPositionKind(text, position);
        var isValidPosition = positionKind is null;

        if (isValidPosition)
        {
            line = text.Lines[position.Line];
            var absolutePosition = line.Start + position.Character;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            token = root.FindToken(absolutePosition, findInsideTrivia: true);
            positionKind = GetPositionKind(text, line, absolutePosition, token);
        }

        var logMessage = KeyValueLogMessage.Create(static (properties, args) =>
        {
            properties["server"] = args.serverTypeName;
            properties["method"] = args.method;
            properties["line"] = args.position.Line;
            properties["character"] = args.position.Character;
            properties["isFirstLine"] = args.position.Line == 0;
            properties["isLastLine"] = args.position.Line == args.text.Lines.Count - 1;
            properties["isAtLineStart"] = args.position.Character == 0;
            properties["isAtLineEnd"] = args.isValidPosition && args.position.Character == args.line.Span.Length;
            properties["language"] = args.document.Project.Language;
            properties["workspaceKind"] = args.document.Project.Solution.WorkspaceKind;
            properties["positionKind"] = args.positionKind;

            if (args.isValidPosition)
            {
                properties["tokenRawKind"] = args.token.RawKind;
                properties["parentNodeRawKind"] = args.token.Parent?.RawKind ?? 0;
            }
        }, (serverTypeName, method, document, position, text, line, positionKind, isValidPosition, token), logLevel: LogLevel.Information);

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

    private static string? GetInvalidPositionKind(SourceText text, LSP.Position position)
    {
        if (position.Line < 0 || position.Line >= text.Lines.Count)
            return "LineOutOfRange";

        if (position.Character < 0 || position.Character > text.Lines[position.Line].Span.Length)
            return "CharacterOutOfRange";

        return null;
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
