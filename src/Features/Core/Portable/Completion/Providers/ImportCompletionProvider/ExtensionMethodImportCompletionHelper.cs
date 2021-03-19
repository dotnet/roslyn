// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
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

        public static async Task<ImmutableArray<SerializableImportCompletionItem>> GetUnimportedExtensionMethodsAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            ImmutableArray<ITypeSymbol> targetTypesSymbols,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            SerializableUnimportedExtensionMethods items;

            var ticks = Environment.TickCount;

            var project = document.Project;
            var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client != null)
            {
                var receiverTypeSymbolKeyData = SymbolKey.CreateString(receiverTypeSymbol, cancellationToken);
                var targetTypesSymbolKeyData = targetTypesSymbols.SelectAsArray(s => SymbolKey.CreateString(s, cancellationToken));

                var result = await client.TryInvokeAsync<IRemoteExtensionMethodImportCompletionService, SerializableUnimportedExtensionMethods>(
                    project.Solution,
                    (service, solutionInfo, cancellationToken) => service.GetUnimportedExtensionMethodsAsync(
                        solutionInfo, document.Id, position, receiverTypeSymbolKeyData, namespaceInScope.ToImmutableArray(),
                        targetTypesSymbolKeyData, forceIndexCreation, cancellationToken),
                    cancellationToken).ConfigureAwait(false);

                if (!result.HasValue)
                {
                    return ImmutableArray<SerializableImportCompletionItem>.Empty;
                }

                items = result.Value;
            }
            else
            {
                items = await GetUnimportedExtensionMethodsInCurrentProcessAsync(
                    document, position, receiverTypeSymbol, namespaceInScope, targetTypesSymbols, forceIndexCreation, cancellationToken)
                    .ConfigureAwait(false);
            }

            // report telemetry:
            var totalTicks = Environment.TickCount - ticks;

            CompletionProvidersLogger.LogExtensionMethodCompletionTicksDataPoint(totalTicks);
            CompletionProvidersLogger.LogExtensionMethodCompletionMethodsProvidedDataPoint(items.CompletionItems.Length);
            CompletionProvidersLogger.LogExtensionMethodCompletionGetSymbolsTicksDataPoint(items.GetSymbolsTicks);
            CompletionProvidersLogger.LogExtensionMethodCompletionCreateItemsTicksDataPoint(items.CreateItemsTicks);

            if (items.IsPartialResult)
            {
                CompletionProvidersLogger.LogExtensionMethodCompletionPartialResultCount();
            }

            return items.CompletionItems;
        }

        public static async Task<SerializableUnimportedExtensionMethods> GetUnimportedExtensionMethodsInCurrentProcessAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            ImmutableArray<ITypeSymbol> targetTypes,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            var ticks = Environment.TickCount;

            // First find symbols of all applicable extension methods.
            // Workspace's syntax/symbol index is used to avoid iterating every method symbols in the solution.
            var symbolComputer = await ExtensionMethodSymbolComputer.CreateAsync(
                document, position, receiverTypeSymbol, namespaceInScope, cancellationToken).ConfigureAwait(false);
            var (extentsionMethodSymbols, isPartialResult) = await symbolComputer.GetExtensionMethodSymbolsAsync(forceIndexCreation, cancellationToken).ConfigureAwait(false);

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
                        s_indexingTask = symbolComputer.PopulateIndicesAsync(CancellationToken.None);
                    }
                }
            }

            var createItemsTicks = Environment.TickCount - ticks;

            return new SerializableUnimportedExtensionMethods(items, isPartialResult, getSymbolsTicks, createItemsTicks);

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
