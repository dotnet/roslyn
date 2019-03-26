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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractTypeImportCompletionProvider : CommonCompletionProvider
    {
        private readonly SyntaxAnnotation _annotation = new SyntaxAnnotation();

        protected abstract Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken);
        protected abstract HashSet<INamespaceSymbol> GetNamespacesInScope(SemanticModel semanticModel, SyntaxNode location, CancellationToken cancellationToken);
        protected abstract Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken);

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
            var tick = Environment.TickCount;
            var document = completionContext.Document;
            var position = completionContext.Position;
            var cancellationToken = completionContext.CancellationToken;

            if (!completionContext.Options.GetOption(CompletionOptions.ShowImportCompletionItems, document.Project.Language))
            {
                return;
            }

            if (completionContext.Trigger.Kind == CompletionTriggerKind.Insertion)
            {
                var isSemanticTriggerCharacter = await IsSemanticTriggerCharacterAsync(document, position - 1, cancellationToken).ConfigureAwait(false);
                if (!isSemanticTriggerCharacter)
                {
                    return;
                }
            }

            var syntaxContext = await CreateContextAsync(document, position, cancellationToken).ConfigureAwait(false);

            var items = await GetCompletionItemsAsync(document, syntaxContext, position, cancellationToken).ConfigureAwait(false);
            completionContext.AddItems(items);

            _debug_total_time_with_ItemCreation = Environment.TickCount - tick;

            return;
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = default, CancellationToken cancellationToken = default)
        {
            var newDocument = await ComputeNewDocumentAsync(document, item, cancellationToken).ConfigureAwait(false);
            var newText = await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            int? newPosition = null;

            // Attempt to find the inserted node and move the caret appropriately
            if (newRoot != null)
            {
                var caretTarget = newRoot.GetAnnotatedNodesAndTokens(_annotation).FirstOrNullable();
                if (caretTarget != null)
                {
                    var targetPosition = caretTarget.Value.AsNode().GetLastToken().Span.End;

                    // Something weird happened and we failed to get a valid position.
                    // Bail on moving the caret.
                    if (targetPosition > 0 && targetPosition <= newText.Length)
                    {
                        newPosition = targetPosition;
                    }
                }
            }

            var changes = await newDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
            var change = Utilities.Collapse(newText, changes.ToList());

            return CompletionChange.Create(change, newPosition, includesCommitCharacter: true);
        }

        private async Task<Document> ComputeNewDocumentAsync(Document document, CompletionItem completionItem, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var finalText = root.GetText(text.Encoding)
                .Replace(completionItem.Span, completionItem.DisplayText.Trim());

            document = document.WithText(finalText);

            tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var addedNode = root.FindNode(completionItem.Span);
            var annotatedNode = addedNode.WithAdditionalAnnotations(_annotation);

            root = root.ReplaceNode(addedNode, annotatedNode);
            document = document.WithSyntaxRoot(root);

            if (completionItem is TypeImportCompletionItem importItem)
            {
                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
                var importNode = CreateImport(document, importItem.ContainingNamespace);

                var addImportService = document.GetLanguageService<IAddImportsService>();
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

                root = addImportService.AddImport(compilation, root, annotatedNode, importNode, placeSystemNamespaceFirst);
                document = document.WithSyntaxRoot(root);

                document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            return document;
        }

        private static SyntaxNode CreateImport(Document document, string namespaceName)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            return syntaxGenerator.NamespaceImportDeclaration(namespaceName).WithAdditionalAnnotations(Formatter.Annotation);
        }

        protected override async Task<CompletionDescription> GetDescriptionWorkerAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (item is TypeImportCompletionItem importItem)
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var symbol = compilation.GetTypeByMetadataName(importItem.MetadataName);
                if (symbol != null)
                {
                    return CompletionDescription.FromText(DebugText);
                    //return await CommonCompletionUtilities.CreateDescriptionAsync(
                    //    document.Project.Solution.Workspace,
                    //    semanticModel,
                    //    0,
                    //    new[] { symbol },
                    //    null,
                    //    cancellationToken).ConfigureAwait(false);
                }
            }

            return CompletionDescription.Empty;
        }

        private static readonly SymbolDisplayFormat QualifiedNameOnlyFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        private async Task<ImmutableArray<CompletionItem>> GetCompletionItemsAsync(Document document, SyntaxContext context, int position, CancellationToken cancellationToken)
        {
            DebugClear();

            if (context.IsTypeContext)
            {
                var project = document.Project;
                var node = context.LeftToken.Parent;

                var namespacesInScope = GetNamespacesInScope(context.SemanticModel, node, cancellationToken)
                    .Select(symbol => symbol.ToDisplayString(QualifiedNameOnlyFormat))
                    .ToImmutableHashSet();

                if (project.SupportsCompilation)
                {
                    var builder = ArrayBuilder<CompletionItem>.GetInstance();

                    using (Logger.LogBlock(FunctionId.Completion_TypeImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
                    {
                        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                        var declarationsInCurrentProject = GetAccessibleOutOfScopeDeclarationInfosFromCompilation(compilation, namespacesInScope, true, cancellationToken);

                        builder.AddRange(declarationsInCurrentProject);

                        foreach (var reference in compilation.References)
                        {
                            if (reference is CompilationReference compilationReference)
                            {
                                var isInternalsVisible = compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(compilationReference.Compilation.Assembly);
                                var declarationsInReference = GetAccessibleOutOfScopeDeclarationInfosFromCompilation(compilationReference.Compilation, namespacesInScope, isInternalsVisible, cancellationToken);

                                builder.AddRange(declarationsInReference);
                            }
                            else if (reference is PortableExecutableReference && compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
                            {
                                var isInternalsVisible = compilation.Assembly.IsSameAssemblyOrHasFriendAccessTo(assemblySymbol);
                                var declarationsInReference = GetAccessibleOutOfScopeTopLevelDeclarationsFromAssembly(assemblySymbol, namespacesInScope, isInternalsVisible);

                                builder.AddRange(declarationsInReference);
                            }
                        }
                    }

                    return builder.ToImmutableAndFree();
                }
            }

            return ImmutableArray<CompletionItem>.Empty;
        }

        private ImmutableArray<CompletionItem> GetAccessibleOutOfScopeDeclarationInfosFromCompilation(
            Compilation compilation,
            ImmutableHashSet<string> namespacesInScope,
            bool isInternalsVisible,
            CancellationToken cancellationToken)
        {
            var tick = Environment.TickCount;

            var items = GetCompletionItemsForTopLevelTypeDeclarations(compilation, namespacesInScope, isInternalsVisible, cancellationToken);

            tick = Environment.TickCount - tick;

            _debug_total_compilation++;
            _debug_total_compilation_decl += items.Length;
            _debug_total_compilation_time += tick;

            return items;

            static ImmutableArray<CompletionItem> GetCompletionItemsForTopLevelTypeDeclarations(
                Compilation compilation,
                ImmutableHashSet<string> namespacesInScope,
                bool isInternalsVisible,
                CancellationToken cancellationToken)
            {
                var builder = ArrayBuilder<CompletionItem>.GetInstance();
                var root = compilation.DeclarationRoot;
                var minimumAccessibility = isInternalsVisible ? Accessibility.Internal : Accessibility.Public;

                VisitDeclaration(root, null, false);

                return builder.ToImmutableAndFree();

                void VisitDeclaration(
                    INamespaceOrTypeDeclaration declaration,
                    string currentNamespace,
                    bool shouldVisitTypesInCurrentNamespace)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (declaration.IsNamespace)
                    {
                        var namespaceDeclaration = (INamespaceDeclaration)declaration;
                        currentNamespace = ConcatNamespace(currentNamespace, namespaceDeclaration.Name);
                        shouldVisitTypesInCurrentNamespace = !namespacesInScope.Contains(currentNamespace);
                        foreach (var child in namespaceDeclaration.Children)
                        {
                            VisitDeclaration(child, currentNamespace, shouldVisitTypesInCurrentNamespace);
                        }
                    }
                    else if (shouldVisitTypesInCurrentNamespace)
                    {
                        // TODO: 
                        // 1. Ignore submission?
                        // 2. Handle Accessibility.NotApplicable
                        var typeDeclaration = (ITypeDeclaration)declaration;
                        if (typeDeclaration.DeclaredAccessibility >= minimumAccessibility)
                        {
                            builder.Add(TypeImportCompletionItem.Create(typeDeclaration, currentNamespace));
                        }
                    }
                }
            }
        }

        private ImmutableArray<CompletionItem> GetAccessibleOutOfScopeTopLevelDeclarationsFromAssembly(
            IAssemblySymbol fromAssembly,
            ImmutableHashSet<string> namespacesInScope,
            bool isInternalsVisible)
        {
            var tick = Environment.TickCount;

            var items = GetCompletionItemsForTopLevelTypeDeclarations(fromAssembly, namespacesInScope, isInternalsVisible);

            tick = Environment.TickCount - tick;

            _debug_total_pe++;
            _debug_total_pe_decl += items.Length;
            _debug_total_pe_time += tick;

            return items;

            static ImmutableArray<CompletionItem> GetCompletionItemsForTopLevelTypeDeclarations(IAssemblySymbol assemblySymbol, ImmutableHashSet<string> namespacesInScope, bool isInternalsVisible)
            {
                var builder = ArrayBuilder<CompletionItem>.GetInstance();
                var root = assemblySymbol.GlobalNamespace;
                var rootNamespace = assemblySymbol.ToDisplayString(QualifiedNameOnlyFormat);

                var minimumAccessibility = isInternalsVisible ? Accessibility.Internal : Accessibility.Public;

                VisitSymbol(root, null, false);

                return builder.ToImmutableAndFree();

                void VisitSymbol(ISymbol symbol, string containingNamespace, bool shouldVisitTypesInCurrentNamespace)
                {
                    if (symbol is INamespaceSymbol namespaceSymbol)
                    {
                        containingNamespace = ConcatNamespace(containingNamespace, namespaceSymbol.Name);
                        shouldVisitTypesInCurrentNamespace = !namespacesInScope.Contains(containingNamespace);

                        foreach (var memberSymbol in namespaceSymbol.GetMembers())
                        {
                            VisitSymbol(memberSymbol, containingNamespace, shouldVisitTypesInCurrentNamespace);
                        }
                    }
                    else if (shouldVisitTypesInCurrentNamespace
                        && symbol is INamedTypeSymbol typeSymbol
                        && typeSymbol.DeclaredAccessibility >= minimumAccessibility)
                    {
                        Debug.Assert(containingNamespace != null);
                        var item = TypeImportCompletionItem.Create(typeSymbol, containingNamespace);
                        builder.Add(item);
                    }
                }
            }
        }

        private static string ConcatNamespace(string containingNamespace, string name)
        {
            Debug.Assert(name != null);
            if (string.IsNullOrEmpty(containingNamespace))
            {
                return name;
            }

            _debug_total_namespace_concat++;
            return containingNamespace + "." + name;
        }

        private string DebugText => string.Format(
            DebugTextFormat,
            _debug_total_compilation, _debug_total_compilation_decl, _debug_total_compilation_time,
            _debug_total_pe, _debug_total_pe_decl, _debug_total_pe_time,
            _debug_total_namespace_concat,
            _debug_total_time_with_ItemCreation);

        private int _debug_total_compilation = 0;
        private int _debug_total_compilation_decl = 0;
        private int _debug_total_compilation_time = 0;

        private int _debug_total_pe = 0;
        private int _debug_total_pe_decl = 0;
        private int _debug_total_pe_time = 0;

        private int _debug_total_time_with_ItemCreation = 0;

        private static int _debug_total_namespace_concat = 0;

        private void DebugClear()
        {
            _debug_total_compilation = 0;
            _debug_total_compilation_decl = 0;
            _debug_total_compilation_time = 0;

            _debug_total_pe = 0;
            _debug_total_pe_decl = 0;
            _debug_total_pe_time = 0;

            _debug_total_time_with_ItemCreation = 0;
            _debug_total_namespace_concat = 0;
        }

        private const string DebugTextFormat = @"
Total Compilations: {0}
Total Declarations: {1}
Elapsed time: {2}

Total PEs: {3}
Total Declarations: {4}
Elapsed time: {5}

Total namespace concat: {6}
Total time: {7}";
    }
}
