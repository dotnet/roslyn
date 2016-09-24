// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class DestructorDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<DestructorDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            DestructorDeclarationSyntax destructorDeclaration,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(destructorDeclaration, spans);

            // fault tolerance
            if (destructorDeclaration.Body == null ||
                destructorDeclaration.Body.OpenBraceToken.IsMissing ||
                destructorDeclaration.Body.CloseBraceToken.IsMissing)
            {
                return;
            }

            spans.Add(CSharpStructureHelpers.CreateBlockSpan(
                destructorDeclaration,
                destructorDeclaration.ParameterList.GetLastToken(includeZeroWidth: true),
                autoCollapse: true,
                type: BlockTypes.Destructor,
                isCollapsible: true));
        }
    }
}