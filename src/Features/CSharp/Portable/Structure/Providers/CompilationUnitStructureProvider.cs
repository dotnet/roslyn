// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal class CompilationUnitStructureProvider : AbstractSyntaxNodeStructureProvider<CompilationUnitSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        CompilationUnitSyntax compilationUnit,
        ref TemporaryArray<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        CSharpStructureHelpers.CollectCommentBlockSpans(compilationUnit, ref spans, options);

        // extern aliases and usings are outlined in a single region
        var externsAndUsings = new List<SyntaxNode>();
        externsAndUsings.AddRange(compilationUnit.Externs);
        externsAndUsings.AddRange(compilationUnit.Usings);
        externsAndUsings.Sort((node1, node2) => node1.SpanStart.CompareTo(node2.SpanStart));

        spans.AddIfNotNull(CSharpStructureHelpers.CreateBlockSpan(
            externsAndUsings,
            compressEmptyLines: false,
            autoCollapse: true,
            type: BlockTypes.Imports,
            isCollapsible: true,
            isDefaultCollapsed: options.CollapseImportsWhenFirstOpened));

        if (compilationUnit.Usings.Count > 0 ||
            compilationUnit.Externs.Count > 0 ||
            compilationUnit.Members.Count > 0 ||
            compilationUnit.AttributeLists.Count > 0)
        {
            CSharpStructureHelpers.CollectCommentBlockSpans(compilationUnit.EndOfFileToken.LeadingTrivia, ref spans);
        }
    }
}
