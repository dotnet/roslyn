// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractTypeImportCompletionProvider : AbstractImportCompletionProvider
    {
        protected override bool ShouldProvideCompletion(CompletionContext completionContext, SyntaxContext syntaxContext)
            => syntaxContext.IsTypeContext;

        protected override void LogCommit()
            => CompletionProvidersLogger.LogCommitOfTypeImportCompletionItem();

        protected override async Task AddCompletionItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, HashSet<string> namespacesInScope, bool isExpandedCompletion, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.Completion_TypeImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
            {
                var telemetryCounter = new TelemetryCounter();
                var typeImportCompletionService = completionContext.Document.GetRequiredLanguageService<ITypeImportCompletionService>();

                var itemsFromAllAssemblies = await typeImportCompletionService.GetAllTopLevelTypesAsync(
                    completionContext.Document.Project,
                    syntaxContext,
                    forceCacheCreation: isExpandedCompletion,
                    cancellationToken).ConfigureAwait(false);

                if (itemsFromAllAssemblies == null)
                {
                    telemetryCounter.CacheMiss = true;
                }
                else
                {
                    foreach (var items in itemsFromAllAssemblies)
                    {
                        AddItems(items, completionContext, namespacesInScope, telemetryCounter);
                    }
                }

                telemetryCounter.Report();
            }
        }

        private static void AddItems(ImmutableArray<CompletionItem> items, CompletionContext completionContext, HashSet<string> namespacesInScope, TelemetryCounter counter)
        {
            counter.ReferenceCount++;
            foreach (var item in items)
            {
                var containingNamespace = ImportCompletionItem.GetContainingNamespace(item);
                if (!namespacesInScope.Contains(containingNamespace))
                {
                    // We can return cached item directly, item's span will be fixed by completion service.
                    // On the other hand, because of this (i.e. mutating the  span of cached item for each run),
                    // the provider can not be used as a service by components that might be run in parallel 
                    // with completion, which would be a race.
                    completionContext.AddItem(item);
                    counter.ItemsCount++;
                }
            }
        }

        private class TelemetryCounter
        {
            protected int Tick { get; }
            public int ItemsCount { get; set; }
            public int ReferenceCount { get; set; }
            public bool CacheMiss { get; set; }

            public TelemetryCounter()
                => Tick = Environment.TickCount;

            public void Report()
            {
                if (CacheMiss)
                {
                    CompletionProvidersLogger.LogTypeImportCompletionCacheMiss();
                }
                else
                {
                    var delta = Environment.TickCount - Tick;
                    CompletionProvidersLogger.LogTypeImportCompletionTicksDataPoint(delta);
                    CompletionProvidersLogger.LogTypeImportCompletionItemCountDataPoint(ItemsCount);
                    CompletionProvidersLogger.LogTypeImportCompletionReferenceCountDataPoint(ReferenceCount);
                }
            }
        }
    }
}
