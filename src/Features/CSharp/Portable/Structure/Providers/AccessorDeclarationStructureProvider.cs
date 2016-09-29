// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class AccessorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<AccessorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            AccessorDeclarationSyntax accessorDeclaration,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(accessorDeclaration, spans);

            // fault tolerance
            if (accessorDeclaration.Body == null ||
                accessorDeclaration.Body.OpenBraceToken.IsMissing ||
                accessorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpStructureHelpers.CreateBlockSpan(
                accessorDeclaration,
                accessorDeclaration.Keyword,
                autoCollapse: true,
                type: BlockTypes.Accessor,
                isCollapsible: true));
        }
    }
}
