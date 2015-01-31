// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class TypeDeclarationOutliner : AbstractSyntaxNodeOutliner<TypeDeclarationSyntax>
    {
        protected override void CollectOutliningSpans(
            TypeDeclarationSyntax typeDeclaration,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(typeDeclaration, spans);

            if (!typeDeclaration.OpenBraceToken.IsMissing &&
                !typeDeclaration.CloseBraceToken.IsMissing)
            {
                var lastToken = typeDeclaration.TypeParameterList == null
                    ? typeDeclaration.Identifier
                    : typeDeclaration.TypeParameterList.GetLastToken(includeZeroWidth: true);

                spans.Add(CSharpOutliningHelpers.CreateRegion(
                    typeDeclaration,
                    lastToken,
                    autoCollapse: false));
            }

            // add any leading comments before the end of the type block
            if (!typeDeclaration.CloseBraceToken.IsMissing)
            {
                var leadingTrivia = typeDeclaration.CloseBraceToken.LeadingTrivia;
                CSharpOutliningHelpers.CollectCommentRegions(leadingTrivia, spans);
            }
        }
    }
}
