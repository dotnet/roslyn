// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        private static readonly object s_gate = new object();
        private static Task s_indexingTask = Task.CompletedTask;

        public static async Task<ImmutableArray<SerializableImportCompletionItem>> GetUnimportedExtensionMethodsAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            async Task<(ImmutableArray<SerializableImportCompletionItem>, StatisticCounter)> GetItemsAsync()
            {
                var project = document.Project;

                var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var result = await client.RunRemoteAsync<(IList<SerializableImportCompletionItem> items, StatisticCounter counter)>(
                        WellKnownServiceHubService.CodeAnalysis,
                        nameof(IRemoteExtensionMethodImportCompletionService.GetUnimportedExtensionMethodsAsync),
                        project.Solution,
                        new object[] { document.Id, position, SymbolKey.CreateString(receiverTypeSymbol, cancellationToken), namespaceInScope.ToArray(), forceIndexCreation },
                        callbackTarget: null,
                        cancellationToken).ConfigureAwait(false);

                    return (result.items.ToImmutableArray(), result.counter);
                }

                return await GetUnimportedExtensionMethodsInCurrentProcessAsync(document, position, receiverTypeSymbol, namespaceInScope, forceIndexCreation, cancellationToken).ConfigureAwait(false);
            }

            var ticks = Environment.TickCount;

            var (items, counter) = await GetItemsAsync().ConfigureAwait(false);

            counter.TotalTicks = Environment.TickCount - ticks;
            counter.TotalExtensionMethodsProvided = items.Length;
            counter.Report();

            return items;
        }

        public static async Task<(ImmutableArray<SerializableImportCompletionItem>, StatisticCounter)> GetUnimportedExtensionMethodsInCurrentProcessAsync(
            Document document,
            int position,
            ITypeSymbol receiverTypeSymbol,
            ISet<string> namespaceInScope,
            bool forceIndexCreation,
            CancellationToken cancellationToken)
        {
            var counter = new StatisticCounter();
            var ticks = Environment.TickCount;

            // First find symbols of all applicable extension methods.
            // Workspace's syntax/symbol index is used to avoid iterating every method symbols in the solution.
            var symbolComputer = await ExtensionMethodSymbolComputer.CreateAsync(
                document, position, receiverTypeSymbol, namespaceInScope, cancellationToken).ConfigureAwait(false);
            var (extentsionMethodSymbols, isPartialResult) = await symbolComputer.GetExtensionMethodSymbolsAsync(forceIndexCreation, cancellationToken).ConfigureAwait(false);

            counter.GetSymbolsTicks = Environment.TickCount - ticks;
            ticks = Environment.TickCount;

            var items = ConvertSymbolsToCompletionItems(extentsionMethodSymbols, cancellationToken);

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

                counter.PartialResult = true;
            }

            counter.CreateItemsTicks = Environment.TickCount - ticks;

            return (items, counter);

        }

        private static ImmutableArray<SerializableImportCompletionItem> ConvertSymbolsToCompletionItems(ImmutableArray<IMethodSymbol> extentsionMethodSymbols, CancellationToken cancellationToken)
        {
            using var _1 = PooledDictionary<INamespaceSymbol, string>.GetInstance(out var namespaceNameCache);
            using var _2 = PooledDictionary<(string containingNamespace, string methodName, bool isGeneric), (IMethodSymbol bestSymbol, int overloadCount)>.GetInstance(out var overloadMap);

            // Aggregate overloads
            foreach (var symbol in extentsionMethodSymbols)
            {
                IMethodSymbol bestSymbol;
                int overloadCount;

                var containingNamespacename = GetFullyQualifiedNamespaceName(symbol.ContainingNamespace, namespaceNameCache);
                var overloadKey = (containingNamespacename, symbol.Name, isGeneric: symbol.Arity > 0);

                // Select the overload with minimum number of parameters to display
                if (overloadMap.TryGetValue(overloadKey, out var currentValue))
                {
                    bestSymbol = currentValue.bestSymbol.Parameters.Length > symbol.Parameters.Length ? symbol : currentValue.bestSymbol;
                    overloadCount = currentValue.overloadCount + 1;
                }
                else
                {
                    bestSymbol = symbol;
                    overloadCount = 1;
                }

                overloadMap[overloadKey] = (bestSymbol, overloadCount);
            }

            // Then convert symbols into completion items
            using var _3 = ArrayBuilder<SerializableImportCompletionItem>.GetInstance(out var itemsBuilder);

            foreach (var ((containingNamespace, _, _), (bestSymbol, overloadCount)) in overloadMap)
            {
                // To display the count of of additional overloads, we need to substract total by 1.
                var item = new SerializableImportCompletionItem(
                    SymbolKey.CreateString(bestSymbol, cancellationToken),
                    bestSymbol.Name,
                    bestSymbol.Arity,
                    bestSymbol.GetGlyph(),
                    containingNamespace,
                    additionalOverloadCount: overloadCount - 1);

                itemsBuilder.Add(item);
            }

            return itemsBuilder.ToImmutable();
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

    internal sealed class StatisticCounter
    {
        public bool PartialResult { get; set; }
        public int TotalTicks { get; set; }
        public int TotalExtensionMethodsProvided { get; set; }
        public int GetSymbolsTicks { get; set; }
        public int CreateItemsTicks { get; set; }

        public void Report()
        {
            CompletionProvidersLogger.LogExtensionMethodCompletionTicksDataPoint(TotalTicks);
            CompletionProvidersLogger.LogExtensionMethodCompletionMethodsProvidedDataPoint(TotalExtensionMethodsProvided);
            CompletionProvidersLogger.LogExtensionMethodCompletionGetSymbolsTicksDataPoint(GetSymbolsTicks);
            CompletionProvidersLogger.LogExtensionMethodCompletionCreateItemsTicksDataPoint(CreateItemsTicks);

            if (PartialResult)
            {
                CompletionProvidersLogger.LogExtensionMethodCompletionPartialResultCount();
            }
        }
    }
}
