// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

/// <summary>
/// Provides completion items for extension methods from unimported namespace.
/// </summary>
/// <remarks>It runs out-of-proc if it's enabled</remarks>
internal static partial class ExtensionMemberImportCompletionHelper
{
    /// <summary>
    /// Technically never gets cleared out.  However, as we're only really storing information about extension
    /// methods in a project, this should not be too bad.  It's only really a leak if the project is truly 
    /// unloaded, and not too bad in that case.
    /// </summary>
    private static readonly ConcurrentDictionary<ProjectId, ExtensionMethodImportCompletionCacheEntry> s_projectItemsCache = new();

    public static async Task WarmUpCacheAsync(Project project, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var result = await client.TryInvokeAsync<IRemoteExtensionMemberImportCompletionService>(
                project,
                (service, solutionInfo, cancellationToken) => service.WarmUpCacheAsync(
                    solutionInfo, project.Id, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            WarmUpCacheInCurrentProcess(project);
        }
    }

    public static void WarmUpCacheInCurrentProcess(Project project)
        => SymbolComputer.QueueCacheWarmUpTask(project);

    public static async Task<ImmutableArray<SerializableImportCompletionItem>> GetUnimportedExtensionMembersAsync(
        SyntaxContext syntaxContext,
        ITypeSymbol receiverTypeSymbol,
        ISet<string> namespaceInScope,
        ImmutableArray<ITypeSymbol> targetTypesSymbols,
        bool forceCacheCreation,
        bool hideAdvancedMembers,
        CancellationToken cancellationToken)
    {
        var document = syntaxContext.Document;
        var position = syntaxContext.Position;
        var project = document.Project;

        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var receiverTypeSymbolKeyData = SymbolKey.CreateString(receiverTypeSymbol, cancellationToken);
            var targetTypesSymbolKeyData = targetTypesSymbols.SelectAsArray(s => SymbolKey.CreateString(s, cancellationToken));

            // Call the project overload.  Add-import-for-extension-method doesn't search outside of the current
            // project cone.
            var remoteResult = await client.TryInvokeAsync<IRemoteExtensionMemberImportCompletionService, ImmutableArray<SerializableImportCompletionItem>>(
                 project,
                 (service, solutionInfo, cancellationToken) => service.GetUnimportedExtensionMethodsAsync(
                     solutionInfo, document.Id, position, receiverTypeSymbolKeyData, [.. namespaceInScope],
                     targetTypesSymbolKeyData, forceCacheCreation, hideAdvancedMembers, cancellationToken),
                 cancellationToken).ConfigureAwait(false);

            return remoteResult.HasValue ? remoteResult.Value : default;
        }
        else
        {
            return await GetUnimportedExtensionMembersInCurrentProcessAsync(
                document, syntaxContext.SemanticModel, position, receiverTypeSymbol, namespaceInScope, targetTypesSymbols, forceCacheCreation, hideAdvancedMembers, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public static async Task<ImmutableArray<SerializableImportCompletionItem>> GetUnimportedExtensionMembersInCurrentProcessAsync(
        Document document,
        SemanticModel? semanticModel,
        int position,
        ITypeSymbol receiverTypeSymbol,
        ISet<string> namespaceInScope,
        ImmutableArray<ITypeSymbol> targetTypes,
        bool forceCacheCreation,
        bool hideAdvancedMembers,
        CancellationToken cancellationToken)
    {
        // First find symbols of all applicable extension methods.
        // Workspace's syntax/symbol index is used to avoid iterating every method symbols in the solution.
        semanticModel ??= await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbolComputer = new SymbolComputer(
            document, semanticModel, receiverTypeSymbol, position, namespaceInScope);
        var extensionMethodSymbols = await symbolComputer.GetExtensionMethodSymbolsAsync(forceCacheCreation, hideAdvancedMembers, cancellationToken).ConfigureAwait(false);

        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var items = ConvertSymbolsToCompletionItems(compilation, extensionMethodSymbols, targetTypes, cancellationToken);

        return items;
    }

    public static async ValueTask BatchUpdateCacheAsync(ImmutableSegmentedList<Project> projects, CancellationToken cancellationToken)
    {
        var latestProjects = CompletionUtilities.GetDistinctProjectsFromLatestSolutionSnapshot(projects);
        foreach (var project in latestProjects)
        {
            await SymbolComputer.UpdateCacheAsync(project, cancellationToken).ConfigureAwait(false);
        }
    }

    private static ImmutableArray<SerializableImportCompletionItem> ConvertSymbolsToCompletionItems(
        Compilation compilation, ImmutableArray<IMethodSymbol> extentsionMethodSymbols, ImmutableArray<ITypeSymbol> targetTypeSymbols, CancellationToken cancellationToken)
    {
        Dictionary<ITypeSymbol, bool> typeConvertibilityCache = [];
        using var _1 = PooledDictionary<INamespaceSymbol, string>.GetInstance(out var namespaceNameCache);
        using var _2 = PooledDictionary<(string containingNamespace, string methodName, bool isGeneric), (IMethodSymbol bestSymbol, int overloadCount, bool includeInTargetTypedCompletion)>
            .GetInstance(out var overloadMap);

        // Aggregate overloads
        foreach (var symbol in extentsionMethodSymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IMethodSymbol bestSymbol;
            int overloadCount;
            var includeInTargetTypedCompletion = ShouldIncludeInTargetTypedCompletion(compilation, symbol, targetTypeSymbols, typeConvertibilityCache);

            var containingNamespacename = GetFullyQualifiedNamespaceName(symbol.ContainingNamespace, namespaceNameCache);
            var overloadKey = (containingNamespacename, symbol.Name, isGeneric: symbol.Arity > 0);

            // Select the overload convertible to any targeted type (if any) and with minimum number of parameters to display
            if (overloadMap.TryGetValue(overloadKey, out var currentValue))
            {
                if (currentValue.includeInTargetTypedCompletion == includeInTargetTypedCompletion)
                {
                    bestSymbol = currentValue.bestSymbol.Parameters.Length > symbol.Parameters.Length ? symbol : currentValue.bestSymbol;
                }
                else if (currentValue.includeInTargetTypedCompletion)
                {
                    bestSymbol = currentValue.bestSymbol;
                }
                else
                {
                    bestSymbol = symbol;
                }

                overloadCount = currentValue.overloadCount + 1;
                includeInTargetTypedCompletion = includeInTargetTypedCompletion || currentValue.includeInTargetTypedCompletion;
            }
            else
            {
                bestSymbol = symbol;
                overloadCount = 1;
            }

            overloadMap[overloadKey] = (bestSymbol, overloadCount, includeInTargetTypedCompletion);
        }

        // Then convert symbols into completion items
        var itemsBuilder = new FixedSizeArrayBuilder<SerializableImportCompletionItem>(overloadMap.Count);

        foreach (var ((containingNamespace, _, _), (bestSymbol, overloadCount, includeInTargetTypedCompletion)) in overloadMap)
        {
            // To display the count of additional overloads, we need to subtract total by 1.
            var item = new SerializableImportCompletionItem(
                SymbolKey.CreateString(bestSymbol, cancellationToken),
                bestSymbol.Name,
                bestSymbol.Arity,
                bestSymbol.GetGlyph(),
                containingNamespace,
                additionalOverloadCount: overloadCount - 1,
                includeInTargetTypedCompletion);

            itemsBuilder.Add(item);
        }

        return itemsBuilder.MoveToImmutable();
    }

    private static bool ShouldIncludeInTargetTypedCompletion(
        Compilation compilation, IMethodSymbol methodSymbol, ImmutableArray<ITypeSymbol> targetTypeSymbols,
        Dictionary<ITypeSymbol, bool> typeConvertibilityCache)
    {
        if (methodSymbol.ReturnsVoid || methodSymbol.ReturnType == null || targetTypeSymbols.IsEmpty)
        {
            return false;
        }

        if (typeConvertibilityCache.TryGetValue(methodSymbol.ReturnType, out var isConvertible))
        {
            return isConvertible;
        }

        isConvertible = CompletionUtilities.IsTypeImplicitlyConvertible(compilation, methodSymbol.ReturnType, targetTypeSymbols);
        typeConvertibilityCache[methodSymbol.ReturnType] = isConvertible;

        return isConvertible;
    }

    private static string GetFullyQualifiedNamespaceName(INamespaceSymbol symbol, Dictionary<INamespaceSymbol, string> stringCache)
    {
        if (symbol.ContainingNamespace == null || symbol.ContainingNamespace.IsGlobalNamespace)
        {
            return symbol.Name;
        }

        if (stringCache.TryGetValue(symbol, out var name))
        {
            return name;
        }

        name = GetFullyQualifiedNamespaceName(symbol.ContainingNamespace, stringCache) + "." + symbol.Name;
        stringCache[symbol] = name;
        return name;
    }

    private static async Task<ExtensionMethodImportCompletionCacheEntry> GetUpToDateCacheEntryAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        var projectId = project.Id;

        // While we are caching data from SyntaxTreeInfo, all the things we cared about here are actually based on sources symbols.
        // So using source symbol checksum would suffice.
        var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);

        // Cache miss, create all requested items.
        if (!s_projectItemsCache.TryGetValue(projectId, out var cacheEntry) ||
            cacheEntry.Checksum != checksum ||
            cacheEntry.Language != project.Language)
        {
            var syntaxFacts = project.Services.GetRequiredService<ISyntaxFactsService>();
            var builder = new ExtensionMethodImportCompletionCacheEntry.Builder(checksum, project.Language, syntaxFacts.StringComparer);

            foreach (var document in project.Documents)
            {
                // Don't look for extension methods in generated code.
                if (document.State.Attributes.IsGenerated)
                {
                    continue;
                }

                var info = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
                if (info.ContainsExtensionMethod)
                {
                    builder.AddItem(info);
                }
            }

            cacheEntry = builder.ToCacheEntry();
            s_projectItemsCache[projectId] = cacheEntry;
        }

        return cacheEntry;
    }
}
