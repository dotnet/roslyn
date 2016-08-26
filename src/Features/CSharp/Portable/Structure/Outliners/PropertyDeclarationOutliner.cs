// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class PropertyDeclarationOutliner : AbstractSyntaxNodeStructureProvider<PropertyDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            PropertyDeclarationSyntax propertyDeclaration,
            ImmutableArray<BlockSpan>.Builder spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(propertyDeclaration, spans);

            // fault tolerance
            if (propertyDeclaration.AccessorList == null ||
                propertyDeclaration.AccessorList.OpenBraceToken.IsMissing ||
                propertyDeclaration.AccessorList.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpOutliningHelpers.CreateRegion(
                propertyDeclaration,
                propertyDeclaration.Identifier,
                autoCollapse: true));
        }
    }
}
