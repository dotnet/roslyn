// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
#if DEBUG
            DebugObject.DebugClear();
            var tick = System.Environment.TickCount;
#endif
            var document = completionContext.Document;
            var position = completionContext.Position;
            var cancellationToken = completionContext.CancellationToken;

            var importCompletionOptionValue = completionContext.Options.GetOption(CompletionOptions.ShowImportCompletionItems, document.Project.Language);

            // Don't trigger import completion if the option value is "default" and the experiment is disabled for the user. 
            if (importCompletionOptionValue == false ||
                (importCompletionOptionValue == null && _experimentationService?.IsExperimentEnabled(WellKnownExperimentNames.TypeImportCompletion) != true))
            {
                return;
            }

            var syntaxContext = await CreateContextAsync(document, position, cancellationToken).ConfigureAwait(false);

            await AddCompletionItemsAsync(document, syntaxContext, position, completionContext, cancellationToken).ConfigureAwait(false);
#if DEBUG
            DebugObject.debug_total_time_with_ItemCreation = System.Environment.TickCount - tick;
#endif
            return;
        }

        private async Task AddCompletionItemsAsync(Document document, SyntaxContext context, int position, CompletionContext completionContext, CancellationToken cancellationToken)
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

#if DEBUG
                DebugObject.IsCurrentCompilation = true;
#endif
                var declarationsInCurrentProject =
                    await _typeImportCompletionService.GetAccessibleTopLevelTypesFromProjectAsync(project, cancellationToken).ConfigureAwait(false);
                var count = AddItems(completionContext, declarationsInCurrentProject, namespacesInScope); 
#if DEBUG
                DebugObject.debug_total_compilation_decl += count;

                DebugObject.IsCurrentCompilation = false;
#endif
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                foreach (var reference in compilation.References)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var declarationsInReference = ImmutableArray<CompletionItem>.Empty;
                    if (reference is CompilationReference compilationReference)
                    {
                        declarationsInReference = await _typeImportCompletionService.GetAccessibleTopLevelTypesFromCompilationReferenceAsync(
                            project.Solution,
                            compilation,
                            compilationReference,
                            cancellationToken).ConfigureAwait(false);
                    }
                    else if (reference is PortableExecutableReference peReference)
                    {
                        declarationsInReference = _typeImportCompletionService.GetAccessibleTopLevelTypesFromPEReference(
                            project.Solution,
                            compilation,
                            peReference,
                            cancellationToken);
                    }

                    var refrenceCount = AddItems(completionContext, declarationsInReference, namespacesInScope);
#if DEBUG
                    if (reference is CompilationReference)
                    {
                        DebugObject.debug_total_compilationRef_decl += refrenceCount;
                    }
                    else if (reference is PortableExecutableReference)
                    {
                        DebugObject.debug_total_pe_decl += refrenceCount;
                    }
#endif
                }
            }

            static int AddItems(CompletionContext completionContext, ImmutableArray<CompletionItem> items, ImmutableHashSet<string> namespacesInScope)
            {
                var count = 0;
                foreach (var item in items)
                {
                    var containingNamespace = TypeImportCompletionItem.GetContainingNamespace(item);
                    if (!namespacesInScope.Contains(containingNamespace))
                    {
                        completionContext.AddItem(item);
                        count++;
                    }
                }
                return count;
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

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(
            Document document, CompletionItem item, CancellationToken cancellationToken)
        {
#if DEBUG
            return Task.FromResult(CompletionDescription.FromText(DebugObject.DebugText));
#else
            return TypeImportCompletionItem.GetCompletionDescriptionAsync(document, item, cancellationToken);
#endif
        }
    }

#if DEBUG
    internal static class DebugObject
    {
        public static string DebugText =>
           $@"
Current Declarations: {debug_total_compilation_decl}   
Current Declarations Created: {debug_total_compilation_decl_created}
Elapsed time: {debug_total_compilation_time}

Ref Compilations: {debug_total_compilationRef}
Ref Declarations: {debug_total_compilationRef_decl}   
Ref Declarations Created: {debug_total_compilationRef_decl_created}
Elapsed time: {debug_total_compilationRef_time}

Total PEs: {debug_total_pe}
Total Declarations: {debug_total_pe_decl}  
Total Declarations Created: {debug_total_pe_decl_created}
Elapsed time: {debug_total_pe_time}

Total namespace concat: {debug_total_namespace_concat}
Total indistinct namespaces: {Namespaces.Count}
Total time: {debug_total_time_with_ItemCreation}";

        public static int debug_total_compilation_decl = 0;
        public static int debug_total_compilation_decl_created = 0;
        public static int debug_total_compilation_time = 0;

        public static int debug_total_compilationRef = 0;
        public static int debug_total_compilationRef_decl = 0;
        public static int debug_total_compilationRef_decl_created = 0;
        public static int debug_total_compilationRef_time = 0;

        public static int debug_total_pe = 0;
        public static int debug_total_pe_decl = 0;
        public static int debug_total_pe_decl_created = 0;
        public static int debug_total_pe_time = 0;

        public static int debug_total_time_with_ItemCreation = 0;

        public static int debug_total_namespace_concat = 0;

        public static bool IsCurrentCompilation { get; set; }

        public static HashSet<string> Namespaces { get; } = new HashSet<string>();


        public static void SetPE(int decl, int decl_created, int time)
        {
            debug_total_pe++;
            debug_total_pe_decl += decl;
            debug_total_pe_decl_created += decl_created;
            debug_total_pe_time += time;
        }

        public static void SetCompilation(int decl, int decl_created, int time)
        {
            if (IsCurrentCompilation)
            {
                debug_total_compilation_decl += decl;
                debug_total_compilation_decl_created += decl_created;
                debug_total_compilation_time += time;
            }
            else
            {
                debug_total_compilationRef++;
                debug_total_compilationRef_decl += decl;
                debug_total_compilationRef_decl_created += decl_created;
                debug_total_compilationRef_time += time;
            }
        }

        public static void DebugClear()
        {
            Namespaces.Clear();
            IsCurrentCompilation = false;

            debug_total_compilation_decl = 0;
            debug_total_compilation_decl_created = 0;
            debug_total_compilation_time = 0;

            debug_total_compilationRef = 0;
            debug_total_compilationRef_decl = 0;
            debug_total_compilationRef_decl_created = 0;
            debug_total_compilationRef_time = 0;

            debug_total_pe = 0;
            debug_total_pe_decl = 0;
            debug_total_pe_decl_created = 0;
            debug_total_pe_time = 0;

            debug_total_time_with_ItemCreation = 0;
            debug_total_namespace_concat = 0;
        }
    }
#endif
}
