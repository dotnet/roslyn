// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

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

        private async Task AddCompletionItemsAsync(CompletionContext completionContext, SyntaxContext syntaxContext, CancellationToken cancellation)
        {
            var document = completionContext.Document;
            var cancellationToken = completionContext.CancellationToken;
            var workspace = document.Project.Solution.Workspace;
            var project = document.Project;
            var typeImportCompletionService = workspace.Services.GetService<ITypeImportCompletionService>();

            // Find all namespaces in scope at current cursor location, 
            // which will be used to filter so the provider only returns out-of-scope types.
            var namespacesInScope = GetNamespacesInScope(document, syntaxContext, cancellationToken);

            // Get completion items from current project. 
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            await typeImportCompletionService.GetTopLevelTypesFromProjectAsync(project, HandlePublicAndInternalItem, cancellationToken)
                .ConfigureAwait(false); 

            // Get completion items from source references.
            foreach (var projectReference in project.ProjectReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var referencedProject = project.Solution.GetProject(projectReference.ProjectId);
                var referencedCompilation = await referencedProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                await typeImportCompletionService.GetTopLevelTypesFromProjectAsync(
                    referencedProject,
                    GetHandler(compilation.Assembly, referencedCompilation.Assembly),
                    cancellationToken).ConfigureAwait(false);
            }

            // Get completion items from PE references.
            var peReferences = project.MetadataReferences
                .Where(reference => reference is PortableExecutableReference)
                .Cast<PortableExecutableReference>();

            foreach (var peReference in peReferences)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (compilation.GetAssemblyOrModuleSymbol(peReference) is IAssemblySymbol referencedAssembly)
                {
                    typeImportCompletionService.GetTopLevelTypesFromPEReference(
                        project.Solution,
                        compilation,
                        peReference,
                        GetHandler(compilation.Assembly, referencedAssembly),
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

            // Complete type name.
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textWithTypeName = text.Replace(completionItem.Span, completionItem.DisplayText);
            var documentWithTypeName = document.WithText(textWithTypeName);

            // Find added node so we can use it to decide where to insert using/imports.
            var treeWithTypeName = await documentWithTypeName.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var rootWithTypeName = await treeWithTypeName.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var addImportContextNode = rootWithTypeName.FindToken(completionItem.Span.Start, findInsideTrivia: true).Parent;

            // Add required using/imports directive.                              
            var addImportService = documentWithTypeName.GetLanguageService<IAddImportsService>();
            var optionSet = await documentWithTypeName.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, documentWithTypeName.Project.Language);
            var compilation = await documentWithTypeName.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var importNode = CreateImport(documentWithTypeName, containingNamespace);

            var rootWithImport = addImportService.AddImport(compilation, rootWithTypeName, addImportContextNode, importNode, placeSystemNamespaceFirst);
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
