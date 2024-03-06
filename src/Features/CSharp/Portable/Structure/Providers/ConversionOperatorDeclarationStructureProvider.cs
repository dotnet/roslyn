// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal class ConversionOperatorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<ConversionOperatorDeclarationSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        ConversionOperatorDeclarationSyntax operatorDeclaration,
        ref TemporaryArray<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        CSharpStructureHelpers.CollectCommentBlockSpans(operatorDeclaration, ref spans, options);

        // fault tolerance
        if (operatorDeclaration.Body == null ||
            operatorDeclaration.Body.OpenBraceToken.IsMissing ||
            operatorDeclaration.Body.CloseBraceToken.IsMissing)
        {
            return;
        }

        SyntaxNodeOrToken current = operatorDeclaration;
        var nextSibling = current.GetNextSibling();

        // Check IsNode to compress blank lines after this node if it is the last child of the parent.
        //
        // Whitespace between conversion operators is collapsed in Metadata as Source.
        var compressEmptyLines = options.IsMetadataAsSource
            && (!nextSibling.IsNode || nextSibling.IsKind(SyntaxKind.ConversionOperatorDeclaration));

        spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
            operatorDeclaration,
            operatorDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
            compressEmptyLines: compressEmptyLines,
            autoCollapse: true,
            type: BlockTypes.Member,
            isCollapsible: true));
    }
}
