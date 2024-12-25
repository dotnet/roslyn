// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

/// <summary>
/// Adds structure guides for the portions of a #if directive that are active.  The inactive sections already have
/// structure guides added through <see cref="DisabledTextTriviaStructureProvider"/>.
/// </summary>
internal sealed class IfDirectiveTriviaStructureProvider : AbstractSyntaxNodeStructureProvider<IfDirectiveTriviaSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        IfDirectiveTriviaSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        var allRelatedDirectives = node.GetRelatedDirectives();
        SourceText? text = null;

        for (int i = 0, n = allRelatedDirectives.Count - 1; i < n; i++)
        {
            var directive = allRelatedDirectives[i];
            if (directive is not BranchingDirectiveTriviaSyntax branchingDirective)
                continue;

            // if the branch isn't taken, we'll have disabled text.  That's handled by DisabledTextTriviaStructureProvider
            if (!branchingDirective.BranchTaken)
                continue;

            var nextDirective = allRelatedDirectives[i + 1];
            text ??= node.SyntaxTree.GetText(cancellationToken);

            var startLineNumber = text.Lines.GetLineFromPosition(directive.SpanStart).LineNumber + 1;
            var endLineNumber = text.Lines.GetLineFromPosition(nextDirective.SpanStart).LineNumber - 1;
            if (startLineNumber >= endLineNumber)
                continue;

            if (startLineNumber >= text.Lines.Count || endLineNumber < 0)
                continue;

            var startLine = text.Lines[startLineNumber];
            var endLine = text.Lines[endLineNumber];

            var span = TextSpan.FromBounds(startLine.Start, endLine.End);
            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: span,
                type: BlockTypes.PreprocessorRegion,
                bannerText: CSharpStructureHelpers.Ellipsis,
                autoCollapse: false));
        }
    }
}
