﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class AccessorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<AccessorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            AccessorDeclarationSyntax accessorDeclaration,
            ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(accessorDeclaration, spans, isMetadataAsSource);

            // fault tolerance
            if (accessorDeclaration.Body == null ||
                accessorDeclaration.Body.OpenBraceToken.IsMissing ||
                accessorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            SyntaxNodeOrToken current = accessorDeclaration;
            var nextSibling = current.GetNextSibling();

            // Check IsNode to compress blank lines after this node if it is the last child of the parent.
            //
            // All accessor kinds are grouped together in Metadata as Source.
            var compressEmptyLines = isMetadataAsSource
                && (!nextSibling.IsNode || nextSibling.AsNode() is AccessorDeclarationSyntax);

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                accessorDeclaration,
                accessorDeclaration.Keyword,
                compressEmptyLines: compressEmptyLines,
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
