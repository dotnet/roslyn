// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class ConstructorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<ConstructorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            SyntaxToken previousToken,
            ConstructorDeclarationSyntax constructorDeclaration,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptions options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(constructorDeclaration, ref spans, options);

            // fault tolerance
            if (constructorDeclaration.Body == null ||
                constructorDeclaration.Body.OpenBraceToken.IsMissing ||
                constructorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            SyntaxNodeOrToken current = constructorDeclaration;
            var nextSibling = current.GetNextSibling();

            // Check IsNode to compress blank lines after this node if it is the last child of the parent.
            //
            // Whitespace between constructors is collapsed in Metadata as Source.
            var compressEmptyLines = options.IsMetadataAsSource
                && (!nextSibling.IsNode || nextSibling.IsKind(SyntaxKind.ConstructorDeclaration));

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                constructorDeclaration,
                constructorDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                compressEmptyLines: compressEmptyLines,
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
