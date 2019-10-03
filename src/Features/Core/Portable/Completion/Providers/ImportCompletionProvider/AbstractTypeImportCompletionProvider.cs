// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.Completion.Log;
using Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractTypeImportCompletionProvider : CommonCompletionProvider
    {
        private bool? _isTypeImportCompletionExperimentEnabled = null;

        protected abstract Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken);

        protected abstract ImmutableArray<string> GetImportedNamespaces(
            SyntaxNode location,
            SemanticModel semanticModel,
            CancellationToken cancellationToken);

        protected abstract Task<bool> IsInImportsDirectiveAsync(Document document, int position, CancellationToken cancellationToken);

        internal override bool IsExpandItemProvider => true;

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            var cancellationToken = completionContext.CancellationToken;
            var document = completionContext.Document;
            var workspace = document.Project.Solution.Workspace;

            // We need to check for context before option values, so we can tell completion service that we are in a context to provide expanded items
            // even though import completion might be disabled. This would show the expander in completion list which user can then use to explicitly ask for unimported items.
            var syntaxContext = await CreateContextAsync(document, completionContext.Position, cancellationToken).ConfigureAwait(false);
            if (!syntaxContext.IsTypeContext)
            {
                return;
            }

            completionContext.ExpandItemsAvailable = true;

            // We will trigger import completion regardless of the option/experiment if extended items is being requested explicitly (via expander in completion list)
            var isExpandedCompletion = completionContext.Options.GetOption(CompletionServiceOptions.IsExpandedCompletion);
            if (!isExpandedCompletion)
            {
                var importCompletionOptionValue = completionContext.Options.GetOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, document.Project.Language);

                // Don't trigger import completion if the option value is "default" and the experiment is disabled for the user. 
                if (importCompletionOptionValue == false ||
                    (importCompletionOptionValue == null && !IsTypeImportCompletionExperimentEnabled(workspace)))
                {
                    return;
                }
            }

            using (Logger.LogBlock(FunctionId.Completion_TypeImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
            using (var telemetryCounter = new TelemetryCounter())
            {
                await AddCompletionItemsAsync(completionContext, syntaxContext, isExpandedCompletion, telemetryCounter, cancellationToken).ConfigureAwait(false);
            }
        }

        private bool IsTypeImportCompletionExperimentEnabled(Workspace workspace)
        {
            if (!_isTypeImportCompletionExperimentEnabled.HasValue)
            {
                var experimentationService = workspace.Services.GetService<IExperimentationService>();
                _isTypeImportCompletionExperimentEnabled = experimentationService.IsExperimentEnabled(WellKnownExperimentNames.TypeImportCompletion);
            }

            return _isTypeImportCompletionExperimentEnabled == true;
        }

        private async Task AddCompletionItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, bool isExpandedCompletion, TelemetryCounter telemetryCounter, CancellationToken cancellationToken)
        {
            var document = completionContext.Document;
            var project = document.Project;
            var workspace = project.Solution.Workspace;
            var typeImportCompletionService = document.GetLanguageService<ITypeImportCompletionService>();

            // Find all namespaces in scope at current cursor location, 
            // which will be used to filter so the provider only returns out-of-scope types.
            var namespacesInScope = GetNamespacesInScope(document, syntaxContext, cancellationToken);

            var tasksToGetCompletionItems = ArrayBuilder<Task<ImmutableArray<CompletionItem>>>.GetInstance();

            // Get completion items from current project. 
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            tasksToGetCompletionItems.Add(Task.Run(() => typeImportCompletionService.GetTopLevelTypesAsync(
                project,
                syntaxContext,
                isInternalsVisible: true,
                cancellationToken)));

            // Get declarations from directly referenced projects and PEs.
            // This can be parallelized because we don't add items to CompletionContext
            // until all the collected tasks are completed.
            var referencedAssemblySymbols = compilation.GetReferencedAssemblySymbols();
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

                // metadataReference can be null for script compilation, because compilations of previous 
                // submissions are treated as referenced assemblies. We don't need to check for unimported
                // type from those previous submissions since namespace declarations is not allowed in script.

                if (metadataReference != null &&
                    (metadataReference.Properties.Aliases.IsEmpty ||
                     metadataReference.Properties.Aliases.Any(alias => alias == MetadataReferenceProperties.GlobalAlias)))
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
                    var containingNamespace = TypeImportCompletionItem.GetContainingNamespace(item);
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

        private HashSet<string> GetNamespacesInScope(Document document, SyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            var semanticModel = syntaxContext.SemanticModel;
            var importedNamespaces = GetImportedNamespaces(syntaxContext.LeftToken.Parent, semanticModel, cancellationToken);

            // This hashset will be used to match namespace names, so it must have the same case-sensitivity as the source language.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var namespacesInScope = new HashSet<string>(importedNamespaces, syntaxFacts.StringComparer);

            // Get containing namespaces.
            var namespaceSymbol = semanticModel.GetEnclosingNamespace(syntaxContext.Position, cancellationToken);
            while (namespaceSymbol != null)
            {
                namespacesInScope.Add(namespaceSymbol.ToDisplayString(SymbolDisplayFormats.NameFormat));
                namespaceSymbol = namespaceSymbol.ContainingNamespace;
            }

            return namespacesInScope;
        }

        internal override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem completionItem, TextSpan completionListSpan, char? commitKey, CancellationToken cancellationToken)
        {
            var containingNamespace = TypeImportCompletionItem.GetContainingNamespace(completionItem);
            Debug.Assert(containingNamespace != null);

            if (await ShouldCompleteWithFullyQualifyTypeName().ConfigureAwait(false))
            {
                var fullyQualifiedName = $"{containingNamespace}.{completionItem.DisplayText}";
                var change = new TextChange(completionListSpan, fullyQualifiedName);

                return CompletionChange.Create(change);
            }
            else
            {
                // Find context node so we can use it to decide where to insert using/imports.
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var addImportContextNode = root.FindToken(completionListSpan.Start, findInsideTrivia: true).Parent;

                // Add required using/imports directive.                              
                var addImportService = document.GetLanguageService<IAddImportsService>();
                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var importNode = CreateImport(document, containingNamespace);

                var rootWithImport = addImportService.AddImport(compilation, root, addImportContextNode, importNode, placeSystemNamespaceFirst, cancellationToken);
                var documentWithImport = document.WithSyntaxRoot(rootWithImport);
                var formattedDocumentWithImport = await Formatter.FormatAsync(documentWithImport, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

                var builder = ArrayBuilder<TextChange>.GetInstance();

                // Get text change for add improt
                var importChanges = await formattedDocumentWithImport.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
                builder.AddRange(importChanges);

                // Create text change for complete type name.
                //
                // Note: Don't try to obtain TextChange for completed type name by replacing the text directly, 
                //       then use Document.GetTextChangesAsync on document created from the changed text. This is
                //       because it will do a diff and return TextChanges with minimum span instead of actual 
                //       replacement span.
                //
                //       For example: If I'm typing "asd", the completion provider could be triggered after "a"
                //       is typed. Then if I selected type "AsnEncodedData" to commit, by using the approach described 
                //       above, we will get a TextChange of "AsnEncodedDat" with 0 length span, instead of a change of 
                //       the full display text with a span of length 1. This will later mess up span-tracking and end up 
                //       with "AsnEncodedDatasd" in the code.
                builder.Add(new TextChange(completionListSpan, completionItem.DisplayText));

                // Then get the combined change
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newText = text.WithChanges(builder);

                return CompletionChange.Create(Utilities.Collapse(newText, builder.ToImmutableAndFree()));
            }

            async Task<bool> ShouldCompleteWithFullyQualifyTypeName()
            {
                var workspace = document.Project.Solution.Workspace;

                // Certain types of workspace don't support document change, e.g. DebuggerIntellisense
                if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
                {
                    return true;
                }

                // During an EnC session, adding import is not supported.
                var encService = workspace.Services.GetService<IEditAndContinueWorkspaceService>();
                if (encService?.IsDebuggingSessionInProgress == true)
                {
                    return true;
                }

                // Certain documents, e.g. Razor document, don't support adding imports
                var documentSupportsFeatureService = workspace.Services.GetService<IDocumentSupportsFeatureService>();
                if (!documentSupportsFeatureService.SupportsRefactorings(document))
                {
                    return true;
                }

                // We might need to qualify unimported types to use them in an import directive, because they only affect members of the containing
                // import container (e.g. namespace/class/etc. declarations).
                //
                // For example, `List` and `StringBuilder` both need to be fully qualified below: 
                // 
                //      using CollectionOfStringBuilders = System.Collections.Generic.List<System.Text.StringBuilder>;
                //
                // However, if we are typing in an C# using directive that is inside a nested import container (i.e. inside a namespace declaration block), 
                // then we can add an using in the outer import container instead (this is not allowed in VB). 
                //
                // For example:
                //
                //      using System.Collections.Generic;
                //      using System.Text;
                //
                //      namespace Foo
                //      {
                //          using CollectionOfStringBuilders = List<StringBuilder>;
                //      }
                //
                // Here we will always choose to qualify the unimported type, just to be consistent and keeps things simple.
                return await IsInImportsDirectiveAsync(document, completionListSpan.Start, cancellationToken).ConfigureAwait(false);
            }
        }

        private static SyntaxNode CreateImport(Document document, string namespaceName)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            return syntaxGenerator.NamespaceImportDeclaration(namespaceName).WithAdditionalAnnotations(Formatter.Annotation);
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => TypeImportCompletionItem.GetCompletionDescriptionAsync(document, item, cancellationToken);

        private class TelemetryCounter : IDisposable
        {
            private readonly int _tick;

            public int ItemsCount { get; set; }

            public int ReferenceCount { get; set; }

            public bool TimedOut { get; set; }

            public TelemetryCounter()
            {
                _tick = Environment.TickCount;
            }

            public void Dispose()
            {
                var delta = Environment.TickCount - _tick;
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
