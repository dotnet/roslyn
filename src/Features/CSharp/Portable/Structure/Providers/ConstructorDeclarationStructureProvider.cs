// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class ConstructorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<ConstructorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            ConstructorDeclarationSyntax constructorDeclaration,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(constructorDeclaration, spans);

            // fault tolerance
            if (constructorDeclaration.Body == null ||
                constructorDeclaration.Body.OpenBraceToken.IsMissing ||
                constructorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpStructureHelpers.CreateBlockSpan(
                constructorDeclaration,
                constructorDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                autoCollapse: true,
                type: BlockTypes.Constructor,
                isCollapsible: true));
        }
    }
}
