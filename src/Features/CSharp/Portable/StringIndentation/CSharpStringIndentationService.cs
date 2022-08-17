// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.StringIndentation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.LineSeparators
{
    [ExportLanguageService(typeof(IStringIndentationService), LanguageNames.CSharp), Shared]
    internal class CSharpStringIndentationService : IStringIndentationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpStringIndentationService()
        {
        }

        public async Task<ImmutableArray<StringIndentationRegion>> GetStringIndentationRegionsAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            using var _1 = ArrayBuilder<(SyntaxNode node, int nextChild)>.GetInstance(out var nodeStack);
            using var _2 = ArrayBuilder<StringIndentationRegion>.GetInstance(out var result);

            nodeStack.Push((root, 0));
            Recurse(text, nodeStack, textSpan, result, cancellationToken);

            return result.ToImmutable();
        }

        private static void Recurse(
            SourceText text, ArrayBuilder<(SyntaxNode node, int nextChild)> nodeStack, TextSpan textSpan, ArrayBuilder<StringIndentationRegion> result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (nodeStack.TryPop(out var nextNode))
            {
                var (node, nextChild) = nextNode;

                if (nextChild == 0)
                {
                    // This is the first time we are processing this node
                    if (!node.Span.IntersectsWith(textSpan))
                        continue;

                    if (node.IsKind(SyntaxKind.InterpolatedStringExpression, out InterpolatedStringExpressionSyntax? interpolatedString) &&
                        interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken))
                    {
                        ProcessInterpolatedStringExpression(text, interpolatedString, result, cancellationToken);
                    }
                }

                var childList = node.ChildNodesAndTokens();
                for (var i = nextChild; i < childList.Count; i++)
                {
                    var child = childList[i];
                    if (child.IsNode)
                    {
                        // If necessary, push the current node so we can pick back up after processing the child
                        if (i < childList.Count - 1)
                        {
                            nodeStack.Push((node, i + 1));
                        }

                        // Push the child for immediate processing
                        nodeStack.Push((child.AsNode()!, 0));

                        // Break out of the 'for' loop, which effectively continues the containing 'while' loop
                        break;
                    }
                    else if (child.IsKind(SyntaxKind.MultiLineRawStringLiteralToken) ||
                             child.IsKind(SyntaxKind.Utf8MultiLineRawStringLiteralToken))
                    {
                        ProcessMultiLineRawStringLiteralToken(text, child.AsToken(), result, cancellationToken);
                    }
                }
            }
        }

        private static void ProcessMultiLineRawStringLiteralToken(
            SourceText text, SyntaxToken token, ArrayBuilder<StringIndentationRegion> result, CancellationToken cancellationToken)
        {
            // Ignore strings with errors as we don't want to draw a line in a bad place that makes things even harder
            // to understand.
            if (token.ContainsDiagnostics && token.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                return;

            cancellationToken.ThrowIfCancellationRequested();
            if (!TryGetIndentSpan(text, (ExpressionSyntax)token.GetRequiredParent(), out _, out var indentSpan))
                return;

            result.Add(new StringIndentationRegion(indentSpan));
        }

        private static void ProcessInterpolatedStringExpression(SourceText text, InterpolatedStringExpressionSyntax interpolatedString, ArrayBuilder<StringIndentationRegion> result, CancellationToken cancellationToken)
        {
            // Ignore strings with errors as we don't want to draw a line in a bad place that makes things even harder
            // to understand.
            if (interpolatedString.ContainsDiagnostics)
            {
                var errors = interpolatedString.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);
                foreach (var error in errors)
                {
                    if (!IsInHole(interpolatedString, error.Location.SourceSpan))
                        return;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!TryGetIndentSpan(text, interpolatedString, out var offset, out var indentSpan))
                return;

            using var _ = ArrayBuilder<TextSpan>.GetInstance(out var builder);

            foreach (var content in interpolatedString.Contents)
            {
                if (content is InterpolationSyntax interpolation &&
                    !IgnoreInterpolation(text, offset, interpolation))
                {
                    builder.Add(interpolation.Span);
                }
            }

            result.Add(new StringIndentationRegion(indentSpan, builder.ToImmutable()));
        }

        private static bool IsInHole(InterpolatedStringExpressionSyntax interpolatedString, TextSpan sourceSpan)
        {
            foreach (var content in interpolatedString.Contents)
            {
                if (content is InterpolationSyntax && content.Span.Contains(sourceSpan))
                    return true;
            }

            return false;
        }

        private static bool IgnoreInterpolation(SourceText text, int offset, InterpolationSyntax interpolation)
        {
            // We can ignore the hole if all the content of it is after the region's indentation level.
            // In that case, it's fine to draw the line through the hole as it won't intersect any code
            // (or show up on the right side of the line).

            var holeStartLine = text.Lines.GetLineFromPosition(interpolation.SpanStart).LineNumber;
            var holeEndLine = text.Lines.GetLineFromPosition(interpolation.Span.End).LineNumber;

            for (var i = holeStartLine; i <= holeEndLine; i++)
            {
                var line = text.Lines[i];
                var currentLineOffset = line.GetFirstNonWhitespaceOffset();

                if (currentLineOffset != null && currentLineOffset < offset)
                    return false;
            }

            return true;
        }

        private static bool TryGetIndentSpan(SourceText text, ExpressionSyntax expression, out int offset, out TextSpan indentSpan)
        {
            indentSpan = default;

            // get the last line of the literal to determine the indentation string.
            var lastLine = text.Lines.GetLineFromPosition(expression.Span.End);
            var offsetOpt = lastLine.GetFirstNonWhitespaceOffset();

            // We should always have a non-null offset in a multi-line raw string without errors.
            Contract.ThrowIfNull(offsetOpt);
            offset = offsetOpt.Value;
            if (offset == 0)
                return false;

            var firstLine = text.Lines.GetLineFromPosition(expression.SpanStart);

            // A literal without errors must span at least three lines.  Like so:
            //      """
            //      foo
            //      """
            Contract.ThrowIfTrue(lastLine.LineNumber - firstLine.LineNumber < 2);
            indentSpan = TextSpan.FromBounds(firstLine.Start, lastLine.Start + offset);
            return true;
        }
    }
}
