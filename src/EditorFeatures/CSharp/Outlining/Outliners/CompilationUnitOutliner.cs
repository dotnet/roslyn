// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class CompilationUnitOutliner : AbstractSyntaxNodeOutliner<CompilationUnitSyntax>
    {
        protected override void CollectOutliningSpans(
            CompilationUnitSyntax compilationUnit,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            CSharpOutliningHelpers.CollectCommentRegions(compilationUnit, spans);

            // extern aliases and usings are outlined in a single region
            var externsAndUsings = new List<SyntaxNode>();
            externsAndUsings.AddRange(compilationUnit.Externs);
            externsAndUsings.AddRange(compilationUnit.Usings);
            externsAndUsings.Sort((node1, node2) => node1.SpanStart.CompareTo(node2.SpanStart));

            spans.Add(CSharpOutliningHelpers.CreateRegion(externsAndUsings, autoCollapse: true));

            if (compilationUnit.Usings.Count > 0 ||
                compilationUnit.Externs.Count > 0 ||
                compilationUnit.Members.Count > 0 ||
                compilationUnit.AttributeLists.Count > 0)
            {
                CSharpOutliningHelpers.CollectCommentRegions(compilationUnit.EndOfFileToken.LeadingTrivia, spans);
            }
        }

        protected override bool SupportedInWorkspaceKind(string kind)
        {
            return true;
        }
    }
}
