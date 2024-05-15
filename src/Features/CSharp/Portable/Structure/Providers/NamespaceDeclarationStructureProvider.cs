// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal class NamespaceDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<NamespaceDeclarationSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        NamespaceDeclarationSyntax namespaceDeclaration,
        ref TemporaryArray<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        // add leading comments
        CSharpStructureHelpers.CollectCommentBlockSpans(namespaceDeclaration, ref spans, options);

        if (!namespaceDeclaration.OpenBraceToken.IsMissing &&
            !namespaceDeclaration.CloseBraceToken.IsMissing)
        {
            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                namespaceDeclaration,
                namespaceDeclaration.Name.GetLastToken(includeZeroWidth: true),
                compressEmptyLines: false,
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
            CSharpStructureHelpers.CollectCommentBlockSpans(externsAndUsings.First(), ref spans, options);
        }

        spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
            externsAndUsings, compressEmptyLines: false, autoCollapse: true,
            type: BlockTypes.Imports, isCollapsible: true, isDefaultCollapsed: options.CollapseImportsWhenFirstOpened));

        // finally, add any leading comments before the end of the namespace block
        if (!namespaceDeclaration.CloseBraceToken.IsMissing)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(
                namespaceDeclaration.CloseBraceToken.LeadingTrivia, ref spans);
        }
    }
}
