// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task<ImmutableArray<TextSpan>> GetStringIndentationSpansAsync(
            Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<TextSpan>.GetInstance(out var result);

            Recurse(text, root, textSpan, result, cancellationToken);

            return result.ToImmutable();
        }

        private void Recurse(
            SourceText text, SyntaxNode node, TextSpan textSpan, ArrayBuilder<TextSpan> result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!node.Span.IntersectsWith(textSpan))
                return;

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    Recurse(text, child.AsNode()!, textSpan, result, cancellationToken);
                }
                else
                {
                    ProcessToken(text, child.AsToken(), result, cancellationToken);
                }
            }
        }

        private static void ProcessToken(
            SourceText text, SyntaxToken token, ArrayBuilder<TextSpan> result, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
                return;

            // Ignore strings with errors as we don't want to draw a line in a bad place that makes things even harder
            // to understand.
            if (token.ContainsDiagnostics && token.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                return;

            // get the last line of the literal to determine the indentation string.
            var lastLine = text.Lines.GetLineFromPosition(token.Span.End);
            var offsetOpt = lastLine.GetFirstNonWhitespaceOffset();

            // We should always have a non-null offset in a multi-line raw string without errors.
            Contract.ThrowIfNull(offsetOpt);
            var offset = offsetOpt.Value;
            if (offset == 0)
                return;

            var firstLine = text.Lines.GetLineFromPosition(token.SpanStart);

            // A literal without errors must span at least three lines.  Like so:
            //      """
            //      foo
            //      """
            Contract.ThrowIfTrue(lastLine.LineNumber - firstLine.LineNumber < 2);
            result.Add(TextSpan.FromBounds(firstLine.Start, lastLine.Start + offset));
        }
    }
}
