// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(SelectionRangeHandler)), Shared]
[Method(Methods.TextDocumentSelectionRangeName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SelectionRangeHandler() : ILspServiceDocumentRequestHandler<SelectionRangeParams, SelectionRange[]?>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(SelectionRangeParams request) => request.TextDocument;

    public async Task<SelectionRange[]?> HandleRequestAsync(SelectionRangeParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;
        if (document is null)
            return null;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<SelectionRange>.GetInstance(out var results);
        foreach (var position in request.Positions)
        {
            var linePosition = ProtocolConversions.PositionToLinePosition(position);
            var absolutePosition = text.Lines.GetPosition(linePosition);

            results.Add(GetSelectionRange(root, text, absolutePosition));
        }

        return results.ToArray();
    }

    private static SelectionRange GetSelectionRange(SyntaxNode root, SourceText text, int position)
    {
        // FindToken().Parent is null only for EOF tokens at the compilation unit level;
        // falling back to root ensures we still return a valid selection range in that case.
        var node = root.FindToken(position).Parent ?? root;

        // Collect spans from innermost to outermost, deduplicating consecutive equal spans.
        using var _ = ArrayBuilder<TextSpan>.GetInstance(out var spans);
        var previousSpan = (TextSpan?)null;
        foreach (var ancestor in node.AncestorsAndSelf())
        {
            var span = ancestor.Span;

            // Skip nodes with empty spans and deduplicate nodes that cover the same span.
            if (span.IsEmpty || span == previousSpan)
                continue;

            spans.Add(span);
            previousSpan = span;
        }

        // Build the SelectionRange linked list from outermost to innermost so that each
        // SelectionRange's Parent refers to a larger enclosing range, as the LSP spec requires.
        SelectionRange? current = null;
        for (var i = spans.Count - 1; i >= 0; i--)
        {
            current = new SelectionRange
            {
                Range = ProtocolConversions.TextSpanToRange(spans[i], text),
                Parent = current
            };
        }

        // If we somehow ended up with nothing (e.g. empty file), return an empty range at the position.
        return current ?? new SelectionRange { Range = ProtocolConversions.TextSpanToRange(new TextSpan(position, 0), text) };
    }
}
