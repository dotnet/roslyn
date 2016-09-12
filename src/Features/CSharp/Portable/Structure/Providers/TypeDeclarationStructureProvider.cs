// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class TypeDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<TypeDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            TypeDeclarationSyntax typeDeclaration,
            ImmutableArray<BlockSpan>.Builder spans,
            CancellationToken cancellationToken)
        {
            CSharpStructureHelpers.CollectCommentRegions(typeDeclaration, spans);

            if (!typeDeclaration.OpenBraceToken.IsMissing &&
                !typeDeclaration.CloseBraceToken.IsMissing)
            {
                var lastToken = typeDeclaration.TypeParameterList == null
                    ? typeDeclaration.Identifier
                    : typeDeclaration.TypeParameterList.GetLastToken(includeZeroWidth: true);

                spans.Add(CSharpStructureHelpers.CreateRegion(
                    typeDeclaration,
                    lastToken,
                    autoCollapse: false));
            }

            // add any leading comments before the end of the type block
            if (!typeDeclaration.CloseBraceToken.IsMissing)
            {
                var leadingTrivia = typeDeclaration.CloseBraceToken.LeadingTrivia;
                CSharpStructureHelpers.CollectCommentRegions(leadingTrivia, spans);
            }
        }
    }
}