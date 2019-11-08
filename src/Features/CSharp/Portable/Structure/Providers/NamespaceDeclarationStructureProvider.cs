// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class NamespaceDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<NamespaceDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            NamespaceDeclarationSyntax namespaceDeclaration,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            // add leading comments
            CSharpStructureHelpers.CollectCommentBlockSpans(namespaceDeclaration, spans);

            if (namespaceDeclaration is { OpenBraceToken: { IsMissing: false }, CloseBraceToken: { IsMissing: false } })
            {
                spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                    namespaceDeclaration,
                    namespaceDeclaration.Name.GetLastToken(includeZeroWidth: true),
                    autoCollapse: false,
                    type: BlockTypes.Namespace,
                    isCollapsible: true));
            }

            // extern aliases and usings are outlined in a single region
            var externsAndUsings = Enumerable.Union<SyntaxNode>(namespaceDeclaration.Externs, namespaceDeclaration.Usings)
                                       .OrderBy(node => node.SpanStart)
                                       .ToList();

            // add any leading comments before the extern aliases and usings
            if (externsAndUsings.Count > 0)
            {
                CSharpStructureHelpers.CollectCommentBlockSpans(externsAndUsings.First(), spans);
            }

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                externsAndUsings, autoCollapse: true,
                type: BlockTypes.Imports, isCollapsible: true));

            // finally, add any leading comments before the end of the namespace block
            if (!namespaceDeclaration.CloseBraceToken.IsMissing)
            {
                CSharpStructureHelpers.CollectCommentBlockSpans(
                    namespaceDeclaration.CloseBraceToken.LeadingTrivia, spans);
            }
        }

        protected override bool SupportedInWorkspaceKind(string kind)
        {
            return true;
        }
    }
}
