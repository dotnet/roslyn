// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class NamespaceDeclarationOutliner : AbstractSyntaxNodeOutliner<NamespaceDeclarationSyntax>
    {
        protected override void CollectOutliningSpans(
            NamespaceDeclarationSyntax namespaceDeclaration,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            // add leading comments
            CSharpOutliningHelpers.CollectCommentRegions(namespaceDeclaration, spans);

            if (!namespaceDeclaration.OpenBraceToken.IsMissing &&
                !namespaceDeclaration.CloseBraceToken.IsMissing)
            {
                spans.Add(CSharpOutliningHelpers.CreateRegion(
                    namespaceDeclaration,
                    namespaceDeclaration.Name.GetLastToken(includeZeroWidth: true),
                    autoCollapse: false));
            }

            // extern aliases and usings are outlined in a single region
            var externsAndUsings = Enumerable.Union<SyntaxNode>(namespaceDeclaration.Externs, namespaceDeclaration.Usings)
                                       .OrderBy(node => node.SpanStart)
                                       .ToList();

            // add any leading comments before the extern aliases and usings
            if (externsAndUsings.Count > 0)
            {
                CSharpOutliningHelpers.CollectCommentRegions(externsAndUsings.First(), spans);
            }

            spans.Add(CSharpOutliningHelpers.CreateRegion(externsAndUsings, autoCollapse: true));

            // finally, add any leading comments before the end of the namespace block
            if (!namespaceDeclaration.CloseBraceToken.IsMissing)
            {
                CSharpOutliningHelpers.CollectCommentRegions(namespaceDeclaration.CloseBraceToken.LeadingTrivia, spans);
            }
        }

        protected override bool SupportedInWorkspaceKind(string kind)
        {
            return true;
        }
    }
}
