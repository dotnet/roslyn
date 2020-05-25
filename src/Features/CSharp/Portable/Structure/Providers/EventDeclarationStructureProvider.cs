// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class EventDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<EventDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            EventDeclarationSyntax eventDeclaration,
            ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(eventDeclaration, spans, isMetadataAsSource);

            // fault tolerance
            if (eventDeclaration.AccessorList == null ||
                eventDeclaration.AccessorList.IsMissing ||
                eventDeclaration.AccessorList.OpenBraceToken.IsMissing ||
                eventDeclaration.AccessorList.CloseBraceToken.IsMissing)
            {
                return;
            }

            SyntaxNodeOrToken current = eventDeclaration;
            var nextSibling = current.GetNextSibling();

            // Check IsNode to compress blank lines after this node if it is the last child of the parent.
            //
            // Full events are grouped together with event field definitions in Metadata as Source.
            var compressEmptyLines = isMetadataAsSource
                && (!nextSibling.IsNode || nextSibling.IsKind(SyntaxKind.EventDeclaration) || nextSibling.IsKind(SyntaxKind.EventFieldDeclaration));

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                eventDeclaration,
                eventDeclaration.Identifier,
                compressEmptyLines: compressEmptyLines,
                autoCollapse: true,
                type: BlockTypes.Member,
                isCollapsible: true));
        }
    }
}
