// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class AnalysisContextExtensions
    {
        /// <summary>
        /// Returns true if the given <paramref name="span"/> is in the analysis span of the given <paramref name="context"/>,
        /// i.e. either of the following is true:
        ///  - <see cref="SyntaxTreeAnalysisContext.FilterSpan"/> is <code>null</code> (we are analyzing the entire tree)
        ///  OR
        ///  - <paramref name="span"/> intersects with <see cref="SyntaxTreeAnalysisContext.FilterSpan"/>.
        /// </summary>
        public static bool ShouldAnalyzeSpan(this SyntaxTreeAnalysisContext context, TextSpan span)
            => !context.FilterSpan.HasValue || span.IntersectsWith(context.FilterSpan.Value);

        /// <summary>
        /// Returns true if the given <paramref name="span"/> is in the analysis span of the given <paramref name="context"/>,
        /// i.e. either of the following is true:
        ///  - <see cref="SemanticModelAnalysisContext.FilterSpan"/> is <code>null</code> (we are analyzing the entire tree)
        ///  OR
        ///  - <paramref name="span"/> intersects with <see cref="SemanticModelAnalysisContext.FilterSpan"/>.
        /// </summary>
        public static bool ShouldAnalyzeSpan(this SemanticModelAnalysisContext context, TextSpan span)
            => !context.FilterSpan.HasValue || span.IntersectsWith(context.FilterSpan.Value);

        /// <summary>
        /// Gets the root node in the analysis span for the given <paramref name="context"/>.
        /// </summary>
        public static SyntaxNode GetAnalysisRoot(this SyntaxTreeAnalysisContext context, bool findInTrivia, bool getInnermostNodeForTie = false)
            => context.Tree.GetNodeForSpan(context.FilterSpan, findInTrivia, getInnermostNodeForTie, context.CancellationToken);

        /// <summary>
        /// Gets the root node in the analysis span for the given <paramref name="context"/>.
        /// </summary>
        public static SyntaxNode GetAnalysisRoot(this SemanticModelAnalysisContext context, bool findInTrivia, bool getInnermostNodeForTie = false)
            => context.SemanticModel.SyntaxTree.GetNodeForSpan(context.FilterSpan, findInTrivia, getInnermostNodeForTie, context.CancellationToken);
    }
}
