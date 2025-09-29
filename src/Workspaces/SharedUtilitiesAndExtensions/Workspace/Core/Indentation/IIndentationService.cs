// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Indentation;

internal interface IIndentationService : ILanguageService
{
    /// <summary>
    /// Determines the desired indentation of a given line.
    /// </summary>
    IndentationResult GetIndentation(ParsedDocument document, int lineNumber, IndentationOptions options, CancellationToken cancellationToken);
}

internal static class IIndentationServiceExtensions
{
    /// <summary>
    /// Get's the preferred indentation for <paramref name="token"/> if that token were on its own line.  This
    /// effectively simulates where the token would be if the user hit enter at the start of the token.
    /// </summary>
    public static string GetPreferredIndentation(this SyntaxToken token, ParsedDocument document, IndentationOptions options, CancellationToken cancellationToken)
    {
        var tokenLine = document.Text.Lines.GetLineFromPosition(token.SpanStart);
        if (tokenLine.Start != token.SpanStart)
        {
            var firstNonWhitespacePos = tokenLine.GetFirstNonWhitespacePosition();
            Contract.ThrowIfNull(firstNonWhitespacePos);
            if (firstNonWhitespacePos.Value == token.SpanStart)
            {
                // token was on it's own line.  Start the end delimiter at the same location as it.
                return document.Text.ToString(TextSpan.FromBounds(tokenLine.Start, token.SpanStart));
            }
        }

        // Token was on a line with something else.  Determine where we would indent the token if it was on the next
        // line and use that to determine the indentation of the final line.

        var annotation = new SyntaxAnnotation();
        var newToken = token.WithAdditionalAnnotations(annotation);

        var syntaxGenerator = document.LanguageServices.GetRequiredService<SyntaxGeneratorInternal>();
        newToken = newToken.WithLeadingTrivia(newToken.LeadingTrivia.Add(syntaxGenerator.EndOfLine(options.FormattingOptions.NewLine)));

        var newRoot = document.Root.ReplaceToken(token, newToken);
        var newDocument = document.WithChangedRoot(newRoot, cancellationToken);

        var newTokenLine = newDocument.Text.Lines.GetLineFromPosition(newRoot.GetAnnotatedTokens(annotation).Single().SpanStart);

        var indenter = document.LanguageServices.GetRequiredService<IIndentationService>();
        var indentation = indenter.GetIndentation(newDocument, newTokenLine.LineNumber, options, cancellationToken);

        return indentation.GetIndentationString(newDocument.Text, options);
    }
}

internal static class IndentationResultExtensions
{
    public static string GetIndentationString(this IndentationResult indentationResult, SourceText sourceText, bool useTabs, int tabSize)
    {
        var baseLine = sourceText.Lines.GetLineFromPosition(indentationResult.BasePosition);
        var baseOffsetInLine = indentationResult.BasePosition - baseLine.Start;

        var indent = baseOffsetInLine + indentationResult.Offset;

        var indentString = indent.CreateIndentationString(useTabs, tabSize);
        return indentString;
    }

    public static string GetIndentationString(this IndentationResult indentationResult, SourceText sourceText, SyntaxFormattingOptions options)
        => GetIndentationString(indentationResult, sourceText, options.UseTabs, options.TabSize);

    public static string GetIndentationString(this IndentationResult indentationResult, SourceText sourceText, IndentationOptions options)
        => GetIndentationString(indentationResult, sourceText, options.FormattingOptions);
}
