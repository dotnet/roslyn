// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.Shared.Utilities.EditorBrowsableHelpers;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
    internal abstract partial class AbstractTypeImportCompletionService : ITypeImportCompletionService
    {
        private static readonly object s_gate = new();
        private static Task s_cachingTask = Task.CompletedTask;

        private IImportCompletionCacheService<CacheEntry, CacheEntry> CacheService { get; }

        protected abstract string GenericTypeSuffix { get; }

        protected abstract bool IsCaseSensitive { get; }

        protected abstract string Language { get; }

        internal AbstractTypeImportCompletionService(Workspace workspace)
            => CacheService = workspace.Services.GetRequiredService<IImportCompletionCacheService<CacheEntry, CacheEntry>>();

        public Task WarmUpCacheAsync(Project? project, CancellationToken cancellationToken)
        {
            return project is null
                ? Task.CompletedTask
                : GetCacheEntriesAsync(project, forceCacheCreation: true, cancellationToken);
        }

        public async Task<ImmutableArray<ImmutableArray<CompletionItem>>?> GetAllTopLevelTypesAsync(
            Project currentProject,
            SyntaxContext syntaxContext,
            bool forceCacheCreation,
            CompletionOptions options,
            CancellationToken cancellationToken)
        {
            var (getCacheResults, isPartialResult) = await GetCacheEntriesAsync(currentProject, forceCacheCreation, cancellationToken).ConfigureAwait(false);

            if (isPartialResult)
            {
                // We use a very simple approach to build the cache in the background:
                // queue a new task only if the previous task is completed, regardless of what
                // that task is doing.
                lock (s_gate)
                {
                    if (s_cachingTask.IsCompleted)
                    {
                        // When building cache in the background, make sure we always use latest snapshot with full semantic
                        var projectId = currentProject.Id;
                        var workspace = currentProject.Solution.Workspace;
                        s_cachingTask = Task.Run(() => WarmUpCacheAsync(workspace.CurrentSolution.GetProject(projectId), CancellationToken.None), CancellationToken.None);
                    }
                }
            }

            var currentCompilation = await currentProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            return getCacheResults.SelectAsArray(GetItemsFromCacheResult);

            ImmutableArray<CompletionItem> GetItemsFromCacheResult(GetCacheResult cacheResult)
            {
                return cacheResult.Entry.GetItemsForContext(
                         Language,
                         GenericTypeSuffix,
                         currentCompilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(cacheResult.Assembly),
                         syntaxContext.IsAttributeNameContext,
                         IsCaseSensitive,
                         options.HideAdvancedMembers);
            }
        }

        private async Task<(ImmutableArray<GetCacheResult> results, bool isPartial)> GetCacheEntriesAsync(Project currentProject, bool forceCacheCreation, CancellationToken cancellationToken)
        {
            var isPartialResult = false;
            var _ = ArrayBuilder<GetCacheResult>.GetInstance(out var builder);

            var currentCompilation = await currentProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var editorBrowsableInfo = new Lazy<EditorBrowsableInfo>(() => new EditorBrowsableInfo(currentCompilation));

            var cacheResult = await GetCacheForProjectAsync(currentProject, forceCacheCreation: true, editorBrowsableInfo, cancellationToken).ConfigureAwait(false);

            // We always force create a cache for current project.
            Contract.ThrowIfFalse(cacheResult.HasValue);
            builder.Add(cacheResult.Value);

            var solution = currentProject.Solution;
            var graph = solution.GetProjectDependencyGraph();
            var referencedProjects = graph.GetProjectsThatThisProjectTransitivelyDependsOn(currentProject.Id).SelectAsArray(id => solution.GetRequiredProject(id));

            foreach (var referencedProject in referencedProjects.Where(p => p.SupportsCompilation))
            {
                var compilation = await referencedProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var assembly = SymbolFinder.FindSimilarSymbols(compilation.Assembly, currentCompilation, cancellationToken).SingleOrDefault();
                var metadataReference = assembly != null ? currentCompilation.GetMetadataReference(assembly) : null;

                if (HasGlobalAlias(metadataReference))
                {
                    cacheResult = await GetCacheForProjectAsync(
                        referencedProject,
                        forceCacheCreation,
                        editorBrowsableInfo: null,
                        cancellationToken).ConfigureAwait(false);

                    if (cacheResult.HasValue)
                    {
                        builder.Add(cacheResult.Value);
                    }
                    else
                    {
                        isPartialResult = true;
                    }
                }
            }

            foreach (var peReference in currentProject.MetadataReferences.OfType<PortableExecutableReference>())
            {
                if (HasGlobalAlias(peReference) &&
                    currentCompilation.GetAssemblyOrModuleSymbol(peReference) is IAssemblySymbol assembly &&
                    TryGetCacheForPEReference(solution, assembly, editorBrowsableInfo, peReference, forceCacheCreation, cancellationToken, out cacheResult))
                {
                    if (cacheResult.HasValue)
                    {
                        builder.Add(cacheResult.Value);
                    }
                    else
                    {
                        isPartialResult = true;
                    }
                }
            }

            return (builder.ToImmutable(), isPartialResult);

            static bool HasGlobalAlias(MetadataReference? metadataReference)
                => metadataReference != null && (metadataReference.Properties.Aliases.IsEmpty || metadataReference.Properties.Aliases.Any(alias => alias == MetadataReferenceProperties.GlobalAlias));
        }

        /// <summary>
        /// Get appropriate completion items for all the visible top level types from given project. 
        /// This method is intended to be used for getting types from source only, so the project must support compilation. 
        /// For getting types from PE, use <see cref="TryGetCacheForPEReference"/>.
        /// </summary>
        private async Task<GetCacheResult?> GetCacheForProjectAsync(
            Project project,
            bool forceCacheCreation,
            Lazy<EditorBrowsableInfo>? editorBrowsableInfo,
            CancellationToken cancellationToken)
        {
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Since we only need top level types from source, therefore we only care if source symbol checksum changes.
            var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);

            return GetCacheWorker(
                project.Id,
                compilation.Assembly,
                checksum,
                forceCacheCreation,
                CacheService.ProjectItemsCache,
                editorBrowsableInfo ?? new Lazy<EditorBrowsableInfo>(() => new EditorBrowsableInfo(compilation)),
                cancellationToken);
        }

        /// <summary>
        /// Get appropriate completion items for all the visible top level types from given PE reference.
        /// </summary>
        private bool TryGetCacheForPEReference(
            Solution solution,
            IAssemblySymbol assemblySymbol,
            Lazy<EditorBrowsableInfo> editorBrowsableInfo,
            PortableExecutableReference peReference,
            bool forceCacheCreation,
            CancellationToken cancellationToken,
            out GetCacheResult? result)
        {
            var key = peReference.FilePath ?? peReference.Display;
            if (key == null)
            {
                // Can't cache items for reference with null key. We don't want risk potential perf regression by 
                // making those items repeatedly, so simply not returning anything from this assembly, until 
                // we have a better understanding on this scenario.
                // TODO: Add telemetry
                result = null;
                return false;
            }

            var checksum = SymbolTreeInfo.GetMetadataChecksum(solution, peReference, cancellationToken);
            result = GetCacheWorker(
                key,
                assemblySymbol,
                checksum,
                forceCacheCreation,
                CacheService.PEItemsCache,
                editorBrowsableInfo,
                cancellationToken);
            return true;
        }

        // Returns null if cache miss and forceCacheCreation == false
        //
        // PERF:
        // Based on profiling results, initializing EditorBrowsableInfo upfront for each referenced
        // project every time a completion is triggered is expensive. Making them lazy would
        // eliminate this overhead when we have a cache hit while keeping it easy to share 
        // between original projects and PE references when trying to get completion items.
        private GetCacheResult? GetCacheWorker<TKey>(
            TKey key,
            IAssemblySymbol assembly,
            Checksum checksum,
            bool forceCacheCreation,
            IDictionary<TKey, CacheEntry> cache,
            Lazy<EditorBrowsableInfo> editorBrowsableInfo,
            CancellationToken cancellationToken)
            where TKey : notnull
        {
            // Cache hit
            if (cache.TryGetValue(key, out var cacheEntry) && cacheEntry.Checksum == checksum)
            {
                return new GetCacheResult(cacheEntry, assembly);
            }

            // Cache miss, create all items only when asked.
            if (forceCacheCreation)
            {
                using var builder = new CacheEntry.Builder(checksum, Language, GenericTypeSuffix, editorBrowsableInfo.Value);
                GetCompletionItemsForTopLevelTypeDeclarations(assembly.GlobalNamespace, builder, cancellationToken);
                cacheEntry = builder.ToReferenceCacheEntry();
                cache[key] = cacheEntry;

                return new GetCacheResult(cacheEntry, assembly);
            }

            return null;
        }

        private static void GetCompletionItemsForTopLevelTypeDeclarations(
            INamespaceSymbol rootNamespaceSymbol,
            CacheEntry.Builder builder,
            CancellationToken cancellationToken)
        {
            VisitNamespace(rootNamespaceSymbol, containingNamespace: null, builder, cancellationToken);
            return;

            static void VisitNamespace(
                INamespaceSymbol symbol,
                string? containingNamespace,
                CacheEntry.Builder builder,
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

        private readonly struct GetCacheResult
        {
            public CacheEntry Entry { get; }
            public IAssemblySymbol Assembly { get; }

            public GetCacheResult(CacheEntry entry, IAssemblySymbol assembly)
            {
                Entry = entry;
                Assembly = assembly;
            }
        }

        private readonly struct TypeImportCompletionItemInfo
        {
            private readonly ItemPropertyKind _properties;

            public TypeImportCompletionItemInfo(CompletionItem item, bool isPublic, bool isGeneric, bool isAttribute, bool isEditorBrowsableStateAdvanced)
            {
                Item = item;
                _properties = (isPublic ? ItemPropertyKind.IsPublic : 0)
                            | (isGeneric ? ItemPropertyKind.IsGeneric : 0)
                            | (isAttribute ? ItemPropertyKind.IsAttribute : 0)
                            | (isEditorBrowsableStateAdvanced ? ItemPropertyKind.IsEditorBrowsableStateAdvanced : 0);
            }

            public CompletionItem Item { get; }

            public bool IsPublic
                => (_properties & ItemPropertyKind.IsPublic) != 0;

            public bool IsGeneric
                => (_properties & ItemPropertyKind.IsGeneric) != 0;

            public bool IsAttribute
                => (_properties & ItemPropertyKind.IsAttribute) != 0;

            public bool IsEditorBrowsableStateAdvanced
                => (_properties & ItemPropertyKind.IsEditorBrowsableStateAdvanced) != 0;

            public TypeImportCompletionItemInfo WithItem(CompletionItem item)
                => new(item, IsPublic, IsGeneric, IsAttribute, IsEditorBrowsableStateAdvanced);

            [Flags]
            private enum ItemPropertyKind : byte
            {
                IsPublic = 0x1,
                IsGeneric = 0x2,
                IsAttribute = 0x4,
                IsEditorBrowsableStateAdvanced = 0x8,
            }
        }
    }
}
