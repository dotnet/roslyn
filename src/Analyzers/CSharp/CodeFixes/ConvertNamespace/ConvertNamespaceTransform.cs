// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertNamespace
{
    internal static class ConvertNamespaceTransform
    {
        public static async Task<Document> ConvertAsync(Document document, BaseNamespaceDeclarationSyntax baseNamespace, SyntaxFormattingOptions options, CancellationToken cancellationToken)
        {
            switch (baseNamespace)
            {
                case FileScopedNamespaceDeclarationSyntax fileScopedNamespace:
                    return await ConvertFileScopedNamespaceAsync(document, fileScopedNamespace, cancellationToken).ConfigureAwait(false);

                case NamespaceDeclarationSyntax namespaceDeclaration:
                    return await ConvertNamespaceDeclarationAsync(document, namespaceDeclaration, options, cancellationToken).ConfigureAwait(false);

                default:
                    throw ExceptionUtilities.UnexpectedValue(baseNamespace.Kind());
            }
        }

        /// <summary>
        /// Asynchrounous implementation for code fixes.
        /// </summary>
        public static async ValueTask<Document> ConvertNamespaceDeclarationAsync(Document document, NamespaceDeclarationSyntax namespaceDeclaration, SyntaxFormattingOptions options, CancellationToken cancellationToken)
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

            using var _ = ArrayBuilder<TextChange>.GetInstance(out var changes);
            for (var line = semicolonLine + 1; line < text.Lines.Count; line++)
                changes.AddIfNotNull(TryDedentLine(syntaxTree, text, indentation, text.Lines[line], cancellationToken));

            var dedentedText = text.WithChanges(changes);
            return (dedentedText, fileScopedNamespace.SemicolonToken.Span);
        }

        private static TextChange? TryDedentLine(
            SyntaxTree tree, SourceText text, string indentation, TextLine textLine, CancellationToken cancellationToken)
        {
            // if this line is inside a string-literal or interpolated-text-content, then we definitely do not want to
            // touch what is inside there.  Note: this will not apply to raw-string literals, which can potentially be
            // dedented safely depending on the position of their close terminator.
            if (tree.IsEntirelyWithinStringLiteral(textLine.Span.Start, cancellationToken))
                return null;

            // Determine the amount of indentation this text line starts with.
            var commonIndentation = 0;
            while (commonIndentation < indentation.Length && commonIndentation < textLine.Span.Length)
            {
                if (indentation[commonIndentation] != text[textLine.Start + commonIndentation])
                    break;

                commonIndentation++;
            }

            return new TextChange(new TextSpan(textLine.Start, commonIndentation), newText: "");
        }

        public static async Task<Document> ConvertFileScopedNamespaceAsync(
            Document document, FileScopedNamespaceDeclarationSyntax fileScopedNamespace, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return document.WithSyntaxRoot(root.ReplaceNode(fileScopedNamespace, ConvertFileScopedNamespace(fileScopedNamespace)));
        }

        private static bool HasLeadingBlankLine(
            SyntaxToken token, out SyntaxToken withoutBlankLine)
        {
            var leadingTrivia = token.LeadingTrivia;

            if (leadingTrivia.Count >= 1 && leadingTrivia[0].Kind() == SyntaxKind.EndOfLineTrivia)
            {
                withoutBlankLine = token.WithLeadingTrivia(leadingTrivia.RemoveAt(0));
                return true;
            }

            if (leadingTrivia.Count >= 2 && leadingTrivia[0].IsKind(SyntaxKind.WhitespaceTrivia) && leadingTrivia[1].IsKind(SyntaxKind.EndOfLineTrivia))
            {
                withoutBlankLine = token.WithLeadingTrivia(leadingTrivia.Skip(2));
                return true;
            }

            withoutBlankLine = default;
            return false;
        }

        private static FileScopedNamespaceDeclarationSyntax ConvertNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
        {
            var semiColon = SyntaxFactory.Token(SyntaxKind.SemicolonToken).WithoutTrivia();

            // Move trivia after the original name token to now be after the new semicolon token.
            var fileScopedNamespace = SyntaxFactory.FileScopedNamespaceDeclaration(
                namespaceDeclaration.AttributeLists,
                namespaceDeclaration.Modifiers,
                namespaceDeclaration.NamespaceKeyword,
                namespaceDeclaration.Name.WithoutTrailingTrivia(),
                semiColon.WithTrailingTrivia(namespaceDeclaration.Name.GetTrailingTrivia()),
                namespaceDeclaration.Externs,
                namespaceDeclaration.Usings,
                namespaceDeclaration.Members);

            var firstBodyToken = fileScopedNamespace.SemicolonToken.GetNextToken();

            // If the open-brace token has any special trivia, then move them to before the first member in the namespace.
            if (namespaceDeclaration.OpenBraceToken.LeadingTrivia.Any(t => t.IsSingleOrMultiLineComment() || t.IsDirective) ||
                namespaceDeclaration.OpenBraceToken.TrailingTrivia.Any(t => t.IsSingleOrMultiLineComment() || t.IsDirective))
            {
                fileScopedNamespace = fileScopedNamespace.ReplaceToken(
                    firstBodyToken,
                    firstBodyToken.WithPrependedLeadingTrivia(namespaceDeclaration.OpenBraceToken.GetAllTrivia()));
                firstBodyToken = fileScopedNamespace.SemicolonToken.GetNextNonZeroWidthTokenOrEndOfFile();
            }

            // Otherwise, ensure there's a blank line between the namespace line and the first body member. Don't bother
            // with this though if we already separated things by moving a pp directive (like a #else) from the open brace
            // to the first token.
            if (firstBodyToken.Kind() != SyntaxKind.EndOfFileToken &&
                !HasLeadingBlankLine(firstBodyToken, out _) &&
                !namespaceDeclaration.OpenBraceToken.LeadingTrivia.Any(t => t.IsDirective))
            {
                fileScopedNamespace = fileScopedNamespace.ReplaceToken(
                    firstBodyToken,
                    firstBodyToken.WithPrependedLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed));
            }

            // Copy leading trivia from the close brace to the end of the file scoped namespace (which means after all of the members)
            fileScopedNamespace = fileScopedNamespace.WithAppendedTrailingTrivia(namespaceDeclaration.CloseBraceToken.LeadingTrivia);

            // If the previous namespace declaration had no trailing trivia and the last member only has a newline, then assume the user
            // doesn't want that newline any more.
            if (!namespaceDeclaration.HasTrailingTrivia &&
                namespaceDeclaration.CloseBraceToken.GetPreviousToken() is var lastMemberToken &&
                lastMemberToken.TrailingTrivia is [{ RawKind: (int)SyntaxKind.EndOfLineTrivia }])
            {
                fileScopedNamespace = fileScopedNamespace.WithoutTrailingTrivia();
            }

            return fileScopedNamespace;
        }

        private static NamespaceDeclarationSyntax ConvertFileScopedNamespace(FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
        {
            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                fileScopedNamespace.AttributeLists,
                fileScopedNamespace.Modifiers,
                fileScopedNamespace.NamespaceKeyword,
                fileScopedNamespace.Name,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(fileScopedNamespace.SemicolonToken.TrailingTrivia),
                fileScopedNamespace.Externs,
                fileScopedNamespace.Usings,
                fileScopedNamespace.Members,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                semicolonToken: default).WithAdditionalAnnotations(Formatter.Annotation);

            // Ensure there is no errant blank line between the open curly and the first body element.
            var firstBodyToken = namespaceDeclaration.OpenBraceToken.GetNextToken();
            if (firstBodyToken != namespaceDeclaration.CloseBraceToken &&
                firstBodyToken.Kind() != SyntaxKind.EndOfFileToken &&
                HasLeadingBlankLine(firstBodyToken, out var firstBodyTokenWithoutBlankLine))
            {
                namespaceDeclaration = namespaceDeclaration.ReplaceToken(firstBodyToken, firstBodyTokenWithoutBlankLine);
            }

            return namespaceDeclaration;
        }
    }
}
