// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractTypeImportCompletionProvider : CommonCompletionProvider
    {
        private readonly SyntaxAnnotation _annotation = new SyntaxAnnotation();
        private readonly ITypeImportCompletionService _typeImportCompletionService;
        private readonly IExperimentationService _experimentationService;

        public AbstractTypeImportCompletionProvider(Workspace workspace)
        {
            _typeImportCompletionService = workspace.Services.GetService<ITypeImportCompletionService>();
            _experimentationService = workspace.Services.GetService<IExperimentationService>();
        }

        protected abstract Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken);

        protected abstract ImmutableHashSet<string> GetNamespacesInScope(SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken);

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            var document = completionContext.Document;
            var cancellationToken = completionContext.CancellationToken;

            var importCompletionOptionValue = completionContext.Options.GetOption(CompletionOptions.ShowImportCompletionItems, document.Project.Language);

            // Don't trigger import completion if the option value is "default" and the experiment is disabled for the user. 
            if (importCompletionOptionValue == false ||
                (importCompletionOptionValue == null && _experimentationService?.IsExperimentEnabled(WellKnownExperimentNames.TypeImportCompletion) != true))
            {
                return;
            }

            var syntaxContext = await CreateContextAsync(document, completionContext.Position, cancellationToken).ConfigureAwait(false);
            await AddCompletionItemsAsync(document, syntaxContext, completionContext, cancellationToken).ConfigureAwait(false);
        }

        private async Task AddCompletionItemsAsync(Document document, SyntaxContext context, CompletionContext completionContext, CancellationToken cancellationToken)
        {
            if (!context.IsTypeContext)
            {
                return;
            }

            using (Logger.LogBlock(FunctionId.Completion_TypeImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
            {
                var node = context.LeftToken.Parent;
                var project = document.Project;

                // Find all namespaces in scope at current cursor location, 
                // which will be used to filter so the provider only returns out-of-scope types.
                var namespacesInScope = GetNamespacesInScope(node, context.SemanticModel, cancellationToken);
                Action<CompletionItem> handleAccessibleItem = item => AddItems(item, completionContext, namespacesInScope);

                await _typeImportCompletionService.GetAccessibleTopLevelTypesFromProjectAsync(project, handleAccessibleItem, cancellationToken)
                    .ConfigureAwait(false);

                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var reference in compilation.References)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var declarationsInReference = ImmutableArray<CompletionItem>.Empty;
                    if (reference is CompilationReference compilationReference)
                    {
                        await _typeImportCompletionService.GetAccessibleTopLevelTypesFromCompilationReferenceAsync(
                            project.Solution,
                            compilation,
                            compilationReference,
                            handleAccessibleItem,
                            cancellationToken).ConfigureAwait(false);
                    }
                    else if (reference is PortableExecutableReference peReference)
                    {
                        _typeImportCompletionService.GetAccessibleTopLevelTypesFromPEReference(
                            project.Solution,
                            compilation,
                            peReference,
                            handleAccessibleItem,
                            cancellationToken);
                    }
                }
            }

            static void AddItems(CompletionItem item, CompletionContext completionContext, ImmutableHashSet<string> namespacesInScope)
            {
                var containingNamespace = TypeImportCompletionItem.GetContainingNamespace(item);
                if (!namespacesInScope.Contains(containingNamespace))
                {
                    completionContext.AddItem(item);
                }
            }
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = default, CancellationToken cancellationToken = default)
        {
            var newDocument = await ComputeNewDocumentAsync(document, item, cancellationToken).ConfigureAwait(false);
            var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var change = Utilities.Collapse(newText, changes.ToImmutableArray());

            return CompletionChange.Create(change);
        }

        private async Task<Document> ComputeNewDocumentAsync(Document document, CompletionItem completionItem, CancellationToken cancellationToken)
        {
            var containingNamespace = TypeImportCompletionItem.GetContainingNamespace(completionItem);
            Debug.Assert(containingNamespace != null);

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // Complete type name.
            var textWithTypeName = root.GetText(text.Encoding).Replace(completionItem.Span, completionItem.DisplayText);
            var documentWithTypeName = document.WithText(textWithTypeName);

            // Find added node so we can use it to decide where to insert using/imports.
            var treeWithTypeName = await documentWithTypeName.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var rootWithTypeName = await treeWithTypeName.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var addedNode = rootWithTypeName.FindToken(completionItem.Span.Start, findInsideTrivia: true).Parent;

            // Add required using/imports directive.                              
            var addImportService = documentWithTypeName.GetLanguageService<IAddImportsService>();
            var optionSet = await documentWithTypeName.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, documentWithTypeName.Project.Language);
            var compilation = await documentWithTypeName.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var importNode = CreateImport(documentWithTypeName, containingNamespace);

            var rootWithImport = addImportService.AddImport(compilation, rootWithTypeName, addedNode, importNode, placeSystemNamespaceFirst);
            var documentWithImport = documentWithTypeName.WithSyntaxRoot(rootWithImport);

            // Format newly added nodes.
            return await Formatter.FormatAsync(documentWithImport, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private static SyntaxNode CreateImport(Document document, string namespaceName)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            return syntaxGenerator.NamespaceImportDeclaration(namespaceName).WithAdditionalAnnotations(Formatter.Annotation);
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => TypeImportCompletionItem.GetCompletionDescriptionAsync(document, item, cancellationToken);
    }
}
