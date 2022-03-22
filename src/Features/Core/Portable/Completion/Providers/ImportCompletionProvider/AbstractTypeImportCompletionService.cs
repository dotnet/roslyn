// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.Shared.Utilities.EditorBrowsableHelpers;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractTypeImportCompletionService : ITypeImportCompletionService
    {
        private IImportCompletionCacheService<TypeImportCompletionCacheEntry, TypeImportCompletionCacheEntry> CacheService { get; }

        protected abstract string GenericTypeSuffix { get; }

        protected abstract bool IsCaseSensitive { get; }

        protected abstract string Language { get; }

        internal AbstractTypeImportCompletionService(Workspace workspace)
        {
            CacheService = workspace.Services.GetRequiredService<IImportCompletionCacheService<TypeImportCompletionCacheEntry, TypeImportCompletionCacheEntry>>();
        }

        public void QueueCacheWarmUpTask(Project project)
        {
            CacheService.WorkQueue.AddWork(project);
        }

        public async Task<(ImmutableArray<ImmutableArray<CompletionItem>>, bool)> GetAllTopLevelTypesAsync(
            Project currentProject,
            SyntaxContext syntaxContext,
            bool forceCacheCreation,
            CompletionOptions options,
            CancellationToken cancellationToken)
        {
            var (getCacheResults, isPartialResult) = await GetCacheEntriesAsync(currentProject, forceCacheCreation, cancellationToken).ConfigureAwait(false);

            var currentCompilation = await currentProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            return (getCacheResults.SelectAsArray(GetItemsFromCacheResult), isPartialResult);

            ImmutableArray<CompletionItem> GetItemsFromCacheResult(TypeImportCompletionCacheEntry cacheEntry)
                => cacheEntry.GetItemsForContext(
                    currentCompilation,
                    Language,
                    GenericTypeSuffix,
                    syntaxContext.IsAttributeNameContext,
                    IsCaseSensitive,
                    options.HideAdvancedMembers);
        }

        private async Task<(ImmutableArray<TypeImportCompletionCacheEntry> results, bool isPartial)> GetCacheEntriesAsync(Project currentProject, bool forceCacheCreation, CancellationToken cancellationToken)
        {
            try
            {
                var isPartialResult = false;
                using var _1 = ArrayBuilder<TypeImportCompletionCacheEntry>.GetInstance(out var resultBuilder);
                using var _2 = ArrayBuilder<Project>.GetInstance(out var projectsBuilder);
                using var _3 = PooledHashSet<ProjectId>.GetInstance(out var nonGlobalAliasedProjectReferencesSet);

                var solution = currentProject.Solution;
                var graph = solution.GetProjectDependencyGraph();
                var referencedProjects = graph.GetProjectsThatThisProjectTransitivelyDependsOn(currentProject.Id).Select(id => solution.GetRequiredProject(id)).Where(p => p.SupportsCompilation);

                projectsBuilder.Add(currentProject);
                projectsBuilder.AddRange(referencedProjects);
                nonGlobalAliasedProjectReferencesSet.AddRange(currentProject.ProjectReferences.Where(pr => !HasGlobalAlias(pr.Aliases)).Select(pr => pr.ProjectId));

                foreach (var project in projectsBuilder)
                {
                    var projectId = project.Id;
                    if (nonGlobalAliasedProjectReferencesSet.Contains(projectId))
                        continue;

                    if (forceCacheCreation)
                    {
                        var upToDateCacheEntry = await GetUpToDateCacheForProjectAsync(project, cancellationToken).ConfigureAwait(false);
                        resultBuilder.Add(upToDateCacheEntry);
                    }
                    else if (CacheService.ProjectItemsCache.TryGetValue(projectId, out var cacheEntry))
                    {
                        resultBuilder.Add(cacheEntry);
                    }
                    else
                    {
                        isPartialResult = true;
                    }
                }

                var originCompilation = await currentProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var editorBrowsableInfo = new Lazy<EditorBrowsableInfo>(() => new EditorBrowsableInfo(originCompilation));
                foreach (var peReference in currentProject.MetadataReferences.OfType<PortableExecutableReference>())
                {
                    // Can't cache items for reference with null key. We don't want risk potential perf regression by 
                    // making those items repeatedly, so simply not returning anything from this assembly, until 
                    // we have a better understanding on this scenario.
                    var peReferenceKey = GetPEReferenceCacheKey(peReference);
                    if (peReferenceKey is null || !HasGlobalAlias(peReference.Properties.Aliases))
                        continue;

                    if (forceCacheCreation)
                    {
                        if (TryGetUpToDateCacheForPEReference(originCompilation, solution, editorBrowsableInfo.Value, peReference, cancellationToken, out var upToDateCacheEntry))
                        {
                            resultBuilder.Add(upToDateCacheEntry);
                        }
                    }
                    else if (CacheService.PEItemsCache.TryGetValue(peReferenceKey, out var cacheEntry))
                    {
                        resultBuilder.Add(cacheEntry);
                    }
                    else
                    {
                        isPartialResult = true;
                    }
                }

                return (resultBuilder.ToImmutable(), isPartialResult);
            }
            finally
            {
                if (!forceCacheCreation)
                    CacheService.WorkQueue.AddWork(currentProject);
            }
        }

        public static async ValueTask BatchUpdateCacheAsync(ImmutableArray<Project> projects, CancellationToken cancellationToken)
        {
            var latestProjects = CompletionUtilities.GetDistinctProjectsFromLatestSolutionSnapshot(projects);
            foreach (var project in latestProjects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var service = (AbstractTypeImportCompletionService)project.GetRequiredLanguageService<ITypeImportCompletionService>();
                _ = await service.GetCacheEntriesAsync(project, forceCacheCreation: true, cancellationToken).ConfigureAwait(false);
            }
        }

        private static bool HasGlobalAlias(ImmutableArray<string> aliases)
            => aliases.IsEmpty || aliases.Any(alias => alias == MetadataReferenceProperties.GlobalAlias);

        private static string? GetPEReferenceCacheKey(PortableExecutableReference peReference)
            => peReference.FilePath ?? peReference.Display;

        /// <summary>
        /// Get appropriate completion items for all the visible top level types from given project. 
        /// This method is intended to be used for getting types from source only, so the project must support compilation. 
        /// For getting types from PE, use <see cref="TryGetUpToDateCacheForPEReference"/>.
        /// </summary>
        private async Task<TypeImportCompletionCacheEntry> GetUpToDateCacheForProjectAsync(Project project, CancellationToken cancellationToken)
        {
            // Since we only need top level types from source, therefore we only care if source symbol checksum changes.
            var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            return CreateCacheWorker(
                project.Id,
                compilation.Assembly,
                checksum,
                CacheService.ProjectItemsCache,
                new EditorBrowsableInfo(compilation),
                cancellationToken);
        }

        /// <summary>
        /// Get appropriate completion items for all the visible top level types from given PE reference.
        /// </summary>
        private bool TryGetUpToDateCacheForPEReference(
            Compilation originCompilation,
            Solution solution,
            EditorBrowsableInfo editorBrowsableInfo,
            PortableExecutableReference peReference,
            CancellationToken cancellationToken,
            out TypeImportCompletionCacheEntry cacheEntry)
        {
            if (originCompilation.GetAssemblyOrModuleSymbol(peReference) is not IAssemblySymbol assemblySymbol)
            {
                cacheEntry = default;
                return false;
            }
            else
            {
                cacheEntry = CreateCacheWorker(
                    GetPEReferenceCacheKey(peReference)!,
                    assemblySymbol,
                    checksum: SymbolTreeInfo.GetMetadataChecksum(solution, peReference, cancellationToken),
                    CacheService.PEItemsCache,
                    editorBrowsableInfo,
                    cancellationToken);
                return true;
            }
        }

        private TypeImportCompletionCacheEntry CreateCacheWorker<TKey>(
            TKey key,
            IAssemblySymbol assembly,
            Checksum checksum,
            IDictionary<TKey, TypeImportCompletionCacheEntry> cache,
            EditorBrowsableInfo editorBrowsableInfo,
            CancellationToken cancellationToken)
            where TKey : notnull
        {
            // Cache hit
            if (cache.TryGetValue(key, out var cacheEntry) && cacheEntry.Checksum == checksum)
            {
                return cacheEntry;
            }

            using var builder = new TypeImportCompletionCacheEntry.Builder(SymbolKey.Create(assembly, cancellationToken), checksum, Language, GenericTypeSuffix, editorBrowsableInfo);
            GetCompletionItemsForTopLevelTypeDeclarations(assembly.GlobalNamespace, builder, cancellationToken);
            cacheEntry = builder.ToReferenceCacheEntry();
            cache[key] = cacheEntry;

            return cacheEntry;
        }

        private static void GetCompletionItemsForTopLevelTypeDeclarations(
            INamespaceSymbol rootNamespaceSymbol,
            TypeImportCompletionCacheEntry.Builder builder,
            CancellationToken cancellationToken)
        {
            VisitNamespace(rootNamespaceSymbol, containingNamespace: null, builder, cancellationToken);
            return;

            static void VisitNamespace(
                INamespaceSymbol symbol,
                string? containingNamespace,
                TypeImportCompletionCacheEntry.Builder builder,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                containingNamespace = CompletionHelper.ConcatNamespace(containingNamespace, symbol.Name);

                foreach (var memberNamespace in symbol.GetNamespaceMembers())
                {
                    VisitNamespace(memberNamespace, containingNamespace, builder, cancellationToken);
                }

                using var _ = PooledDictionary<string, TypeOverloadInfo>.GetInstance(out var overloads);
                var types = symbol.GetTypeMembers();

                // Iterate over all top level internal and public types, keep track of "type overloads".
                foreach (var type in types)
                {
                    // No need to check accessibility here, since top level types can only be internal or public.
                    if (type.CanBeReferencedByName)
                    {
                        overloads.TryGetValue(type.Name, out var overloadInfo);
                        overloads[type.Name] = overloadInfo.Aggregate(type);
                    }
                }

                foreach (var pair in overloads)
                {
                    var overloadInfo = pair.Value;

                    // Create CompletionItem for non-generic type overload, if exists.
                    if (overloadInfo.NonGenericOverload != null)
                    {
                        builder.AddItem(
                            overloadInfo.NonGenericOverload,
                            containingNamespace,
                            overloadInfo.NonGenericOverload.DeclaredAccessibility == Accessibility.Public);
                    }

                    // Create one CompletionItem for all generic type overloads, if there's any.
                    // For simplicity, we always show the type symbol with lowest arity in CompletionDescription
                    // and without displaying the total number of overloads.
                    if (overloadInfo.BestGenericOverload != null)
                    {
                        // If any of the generic overloads is public, then the completion item is considered public.
                        builder.AddItem(
                            overloadInfo.BestGenericOverload,
                            containingNamespace,
                            overloadInfo.ContainsPublicGenericOverload);
                    }
                }
            }
        }

        private readonly struct TypeOverloadInfo
        {
            public TypeOverloadInfo(INamedTypeSymbol nonGenericOverload, INamedTypeSymbol bestGenericOverload, bool containsPublicGenericOverload)
            {
                NonGenericOverload = nonGenericOverload;
                BestGenericOverload = bestGenericOverload;
                ContainsPublicGenericOverload = containsPublicGenericOverload;
            }

            public INamedTypeSymbol NonGenericOverload { get; }

            // Generic with fewest type parameters is considered best symbol to show in description.
            public INamedTypeSymbol BestGenericOverload { get; }

            public bool ContainsPublicGenericOverload { get; }

            public TypeOverloadInfo Aggregate(INamedTypeSymbol type)
            {
                if (type.Arity == 0)
                {
                    return new TypeOverloadInfo(nonGenericOverload: type, BestGenericOverload, ContainsPublicGenericOverload);
                }

                // We consider generic with fewer type parameters better symbol to show in description
                var newBestGenericOverload = BestGenericOverload == null || type.Arity < BestGenericOverload.Arity
                    ? type
                    : BestGenericOverload;

                var newContainsPublicGenericOverload = type.DeclaredAccessibility >= Accessibility.Public || ContainsPublicGenericOverload;

                return new TypeOverloadInfo(NonGenericOverload, newBestGenericOverload, newContainsPublicGenericOverload);
            }
        }
    }
}
