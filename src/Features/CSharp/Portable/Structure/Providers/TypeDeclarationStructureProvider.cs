// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal class TypeDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<TypeDeclarationSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        TypeDeclarationSyntax typeDeclaration,
        ref TemporaryArray<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        CSharpStructureHelpers.CollectCommentBlockSpans(typeDeclaration, ref spans, options);

        if (!typeDeclaration.OpenBraceToken.IsMissing &&
            !typeDeclaration.CloseBraceToken.IsMissing)
        {
            var lastToken = typeDeclaration.TypeParameterList == null
                ? typeDeclaration.Identifier
                : typeDeclaration.TypeParameterList.GetLastToken(includeZeroWidth: true);

            SyntaxNodeOrToken current = typeDeclaration;
            var nextSibling = current.GetNextSibling();

            // Check IsNode to compress blank lines after this node if it is the last child of the parent.
            //
            // Collapse to Definitions doesn't collapse type nodes, but a Toggle All Outlining would collapse groups
            // of types to the compressed form of not showing blank lines. All kinds of types are grouped together
            // in Metadata as Source.
            var compressEmptyLines = options.IsMetadataAsSource
                && (!nextSibling.IsNode || nextSibling.AsNode() is BaseTypeDeclarationSyntax);

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                typeDeclaration,
                lastToken,
                compressEmptyLines: compressEmptyLines,
                autoCollapse: false,
                type: BlockTypes.Type,
                isCollapsible: true));
        }

        // add any leading comments before the end of the type block
        if (!typeDeclaration.CloseBraceToken.IsMissing)
        {
            var leadingTrivia = typeDeclaration.CloseBraceToken.LeadingTrivia;
            CSharpStructureHelpers.CollectCommentBlockSpans(leadingTrivia, ref spans);
        }
    }
}
