// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class ConvertNamespaceTransform
{
    public static Task<Document> ConvertAsync(Document document, BaseNamespaceDeclarationSyntax baseNamespace, CSharpSyntaxFormattingOptions options, CancellationToken cancellationToken)
        => baseNamespace switch
        {
            FileScopedNamespaceDeclarationSyntax fileScopedNamespace => ConvertFileScopedNamespaceAsync(document, fileScopedNamespace, options, cancellationToken),
            NamespaceDeclarationSyntax namespaceDeclaration => ConvertNamespaceDeclarationAsync(document, namespaceDeclaration, options, cancellationToken),
            _ => throw ExceptionUtilities.UnexpectedValue(baseNamespace.Kind()),
        };

    /// <summary>
    /// Asynchronous implementation for code fixes.
    /// </summary>
    public static async Task<Document> ConvertNamespaceDeclarationAsync(Document document, NamespaceDeclarationSyntax namespaceDeclaration, SyntaxFormattingOptions options, CancellationToken cancellationToken)
    {
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Replace the block namespace with the file scoped namespace.
        var annotation = new SyntaxAnnotation();
        var (updatedRoot, _) = ReplaceWithFileScopedNamespace(parsedDocument, namespaceDeclaration, annotation);
        var updatedDocument = document.WithSyntaxRoot(updatedRoot);

        // Determine how much indentation we had inside the original block namespace. We'll attempt to remove
        // that much indentation from each applicable line after we conver the block namespace to a file scoped
        // namespace.
        var indentation = GetIndentation(parsedDocument, namespaceDeclaration, options, cancellationToken);
        if (indentation == null)
            return updatedDocument;

        // Now, find the file scoped namespace in the updated doc and go and dedent every line if applicable.
        var updatedParsedDocument = await ParsedDocument.CreateAsync(updatedDocument, cancellationToken).ConfigureAwait(false);
        var (dedentedText, _) = DedentNamespace(updatedParsedDocument, indentation, annotation, cancellationToken);
        return document.WithText(dedentedText);
    }

    /// <summary>
    /// Synchronous implementation for a command handler.
    /// </summary>
    public static (SourceText text, TextSpan semicolonSpan) ConvertNamespaceDeclaration(ParsedDocument document, NamespaceDeclarationSyntax namespaceDeclaration, SyntaxFormattingOptions options, CancellationToken cancellationToken)
    {
        // Replace the block namespace with the file scoped namespace.
        var annotation = new SyntaxAnnotation();
        var (updatedRoot, semicolonSpan) = ReplaceWithFileScopedNamespace(document, namespaceDeclaration, annotation);
        var updatedDocument = document.WithChangedRoot(updatedRoot, cancellationToken);

        // Determine how much indentation we had inside the original block namespace. We'll attempt to remove
        // that much indentation from each applicable line after we conver the block namespace to a file scoped
        // namespace.

        var indentation = GetIndentation(document, namespaceDeclaration, options, cancellationToken);
        if (indentation == null)
            return (updatedDocument.Text, semicolonSpan);

        // Now, find the file scoped namespace in the updated doc and go and dedent every line if applicable.
        return DedentNamespace(updatedDocument, indentation, annotation, cancellationToken);
    }

    private static (SyntaxNode root, TextSpan semicolonSpan) ReplaceWithFileScopedNamespace(
        ParsedDocument document, NamespaceDeclarationSyntax namespaceDeclaration, SyntaxAnnotation annotation)
    {
        var converted = ConvertNamespaceDeclaration(namespaceDeclaration);
        var updatedRoot = document.Root.ReplaceNode(
            namespaceDeclaration,
            converted.WithAdditionalAnnotations(annotation));
        var fileScopedNamespace = (FileScopedNamespaceDeclarationSyntax)updatedRoot.GetAnnotatedNodes(annotation).Single();
        return (updatedRoot, fileScopedNamespace.SemicolonToken.Span);
    }

    private static string? GetIndentation(ParsedDocument document, NamespaceDeclarationSyntax namespaceDeclaration, SyntaxFormattingOptions options, CancellationToken cancellationToken)
    {
        var openBraceLine = document.Text.Lines.GetLineFromPosition(namespaceDeclaration.OpenBraceToken.SpanStart).LineNumber;
        var closeBraceLine = document.Text.Lines.GetLineFromPosition(namespaceDeclaration.CloseBraceToken.SpanStart).LineNumber;
        if (openBraceLine == closeBraceLine)
            return null;

        // Auto-formatting options are not relevant since they only control behavior on typing.
        var indentationOptions = new IndentationOptions(options);

        var indentationService = document.LanguageServices.GetRequiredService<IIndentationService>();
        var indentation = indentationService.GetIndentation(document, openBraceLine + 1, indentationOptions, cancellationToken);

        return indentation.GetIndentationString(document.Text, options.UseTabs, options.TabSize);
    }

    private static (SourceText text, TextSpan semicolonSpan) DedentNamespace(
        ParsedDocument document, string indentation, SyntaxAnnotation annotation, CancellationToken cancellationToken)
    {
        var syntaxTree = document.SyntaxTree;
        var text = document.Text;
        var root = document.Root;

        var fileScopedNamespace = (FileScopedNamespaceDeclarationSyntax)root.GetAnnotatedNodes(annotation).Single();
        var semicolonLine = text.Lines.GetLineFromPosition(fileScopedNamespace.SemicolonToken.SpanStart).LineNumber;

        // Cache what we compute so we don't have to recompute it for every line of a raw string literal.
        (SyntaxNode stringNode, int closeTerminatorIndentationLength) lastRawStringLiteralData = default;

        using var _ = ArrayBuilder<TextChange>.GetInstance(out var changes);
        for (var line = semicolonLine + 1; line < text.Lines.Count; line++)
            changes.AddIfNotNull(TryDedentLine(text.Lines[line]));

        var dedentedText = text.WithChanges(changes);
        return (dedentedText, fileScopedNamespace.SemicolonToken.Span);

        TextChange? TryDedentLine(TextLine textLine)
        {
            // if this line is inside a string-literal or interpolated-text-content, then we definitely do not want to
            // touch what is inside there.  Note: this will not apply to raw-string literals, which can potentially be
            // dedented safely depending on the position of their close terminator.
            if (syntaxTree.IsEntirelyWithinStringLiteral(textLine.Span.Start, out var stringLiteral, cancellationToken))
            {
                SyntaxNode stringNode;
                if (stringLiteral.Kind() is SyntaxKind.InterpolatedStringTextToken)
                {
                    if (stringLiteral.GetRequiredParent() is not InterpolatedStringTextSyntax { Parent: InterpolatedStringExpressionSyntax { StringStartToken: (kind: SyntaxKind.InterpolatedMultiLineRawStringStartToken) } interpolatedString })
                        return null;

                    stringNode = interpolatedString;
                }
                else if (stringLiteral.Kind() is SyntaxKind.InterpolatedRawStringEndToken)
                {
                    if (stringLiteral.GetRequiredParent() is not InterpolatedStringExpressionSyntax { StringStartToken: (kind: SyntaxKind.InterpolatedMultiLineRawStringStartToken) } interpolatedString)
                        return null;

                    stringNode = interpolatedString;
                }
                else if (stringLiteral.Kind() is SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken)
                {
                    stringNode = stringLiteral.GetRequiredParent();
                }
                else
                {
                    return null;
                }

                // Don't touch the raw string if it already has issues.
                if (stringNode.ContainsDiagnostics)
                    return null;

                // Ok, only dedent the raw string contents if we can dedent the closing terminator of the raw string.
                if (lastRawStringLiteralData.stringNode != stringNode)
                    lastRawStringLiteralData = (stringNode, ComputeCommonIndentationLength(text.Lines.GetLineFromPosition(stringNode.Span.End)));

                // If we can't dedent the close terminator the right amount, don't dedent any contents.
                if (lastRawStringLiteralData.closeTerminatorIndentationLength != indentation.Length)
                    return null;
            }

            // Determine the amount of indentation this text line starts with.

            return new TextChange(
                new TextSpan(textLine.Start, ComputeCommonIndentationLength(textLine)),
                newText: "");
        }

        int ComputeCommonIndentationLength(TextLine textLine)
        {
            var commonIndentation = 0;
            while (commonIndentation < indentation.Length && commonIndentation < textLine.Span.Length)
            {
                if (indentation[commonIndentation] != text[textLine.Start + commonIndentation])
                    break;

                commonIndentation++;
            }

            return commonIndentation;
        }
    }

    private static SourceText IndentNamespace(
        ParsedDocument document, string indentation, SyntaxAnnotation annotation, CancellationToken cancellationToken)
    {
        var syntaxTree = document.SyntaxTree;
        var text = document.Text;
        var root = document.Root;

        var blockScopedNamespace = (NamespaceDeclarationSyntax)root.GetAnnotatedNodes(annotation).Single();
        var openBraceLine = text.Lines.GetLineFromPosition(blockScopedNamespace.OpenBraceToken.SpanStart).LineNumber;
        var closeBraceLine = text.Lines.GetLineFromPosition(blockScopedNamespace.CloseBraceToken.SpanStart).LineNumber;

        using var _ = ArrayBuilder<TextChange>.GetInstance(Math.Max(0, closeBraceLine - openBraceLine - 1), out var changes);
        for (var line = openBraceLine + 1; line < closeBraceLine; line++)
            changes.AddIfNotNull(TryIndentLine(syntaxTree, root, indentation, text.Lines[line], cancellationToken));

        var dedentedText = text.WithChanges(changes);
        return dedentedText;
    }

    private static TextChange? TryIndentLine(
        SyntaxTree tree, SyntaxNode root, string indentation, TextLine textLine, CancellationToken cancellationToken)
    {
        if (textLine.IsEmptyOrWhitespace())
            return null;

        // if this line is inside a string-literal or interpolated-text-content, then we definitely do not want to
        // touch what is inside there.  Note: this will not apply to raw-string literals, which can be indented
        // safely.
        if (tree.IsEntirelyWithinStringLiteral(textLine.Span.Start, cancellationToken))
            return null;

        if (textLine.Text![textLine.Start] == '#')
        {
            var token = root.FindToken(textLine.Start, findInsideTrivia: true);
            if (token.IsKind(SyntaxKind.HashToken) && token.Parent!.Kind() is not (SyntaxKind.RegionDirectiveTrivia or SyntaxKind.EndRegionDirectiveTrivia))
            {
                // only #region and #endregion get indented
                return null;
            }
        }

        return new TextChange(new TextSpan(textLine.Start, 0), newText: indentation);
    }

    public static async Task<Document> ConvertFileScopedNamespaceAsync(
        Document document, FileScopedNamespaceDeclarationSyntax fileScopedNamespace, CSharpSyntaxFormattingOptions options, CancellationToken cancellationToken)
    {
        var parsedDocument = await ParsedDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // Replace the block namespace with the file scoped namespace.
        var annotation = new SyntaxAnnotation();
        var updatedRoot = ReplaceWithBlockScopedNamespace(parsedDocument, fileScopedNamespace, options.NewLine, options.NewLines, annotation);
        var updatedDocument = document.WithSyntaxRoot(updatedRoot);

        // Auto-formatting options are not relevant since they only control behavior on typing.
        var indentation = FormattingExtensions.CreateIndentationString(options.IndentationSize, options.UseTabs, options.TabSize);
        if (indentation == "")
            return updatedDocument;

        // Now, find the file scoped namespace in the updated doc and go and dedent every line if applicable.
        var updatedParsedDocument = await ParsedDocument.CreateAsync(updatedDocument, cancellationToken).ConfigureAwait(false);
        var indentedText = IndentNamespace(updatedParsedDocument, indentation, annotation, cancellationToken);
        return document.WithText(indentedText);
    }

    private static SyntaxNode ReplaceWithBlockScopedNamespace(
        ParsedDocument document, FileScopedNamespaceDeclarationSyntax namespaceDeclaration, string lineEnding, NewLinePlacement newLinePlacement, SyntaxAnnotation annotation)
    {
        var converted = ConvertFileScopedNamespace(document, namespaceDeclaration, lineEnding, newLinePlacement);

        // If the leading trivia of the token after the end of the file scoped namespace spans multiple lines, make
        // sure all the lines preceding the line with the next token are placed inside the block body namespace.
        var tokenAfterNamespace = namespaceDeclaration.GetLastToken(includeZeroWidth: true, includeSkipped: true).GetNextTokenOrEndOfFile(includeZeroWidth: true, includeSkipped: true);
        var lineWithNextToken = document.Text.Lines.GetLineFromPosition(tokenAfterNamespace.SpanStart);
        var (splitPosition, needsAdditionalLineEnding) = lineWithNextToken.GetFirstNonWhitespacePosition() < tokenAfterNamespace.SpanStart
            ? (tokenAfterNamespace.SpanStart, true)
            : (document.Text.Lines.GetLineFromPosition(tokenAfterNamespace.SpanStart).Start, false);
        var triviaBeforeSplit = tokenAfterNamespace.LeadingTrivia.TakeWhile(trivia => trivia.SpanStart < splitPosition).ToArray();
        var triviaAfterSplit = tokenAfterNamespace.LeadingTrivia.Skip(triviaBeforeSplit.Length).ToArray();

        if (triviaBeforeSplit.Length > 0)
        {
            if (needsAdditionalLineEnding)
                triviaBeforeSplit = triviaBeforeSplit.Append(EndOfLine(lineEnding));

            converted = converted.WithCloseBraceToken(converted.CloseBraceToken.WithPrependedLeadingTrivia(triviaBeforeSplit));
        }

        // If the block namespace starts with a blank line, remove one blank line as an adjustment relative to the
        // file scoped namespace. This check is performed here to account for cases where the token after the
        // opening brace is the closing brace token, and the leading newline for the closing brace token was
        // introduced by the trivia relocation above.
        var firstBodyToken = converted.OpenBraceToken.GetNextToken(includeZeroWidth: true, includeSkipped: true);
        if (firstBodyToken.Kind() != SyntaxKind.EndOfFileToken
            && HasLeadingBlankLine(firstBodyToken, out var firstBodyTokenWithoutBlankLine))
        {
            converted = converted.ReplaceToken(firstBodyToken, firstBodyTokenWithoutBlankLine);
        }

        return document.Root.ReplaceSyntax(
            [namespaceDeclaration],
            (_, _) => converted.WithAdditionalAnnotations(annotation),
            [tokenAfterNamespace],
            (_, _) => tokenAfterNamespace.WithLeadingTrivia(triviaAfterSplit),
            [],
            (_, _) => throw ExceptionUtilities.Unreachable());
    }

    private static bool HasLeadingBlankLine(
        SyntaxToken token, out SyntaxToken withoutBlankLine)
    {
        var leadingTrivia = token.LeadingTrivia;

        if (leadingTrivia is [(kind: SyntaxKind.EndOfLineTrivia), ..])
        {
            withoutBlankLine = token.WithLeadingTrivia(leadingTrivia.RemoveAt(0));
            return true;
        }

        if (leadingTrivia is [(kind: SyntaxKind.WhitespaceTrivia), (kind: SyntaxKind.EndOfLineTrivia), ..])
        {
            withoutBlankLine = token.WithLeadingTrivia(leadingTrivia.Skip(2));
            return true;
        }

        withoutBlankLine = default;
        return false;
    }

    private static FileScopedNamespaceDeclarationSyntax ConvertNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
    {
        // If the open-brace token has any special trivia, then move them to after the semicolon.
        var semiColon = SemicolonToken
            .WithoutTrivia()
            .WithTrailingTrivia(namespaceDeclaration.Name.GetTrailingTrivia())
            .WithAppendedTrailingTrivia(namespaceDeclaration.OpenBraceToken.LeadingTrivia);

        if (!namespaceDeclaration.OpenBraceToken.TrailingTrivia.All(static t => t.IsWhitespace()))
            semiColon = semiColon.WithAppendedTrailingTrivia(namespaceDeclaration.OpenBraceToken.TrailingTrivia);

        // Move trivia after the original name token to now be after the new semicolon token.
        var fileScopedNamespace = FileScopedNamespaceDeclaration(
            namespaceDeclaration.AttributeLists,
            namespaceDeclaration.Modifiers,
            namespaceDeclaration.NamespaceKeyword,
            namespaceDeclaration.Name.WithoutTrailingTrivia(),
            semiColon,
            namespaceDeclaration.Externs,
            namespaceDeclaration.Usings,
            namespaceDeclaration.Members);

        // Copy trivia from the close brace to the end of the file scoped namespace (which means after all of the members)
        fileScopedNamespace = fileScopedNamespace
            .WithAppendedTrailingTrivia(namespaceDeclaration.CloseBraceToken.LeadingTrivia)
            .WithAppendedTrailingTrivia(namespaceDeclaration.CloseBraceToken.TrailingTrivia);

        var originalHadTrailingNewLine = namespaceDeclaration.GetTrailingTrivia() is [.., (kind: SyntaxKind.EndOfLineTrivia)];

        // now, intelligently trim excess newlines to try to match what the original namespace looked like.
        while (fileScopedNamespace.HasTrailingTrivia)
        {
            var trailingTrivia = fileScopedNamespace.GetTrailingTrivia();

            // if the new namespace doesn't end with a newline, nothing for us to do.
            if (trailingTrivia is not [.., (kind: SyntaxKind.EndOfLineTrivia)])
                break;

            // if the original had a newline, then we only want to trim the newlines as long as there is still one
            // left at the end.

            if (originalHadTrailingNewLine && trailingTrivia is not
                [
                    ..,
                    (kind: SyntaxKind.EndOfLineTrivia or SyntaxKind.EndIfDirectiveTrivia or SyntaxKind.EndRegionDirectiveTrivia),
                    (kind: SyntaxKind.EndOfLineTrivia)
                ])
            {
                break;
            }

            // New namespace has excess newlines, remove the last one and try again.
            fileScopedNamespace = fileScopedNamespace.WithTrailingTrivia(
                trailingTrivia.Take(trailingTrivia.Count - 1));
        }

        return fileScopedNamespace;
    }

    private static NamespaceDeclarationSyntax ConvertFileScopedNamespace(ParsedDocument document, FileScopedNamespaceDeclarationSyntax fileScopedNamespace, string lineEnding, NewLinePlacement newLinePlacement)
    {
        var nameSyntax = fileScopedNamespace.Name.WithAppendedTrailingTrivia(fileScopedNamespace.SemicolonToken.LeadingTrivia)
            .WithAppendedTrailingTrivia(newLinePlacement.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes) ? EndOfLine(lineEnding) : Space);
        var openBraceToken = OpenBraceToken.WithoutLeadingTrivia().WithTrailingTrivia(fileScopedNamespace.SemicolonToken.TrailingTrivia);

        if (openBraceToken.TrailingTrivia is not [.., SyntaxTrivia(SyntaxKind.EndOfLineTrivia)])
        {
            openBraceToken = openBraceToken.WithAppendedTrailingTrivia(EndOfLine(lineEnding));
        }

        FileScopedNamespaceDeclarationSyntax adjustedFileScopedNamespace;
        var closeBraceToken = CloseBraceToken.WithoutLeadingTrivia().WithoutTrailingTrivia();

        // Normally the block scoped namespace will have a newline after the closing brace. The only exception to
        // this occurs when there are no tokens after the closing brace and the document with a file scoped
        // namespace did not end in a trailing newline. For this case, we want the converted block scope namespace
        // to also terminate without a final newline.
        if (!fileScopedNamespace.GetLastToken().GetNextTokenOrEndOfFile().IsKind(SyntaxKind.EndOfFileToken)
            || document.Text.Lines.GetLinePosition(document.Text.Length).Character == 0)
        {
            closeBraceToken = closeBraceToken.WithAppendedTrailingTrivia(EndOfLine(lineEnding));
            adjustedFileScopedNamespace = fileScopedNamespace;
        }
        else
        {
            // Make sure the body of the file scoped namespace ends with a trailing new line (so the closing brace
            // of the converted block-body namespace appears on its own line), but don't add a new line after the
            // closing brace.
            adjustedFileScopedNamespace = fileScopedNamespace.WithAppendedTrailingTrivia(EndOfLine(lineEnding));
        }

        // If the file scoped namespace is indented, also indent the newly added braces to match
        var outerIndentation = document.Text.GetLeadingWhitespaceOfLineAtPosition(fileScopedNamespace.SpanStart);
        if (outerIndentation.Length > 0)
        {
            if (newLinePlacement.HasFlag(NewLinePlacement.BeforeOpenBraceInTypes))
                openBraceToken = openBraceToken.WithLeadingTrivia(openBraceToken.LeadingTrivia.Add(Whitespace(outerIndentation)));

            closeBraceToken = closeBraceToken.WithLeadingTrivia(closeBraceToken.LeadingTrivia.Add(Whitespace(outerIndentation)));
        }

        var namespaceDeclaration = NamespaceDeclaration(
            adjustedFileScopedNamespace.AttributeLists,
            adjustedFileScopedNamespace.Modifiers,
            adjustedFileScopedNamespace.NamespaceKeyword,
            nameSyntax,
            openBraceToken,
            adjustedFileScopedNamespace.Externs,
            adjustedFileScopedNamespace.Usings,
            adjustedFileScopedNamespace.Members,
            closeBraceToken,
            semicolonToken: default);

        return namespaceDeclaration;
    }
}
