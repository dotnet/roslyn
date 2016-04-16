// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class EnumDeclarationOutliner : AbstractSyntaxNodeOutliner<EnumDeclarationSyntax>
    {
        protected override void CollectOutliningSpans(
            EnumDeclarationSyntax enumDeclaration,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(enumDeclaration, spans);

            if (!enumDeclaration.OpenBraceToken.IsMissing &&
                !enumDeclaration.CloseBraceToken.IsMissing)
            {
                spans.Add(CSharpOutliningHelpers.CreateRegion(
                    enumDeclaration,
                    enumDeclaration.Identifier,
                    autoCollapse: false));
            }

            // add any leading comments before the end of the type block
            if (!enumDeclaration.CloseBraceToken.IsMissing)
            {
                var leadingTrivia = enumDeclaration.CloseBraceToken.LeadingTrivia;
                CSharpOutliningHelpers.CollectCommentRegions(leadingTrivia, spans);
            }
        }
    }
}
