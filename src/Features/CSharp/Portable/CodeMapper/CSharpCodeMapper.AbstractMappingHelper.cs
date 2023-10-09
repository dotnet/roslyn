// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeMapper;

internal sealed partial class CSharpCodeMapper
{
    private abstract class AbstractMappingHelper(DocumentSpan target, ImmutableArray<CSharpSourceNode> sourceNodes)
    {
        protected DocumentSpan Target { get; } = target;
        protected ImmutableArray<CSharpSourceNode> SourceNodes { get; } = sourceNodes;

        protected Document Document => Target.Document;
        protected TextSpan TargetSpan => Target.SourceSpan;

        public async Task<ImmutableArray<TextChange>> MapCodeAsync(CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var mappedEdits);
            var targetRoot = await Document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            foreach (var sourceNode in GetValidInsertions(targetRoot, SourceNodes))
            {
                // When calculating the insert span, the insert text might suffer adjustments.
                // These adjustments will be visible in the adjustedInsertion, if it's not null that means
                // there were adjustments to the text when calculating the insert spans.

                var insertionSpan = GetInsertSpan(targetRoot, sourceNode, Target, out var adjustedNodeToMap);

                if (insertionSpan.HasValue)
                {
                    var nodeToMap = adjustedNodeToMap ?? sourceNode.Node;
                    var insertion = nodeToMap.ToFullString();
                    mappedEdits.Add(new(insertionSpan.Value, insertion));
                }
            }

            return mappedEdits.ToImmutable();
        }

        protected abstract ImmutableArray<CSharpSourceNode> GetValidInsertions(SyntaxNode target, ImmutableArray<CSharpSourceNode> sourceNodes);

        protected abstract TextSpan? GetInsertSpan(SyntaxNode documentSyntax, CSharpSourceNode insertion, DocumentSpan target, out SyntaxNode? adjustedNodeToMap);
    }
}
