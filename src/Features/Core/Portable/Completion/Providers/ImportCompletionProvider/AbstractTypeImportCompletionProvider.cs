// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion;
using Microsoft.CodeAnalysis.Internal.Log;
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
            using var _ = Logger.LogBlock(FunctionId.Completion_TypeImportCompletionProvider_GetCompletionItemsAsync, cancellationToken);
            var telemetryCounter = new TelemetryCounter();

            var document = completionContext.Document;
            var project = document.Project;
            var workspace = project.Solution.Workspace;
            var typeImportCompletionService = document.GetLanguageService<ITypeImportCompletionService>()!;

            using var disposer = ArrayBuilder<CompletionItem>.GetInstance(out var itemsBuilder);

            // Get completion items from current project. 
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var items = await typeImportCompletionService.GetTopLevelTypesAsync(
                project,
                syntaxContext,
                isInternalsVisible: true,
                cancellationToken).ConfigureAwait(false);

            AddItems(items, completionContext, namespacesInScope, telemetryCounter);

            // Get declarations from directly referenced projects and PEs.
            // For script compilation, we don't want previous submissions returned as referenced assemblies,
            // there's no need to check for unimported type from them since namespace declaration is not allowed in script.
            var referencedAssemblySymbols = compilation.GetReferencedAssemblySymbols(excludePreviousSubmissions: true);

            // This can be parallelized because we don't add items to CompletionContext
            // until all the collected tasks are completed.
            foreach (var refernecedAssembly in referencedAssemblySymbols)
            {
                items = await HandleReferenceAsync(refernecedAssembly).ConfigureAwait(false);
                AddItems(items, completionContext, namespacesInScope, telemetryCounter);
            }

            telemetryCounter.ReferenceCount = referencedAssemblySymbols.Length;
            telemetryCounter.Report();

            return;

            async Task<ImmutableArray<CompletionItem>> HandleReferenceAsync(IAssemblySymbol referencedAssemblySymbol)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Skip reference with only non-global alias.
                var metadataReference = compilation.GetMetadataReference(referencedAssemblySymbol);

                if (HasGlobalAlias(metadataReference))
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

            static bool HasGlobalAlias(MetadataReference metadataReference)
                => metadataReference.Properties.Aliases.IsEmpty || metadataReference.Properties.Aliases.Any(alias => alias == MetadataReferenceProperties.GlobalAlias);

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
