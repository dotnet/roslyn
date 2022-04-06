// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractTypeImportCompletionProvider<AliasDeclarationTypeNode> : AbstractImportCompletionProvider
        where AliasDeclarationTypeNode : SyntaxNode
    {
        protected override bool ShouldProvideCompletion(CompletionContext completionContext, SyntaxContext syntaxContext)
            => syntaxContext.IsTypeContext;

        protected override void LogCommit()
            => CompletionProvidersLogger.LogCommitOfTypeImportCompletionItem();

        protected abstract ImmutableArray<AliasDeclarationTypeNode> GetAliasDeclarationNodes(SyntaxNode node);

        protected override void WarmUpCacheInBackground(Document document)
        {
            var typeImportCompletionService = document.GetRequiredLanguageService<ITypeImportCompletionService>();
            typeImportCompletionService.QueueCacheWarmUpTask(document.Project);
        }

        protected override async Task AddCompletionItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, HashSet<string> namespacesInScope, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Completion_TypeImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
            {
                var telemetryCounter = new TelemetryCounter();
                var typeImportCompletionService = completionContext.Document.GetRequiredLanguageService<ITypeImportCompletionService>();

                var (itemsFromAllAssemblies, isPartialResult) = await typeImportCompletionService.GetAllTopLevelTypesAsync(
                    completionContext.Document.Project,
                    syntaxContext,
                    forceCacheCreation: completionContext.CompletionOptions.ForceExpandedCompletionIndexCreation,
                    completionContext.CompletionOptions,
                    cancellationToken).ConfigureAwait(false);

                var aliasTargetNamespaceToTypeNameMap = GetAliasTypeDictionary(completionContext.Document, syntaxContext, cancellationToken);
                foreach (var items in itemsFromAllAssemblies)
                    AddItems(items, completionContext, namespacesInScope, aliasTargetNamespaceToTypeNameMap, telemetryCounter);

                if (isPartialResult)
                    telemetryCounter.CacheMiss = true;

                telemetryCounter.Report();
            }
        }

        /// <summary>
        /// Get a multi-Dictionary stores the information about the target of all alias Symbol in the syntax tree.
        /// Multiple aliases might live under same namespace.
        /// Key is the namespace of the symbol, value is the name of the symbol.
        /// </summary>
        private MultiDictionary<string, string> GetAliasTypeDictionary(
            Document document,
            SyntaxContext syntaxContext,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var dictionary = new MultiDictionary<string, string>(syntaxFactsService.StringComparer);

            var nodeToCheck = syntaxContext.LeftToken.Parent;
            if (nodeToCheck == null)
            {
                return dictionary;
            }

            // In case the caret is at the beginning of the file, take the root node.
            var aliasDeclarations = GetAliasDeclarationNodes(nodeToCheck);
            foreach (var aliasNode in aliasDeclarations)
            {
                var symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(aliasNode, cancellationToken);
                if (symbol is IAliasSymbol { Target: ITypeSymbol { TypeKind: not TypeKind.Error } target })
                {
                    // If the target type is a type constructs from generics type, e.g.
                    // using AliasBar = Bar<int>
                    // namespace Foo
                    // {
                    //      public class Bar<T>
                    //      {
                    //      }
                    // }
                    // namespace Foo2
                    // {
                    //      public class Main
                    //      {
                    //          $$
                    //      }
                    // }
                    // In such case, user might want to type Bar<string> and still want 'using Foo'.
                    // We shouldn't try to filter the CompletionItem for Bar<T> later.
                    // so just ignore the Bar<int> here.
                    var typeParameter = target.GetTypeParameters();
                    if (typeParameter.IsEmpty)
                    {
                        var namespaceOfTarget = target.ContainingNamespace.ToDisplayString(SymbolDisplayFormats.NameFormat);
                        var typeNameOfTarget = target.Name;
                        dictionary.Add(namespaceOfTarget, typeNameOfTarget);
                    }
                }
            }

            return dictionary;
        }

        private static void AddItems(
            ImmutableArray<CompletionItem> items,
            CompletionContext completionContext,
            HashSet<string> namespacesInScope,
            MultiDictionary<string, string> aliasTargetNamespaceToTypeNameMap,
            TelemetryCounter counter)
        {
            counter.ReferenceCount++;
            foreach (var item in items)
            {
                if (ShouldAddItem(item, namespacesInScope, aliasTargetNamespaceToTypeNameMap))
                {
                    // We can return cached item directly, item's span will be fixed by completion service.
                    // On the other hand, because of this (i.e. mutating the  span of cached item for each run),
                    // the provider can not be used as a service by components that might be run in parallel
                    // with completion, which would be a race.
                    completionContext.AddItem(item);
                    counter.ItemsCount++;
                }
            }

            static bool ShouldAddItem(
                CompletionItem item,
                HashSet<string> namespacesInScope,
                MultiDictionary<string, string> aliasTargetNamespaceToTypeNameMap)
            {
                var containingNamespace = ImportCompletionItem.GetContainingNamespace(item);
                // 1. if the namespace of the item is in scoop. Don't add the item
                if (namespacesInScope.Contains(containingNamespace))
                {
                    return false;
                }

                // 2. If the item might be an alias target. First check if the target alias map has any value then
                // check if the type name is in the dictionary.
                // It is done in this way to avoid calling ImportCompletionItem.GetTypeName for all the CompletionItems
                if (!aliasTargetNamespaceToTypeNameMap.IsEmpty
                    && aliasTargetNamespaceToTypeNameMap[containingNamespace].Contains(ImportCompletionItem.GetTypeName(item)))
                {
                    return false;
                }

                return true;
            }
        }

        private class TelemetryCounter
        {
            private readonly int _tick;

            public int ItemsCount { get; set; }
            public int ReferenceCount { get; set; }
            public bool CacheMiss { get; set; }

            public TelemetryCounter()
            {
                _tick = Environment.TickCount;
            }

            public void Report()
            {
                if (CacheMiss)
                {
                    CompletionProvidersLogger.LogTypeImportCompletionCacheMiss();
                }

                // cache miss still count towards the cost of completion, so we need to log regardless of it.
                var delta = Environment.TickCount - _tick;
                CompletionProvidersLogger.LogTypeImportCompletionTicksDataPoint(delta);
                CompletionProvidersLogger.LogTypeImportCompletionItemCountDataPoint(ItemsCount);
                CompletionProvidersLogger.LogTypeImportCompletionReferenceCountDataPoint(ReferenceCount);
            }
        }
    }
}
