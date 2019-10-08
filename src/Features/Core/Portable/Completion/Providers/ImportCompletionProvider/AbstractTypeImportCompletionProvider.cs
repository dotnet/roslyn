// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractTypeImportCompletionProvider : AbstractImportCompletionProvider
    {
        protected override bool ShouldProvideCompletion(Document document, SyntaxContext syntaxContext)
            => syntaxContext.IsTypeContext;

        protected override async Task AddCompletionItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, HashSet<string> namespacesInScope, bool isExpandedCompletion, CancellationToken cancellationToken)
        {
            using var telemetryCounter = new TelemetryCounter();

            var document = completionContext.Document;
            var project = document.Project;
            var workspace = project.Solution.Workspace;
            var typeImportCompletionService = document.GetLanguageService<ITypeImportCompletionService>();

            var tasksToGetCompletionItems = ArrayBuilder<Task<ImmutableArray<CompletionItem>>>.GetInstance();

            // Get completion items from current project. 
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            tasksToGetCompletionItems.Add(Task.Run(() => typeImportCompletionService.GetTopLevelTypesAsync(
                project,
                syntaxContext,
                isInternalsVisible: true,
                cancellationToken)));

            // Get declarations from directly referenced projects and PEs.
            // For script compilation, we don't want previous submissions returned as referenced assemblies,
            // there's no need to check for unimported type from them since namespace declaration is not allowed in script.
            var referencedAssemblySymbols = compilation.GetReferencedAssemblySymbols(excludePreviousSubmissions: true);

            // This can be parallelized because we don't add items to CompletionContext
            // until all the collected tasks are completed.
            tasksToGetCompletionItems.AddRange(
                referencedAssemblySymbols.Select(symbol => Task.Run(() => HandleReferenceAsync(symbol))));

            // We want to timebox the operation that might need to traverse all the type symbols and populate the cache. 
            // The idea is not to block completion for too long (likely to happen the first time import completion is triggered).
            // The trade-off is we might not provide unimported types until the cache is warmed up.
            var timeoutInMilliseconds = completionContext.Options.GetOption(CompletionServiceOptions.TimeoutInMillisecondsForImportCompletion);
            var combinedTask = Task.WhenAll(tasksToGetCompletionItems.ToImmutableAndFree());

            if (isExpandedCompletion ||
                timeoutInMilliseconds != 0 && await Task.WhenAny(combinedTask, Task.Delay(timeoutInMilliseconds, cancellationToken)).ConfigureAwait(false) == combinedTask)
            {
                // Either there's no timeout, and we now have all completion items ready,
                // or user asked for unimported type explicitly so we need to wait until they are calculated.
                var completionItemsToAdd = await combinedTask.ConfigureAwait(false);
                foreach (var completionItems in completionItemsToAdd)
                {
                    AddItems(completionItems, completionContext, namespacesInScope, telemetryCounter);
                }
            }
            else
            {
                // If timed out, we don't want to cancel the computation so next time the cache would be populated.
                // We do not keep track if previous compuation for a given project/PE reference is still running. So there's a chance 
                // we queue same computation again later. However, we expect such computation for an individual reference to be relatively 
                // fast so the actual cycles wasted would be insignificant.
                telemetryCounter.TimedOut = true;
            }

            telemetryCounter.ReferenceCount = referencedAssemblySymbols.Length;

            return;

            async Task<ImmutableArray<CompletionItem>> HandleReferenceAsync(IAssemblySymbol referencedAssemblySymbol)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip reference with only non-global alias.
                var metadataReference = compilation.GetMetadataReference(referencedAssemblySymbol);

                if (metadataReference.Properties.Aliases.IsEmpty ||
                    metadataReference.Properties.Aliases.Any(alias => alias == MetadataReferenceProperties.GlobalAlias))
                {
                    var assemblyProject = project.Solution.GetProject(referencedAssemblySymbol, cancellationToken);
                    if (assemblyProject != null && assemblyProject.SupportsCompilation)
                    {
                        return await typeImportCompletionService.GetTopLevelTypesAsync(
                            assemblyProject,
                            syntaxContext,
                            isInternalsVisible: compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(referencedAssemblySymbol),
                            cancellationToken).ConfigureAwait(false);
                    }
                    else if (metadataReference is PortableExecutableReference peReference)
                    {
                        return typeImportCompletionService.GetTopLevelTypesFromPEReference(
                            project.Solution,
                            compilation,
                            peReference,
                            syntaxContext,
                            isInternalsVisible: compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(referencedAssemblySymbol),
                            cancellationToken);
                    }
                }

                return ImmutableArray<CompletionItem>.Empty;
            }

            static void AddItems(ImmutableArray<CompletionItem> items, CompletionContext completionContext, HashSet<string> namespacesInScope, TelemetryCounter counter)
            {
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
                        counter.ItemsCount++; ;
                    }
                }
            }
        }

        private class TelemetryCounter : IDisposable
        {
            protected int Tick { get; }
            public int ItemsCount { get; set; }
            public int ReferenceCount { get; set; }
            public bool TimedOut { get; set; }

            public TelemetryCounter()
            {
                Tick = Environment.TickCount;
            }

            public void Dispose()
            {
                var delta = Environment.TickCount - Tick;
                CompletionProvidersLogger.LogTypeImportCompletionTicksDataPoint(delta);
                CompletionProvidersLogger.LogTypeImportCompletionItemCountDataPoint(ItemsCount);
                CompletionProvidersLogger.LogTypeImportCompletionReferenceCountDataPoint(ReferenceCount);

                if (TimedOut)
                {
                    CompletionProvidersLogger.LogTypeImportCompletionTimeout();
                }
            }
        }
    }
}
