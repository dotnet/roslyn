﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class EnumDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<EnumDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            EnumDeclarationSyntax enumDeclaration,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptionProvider optionProvider,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(enumDeclaration, ref spans, optionProvider);

            if (!enumDeclaration.OpenBraceToken.IsMissing &&
                !enumDeclaration.CloseBraceToken.IsMissing)
            {
                SyntaxNodeOrToken current = enumDeclaration;
                var nextSibling = current.GetNextSibling();

                // Check IsNode to compress blank lines after this node if it is the last child of the parent.
                //
                // Whitespace between type declarations is collapsed in Metadata as Source.
                var compressEmptyLines = optionProvider.IsMetadataAsSource
                    && (!nextSibling.IsNode || nextSibling.AsNode() is BaseTypeDeclarationSyntax);

                spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                    enumDeclaration,
                    enumDeclaration.Identifier,
                    compressEmptyLines: compressEmptyLines,
                    autoCollapse: false,
                    type: BlockTypes.Member,
                    isCollapsible: true));
            }

            // add any leading comments before the end of the type block
            if (!enumDeclaration.CloseBraceToken.IsMissing)
            {
                var leadingTrivia = enumDeclaration.CloseBraceToken.LeadingTrivia;
                CSharpStructureHelpers.CollectCommentBlockSpans(leadingTrivia, ref spans);
            }
        }
    }
}
