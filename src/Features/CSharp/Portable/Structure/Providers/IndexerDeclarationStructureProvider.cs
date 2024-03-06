// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal class IndexerDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<IndexerDeclarationSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        IndexerDeclarationSyntax indexerDeclaration,
        ref TemporaryArray<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        CSharpStructureHelpers.CollectCommentBlockSpans(indexerDeclaration, ref spans, options);

        // fault tolerance
        if (indexerDeclaration.AccessorList == null ||
            indexerDeclaration.AccessorList.IsMissing ||
            indexerDeclaration.AccessorList.OpenBraceToken.IsMissing ||
            indexerDeclaration.AccessorList.CloseBraceToken.IsMissing)
        {
            return;
        }

        SyntaxNodeOrToken current = indexerDeclaration;
        var nextSibling = current.GetNextSibling();

        // Check IsNode to compress blank lines after this node if it is the last child of the parent.
        //
        // Indexers are grouped together with properties in Metadata as Source.
        var compressEmptyLines = options.IsMetadataAsSource
            && (!nextSibling.IsNode || nextSibling.Kind() is SyntaxKind.IndexerDeclaration or SyntaxKind.PropertyDeclaration);

        spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
            indexerDeclaration,
            indexerDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
            compressEmptyLines: compressEmptyLines,
            autoCollapse: true,
            type: BlockTypes.Member,
            isCollapsible: true));
    }
}
