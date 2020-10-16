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
    internal class PropertyDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<PropertyDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            PropertyDeclarationSyntax propertyDeclaration,
            ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(propertyDeclaration, spans, isMetadataAsSource);

            // fault tolerance
            if (propertyDeclaration.AccessorList == null ||
                propertyDeclaration.AccessorList.OpenBraceToken.IsMissing ||
                propertyDeclaration.AccessorList.CloseBraceToken.IsMissing)
            {
                return;
            }

            SyntaxNodeOrToken current = propertyDeclaration;
            var nextSibling = current.GetNextSibling();

            // Check IsNode to compress blank lines after this node if it is the last child of the parent.
            //
            // Properties are grouped together with indexers in Metadata as Source.
            var compressEmptyLines = isMetadataAsSource
                && (!nextSibling.IsNode || nextSibling.IsKind(SyntaxKind.PropertyDeclaration) || nextSibling.IsKind(SyntaxKind.IndexerDeclaration));

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                propertyDeclaration,
                propertyDeclaration.Identifier,
                compressEmptyLines: compressEmptyLines,
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
