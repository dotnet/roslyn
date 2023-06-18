// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;

namespace Microsoft.CodeAnalysis.CSharp.Snippets;

internal static class СSharpSnippetIndentationHelpers
{
    public static string GetBlockLikeIndentationString(Document document, int startPositionOfOpenCurlyBrace, SyntaxFormattingOptions syntaxFormattingOptions, CancellationToken cancellationToken)
    {
        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
        var openBraceLine = parsedDocument.Text.Lines.GetLineFromPosition(startPositionOfOpenCurlyBrace).LineNumber;

        var indentationOptions = new IndentationOptions(syntaxFormattingOptions);
        var newLine = indentationOptions.FormattingOptions.NewLine;

        var indentationService = parsedDocument.LanguageServices.GetRequiredService<IIndentationService>();
        var indentation = indentationService.GetIndentation(parsedDocument, openBraceLine + 1, indentationOptions, cancellationToken);

        // Adding the offset calculated with one tab so that it is indented once past the line containing the opening brace
        var newIndentation = new IndentationResult(indentation.BasePosition, indentation.Offset + syntaxFormattingOptions.TabSize);
        return newIndentation.GetIndentationString(parsedDocument.Text, syntaxFormattingOptions.UseTabs, syntaxFormattingOptions.TabSize) + newLine;
    }
}
