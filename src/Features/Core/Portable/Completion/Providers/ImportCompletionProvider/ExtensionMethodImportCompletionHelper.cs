// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    /// <summary>
    /// Provides completion items for extension methods from unimported namespace.
    /// </summary>
    /// <remarks>It runs out-of-proc if it's enabled</remarks>
    internal static partial class ExtensionMethodImportCompletionHelper
    {
        private static readonly object s_gate = new();
        private static Task s_indexingTask = Task.CompletedTask;

        public static async Task WarmUpCacheAsync(Document document, CancellationToken cancellationToken)
        {
            var project = document.Project;
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var result = await client.TryInvokeAsync<IRemoteExtensionMethodImportCompletionService>(
                    project,
                    (service, solutionInfo, cancellationToken) => service.WarmUpCacheAsync(
                        solutionInfo, document.Id, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await WarmUpCacheInCurrentProcessAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task WarmUpCacheInCurrentProcessAsync(Document document, CancellationToken cancellationToken)
        {
            var cacheService = GetCacheService(document.Project.Solution.Workspace);
            return ExtensionMethodSymbolComputer.PopulateIndicesAsync(document.Project, cacheService, cancellationToken);
        }

        public static async Task<SerializableUnimportedExtensionMethods?> GetUnimportedExtensionMethodsAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            ImmutableArray<ITypeSymbol> targetTypesSymbols,
            bool forceIndexCreation,
            bool hideAdvancedMembers,
            CancellationToken cancellationToken)
        {
            var project = document.Project;
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var receiverTypeSymbolKeyData = SymbolKey.CreateString(receiverTypeSymbol, cancellationToken);
                var targetTypesSymbolKeyData = targetTypesSymbols.SelectAsArray(s => SymbolKey.CreateString(s, cancellationToken));

                // Call the project overload.  Add-import-for-extension-method doesn't search outside of the current
                // project cone.
                var result = await client.TryInvokeAsync<IRemoteExtensionMethodImportCompletionService, SerializableUnimportedExtensionMethods?>(
                     project,
                     (service, solutionInfo, cancellationToken) => service.GetUnimportedExtensionMethodsAsync(
                         solutionInfo, document.Id, position, receiverTypeSymbolKeyData, namespaceInScope.ToImmutableArray(),
                         targetTypesSymbolKeyData, forceIndexCreation, hideAdvancedMembers, cancellationToken),
                     cancellationToken).ConfigureAwait(false);

                return result.HasValue ? result.Value : null;
            }
            else
            {
                return await GetUnimportedExtensionMethodsInCurrentProcessAsync(
                    document, position, receiverTypeSymbol, namespaceInScope, targetTypesSymbols, forceIndexCreation, hideAdvancedMembers, isRemote: false, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public static async Task<SerializableUnimportedExtensionMethods> GetUnimportedExtensionMethodsInCurrentProcessAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            ImmutableArray<ITypeSymbol> targetTypes,
            bool forceIndexCreation,
            bool hideAdvancedMembers,
            bool isRemote,
            CancellationToken cancellationToken)
        {
            var ticks = Environment.TickCount;

            // First find symbols of all applicable extension methods.
            // Workspace's syntax/symbol index is used to avoid iterating every method symbols in the solution.
            var symbolComputer = await ExtensionMethodSymbolComputer.CreateAsync(
                document, position, receiverTypeSymbol, namespaceInScope, cancellationToken).ConfigureAwait(false);
            var (extentsionMethodSymbols, isPartialResult) = await symbolComputer.GetExtensionMethodSymbolsAsync(forceIndexCreation, hideAdvancedMembers, cancellationToken).ConfigureAwait(false);

            var getSymbolsTicks = Environment.TickCount - ticks;
            ticks = Environment.TickCount;

            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var items = ConvertSymbolsToCompletionItems(compilation, extentsionMethodSymbols, targetTypes, cancellationToken);

            // If we don't have all the indices available already, queue a backgrounds task to create them.
            if (isPartialResult)
            {
                lock (s_gate)
                {
                    // We use a very simple approach to build the cache in the background:
                    // queue a new task only if the previous task is completed. This is to avoid
                    // queueing calculation for the same set of references repeatedly while
                    // index is being constrcuted, which might take some time.
                    if (s_indexingTask.IsCompleted)
                    {
                        // When building cache in the background, make sure we always use latest snapshot with full semantic
                        var id = document.Id;
                        var workspace = document.Project.Solution.Workspace;
                        s_indexingTask = Task.Run(() => symbolComputer.PopulateIndicesAsync(workspace.CurrentSolution.GetDocument(id)?.Project, CancellationToken.None), CancellationToken.None);
                    }
                }
            }

            var createItemsTicks = Environment.TickCount - ticks;

            return new SerializableUnimportedExtensionMethods(items, isPartialResult, getSymbolsTicks, createItemsTicks, isRemote);
        }

        private static ImmutableArray<SerializableImportCompletionItem> ConvertSymbolsToCompletionItems(
            Compilation compilation, ImmutableArray<IMethodSymbol> extentsionMethodSymbols, ImmutableArray<ITypeSymbol> targetTypeSymbols, CancellationToken cancellationToken)
        {
            Dictionary<ITypeSymbol, bool> typeConvertibilityCache = new();
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

                // Select the overload convertable to any targeted type (if any) and with minimum number of parameters to display
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
            using var _3 = ArrayBuilder<SerializableImportCompletionItem>.GetInstance(out var itemsBuilder);

            foreach (var ((containingNamespace, _, _), (bestSymbol, overloadCount, includeInTargetTypedCompletion)) in overloadMap)
            {
                // To display the count of of additional overloads, we need to substract total by 1.
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

            return itemsBuilder.ToImmutable();
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
    }
}
