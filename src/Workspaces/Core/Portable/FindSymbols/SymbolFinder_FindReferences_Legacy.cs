// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // This file contains the legacy FindReferences APIs.  The APIs are legacy because they
    // do not contain enough information for us to effectively remote them over to the OOP
    // process to do the work.  Specifically, they lack the "current project context" necessary
    // to be able to effectively serialize symbols to/from the remote process.

    public static partial class SymbolFinder
    {
        /// <summary>
        /// Finds all references to a symbol throughout a solution
        /// </summary>
        /// <param name="symbol">The symbol to find references to.</param>
        /// <param name="solution">The solution to find references within.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        [Obsolete("Use the overload of FindReferencesAsync that takes a Project", error: false)]
        public static async Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken = default)
        {
            return await FindReferencesAsync(new SymbolAndProjectId(symbol, projectId: null), solution, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds all references to the provided <paramref name="symbol"/> in the <see cref="Solution"/> that <paramref
        /// name="project"/> is part of. <paramref name="symbol"/> must either be a source symbol from <paramref
        /// name="project"/> or a metadata symbol from one of <paramref name="project"/>'s <see
        /// cref="Project.MetadataReferences"/>.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            return FindReferencesAsync(new SymbolAndProjectId(symbol, project.Id), project.Solution, cancellationToken);
        }

        internal static Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(SymbolAndProjectId symbolAndProjectId, Solution solution, CancellationToken cancellationToken)
            => FindReferencesAsync(symbolAndProjectId, solution, FindReferencesSearchOptions.Default, cancellationToken);

        internal static async Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var progressCollector = new StreamingProgressCollector();
            await FindReferencesAsync(
                symbolAndProjectId, solution, progressCollector,
                documents: null, options, cancellationToken).ConfigureAwait(false);
            return progressCollector.GetReferencedSymbols();
        }

        /// <summary>
        /// Finds all references to a symbol throughout a solution
        /// </summary>
        /// <param name="symbol">The symbol to find references to.</param>
        /// <param name="solution">The solution to find references within.</param>
        /// <param name="documents">A set of documents to be searched. If documents is null, then that means "all documents".</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        [Obsolete("Use the overload of FindReferencesAsync that takes a Project", error: false)]
        public static Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol,
            Solution solution,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default)
        {
            return FindReferencesAsync(symbol, solution, progress: null, documents: documents, cancellationToken: cancellationToken);
        }

        /// <inheritdoc cref="FindReferencesAsync(ISymbol, Project, CancellationToken)"/>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol, Project project, IImmutableSet<Document> documents, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            return FindReferencesAsync(symbol, project, progress: null, documents: documents, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Finds all references to a symbol throughout a solution
        /// </summary>
        /// <param name="symbol">The symbol to find references to.</param>
        /// <param name="solution">The solution to find references within.</param>
        /// <param name="progress">An optional progress object that will receive progress
        /// information as the search is undertaken.</param>
        /// <param name="documents">An optional set of documents to be searched. If documents is null, then that means "all documents".</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        [Obsolete("Use the overload of FindReferencesAsync that takes a Project", error: false)]
        public static async Task<IEnumerable<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol,
            Solution solution,
            IFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default)
        {
            return await FindReferencesAsync(
                new SymbolAndProjectId(symbol, projectId: null), solution, progress,
                documents, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindReferencesAsync(ISymbol, Project, CancellationToken)"/>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(
            ISymbol symbol,
            Project project,
            IFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            return FindReferencesAsync(
                new SymbolAndProjectId(symbol, project.Id), project.Solution, progress,
                documents, FindReferencesSearchOptions.Default, cancellationToken);
        }

        private static async Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            IFindReferencesProgress progress,
            IImmutableSet<Document> documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            progress ??= NoOpFindReferencesProgress.Instance;
            var streamingProgress = new StreamingProgressCollector(
                new StreamingFindReferencesProgressAdapter(progress));
            await FindReferencesAsync(
                symbolAndProjectId, solution, streamingProgress,
                documents, options, cancellationToken).ConfigureAwait(false);
            return streamingProgress.GetReferencedSymbols();
        }

        internal static class TestAccessor
        {
            internal static Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(
                ISymbol symbol,
                Project project,
                IFindReferencesProgress progress,
                IImmutableSet<Document> documents,
                FindReferencesSearchOptions options,
                CancellationToken cancellationToken)
            {
                return SymbolFinder.FindReferencesAsync(
                    new SymbolAndProjectId(symbol, project.Id), project.Solution, progress, documents, options, cancellationToken);
            }
        }
    }
}
