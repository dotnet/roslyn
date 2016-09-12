// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class OperatorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<OperatorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            OperatorDeclarationSyntax operatorDeclaration,
            ImmutableArray<BlockSpan>.Builder spans,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentRegions(operatorDeclaration, spans);

            // fault tolerance
            if (operatorDeclaration.Body == null ||
                operatorDeclaration.Body.OpenBraceToken.IsMissing ||
                operatorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpStructureHelpers.CreateRegion(
                operatorDeclaration,
                operatorDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                autoCollapse: true));
        }
    }
}