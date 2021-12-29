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
            using var _ = ArrayBuilder<StringIndentationRegion>.GetInstance(out var result);

            Recurse(text, root, textSpan, result, cancellationToken);

            return result.ToImmutable();
        }

        private void Recurse(
            SourceText text, SyntaxNode node, TextSpan textSpan, ArrayBuilder<StringIndentationRegion> result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!node.Span.IntersectsWith(textSpan))
                return;

            if (node.IsKind(SyntaxKind.InterpolatedStringExpression, out InterpolatedStringExpressionSyntax? interpolatedString) &&
                interpolatedString.StringStartToken.IsKind(SyntaxKind.InterpolatedMultiLineRawStringStartToken))
            {
                .ProcessInterpolatedStringExpression(text, interpolatedString, result, cancellationToken);
            }

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                    Recurse(text, child.AsNode()!, textSpan, result, cancellationToken);
                else if (child.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
                    ProcessMultiLineRawStringLiteralToken(text, child.AsToken(), result, cancellationToken);
            }
        }

        private static void ProcessMultiLineRawStringLiteralToken(
            SourceText text, SyntaxToken token, ArrayBuilder<StringIndentationRegion> result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryGetIndentSpan(text, (ExpressionSyntax)token.GetRequiredParent(), out var indentSpan))
                return;

            result.Add(new StringIndentationRegion(indentSpan));
        }

        private static void ProcessInterpolatedStringExpression(SourceText text, InterpolatedStringExpressionSyntax interpolatedString, ArrayBuilder<StringIndentationRegion> result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryGetIndentSpan(text, interpolatedString, out var indentSpan))
                return;

            using var _ = ArrayBuilder<TextSpan>.GetInstance(out var builder);

            foreach (var content in interpolatedString.Contents)
            {
                if (content is InterpolationSyntax interpolation)
                    builder.Add(interpolation.Span);
            }

            result.Add(new StringIndentationRegion(indentSpan, builder.ToImmutable()));
        }

        private static bool TryGetIndentSpan(SourceText text, ExpressionSyntax expression, out TextSpan indentSpan)
        {
            indentSpan = default;
            // Ignore strings with errors as we don't want to draw a line in a bad place that makes things even harder
            // to understand.
            if (expression.ContainsDiagnostics && expression.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                return false;

            // get the last line of the literal to determine the indentation string.
            var lastLine = text.Lines.GetLineFromPosition(expression.Span.End);
            var offsetOpt = lastLine.GetFirstNonWhitespaceOffset();

            // We should always have a non-null offset in a multi-line raw string without errors.
            Contract.ThrowIfNull(offsetOpt);
            var offset = offsetOpt.Value;
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
