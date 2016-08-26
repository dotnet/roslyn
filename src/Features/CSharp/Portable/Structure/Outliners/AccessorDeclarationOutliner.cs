// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class AccessorDeclarationOutliner : AbstractSyntaxNodeStructureProvider<AccessorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            AccessorDeclarationSyntax accessorDeclaration,
            ImmutableArray<BlockSpan>.Builder spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(accessorDeclaration, spans);

            // fault tolerance
            if (accessorDeclaration.Body == null ||
                accessorDeclaration.Body.OpenBraceToken.IsMissing ||
                accessorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpOutliningHelpers.CreateRegion(
                accessorDeclaration,
                accessorDeclaration.Keyword,
                autoCollapse: true));
        }
    }
}
