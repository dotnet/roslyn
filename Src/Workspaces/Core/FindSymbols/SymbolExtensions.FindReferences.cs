using System;
using System.Collections.Generic;
using System.Threading;
using Roslyn.Compilers.Common;
using Roslyn.Services.FindReferences;
using Roslyn.Services.Internal.Log;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    public static partial class SymbolExtensions
    {
        /// <summary>
        /// Finds all references to a symbol throughout a solution
        /// </summary>
        /// <param name="symbol">The symbol to find references to.</param>
        /// <param name="solution">The solution to find references within.</param>
        public static IEnumerable<ReferencedSymbol> FindReferences(
            this ISymbol symbol,
            Solution solution)
        {
            return FindReferences(symbol, solution, CancellationToken.None);
        }

        /// <summary>
        /// Finds all references to a symbol throughout a solution
        /// </summary>
        /// <param name="symbol">The symbol to find references to.</param>
        /// <param name="solution">The solution to find references within.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        public static IEnumerable<ReferencedSymbol> FindReferences(
            this ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return FindReferences(symbol, solution, progress: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Finds all references to a symbol throughout a solution
        /// </summary>
        /// <param name="symbol">The symbol to find references to.</param>
        /// <param name="solution">The solution to find references within.</param>
        /// <param name="includeDocument">An optional predicate function that determines if
        /// particular documents should be searched.</param>
        /// <param name="referenceFinders">An optional list of reference finders that determine the
        /// behavior of searching for different kinds of symbols. If this parameter is not
        /// specified, the common list of reference finders is used.</param>
        /// <param name="progress">An optional progress object that will receive progress
        /// information as the search is undertaken.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        public static IEnumerable<ReferencedSymbol> FindReferences(
            this ISymbol symbol,
            Solution solution,
            IFindReferencesProgress progress = null,
            IEnumerable<IReferenceFinder> referenceFinders = null,
            Func<Document, bool> includeDocument = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.LogBlock(FeatureId.FindReference, FunctionId.FindReference_Start, cancellationToken))
            {
                referenceFinders = referenceFinders ?? ReferenceFinder.DefaultReferenceFinders;
                progress = progress ?? FindReferencesProgress.Instance;
                var engine = new FindReferencesSearchEngine(solution, includeDocument, referenceFinders.ToImmutableList(), progress, cancellationToken);
                return engine.FindReferences(symbol);
            }
        }
    }
}