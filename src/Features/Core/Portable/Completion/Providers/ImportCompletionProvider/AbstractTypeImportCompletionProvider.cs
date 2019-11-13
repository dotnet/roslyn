// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractTypeImportCompletionProvider : AbstractImportCompletionProvider
    {
        protected override bool ShouldProvideCompletion(Document document, SyntaxContext syntaxContext)
            => syntaxContext.IsTypeContext;

        protected override async Task AddCompletionItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, HashSet<string> namespacesInScope, bool isExpandedCompletion, CancellationToken cancellationToken)
        {
            using var _ = Logger.LogBlock(FunctionId.Completion_TypeImportCompletionProvider_GetCompletionItemsAsync, cancellationToken);
            var telemetryCounter = new TelemetryCounter();

            var document = completionContext.Document;
            var project = document.Project;
            var workspace = project.Solution.Workspace;
            var typeImportCompletionService = document.GetLanguageService<ITypeImportCompletionService>()!;

            using var disposer = ArrayBuilder<CompletionItem>.GetInstance(out var itemsBuilder);

            var builder = ArrayBuilder<CompletionItem>.GetInstance();

            var currentProject = document.Project;

            var items = await typeImportCompletionService.GetTopLevelTypesAsync(currentProject,
                syntaxContext,
                isInternalsVisible: true,
                cancellationToken).ConfigureAwait(false);

            AddItems(items, completionContext, namespacesInScope, telemetryCounter);

            var solution = currentProject.Solution;
            var graph = solution.GetProjectDependencyGraph();
            var referencedProjects = graph.GetProjectsThatThisProjectTransitivelyDependsOn(currentProject.Id).SelectAsArray(id => solution.GetRequiredProject(id));
            var currentCompilation = await currentProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            foreach (var referencedProject in referencedProjects.Where(p => p.SupportsCompilation))
            {
                var compilation = await referencedProject.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                var assembly = SymbolFinder.FindSimilarSymbols(compilation.Assembly, currentCompilation).SingleOrDefault();
                var metadataReference = currentCompilation.GetMetadataReference(assembly);

                if (HasGlobalAlias(metadataReference))
                {
                    items = await typeImportCompletionService.GetTopLevelTypesAsync(
                        referencedProject,
                        syntaxContext,
                        isInternalsVisible: currentCompilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(compilation.Assembly),
                        cancellationToken).ConfigureAwait(false);

                    AddItems(items, completionContext, namespacesInScope, telemetryCounter);
                }

                telemetryCounter.ReferenceCount++;
            }

            foreach (var peReference in currentProject.MetadataReferences.OfType<PortableExecutableReference>())
            {
                if (HasGlobalAlias(peReference))
                {
                    if (currentCompilation.GetAssemblyOrModuleSymbol(peReference) is IAssemblySymbol assembly)
                    {
                        items = typeImportCompletionService.GetTopLevelTypesFromPEReference(
                            solution,
                            currentCompilation,
                            peReference,
                            syntaxContext,
                            isInternalsVisible: currentCompilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assembly),
                            cancellationToken);

                        AddItems(items, completionContext, namespacesInScope, telemetryCounter);
                    }
                }

                telemetryCounter.ReferenceCount++;
            }

            telemetryCounter.Report();
            return;

            static bool HasGlobalAlias(MetadataReference metadataReference)
                => metadataReference != null && (metadataReference.Properties.Aliases.IsEmpty || metadataReference.Properties.Aliases.Any(alias => alias == MetadataReferenceProperties.GlobalAlias));

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

        private class TelemetryCounter
        {
            protected int Tick { get; }
            public int ItemsCount { get; set; }
            public int ReferenceCount { get; set; }
            public bool TimedOut { get; set; }

            public TelemetryCounter()
            {
                Tick = Environment.TickCount;
            }

            public void Report()
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
