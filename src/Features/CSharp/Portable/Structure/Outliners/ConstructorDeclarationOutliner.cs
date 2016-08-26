// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class ConstructorDeclarationOutliner : AbstractSyntaxNodeStructureProvider<ConstructorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            ConstructorDeclarationSyntax constructorDeclaration,
            ImmutableArray<BlockSpan>.Builder spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(constructorDeclaration, spans);

            // fault tolerance
            if (constructorDeclaration.Body == null ||
                constructorDeclaration.Body.OpenBraceToken.IsMissing ||
                constructorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpOutliningHelpers.CreateRegion(
                constructorDeclaration,
                constructorDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                autoCollapse: true));
        }
    }
}
