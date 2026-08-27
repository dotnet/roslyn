// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal static class SymbolRequestTelemetryLogger
{
    public static async Task ReportEmptyResultAsync(
        string method,
        Document document,
        LinePosition linePosition,
        CancellationToken cancellationToken)
    {
        var logger = Logger.GetLogger();
        if (logger?.IsEnabled(FunctionId.LSP_SymbolRequest_EmptyResult) != true)
            return;

        await ReportEmptyResultAsync(logger, method, document, linePosition, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task ReportEmptyResultAsync(
        ILogger logger,
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
            properties["method"] = args.method;
            properties["line"] = args.linePosition.Line;
            properties["character"] = args.linePosition.Character;
            properties["lineCount"] = args.text.Lines.Count;
            properties["lineLength"] = args.line.Span.Length;
            properties["absolutePosition"] = args.absolutePosition;
            properties["language"] = args.document.Project.Language;
            properties["workspaceKind"] = args.document.Project.Solution.WorkspaceKind;
            properties["positionKind"] = SymbolRequestTelemetryLogger.GetPositionKind(args.text, args.line, args.absolutePosition, args.token);
            properties["tokenRawKind"] = args.token.RawKind;
        }, (method, document, linePosition, text, line, absolutePosition, token), logLevel: LogLevel.Information);

        try
        {
            logger.Log(FunctionId.LSP_SymbolRequest_EmptyResult, logMessage);
        }
        finally
        {
            logMessage.Free();
        }
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
}
