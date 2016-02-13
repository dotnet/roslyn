// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal enum SearchKind
    {
        /// <summary>
        /// Use an case-sensitive comparison when searching for matching items.
        /// </summary>
        Exact,

        /// <summary>
        /// Use a case-insensitive comparison when searching for matching items.
        /// </summary>
        ExactIgnoreCase,

        /// <summary>
        /// Use a fuzzy comparison when searching for matching items. Fuzzy matching allows for 
        /// a certain amount of misspellings, missing words, etc. See <see cref="SpellChecker"/> for 
        /// more details.
        /// </summary>
        Fuzzy,

        /// <summary>
        /// Search term is matched in a custom manner (i.e. with a user provided predicate).
        /// </summary>
        Custom
    }

    internal class SearchQuery
    {
        /// <summary>The name being searched for.  Is null in the case of custom predicate searching..  But 
        /// can be used for faster index based searching when it is available.</summary> 
        public readonly string Name;

        ///<summary>The kind of search this is.  Faster index-based searching can be used if the 
        /// SearchKind is not <see cref="SearchKind.Custom"/>.</summary>
        public readonly SearchKind Kind;

        ///<summary>The predicate to fall back on if faster index searching is not possible.</summary>
        private readonly Func<string, bool> _predicate;

        private SearchQuery(string name, SearchKind kind)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Kind = kind;

            switch (kind)
            {
                case SearchKind.Exact:
                    _predicate = s => StringComparer.Ordinal.Equals(name, s);
                    break;
                case SearchKind.ExactIgnoreCase:
                    _predicate = s => CaseInsensitiveComparison.Comparer.Equals(name, s);
                    break;
                case SearchKind.Fuzzy:
                    // Create a single WordSimilarityChecker and capture a delegate reference to 
                    // its 'AreSimilar' method. That way we only create the WordSimilarityChecker
                    // once and it can cache all the information it needs while it does the AreSimilar
                    // check against all the possible candidates.
                    var editDistance = new WordSimilarityChecker(name);
                    _predicate = editDistance.AreSimilar;
                    break;
            }
        }

        private SearchQuery(Func<string, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            _predicate = predicate;
        }

        public static SearchQuery Create(string name, bool ignoreCase)
        {
            return new SearchQuery(name, ignoreCase ? SearchKind.ExactIgnoreCase : SearchKind.Exact);
        }

        public static SearchQuery CreateFuzzy(string name)
        {
            return new SearchQuery(name, SearchKind.Fuzzy);
        }

        public static SearchQuery CreateCustom(Func<string, bool> predicate)
        {
            return new SearchQuery(predicate);
        }

        public Func<string, bool> GetPredicate()
        {
            return _predicate;
        }
    }

    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindDeclarationsAsync(Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            return FindDeclarationsAsync(project, SearchQuery.Create(name, ignoreCase), cancellationToken: cancellationToken);
        }

        internal static Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, SearchQuery query, CancellationToken cancellationToken)
        {
            return FindDeclarationsAsync(project, query, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the declared symbols from either source, referenced projects or metadata assemblies with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            return FindDeclarationsAsync(project, SearchQuery.Create(name, ignoreCase), filter, cancellationToken: cancellationToken);
        }

        internal static Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Project project, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_FindDeclarationsAsync, cancellationToken))
            {
                return FindDeclarationsAsyncImpl(project, query, filter, cancellationToken);
            }
        }

        private static async Task<IEnumerable<ISymbol>> FindDeclarationsAsyncImpl(
            Project project, SearchQuery query, SymbolFilter criteria, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            var list = new List<ISymbol>();

            // get declarations from the compilation's assembly
            await AddDeclarationsAsync(project, query, criteria, list, cancellationToken).ConfigureAwait(false);

            // get declarations from directly referenced projects and metadata
            foreach (var assembly in compilation.GetReferencedAssemblySymbols())
            {
                var assemblyProject = project.Solution.GetProject(assembly, cancellationToken);
                if (assemblyProject != null)
                {
                    await AddDeclarationsAsync(assemblyProject, query, criteria, list, compilation, assembly, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await AddDeclarationsAsync(
                        project.Solution, assembly, compilation.GetMetadataReference(assembly) as PortableExecutableReference,
                        query, criteria, list, cancellationToken).ConfigureAwait(false);
                }
            }

            return TranslateNamespaces(list, compilation);
        }

        private static string GetMetadataReferenceFilePath(MetadataReference metadataReference)
        {
            return (metadataReference as PortableExecutableReference)?.FilePath;
        }

        /// <summary>
        /// Makes certain all namespace symbols returned by API are from the compilation.
        /// </summary>
        private static IEnumerable<ISymbol> TranslateNamespaces(List<ISymbol> symbols, Compilation compilation)
        {
            foreach (var symbol in symbols)
            {
                var ns = symbol as INamespaceSymbol;
                if (ns != null)
                {
                    yield return compilation.GetCompilationNamespace(ns);
                }
                else
                {
                    yield return symbol;
                }
            }
        }

        private static async Task AddDeclarationsAsync(
            Project project, SearchQuery query, SymbolFilter filter, List<ISymbol> list, CancellationToken cancellationToken)
        {
            await AddDeclarationsAsync(
                project, query, filter, list,
                startingCompilation: null,
                startingAssembly: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static async Task AddDeclarationsAsync(
            Project project,
            SearchQuery query,
            SymbolFilter filter,
            List<ISymbol> list,
            Compilation startingCompilation,
            IAssemblySymbol startingAssembly,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_AddDeclarationsAsync, cancellationToken))
            using (var set = SharedPools.Default<HashSet<ISymbol>>().GetPooledObject())
            {
                if (!await project.ContainsSymbolsWithNameAsync(query.GetPredicate(), filter, cancellationToken).ConfigureAwait(false))
                {
                    return;
                }

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (startingCompilation != null && startingAssembly != null && compilation.Assembly != startingAssembly)
                {
                    // Return symbols from skeleton assembly in this case so that symbols have the same language as startingCompilation.
                    list.AddRange(
                        FilterByCriteria(compilation.GetSymbolsWithName(query.GetPredicate(), filter, cancellationToken), filter)
                            .Select(s => s.GetSymbolKey().Resolve(startingCompilation, cancellationToken: cancellationToken).Symbol).WhereNotNull());
                }
                else
                {
                    list.AddRange(FilterByCriteria(compilation.GetSymbolsWithName(query.GetPredicate(), filter, cancellationToken), filter));
                }
            }
        }

        internal static async Task<IEnumerable<ISymbol>> FindDeclarationsAsync(
            Solution solution, IAssemblySymbol assembly, PortableExecutableReference reference, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            var result = new List<ISymbol>();
            await AddDeclarationsAsync(solution, assembly, reference, query, filter, result, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private static async Task AddDeclarationsAsync(
            Solution solution, IAssemblySymbol assembly, PortableExecutableReference referenceOpt, SearchQuery query, SymbolFilter filter, List<ISymbol> list, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.SymbolFinder_Assembly_AddDeclarationsAsync, cancellationToken))
            {
                if (referenceOpt != null)
                {
                    var info = await SymbolTreeInfo.TryGetInfoForMetadataAssemblyAsync(solution, assembly, referenceOpt, loadOnly: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (info != null)
                    {
                        var symbols = await info.FindAsync(query, assembly, cancellationToken).ConfigureAwait(false);
                        list.AddRange(FilterByCriteria(symbols, filter));
                    }
                }
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(solution, name, ignoreCase, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
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
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                return FindSourceDeclarationsAsyncImpl(solution, SearchQuery.Create(name, ignoreCase), filter, cancellationToken);
            }
        }

        private static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsyncImpl(
            Solution solution, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            var result = new List<ISymbol>();
            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                var symbols = await FindSourceDeclarationsAsyncImpl(project, query, filter, cancellationToken).ConfigureAwait(false);
                result.AddRange(symbols);
            }

            return result;
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, string name, bool ignoreCase, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(project, name, ignoreCase, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the symbols for declarations made in source with the specified name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, string name, bool ignoreCase, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
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
                return SpecializedTasks.EmptyEnumerable<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Name_FindSourceDeclarationsAsync, cancellationToken))
            {
                return FindSourceDeclarationsAsyncImpl(project, SearchQuery.Create(name, ignoreCase), filter, cancellationToken);
            }
        }

        private static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsyncImpl(
            Project project, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            var list = new List<ISymbol>();
            await AddDeclarationsAsync(project, query, filter, list, cancellationToken).ConfigureAwait(false);
            return list;
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, Func<string, bool> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(solution, predicate, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(solution, SearchQuery.CreateCustom(predicate), filter, cancellationToken);
        }

        internal static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Solution solution, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Solution_Predicate_FindSourceDeclarationsAsync, cancellationToken))
            {
                var result = new List<ISymbol>();
                foreach (var projectId in solution.ProjectIds)
                {
                    var project = solution.GetProject(projectId);
                    var symbols = await FindSourceDeclarationsAsync(project, query, filter, cancellationToken).ConfigureAwait(false);
                    result.AddRange(symbols);
                }

                return result;
            }
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, Func<string, bool> predicate, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(project, predicate, SymbolFilter.All, cancellationToken);
        }

        /// <summary>
        /// Find the symbols for declarations made in source with a matching name.
        /// </summary>
        public static Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, Func<string, bool> predicate, SymbolFilter filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindSourceDeclarationsAsync(project, SearchQuery.CreateCustom(predicate), filter, cancellationToken);
        }

        internal static async Task<IEnumerable<ISymbol>> FindSourceDeclarationsAsync(Project project, SearchQuery query, SymbolFilter filter, CancellationToken cancellationToken)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (query.Name != null && string.IsNullOrWhiteSpace(query.Name))
            {
                return SpecializedCollections.EmptyEnumerable<ISymbol>();
            }

            using (Logger.LogBlock(FunctionId.SymbolFinder_Project_Predicate_FindSourceDeclarationsAsync, cancellationToken))
            {
                var result = new List<ISymbol>();
                if (!await project.ContainsSymbolsWithNameAsync(query.GetPredicate(), filter, cancellationToken).ConfigureAwait(false))
                {
                    return result;
                }

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                result.AddRange(FilterByCriteria(compilation.GetSymbolsWithName(query.GetPredicate(), filter, cancellationToken), filter));
                return result;
            }
        }

        internal static IEnumerable<ISymbol> FilterByCriteria(IEnumerable<ISymbol> symbols, SymbolFilter criteria)
        {
            foreach (var symbol in symbols)
            {
                if (symbol.IsImplicitlyDeclared || symbol.IsAccessor())
                {
                    continue;
                }

                if (MeetCriteria(symbol, criteria))
                {
                    yield return symbol;
                }
            }
        }

        private static bool MeetCriteria(ISymbol symbol, SymbolFilter filter)
        {
            if (IsOn(filter, SymbolFilter.Namespace) && symbol.Kind == SymbolKind.Namespace)
            {
                return true;
            }

            if (IsOn(filter, SymbolFilter.Type) && symbol is ITypeSymbol)
            {
                return true;
            }

            if (IsOn(filter, SymbolFilter.Member) && IsNonTypeMember(symbol))
            {
                return true;
            }

            return false;
        }

        private static bool IsNonTypeMember(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Method ||
                   symbol.Kind == SymbolKind.Property ||
                   symbol.Kind == SymbolKind.Event ||
                   symbol.Kind == SymbolKind.Field;
        }

        private static bool IsOn(SymbolFilter filter, SymbolFilter flag)
        {
            return (filter & flag) == flag;
        }
    }
}