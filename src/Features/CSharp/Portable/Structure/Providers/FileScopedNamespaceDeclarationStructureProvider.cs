// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal class FileScopedNamespaceDeclarationStructureProvider : AbstractSyntaxNodeStructureProvider<FileScopedNamespaceDeclarationSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclaration,
        ref TemporaryArray<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        // add leading comments
        CSharpStructureHelpers.CollectCommentBlockSpans(fileScopedNamespaceDeclaration, ref spans, options);

        // extern aliases and usings are outlined in a single region
        var externsAndUsings = Enumerable.Union<SyntaxNode>(fileScopedNamespaceDeclaration.Externs, fileScopedNamespaceDeclaration.Usings).ToImmutableArray();

        // add any leading comments before the extern aliases and usings
        if (externsAndUsings.Any())
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(externsAndUsings.First(), ref spans, options);
        }

        spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
            externsAndUsings, compressEmptyLines: false, autoCollapse: true,
            type: BlockTypes.Imports, isCollapsible: true, isDefaultCollapsed: options.CollapseImportsWhenFirstOpened));
    }
}
