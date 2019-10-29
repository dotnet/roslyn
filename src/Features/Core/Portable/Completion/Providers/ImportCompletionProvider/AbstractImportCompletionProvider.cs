// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractImportCompletionProvider : CommonCompletionProvider
    {
        protected abstract Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken);
        protected abstract ImmutableArray<string> GetImportedNamespaces(SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken);
        protected abstract Task<bool> IsInImportsDirectiveAsync(Document document, int position, CancellationToken cancellationToken);
        protected abstract bool ShouldProvideCompletion(Document document, SyntaxContext syntaxContext);
        protected abstract Task AddCompletionItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, HashSet<string> namespacesInScope, bool isExpandedCompletion, CancellationToken cancellationToken);

        internal override bool IsExpandItemProvider => true;

        private bool? _isImportCompletionExperimentEnabled = null;

        private bool IsExperimentEnabled(Workspace workspace)
        {
            if (!_isImportCompletionExperimentEnabled.HasValue)
            {
                var experimentationService = workspace.Services.GetRequiredService<IExperimentationService>();
                _isImportCompletionExperimentEnabled = experimentationService.IsExperimentEnabled(WellKnownExperimentNames.TypeImportCompletion);
            }

            return _isImportCompletionExperimentEnabled == true;
        }

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            var cancellationToken = completionContext.CancellationToken;
            var document = completionContext.Document;

            // We need to check for context before option values, so we can tell completion service that we are in a context to provide expanded items
            // even though import completion might be disabled. This would show the expander in completion list which user can then use to explicitly ask for unimported items.
            var syntaxContext = await CreateContextAsync(document, completionContext.Position, cancellationToken).ConfigureAwait(false);
            if (!ShouldProvideCompletion(document, syntaxContext))
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
                    (importCompletionOptionValue == null && !IsExperimentEnabled(document.Project.Solution.Workspace)))
                {
                    return;
                }
            }

            // Find all namespaces in scope at current cursor location, 
            // which will be used to filter so the provider only returns out-of-scope types.
            var namespacesInScope = GetNamespacesInScope(document, syntaxContext, cancellationToken);
            await AddCompletionItemsAsync(completionContext, syntaxContext, namespacesInScope, isExpandedCompletion, cancellationToken).ConfigureAwait(false);
        }

        private HashSet<string> GetNamespacesInScope(Document document, SyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            var semanticModel = syntaxContext.SemanticModel;
            var importedNamespaces = GetImportedNamespaces(syntaxContext.LeftToken.Parent, semanticModel, cancellationToken);

            // This hashset will be used to match namespace names, so it must have the same case-sensitivity as the source language.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>()!;
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
            var containingNamespace = ImportCompletionItem.GetContainingNamespace(completionItem);

            if (await ShouldCompleteWithFullyQualifyTypeName().ConfigureAwait(false))
            {
                var fullyQualifiedName = $"{containingNamespace}.{completionItem.DisplayText}";
                var change = new TextChange(completionListSpan, fullyQualifiedName);

                return CompletionChange.Create(change);
            }

            // Find context node so we can use it to decide where to insert using/imports.
            var tree = (await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false))!;
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var addImportContextNode = root.FindToken(completionListSpan.Start, findInsideTrivia: true).Parent;

            // Add required using/imports directive.                              
            var addImportService = document.GetLanguageService<IAddImportsService>()!;
            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
            var compilation = (await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false))!;
            var importNode = CreateImport(document, containingNamespace);

            var rootWithImport = addImportService.AddImport(compilation, root, addImportContextNode, importNode, placeSystemNamespaceFirst, cancellationToken);
            var documentWithImport = document.WithSyntaxRoot(rootWithImport);
            // This only formats the annotated import we just added, not the entire document.
            var formattedDocumentWithImport = await Formatter.FormatAsync(documentWithImport, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            var builder = ArrayBuilder<TextChange>.GetInstance();

            // Get text change for add import
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

            async Task<bool> ShouldCompleteWithFullyQualifyTypeName()
            {
                if (!IsAddingImportsSupported(document))
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

        protected static bool IsAddingImportsSupported(Document document)
        {
            var workspace = document.Project.Solution.Workspace;

            // Certain types of workspace don't support document change, e.g. DebuggerIntellisense
            if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
            {
                return false;
            }

            // During an EnC session, adding import is not supported.
            var encService = workspace.Services.GetService<IEditAndContinueWorkspaceService>();
            if (encService?.IsDebuggingSessionInProgress == true)
            {
                return false;
            }

            // Certain documents, e.g. Razor document, don't support adding imports
            var documentSupportsFeatureService = workspace.Services.GetRequiredService<IDocumentSupportsFeatureService>();
            if (!documentSupportsFeatureService.SupportsRefactorings(document))
            {
                return false;
            }

            return true;
        }

        private static SyntaxNode CreateImport(Document document, string namespaceName)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            return syntaxGenerator.NamespaceImportDeclaration(namespaceName).WithAdditionalAnnotations(Formatter.Annotation);
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => ImportCompletionItem.GetCompletionDescriptionAsync(document, item, cancellationToken);
    }
}
