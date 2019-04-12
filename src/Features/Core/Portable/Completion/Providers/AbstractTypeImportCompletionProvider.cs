// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractTypeImportCompletionProvider : CommonCompletionProvider
    {
        protected abstract Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken);

        protected abstract ImmutableArray<string> GetImportedNamespaces(
            SyntaxNode location,
            SemanticModel semanticModel,
            CancellationToken cancellationToken);

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            var cancellationToken = completionContext.CancellationToken;
            var document = completionContext.Document;
            var workspace = document.Project.Solution.Workspace;
            var experimentationService = workspace.Services.GetService<IExperimentationService>();

            var importCompletionOptionValue = completionContext.Options.GetOption(CompletionOptions.ShowImportCompletionItems, document.Project.Language);

            // Don't trigger import completion if the option value is "default" and the experiment is disabled for the user. 
            if (importCompletionOptionValue == false ||
                (importCompletionOptionValue == null && experimentationService?.IsExperimentEnabled(WellKnownExperimentNames.TypeImportCompletion) != true))
            {
                return;
            }

            var syntaxContext = await CreateContextAsync(document, completionContext.Position, cancellationToken).ConfigureAwait(false);
            if (!syntaxContext.IsTypeContext)
            {
                return;
            }

            using (Logger.LogBlock(FunctionId.Completion_TypeImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
            {
                await AddCompletionItemsAsync(completionContext, syntaxContext, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AddCompletionItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            var document = completionContext.Document;
            var project = document.Project;
            var workspace = project.Solution.Workspace;
            var typeImportCompletionService = workspace.Services.GetService<ITypeImportCompletionService>();

            // Find all namespaces in scope at current cursor location, 
            // which will be used to filter so the provider only returns out-of-scope types.
            var namespacesInScope = GetNamespacesInScope(document, syntaxContext, cancellationToken);

            // Get completion items from current project. 
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            await typeImportCompletionService.GetTopLevelTypesAsync(project, HandlePublicAndInternalItem, cancellationToken)
                .ConfigureAwait(false);

            // Get declarations from directly referenced projects and PEs
            foreach (var assembly in compilation.GetReferencedAssemblySymbols())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var assemblyProject = project.Solution.GetProject(assembly, cancellationToken);
                if (assemblyProject != null && assemblyProject.SupportsCompilation)
                {
                    await typeImportCompletionService.GetTopLevelTypesAsync(
                        assemblyProject,
                        GetHandler(compilation.Assembly, assembly),
                        cancellationToken).ConfigureAwait(false);
                }
                else if (compilation.GetMetadataReference(assembly) is PortableExecutableReference peReference)
                {
                    typeImportCompletionService.GetTopLevelTypesFromPEReference(
                        project.Solution,
                        compilation,
                        peReference,
                        GetHandler(compilation.Assembly, assembly),
                        cancellationToken);
                }
            }

            return;

            // Decide which item handler to use based on IVT
            Action<TypeImportCompletionItemInfo> GetHandler(IAssemblySymbol assembly, IAssemblySymbol referencedAssembly)
                => assembly.IsSameAssemblyOrHasFriendAccessTo(referencedAssembly)
                        ? (Action<TypeImportCompletionItemInfo>)HandlePublicAndInternalItem
                        : HandlePublicItem;

            // Add only public types to completion list
            void HandlePublicItem(TypeImportCompletionItemInfo itemInfo)
                => AddItems(itemInfo, isInternalsVisible: false, completionContext, namespacesInScope);

            // Add both public and internal types to completion list
            void HandlePublicAndInternalItem(TypeImportCompletionItemInfo itemInfo)
                => AddItems(itemInfo, isInternalsVisible: true, completionContext, namespacesInScope);

            static void AddItems(TypeImportCompletionItemInfo itemInfo, bool isInternalsVisible, CompletionContext completionContext, HashSet<string> namespacesInScope)
            {
                if (itemInfo.IsPublic || isInternalsVisible)
                {
                    var containingNamespace = TypeImportCompletionItem.GetContainingNamespace(itemInfo.Item);
                    if (!namespacesInScope.Contains(containingNamespace))
                    {
                        // We can return cached item directly, because the span and document 
                        // of item will be fixed by completion service.
                        completionContext.AddItem(itemInfo.Item);
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

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem completionItem, char? commitKey = default, CancellationToken cancellationToken = default)
        {
            var containingNamespace = TypeImportCompletionItem.GetContainingNamespace(completionItem);
            Debug.Assert(containingNamespace != null);

            // Find context node so we can use it to decide where to insert using/imports.
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var addImportContextNode = root.FindToken(completionItem.Span.Start, findInsideTrivia: true).Parent; 

            // Add required using/imports directive.                              
            var addImportService = document.GetLanguageService<IAddImportsService>();
            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var importNode = CreateImport(document, containingNamespace);

            var rootWithImport = addImportService.AddImport(compilation, root, addImportContextNode, importNode, placeSystemNamespaceFirst);
            var documentWithImport = document.WithSyntaxRoot(rootWithImport);
            var formattedDocumentWithImport = await Formatter.FormatAsync(documentWithImport, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

            var builder = ArrayBuilder<TextChange>.GetInstance();

            // Get text change for add improt
            var importChanges = await formattedDocumentWithImport.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            builder.AddRange(importChanges);

            // Create text change for complete type name.
            //
            // Note: Don't try to obtain TextChange for completed tyep name by replace the text directly, 
            //       then use Document.GetTextChangesAsync on document creatd from changed text. This is
            //       because it will do a diff and return TextChanges with minimum span.
            //
            //       For example: If I'm typing "asd", the completion provider might be triggered after "a"
            //       is typed. Then if I selected type "AsnEncodedData" to commit, by using the approach described 
            //       above, we will end up with a TextChange of "AsnEncodedDat" instead of the full display text. 
            //       This will later mess up span-tracking and we end up with "AsnEncodedDatasd" in the code.
            builder.Add(new TextChange(completionItem.Span, completionItem.DisplayText));

            // Then get the combined change
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.WithChanges(builder);

            return CompletionChange.Create(Utilities.Collapse(newText, builder.ToImmutableAndFree()));
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
