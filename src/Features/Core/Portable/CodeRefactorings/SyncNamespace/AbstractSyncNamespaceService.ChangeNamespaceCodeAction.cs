// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceService<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax>
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
    {
        /// <summary>
        /// This code action tries to change the name of the namespace declaration to 
        /// match the folder hierarchy of the document. The new namespace is constructed 
        /// by concatenating the default namespace of the project and all the folders in 
        /// the file path up to the project root.
        /// 
        /// For example, if he default namespace is `A.B.C`, file path is 
        /// "[project root dir]\D\E\F\Class1.cs" and declared namespace in the file is
        /// `Foo.Bar.Baz`, then this action will change the namespace declaration
        /// to `A.B.C.D.E.F`. 
        /// 
        /// Note that it also handles the case where the target namespace or declared namespace 
        /// is global namespace, i.e. default namespace is "" and the file is located at project 
        /// root directory, and no namespace declaration in the document, respectively.
        /// </summary>
        internal sealed class ChangeNamespaceCodeAction : CodeAction
        {
            private readonly State _state;
            private readonly AbstractSyncNamespaceService<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax> _service;
            private readonly ImmutableArray<string> _oldNamespaceParts;
            private readonly ImmutableArray<string> _newNamespaceParts;

            public override string Title => _state.TargetNamespace.Length == 0 
                ? FeaturesResources.Change_to_global_namespace
                : string.Format(FeaturesResources.Change_namespace_to_0, _state.TargetNamespace);

            public ChangeNamespaceCodeAction(
                AbstractSyncNamespaceService<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax> service,
                State state)
            {
                Debug.Assert(state.TargetNamespace != null);

                _service = service;
                _state = state;

                var dotSeparator = new[] { '.' };
                _oldNamespaceParts = _state.DeclaredNamespace.Split(dotSeparator).ToImmutableArray();
                _newNamespaceParts = _state.TargetNamespace.Split(dotSeparator).ToImmutableArray();
            }

            private ImmutableArray<ISymbol> GetDeclaredSymbolsInContainer(
                SemanticModel semanticModel, 
                SyntaxNode node, 
                CancellationToken cancellationToken)
            {
                var declarations = _service.GetMemberDeclarationsInContainer(node);
                var builder = ArrayBuilder<ISymbol>.GetInstance();
                foreach (var declaration in declarations)
                {
                    var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
                    builder.AddIfNotNull(symbol);
                }

                return builder.ToImmutableAndFree();
            }

            protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                // Here's the entire process for changing namespace:
                // 1. Change the namespace declaration, fix references and add imports that might be necessary.
                // 2. Explicitly merge the diff to get a new solution.
                // 3. Remove added imports that are unnecessary.
                // 4. Do another explicit diff merge based on last merged solution.
                //
                // The reason for doing explicit diff merge twice is so merging after remove unnecessaty imports can be correctly handled.

                var solutionAfterNamespaceChange = _state.Solution;
                var referenceDocuments = PooledHashSet<DocumentId>.GetInstance();

                foreach (var id in _state.DocumentIds)
                {
                    var (newSolution, refDocumentIds) = await ChangeNamespaceToMatchFoldersAsync(solutionAfterNamespaceChange, id, cancellationToken).ConfigureAwait(false);
                    solutionAfterNamespaceChange = newSolution;
                    referenceDocuments.AddRange(refDocumentIds);
                }

                var solutionAfterFirstMerge = await MergeDiffAsync(_state.Solution, solutionAfterNamespaceChange, cancellationToken).ConfigureAwait(false);

                // After changing documents, we still need to remove unnecessary imports related to our change.
                // We don't try to remove all imports that might become unnecessary/invalid after the namespace change, 
                // just ones that fully matche the old/new namespace. Because it's hard to get it right and will almost 
                // certainly cause perf issue.
                // For example, if we are changing namespace `Foo.Bar` (which is the only namespace declaration with such name)
                // to `A.B`, the using of name `Bar` in a different file below would remain untouched, even it's no longer valid:
                //
                //      namespace Foo
                //      {
                //          using Bar;
                //          ~~~~~~~~~
                //      }
                //
                // Also, because we may have added different imports to document that triggered the refactoring
                // and the documents that reference affected types declared in changed namespace, we try to remove
                // unnecessary imports separately.

                var solutionAfterImportsRemoved = await RemoveUnnecessaryImportsAsync(
                    solutionAfterFirstMerge,
                    _state.DocumentIds, 
                    CreateAllContainingNamespaces(_oldNamespaceParts), 
                    cancellationToken).ConfigureAwait(false);

                solutionAfterImportsRemoved = await RemoveUnnecessaryImportsAsync(
                    solutionAfterImportsRemoved, 
                    referenceDocuments.ToImmutableArray(), 
                    ImmutableArray.Create(_state.DeclaredNamespace, _state.TargetNamespace), 
                    cancellationToken).ConfigureAwait(false);

                referenceDocuments.Free();
                return await MergeDiffAsync(solutionAfterFirstMerge, solutionAfterImportsRemoved, cancellationToken).ConfigureAwait(false);
            }

            private ImmutableArray<string> CreateAllContainingNamespaces(ImmutableArray<string> parts)
            {
                var builder = ArrayBuilder<string>.GetInstance();
                for (var i = 1; i <= _oldNamespaceParts.Length; ++i)
                {
                    builder.Add(string.Join(".", _oldNamespaceParts.Take(i)));
                }

                return builder.ToImmutableAndFree();
            }

            private ImmutableArray<SyntaxNode> CreateImports(Document document, ImmutableArray<string> names, bool withFormatterAnnotation)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var builder = ArrayBuilder<SyntaxNode>.GetInstance(names.Length);
                for (var i = 0; i < names.Length; ++i)
                {
                    builder.Add(CreateImport(generator, names[i], withFormatterAnnotation));
                }

                return builder.ToImmutableAndFree();
            }

            private SyntaxNode CreateImport(SyntaxGenerator syntaxGenerator, string name, bool withFormatterAnnotation)
            {
                var import = syntaxGenerator.NamespaceImportDeclaration(name);
                if (withFormatterAnnotation)
                {
                    import = import.WithAdditionalAnnotations(Formatter.Annotation);
                }
                return import;
            }

            /// <summary>
            /// Try to change the namespace declaration in the document (specified by <paramref name="id"/> in <paramref name="solution"/>),
            /// so that the namespace is in sync with project's default namespace and the folder structure where the document is located.
            /// Returns a new solution after changing namespace, and a list of IDs for documents that also changed becuase they referenced
            /// the types declared in the changed namespace (not include the document contains the declaration itself).
            /// </summary>
            private async Task<(Solution, ImmutableArray<DocumentId>)> ChangeNamespaceToMatchFoldersAsync(Solution solution, DocumentId id, CancellationToken cancellationToken)
            {
                var document = solution.GetDocument(id);
                var targetNamespace = _state.TargetNamespace;

                var declarationRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var container = declarationRoot.DescendantNodes().FirstOrDefault(node => node is TNamespaceDeclarationSyntax) ?? declarationRoot;

                // Get types declared in the changing namespace, because ee need to fix all references to them, 
                // e.g. change the namespace for qualified name, add imports to proper containers, etc.
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var declaredSymbols = GetDeclaredSymbolsInContainer(semanticModel, container, cancellationToken);

                var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                // Separating references to declaredSymbols into two groups based on wheter it's located in the same 
                // document as the namespace declaration. This is because code change required for them are different.
                var refLocationsInCurrentDocument = new List<ReferenceLocation>();
                var refLocationsInOtherDocuments = new List<ReferenceLocation>();

                var refLocations = await Task.WhenAll(
                    declaredSymbols.Select(declaredSymbol 
                        => FindReferenceLocationsForSymbol(document, declaredSymbol, cancellationToken))).ConfigureAwait(false);

                foreach (var refLocation in refLocations.SelectMany(locs => locs))
                {
                    if (refLocation.Document.Id == document.Id)
                    {
                        refLocationsInCurrentDocument.Add(refLocation);
                    }
                    else
                    {
                        Debug.Assert(!PathUtilities.PathsEqual(refLocation.Document.FilePath, document.FilePath));
                        refLocationsInOtherDocuments.Add(refLocation);
                    }
                }

                var documentWithNewNamespace = await FixDeclarationDocumentAsync(document, refLocationsInCurrentDocument, cancellationToken)
                    .ConfigureAwait(false);
                var solutionWithChangedNamespace = documentWithNewNamespace.Project.Solution;

                var refLocationGroups = refLocationsInOtherDocuments.GroupBy(loc => loc.Document.Id);

                var fixedDocuments = await Task.WhenAll(
                    refLocationGroups.Select(refInOneDocument => 
                        FixReferencingDocumentAsync(
                            solutionWithChangedNamespace.GetDocument(refInOneDocument.Key),
                            refInOneDocument,
                            cancellationToken))).ConfigureAwait(false);

                var solutionWithFixedReferences = await MergeDocumentChangesAsync(solutionWithChangedNamespace, fixedDocuments, cancellationToken).ConfigureAwait(false);

                return (solutionWithFixedReferences, refLocationGroups.SelectAsArray(g => g.Key));
            }

            private static async Task<Solution> MergeDocumentChangesAsync(Solution originalSolution, Document[] changedDocuments, CancellationToken cancellationToken)
            {
                foreach (var document in changedDocuments)
                {
                    originalSolution = originalSolution.WithDocumentSyntaxRoot(
                        document.Id,
                        await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
                }

                return originalSolution;
            }

            private static async Task<ImmutableArray<ReferenceLocation>> FindReferenceLocationsForSymbol(
                Document document, ISymbol symbol, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var progress = new StreamingProgressCollector(StreamingFindReferencesProgress.Instance);

                await SymbolFinder.FindReferencesAsync(
                    symbolAndProjectId: SymbolAndProjectId.Create(symbol, document.Project.Id),
                    solution: document.Project.Solution,
                    documents: null,
                    progress: progress,
                    options: FindReferencesSearchOptions.Default,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                var referencedSymbols = progress.GetReferencedSymbols();
                return referencedSymbols.Where(refSymbol => refSymbol.Definition.Equals(symbol))
                        .SelectMany(refSymbol => refSymbol.Locations).ToImmutableArray();
            }

            private async Task<Document> FixDeclarationDocumentAsync(
                Document document, 
                IReadOnlyList<ReferenceLocation> refLocations, 
                CancellationToken cancellationToken)
            {
                Debug.Assert(!_newNamespaceParts.IsDefault);

                // 1. Fix references to the affected types in this document if necessary.
                // 2. Add usings for containing namespaces, in case we have references 
                //    relying on old namespace declaration for resolution. 
                //
                //      For example, in the code below, after we change namespace to 
                //      "A.B.C", we will need to add "using Foo.Bar;".     
                //
                //      namespace Foo.Bar.Baz
                //      {
                //          class C1
                //          {
                //               C2 _c2;    // C2 is define in namespace "Foo.Bar" in another document.
                //          }
                //      }
                //
                // 3. Change namespace declaration to target namespace.
                // 4. Simplify away unnecessary qualifications.

                var addImportService = document.GetLanguageService<IAddImportsService>();
                ImmutableArray<SyntaxNode> containers;

                if (refLocations.Count > 0)
                {
                    (document, containers) = await FixReferencesAsync(document, addImportService, _service, refLocations, _newNamespaceParts, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    // If there's no reference to types declared in this document,
                    // we will use root node as import container.
                    containers = ImmutableArray.Create(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
                }

                Debug.Assert(containers.Length > 0);

                // Need to import all containing namespaces of old namespace and add them to the document (if it's not global namespace)
                var namesToImport = CreateAllContainingNamespaces(_oldNamespaceParts);

                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
                var documentWithAddedImports = await AddImportsInContainersAsync(
                        document, 
                        addImportService, 
                        containers, 
                        namesToImport, 
                        placeSystemNamespaceFirst, 
                        cancellationToken).ConfigureAwait(false);

                var root = await documentWithAddedImports.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                root = _service.ChangeNamespaceDeclaration((TCompilationUnitSyntax)root, _oldNamespaceParts, _newNamespaceParts)
                    .WithAdditionalAnnotations(Formatter.Annotation);

                // Need to invoke formatter explicitly since we are doing the diff merge ourselves.
                root = await Formatter.FormatAsync(root, Formatter.Annotation, documentWithAddedImports.Project.Solution.Workspace, optionSet, cancellationToken)
                    .ConfigureAwait(false);

                root = root.WithAdditionalAnnotations(Simplifier.Annotation);
                var formattedDocument = documentWithAddedImports.WithSyntaxRoot(root);
                return await Simplifier.ReduceAsync(formattedDocument, optionSet, cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> FixReferencingDocumentAsync(
                Document document,
                IEnumerable<ReferenceLocation> refLocations,    
                CancellationToken cancellationToken)
            {
                // 1. Fully qualify all simple references (i.e. not via an alias) with new namespace.
                // 2. Add using of new namespace (for each reference's container).
                // 3. Try to simplify qualified names introduced from step(1).

                var addImportService = document.GetLanguageService<IAddImportsService>();
                var syncNamespaceService = document.GetLanguageService<ISyncNamespaceService>();
                
                var (documentWithRefFixed, containers) = 
                    await FixReferencesAsync(document, addImportService, syncNamespaceService, refLocations, _newNamespaceParts, cancellationToken)
                        .ConfigureAwait(false);

                var optionSet = await documentWithRefFixed.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, documentWithRefFixed.Project.Language);

                var documentWithAdditionalImports = await AddImportsInContainersAsync(
                    documentWithRefFixed, 
                    addImportService,
                    containers,
                    ImmutableArray.Create(_state.TargetNamespace), 
                    placeSystemNamespaceFirst, 
                    cancellationToken).ConfigureAwait(false);

                // Need to invoke formatter explicitly since we are doing the diff merge ourselves.
                var formattedDocument = await Formatter.FormatAsync(documentWithAdditionalImports, Formatter.Annotation, optionSet, cancellationToken)
                    .ConfigureAwait(false);

                return await Simplifier.ReduceAsync(formattedDocument, optionSet, cancellationToken).ConfigureAwait(false);
            }

            /// <summary>
            /// Fix each reference and return a collection of proper containers (innermost container
            /// with imports) that new import should be added to based on reference locations.
            /// If <paramref name="namespaceParts"/> is specified (not default), the fix would be:
            ///     1. qualify the reference with new namespace and mark it for simplification, or
            ///     2. find and mark the qualified reference for simplification.
            /// Otherwise, there would be no namespace replacement.
            /// </summary>
            private async Task<(Document, ImmutableArray<SyntaxNode>)> FixReferencesAsync(
                Document document, 
                IAddImportsService addImportService, 
                ISyncNamespaceService syncNamespaceService,
                IEnumerable<ReferenceLocation> refLocations, 
                ImmutableArray<string> namespaceParts,
                CancellationToken cancellationToken)
            {
                var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                var root = editor.OriginalRoot;
                var containers = PooledHashSet<SyntaxNode>.GetInstance();

                var generator = SyntaxGenerator.GetGenerator(document);
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

                // We need a dummy import to figure out the container for given reference.
                var dummyImport = CreateImport(generator, "Dummy", withFormatterAnnotation: false);

                foreach (var refLoc in refLocations)
                {
                    Debug.Assert(document.Id == refLoc.Document.Id);

                    // Ignore references via alias. For simple cases where the alias is defined as the type we are interested,
                    // it will be handled properly because it is one of the reference to the type symbol. Otherwise, we don't
                    // attempt to make a potential fix, and user might end up with errors as a result.                    
                    if (refLoc.Alias != null)
                    {
                        continue;
                    }

                    // Other documents in the solution might have changed after we calculated those ReferenceLocation, 
                    // so we can't trust anything to be still up-to-date except their spans.

                    // Get inner most node in case of type used as a base type. e.g.
                    //
                    //      public class Foo {}
                    //      public class Bar : Foo {}
                    //
                    // For the reference to Foo where it is used as a base class, the BaseTypeSyntax and the TypeSyntax
                    // have exact same span.

                    var refNode = root.FindNode(refLoc.Location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
                    if (syncNamespaceService.TryGetReplacementReferenceSyntax(
                            refNode, namespaceParts, syntaxFacts, out var oldNode, out var newNode))
                    {
                        editor.ReplaceNode(oldNode, newNode.WithAdditionalAnnotations(Simplifier.Annotation));
                    }

                    // Use a dummy import node to figure out which container the new import will be added to.
                    var container = addImportService.GetImportContainer(root, refNode, dummyImport);
                    containers.Add(container);
                }

                foreach(var container in containers)
                {
                    editor.TrackNode(container);
                }

                var fixedDocument = editor.GetChangedDocument();
                root = await fixedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var result = (fixedDocument, containers.SelectAsArray(c => root.GetCurrentNode(c)));

                containers.Free();
                return result;
            }

            private async Task<Solution> RemoveUnnecessaryImportsAsync(
                Solution solution,
                ImmutableArray<DocumentId> ids,
                ImmutableArray<string> names,
                CancellationToken cancellationToken)
            {
                var LinkedDocumentsToSkip = PooledHashSet<DocumentId>.GetInstance();
                var documentsToProcessBuilder = ArrayBuilder<Document>.GetInstance();

                foreach (var id in ids)
                {
                    if (LinkedDocumentsToSkip.Contains(id))
                    {
                        continue;
                    }

                    var document = solution.GetDocument(id);
                    LinkedDocumentsToSkip.AddRange(document.GetLinkedDocumentIds());
                    documentsToProcessBuilder.Add(document);

                    document = await RemoveUnnecessaryImportsWorker(
                        document,
                        CreateImports(document, names, withFormatterAnnotation: false),
                        cancellationToken).ConfigureAwait(false);
                    solution = document.Project.Solution;
                }

                var documentsToProcess = documentsToProcessBuilder.ToImmutableAndFree();
                LinkedDocumentsToSkip.Free();

                var changeDocuments = await Task.WhenAll(documentsToProcess.Select(
                        doc => RemoveUnnecessaryImportsWorker(
                            doc,
                            CreateImports(doc, names, withFormatterAnnotation: false),
                            cancellationToken))).ConfigureAwait(false);

                return await MergeDocumentChangesAsync(solution, changeDocuments, cancellationToken).ConfigureAwait(false);

                Task<Document> RemoveUnnecessaryImportsWorker(
                    Document doc,
                    IEnumerable<SyntaxNode> importsToRemove,
                    CancellationToken token)
                {
                    var removeImportService = doc.GetLanguageService<IRemoveUnnecessaryImportsService>();
                    var syntaxFacts = doc.GetLanguageService<ISyntaxFactsService>();

                    return removeImportService.RemoveUnnecessaryImportsAsync(
                        doc,
                        import => importsToRemove.Any(importToRemove => syntaxFacts.AreEquivalent(importToRemove, import)),
                        token);
                }
            }

            /// <summary>
            /// Add imports for the namespace specified by <paramref name="names"/>
            /// to the provided <paramref name="containers"/>
            /// </summary>
            private async Task<Document> AddImportsInContainersAsync(
                Document document,
                IAddImportsService addImportService,
                ImmutableArray<SyntaxNode> containers, 
                ImmutableArray<string> names,
                bool placeSystemNamespaceFirst, 
                CancellationToken cancellationToken)
            {
                // Sort containers based on their span start, to make the result of 
                // adding imports deterministic. 
                if (containers.Length > 1)
                {
                    containers = containers.Sort(SyntaxNodeSpanStartComparer.Instance);
                }

                var imports = CreateImports(document, names, withFormatterAnnotation: true);
                foreach (var container in containers)
                {
                    // If the container is a namespace declaration, the context we pass to 
                    // AddImportService must be a child of the declaration, otherwise the 
                    // import will be added to root node instead.
                    var contextLocation = container is TNamespaceDeclarationSyntax 
                        ? container.DescendantNodes().First() 
                        : container;

                    var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    root = addImportService.AddImports(compilation, root, contextLocation, imports, placeSystemNamespaceFirst);
                    document = document.WithSyntaxRoot(root);
                }

                return document;
            }

            private static async Task<Solution> MergeDiffAsync(Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
            {
                var diffMergingSession = new LinkedFileDiffMergingSession(oldSolution, newSolution, newSolution.GetChanges(oldSolution), logSessionInfo: false);
                var mergeResult = await diffMergingSession.MergeDiffsAsync(mergeConflictHandler: null, cancellationToken: cancellationToken).ConfigureAwait(false);
                return mergeResult.MergedSolution;
            }

            private class SyntaxNodeSpanStartComparer : IComparer<SyntaxNode>
            {
                private SyntaxNodeSpanStartComparer()
                {
                }

                public static SyntaxNodeSpanStartComparer Instance { get; } = new SyntaxNodeSpanStartComparer();

                public int Compare(SyntaxNode x, SyntaxNode y)
                    => x.Span.Start - y.Span.Start;
            }
        }
    }
}
