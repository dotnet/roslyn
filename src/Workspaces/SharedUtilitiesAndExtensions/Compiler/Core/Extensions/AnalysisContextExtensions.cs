// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class AnalysisContextExtensions
    {
        private static bool ShouldAnalyze(TextSpan? contextFilterSpan, TextSpan span)
            => !contextFilterSpan.HasValue || span.IntersectsWith(contextFilterSpan.Value);

        /// <summary>
        /// Returns true if the given <paramref name="span"/> should be analyzed for the given <paramref name="context"/>,
        /// i.e. either of the following is true:
        ///  - <see cref="SyntaxTreeAnalysisContext.FilterSpan"/> is <code>null</code> (we are analyzing the entire tree)
        ///  OR
        ///  - <paramref name="span"/> intersects with <see cref="SyntaxTreeAnalysisContext.FilterSpan"/>.
        /// </summary>
        public static bool ShouldAnalyzeSpan(this SyntaxTreeAnalysisContext context, TextSpan span)
            => ShouldAnalyze(context.FilterSpan, span);

        /// <summary>
        /// Returns true if the given <paramref name="span"/> should be analyzed for the given <paramref name="context"/>,
        /// i.e. either of the following is true:
        ///  - <see cref="SemanticModelAnalysisContext.FilterSpan"/> is <code>null</code> (we are analyzing the entire tree)
        ///  OR
        ///  - <paramref name="span"/> intersects with <see cref="SemanticModelAnalysisContext.FilterSpan"/>.
        /// </summary>
        public static bool ShouldAnalyzeSpan(this SemanticModelAnalysisContext context, TextSpan span)
            => ShouldAnalyze(context.FilterSpan, span);

        /// <summary>
        /// Returns true if the given <paramref name="span"/> should be analyzed for the given <paramref name="context"/>,
        /// i.e. either of the following is true:
        ///  - <see cref="SymbolStartAnalysisContext.FilterSpan"/> is <code>null</code> (we are analyzing the entire tree)
        ///  OR
        ///  - <paramref name="span"/> intersects with <see cref="SymbolStartAnalysisContext.FilterSpan"/>.
        /// </summary>
        public static bool ShouldAnalyzeSpan(this SymbolStartAnalysisContext context, TextSpan span, SyntaxTree tree)
            => context.FilterTree == null || context.FilterTree == tree && ShouldAnalyze(context.FilterSpan, span);

        /// <summary>
        /// Returns true if the given <paramref name="location"/> should be analyzed for the given <paramref name="context"/>,
        /// i.e. either of the following is true:
        ///  - <see cref="SymbolStartAnalysisContext.FilterSpan"/> is <code>null</code> (we are analyzing the entire tree)
        ///  OR
        ///  - <paramref name="location"/> intersects with <see cref="SymbolStartAnalysisContext.FilterSpan"/>.
        /// </summary>
        public static bool ShouldAnalyzeLocation(this SymbolStartAnalysisContext context, Location location)
            => location.SourceTree != null && context.ShouldAnalyzeSpan(location.SourceSpan, location.SourceTree);

        /// <summary>
        /// Returns true if the given <paramref name="span"/> should be analyzed for the given <paramref name="context"/>,
        /// i.e. either of the following is true:
        ///  - <see cref="SymbolAnalysisContext.FilterSpan"/> is <code>null</code> (we are analyzing the entire tree)
        ///  OR
        ///  - <paramref name="span"/> intersects with <see cref="SymbolAnalysisContext.FilterSpan"/>.
        /// </summary>
        public static bool ShouldAnalyzeSpan(this SymbolAnalysisContext context, TextSpan span, SyntaxTree tree)
            => context.FilterTree == null || context.FilterTree == tree && ShouldAnalyze(context.FilterSpan, span);

        /// <summary>
        /// Returns true if the given <paramref name="location"/> should be analyzed for the given <paramref name="context"/>,
        /// i.e. either of the following is true:
        ///  - <see cref="SymbolAnalysisContext.FilterSpan"/> is <code>null</code> (we are analyzing the entire tree)
        ///  OR
        ///  - <paramref name="location"/> intersects with <see cref="SymbolAnalysisContext.FilterSpan"/>.
        /// </summary>
        public static bool ShouldAnalyzeLocation(this SymbolAnalysisContext context, Location location)
            => location.SourceTree != null && context.ShouldAnalyzeSpan(location.SourceSpan, location.SourceTree);

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

        /// <summary>
        /// Gets the root node in the analysis span for the given <paramref name="context"/>.
        /// NOTE: This method expects <see cref="SymbolStartAnalysisContext.FilterTree"/>
        /// and <see cref="SymbolStartAnalysisContext.FilterSpan"/> to be non-null. 
        /// </summary>
        public static SyntaxNode GetAnalysisRoot(this SymbolStartAnalysisContext context, bool findInTrivia, bool getInnermostNodeForTie = false)
        {
            Contract.ThrowIfNull(context.FilterTree);
            Contract.ThrowIfFalse(context.FilterSpan.HasValue);
            return context.FilterTree.GetNodeForSpan(context.FilterSpan, findInTrivia, getInnermostNodeForTie, context.CancellationToken);
        }
    }
}
