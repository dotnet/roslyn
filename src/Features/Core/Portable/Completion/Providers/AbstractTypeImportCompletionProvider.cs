// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractTypeImportCompletionProvider : CommonCompletionProvider
    {
        private readonly SyntaxAnnotation _annotation = new SyntaxAnnotation();

        protected abstract Task<SyntaxContext> CreateContextAsync(Document document, int position, CancellationToken cancellationToken);
        protected abstract ImmutableHashSet<string> GetNamespacesInScope(SyntaxNode location, SemanticModel semanticModel, CancellationToken cancellationToken);

        private readonly ITypeImportCompletionService _typeImportCompletionService;
        private readonly IExperimentationService _experimentationService;

        public AbstractTypeImportCompletionProvider(Workspace workspace)
            : base()
        {
            _typeImportCompletionService = workspace.Services.GetService<ITypeImportCompletionService>();
            _experimentationService = workspace.Services.GetService<IExperimentationService>();
        }

        public override async Task ProvideCompletionsAsync(CompletionContext completionContext)
        {
#if DEBUG
            DebugObject.DebugClear();
            var tick = Environment.TickCount;
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

            var items = await GetCompletionItemsAsync(document, syntaxContext, position, cancellationToken).ConfigureAwait(false);
            completionContext.AddItems(items);
#if DEBUG
            DebugObject.debug_total_time_with_ItemCreation = Environment.TickCount - tick;
#endif
            return;
        }

        private async Task<ImmutableArray<CompletionItem>> GetCompletionItemsAsync(Document document, SyntaxContext context, int position, CancellationToken cancellationToken)
        {
            if (context.IsTypeContext)
            {
                using (Logger.LogBlock(FunctionId.Completion_TypeImportCompletionProvider_GetCompletionItemsAsync, cancellationToken))
                {
                    var project = document.Project;
                    var node = context.LeftToken.Parent;

                    var namespacesInScope = GetNamespacesInScope(node, context.SemanticModel, cancellationToken);
                    var builder = ArrayBuilder<TypeImportCompletionItem>.GetInstance();

                    var declarationsInCurrentProject = await _typeImportCompletionService
                        .GetAccessibleTopLevelTypesFromProjectAsync(project, namespacesInScope, cancellationToken).ConfigureAwait(false);
                    builder.AddRange(declarationsInCurrentProject);

                    var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    foreach (var reference in compilation.References)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var declarationsInReference = ImmutableArray<TypeImportCompletionItem>.Empty;
                        if (reference is CompilationReference compilationReference)
                        {
                            declarationsInReference = await _typeImportCompletionService.GetAccessibleTopLevelTypesFromCompilationReferenceAsync(
                                project.Solution,
                                compilation,
                                compilationReference,
                                namespacesInScope,
                                cancellationToken).ConfigureAwait(false);

                        }
                        else if (reference is PortableExecutableReference peReference)
                        {
                            declarationsInReference = _typeImportCompletionService.GetAccessibleTopLevelTypesFromPEReference(
                                project.Solution,
                                compilation,
                                peReference,
                                namespacesInScope,
                                cancellationToken);
                        }

                        builder.AddRange(declarationsInReference);
                    }

                    return ImmutableArray<CompletionItem>.CastUp(builder.ToImmutableAndFree());
                }
            }

            return ImmutableArray<CompletionItem>.Empty;
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
            var change = Utilities.Collapse(newText, changes.ToImmutableArray());

            return CompletionChange.Create(change, newPosition, includesCommitCharacter: true);
        }

        private async Task<Document> ComputeNewDocumentAsync(Document document, CompletionItem completionItem, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // Complete type name.
            var finalText = root.GetText(text.Encoding)
                .Replace(completionItem.Span, completionItem.DisplayText);
            document = document.WithText(finalText);

            // Annotate added node so we can move caret to proper location later.
            tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var addedNode = root.FindNode(completionItem.Span);
            var annotatedNode = addedNode.WithAdditionalAnnotations(_annotation);
            root = root.ReplaceNode(addedNode, annotatedNode);
            document = document.WithSyntaxRoot(root);

            // Add required using/imports directive.
            var importItem = (TypeImportCompletionItem)completionItem;
            var importNode = CreateImport(document, importItem.ContainingNamespace);

            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);

            var addImportService = document.GetLanguageService<IAddImportsService>();
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            root = addImportService.AddImport(compilation, root, annotatedNode, importNode, placeSystemNamespaceFirst);
            document = document.WithSyntaxRoot(root);

            // Format newly added nodes.
            document = await Formatter.FormatAsync(document, Formatter.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

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
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
#if DEBUG
                    return CompletionDescription.FromText(DebugObject.DebugText);
#else
                    return await CommonCompletionUtilities.CreateDescriptionAsync(
                        document.Project.Solution.Workspace,
                        semanticModel,
                        position: 0,
                        symbol,
                        importItem.OverloadCount,
                        supportedPlatforms: null,
                        cancellationToken).ConfigureAwait(false);
#endif
                }
            }

            return CompletionDescription.Empty;
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
