// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class NamespaceDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<NamespaceDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            NamespaceDeclarationSyntax namespaceDeclaration,
            ImmutableArray<BlockSpan>.Builder spans,
            CancellationToken cancellationToken)
        {
            // add leading comments
            CSharpStructureHelpers.CollectCommentRegions(namespaceDeclaration, spans);

            if (!namespaceDeclaration.OpenBraceToken.IsMissing &&
                !namespaceDeclaration.CloseBraceToken.IsMissing)
            {
                spans.Add(CSharpStructureHelpers.CreateRegion(
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
                CSharpStructureHelpers.CollectCommentRegions(externsAndUsings.First(), spans);
            }

            spans.Add(CSharpStructureHelpers.CreateRegion(externsAndUsings, autoCollapse: true));

            // finally, add any leading comments before the end of the namespace block
            if (!namespaceDeclaration.CloseBraceToken.IsMissing)
            {
                CSharpStructureHelpers.CollectCommentRegions(namespaceDeclaration.CloseBraceToken.LeadingTrivia, spans);
            }
        }

        protected override bool SupportedInWorkspaceKind(string kind)
        {
            return true;
        }
    }
}