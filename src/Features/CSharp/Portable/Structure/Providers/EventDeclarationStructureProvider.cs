// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class EventDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<EventDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            EventDeclarationSyntax eventDeclaration,
            ImmutableArray<BlockSpan>.Builder spans,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentRegions(eventDeclaration, spans);

            // fault tolerance
            if (eventDeclaration.AccessorList.IsMissing ||
                eventDeclaration.AccessorList.OpenBraceToken.IsMissing ||
                eventDeclaration.AccessorList.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpStructureHelpers.CreateRegion(
                eventDeclaration,
                eventDeclaration.Identifier,
                autoCollapse: true));
        }
    }
}