// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class FileScopedNamespaceDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<FileScopedNamespaceDeclarationSyntax>
    {
        protected override void CollectBlockSpans(
            SyntaxToken previousToken,
            FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclaration,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptionProvider optionProvider,
            CancellationToken cancellationToken)
        {
            // add leading comments
            CSharpStructureHelpers.CollectCommentBlockSpans(fileScopedNamespaceDeclaration, ref spans, optionProvider);

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                fileScopedNamespaceDeclaration,
                fileScopedNamespaceDeclaration.Name.GetLastToken(includeZeroWidth: true),
                compressEmptyLines: false,
                autoCollapse: false,
                type: BlockTypes.Namespace,
                isCollapsible: true));

            // extern aliases and usings are outlined in a single region
            var externsAndUsings = Enumerable.Union<SyntaxNode>(fileScopedNamespaceDeclaration.Externs, fileScopedNamespaceDeclaration.Usings)
                                       .OrderBy(node => node.SpanStart)
                                       .ToList();

            // add any leading comments before the extern aliases and usings
            if (externsAndUsings.Count > 0)
            {
                CSharpStructureHelpers.CollectCommentBlockSpans(externsAndUsings.First(), ref spans, optionProvider);
            }

            spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
                externsAndUsings, compressEmptyLines: false, autoCollapse: true,
                type: BlockTypes.Imports, isCollapsible: true));

            // add ending comments
            CSharpStructureHelpers.CollectCommentBlockSpans(
                fileScopedNamespaceDeclaration.SemicolonToken.LeadingTrivia, ref spans);
        }
    }
}
