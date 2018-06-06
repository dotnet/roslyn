// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    // All the logic for finding source declarations in a given solution/project with some name 
    // is in this file.  

    internal static partial class DeclarationFinder
    {
        #region Dispatch Members

        // These are the public entrypoints to finding source declarations.  They will attempt to
        // remote the query to the OOP process, and will fallback to local processing if they can't.

        public static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var (succeded, results) = await TryFindSourceDeclarationsWithNormalQueryInRemoteProcessAsync(
                solution, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

            if (succeded)
            {
                return results;
            }

            return await FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                solution, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryAsync(
            Project project, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return ImmutableArray<SymbolAndProjectId>.Empty;
            }

            var (succeded, results) = await TryFindSourceDeclarationsWithNormalQueryInRemoteProcessAsync(
                project, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);

            if (succeded)
            {
                return results;
            }

            return await FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
                project, name, ignoreCase, criteria, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithPatternAsync(
            Solution solution, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var (succeded, results) = await TryFindSourceDeclarationsWithPatternInRemoteProcessAsync(
                solution, pattern, criteria, cancellationToken).ConfigureAwait(false);

            if (succeded)
            {
                return results;
            }

            return await FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                solution, pattern, criteria, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithPatternAsync(
            Project project, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (pattern == null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var (succeded, results) = await TryFindSourceDeclarationsWithPatternInRemoteProcessAsync(
                project, pattern, criteria, cancellationToken).ConfigureAwait(false);

            if (succeded)
            {
                return results;
            }

            return await FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                project, pattern, criteria, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Remote Dispatch

        // These are the members that actually try to send the request to the remote process.

        private static async Task<(bool succeeded, ImmutableArray<SymbolAndProjectId> results)> TryFindSourceDeclarationsWithNormalQueryInRemoteProcessAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var result = await solution.TryRunCodeAnalysisRemoteAsync<IList<SerializableSymbolAndProjectId>>(
                RemoteFeatureOptions.SymbolFinderEnabled,
                nameof(IRemoteSymbolFinder.FindSolutionSourceDeclarationsWithNormalQueryAsync),
                new object[] { name, ignoreCase, criteria }, cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return (false, ImmutableArray<SymbolAndProjectId>.Empty);
            }

            var rehydrated = await RehydrateAsync(
                solution, result, cancellationToken).ConfigureAwait(false);

            return (true, rehydrated);
        }

        private static async Task<(bool succeeded, ImmutableArray<SymbolAndProjectId> results)> TryFindSourceDeclarationsWithNormalQueryInRemoteProcessAsync(
            Project project, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return (false, ImmutableArray<SymbolAndProjectId>.Empty);
            }

            var result = await project.Solution.TryRunCodeAnalysisRemoteAsync<IList<SerializableSymbolAndProjectId>>(
                RemoteFeatureOptions.SymbolFinderEnabled,
                nameof(IRemoteSymbolFinder.FindProjectSourceDeclarationsWithNormalQueryAsync),
                new object[] { project.Id, name, ignoreCase, criteria }, cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return (false, ImmutableArray<SymbolAndProjectId>.Empty);
            }

            var rehydrated = await RehydrateAsync(
                project.Solution, result, cancellationToken).ConfigureAwait(false);

            return (true, rehydrated);
        }

        private static async Task<(bool succeeded, ImmutableArray<SymbolAndProjectId> results)> TryFindSourceDeclarationsWithPatternInRemoteProcessAsync(
            Solution solution, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var result = await solution.TryRunCodeAnalysisRemoteAsync<IList<SerializableSymbolAndProjectId>>(
                RemoteFeatureOptions.SymbolFinderEnabled,
                nameof(IRemoteSymbolFinder.FindSolutionSourceDeclarationsWithPatternAsync),
                new object[] { pattern, criteria }, cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return (false, ImmutableArray<SymbolAndProjectId>.Empty);
            }

            var rehydrated = await RehydrateAsync(
                solution, result, cancellationToken).ConfigureAwait(false);

            return (true, rehydrated);
        }

        private static async Task<(bool succeeded, ImmutableArray<SymbolAndProjectId> results)> TryFindSourceDeclarationsWithPatternInRemoteProcessAsync(
            Project project, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            if (!RemoteSupportedLanguages.IsSupported(project.Language))
            {
                return (false, ImmutableArray<SymbolAndProjectId>.Empty);
            }

            var result = await project.Solution.TryRunCodeAnalysisRemoteAsync<IList<SerializableSymbolAndProjectId>>(
                RemoteFeatureOptions.SymbolFinderEnabled,
                nameof(IRemoteSymbolFinder.FindProjectSourceDeclarationsWithPatternAsync),
                new object[] { project.Id, pattern, criteria }, cancellationToken).ConfigureAwait(false);

            if (result == null)
            {
                return (false, ImmutableArray<SymbolAndProjectId>.Empty);
            }

            var rehydrated = await RehydrateAsync(
                project.Solution, result, cancellationToken).ConfigureAwait(false);

            return (true, rehydrated);
        }

        #endregion

        #region Local processing

        // These are the members that have the core logic that does the actual finding.  They will
        // be called 'in proc' in the remote process if we are able to remote the request.  Or they
        // will be called 'in proc' from within VS if we are not able to remote the request.

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
            Solution solution, string name, bool ignoreCase, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            using (var query = SearchQuery.Create(name, ignoreCase))
            {
                var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();
                foreach (var projectId in solution.ProjectIds)
                {
                    var project = solution.GetProject(projectId);
                    await AddCompilationDeclarationsWithNormalQueryAsync(
                        project, query, criteria, result, cancellationToken).ConfigureAwait(false);
                }

                return result.ToImmutableAndFree();
            }
        }

        internal static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithNormalQueryInCurrentProcessAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken)
        {
            var list = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            using (var query = SearchQuery.Create(name, ignoreCase))
            {
                await AddCompilationDeclarationsWithNormalQueryAsync(
                    project, query, filter, list, cancellationToken).ConfigureAwait(false);
                return list.ToImmutableAndFree();
            }
        }

        private static async Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithPatternInCurrentProcessAsync(
            string pattern, Func<SearchQuery, Task<ImmutableArray<SymbolAndProjectId>>> searchAsync)
        {
            // The compiler API only supports a predicate which is given a symbol's name.  Because
            // we only have the name, and nothing else, we need to check it against the last segment
            // of the pattern.  i.e. if the pattern is 'Console.WL' and we are given 'WriteLine', then
            // we don't want to check the whole pattern against it (as it will clearly fail), instead
            // we only want to check the 'WL' portion.  Then, after we get all the candidate symbols
            // we'll check if the full name matches the full pattern.
            var (namePart, containerPart) = PatternMatcher.GetNameAndContainer(pattern);

            var dotIndex = pattern.LastIndexOf('.');
            var isDottedPattern = dotIndex >= 0;

            // If we don't have a dot in the pattern, just make a pattern matcher for the entire
            // pattern they passed in.  Otherwise, make a pattern matcher just for the part after
            // the dot.
            using (var nameMatcher = PatternMatcher.CreatePatternMatcher(namePart, includeMatchedSpans: false))
            using (var query = SearchQuery.CreateCustom(nameMatcher.Matches))
            {
                var symbolAndProjectIds = await searchAsync(query).ConfigureAwait(false);

                if (symbolAndProjectIds.Length == 0 ||
                    !isDottedPattern)
                {
                    // If it wasn't a dotted pattern, or we didn't get anything back, then we're done.
                    // We can just return whatever set of results we got so far.
                    return symbolAndProjectIds;
                }

                // Ok, we had a dotted pattern.  Have to see if the symbol's container matches the 
                // pattern as well.
                using (var containerPatternMatcher = PatternMatcher.CreateDotSeparatedContainerMatcher(containerPart))
                {
                    return symbolAndProjectIds.WhereAsArray(t =>
                        containerPatternMatcher.Matches(GetContainer(t.Symbol)));
                }
            }
        }

        internal static Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithPatternInCurrentProcessAsync(
            Solution solution, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                pattern,
                query => SymbolFinder.FindSourceDeclarationsWithCustomQueryAsync(solution, query, criteria, cancellationToken));
        }

        internal static Task<ImmutableArray<SymbolAndProjectId>> FindSourceDeclarationsWithPatternInCurrentProcessAsync(
            Project project, string pattern, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            return FindSourceDeclarationsWithPatternInCurrentProcessAsync(
                pattern,
                query => SymbolFinder.FindSourceDeclarationsWithCustomQueryAsync(project, query, criteria, cancellationToken));
        }

        private static string GetContainer(ISymbol symbol)
        {
            var container = symbol.ContainingSymbol;
            if (container == null)
            {
                return null;
            }

            return container.ToDisplayString(DottedNameFormat);
        }

        private static readonly SymbolDisplayFormat DottedNameFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                delegateStyle: SymbolDisplayDelegateStyle.NameOnly,
                extensionMethodStyle: SymbolDisplayExtensionMethodStyle.StaticMethod,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly);

        #endregion
    }
}
