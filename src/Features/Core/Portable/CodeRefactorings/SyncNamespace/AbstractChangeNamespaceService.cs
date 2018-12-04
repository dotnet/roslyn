// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeNamespace
{
    // This intermediate class is used to hide language specific method  `TryGetReplacementReferenceSyntax` from the service interface.
    internal abstract class AbstractChangeNamespaceService : IChangeNamespaceService
    {
        public abstract Task<Solution> ChangeNamespaceAsync(Solution solution, ImmutableArray<DocumentId> documentIds, string declaredNamespace, string targetNamespace, CancellationToken cancellationToken);

        /// <summary>
        /// Try to get a new node to replace given node, which is a reference to a top-level type declared inside the 
        /// namespce to be changed. If this reference is the right side of a qualified name, the new node returned would
        /// be the entire qualified name. Depends on whether <paramref name="newNamespaceParts"/> is provided, the name 
        /// in the new node might be qualified with this new namespace instead.
        /// </summary>
        /// <param name="reference">A reference to a type declared inside the namespce to be changed, which is calculated 
        /// based on results from `SymbolFinder.FindReferencesAsync`.</param>
        /// <param name="newNamespaceParts">If specified, the namespace of original reference will be replaced with given 
        /// namespace in the replacement node.</param>
        /// <param name="old">The node to be replaced. This might be an ancestor of original </param>
        /// <param name="new">The replacement node.</param>
        public abstract bool TryGetReplacementReferenceSyntax(SyntaxNode reference, ImmutableArray<string> newNamespaceParts, ISyntaxFactsService syntaxFacts, out SyntaxNode old, out SyntaxNode @new);
    }

    internal abstract class AbstractChangeNamespaceService<TNamespaceDeclarationSyntax, TCompilationUnitSyntax, TMemberDeclarationSyntax>
        : AbstractChangeNamespaceService
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
    {
        private static readonly char[] s_dotSeparator = new[] { '.' };

        protected abstract TCompilationUnitSyntax ChangeNamespaceDeclaration(
            TCompilationUnitSyntax root, ImmutableArray<string> declaredNamespaceParts, ImmutableArray<string> targetNamespaceParts);

        protected abstract SyntaxList<TMemberDeclarationSyntax> GetMemberDeclarationsInContainer(SyntaxNode compilationUnitOrNamespaceDecl);

        protected static SyntaxAnnotation WarningAnnotation { get; }
            = CodeActions.WarningAnnotation.Create(
                FeaturesResources.Warning_colon_changing_namespace_may_produce_invalid_code_and_change_code_meaning);

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
        public override async Task<Solution> ChangeNamespaceAsync(
            Solution solution, 
            ImmutableArray<DocumentId> documentIds, 
            string declaredNamespace, 
            string targetNamespace, 
            CancellationToken cancellationToken)
        {
            Debug.Assert(targetNamespace != null);            

            // Here's the entire process for changing namespace:
            // 1. Change the namespace declaration, fix references and add imports that might be necessary.
            // 2. Explicitly merge the diff to get a new solution.
            // 3. Remove added imports that are unnecessary.
            // 4. Do another explicit diff merge based on last merged solution.
            //
            // The reason for doing explicit diff merge twice is so merging after remove unnecessaty imports can be correctly handled.

            var solutionAfterNamespaceChange = solution;
            var referenceDocuments = PooledHashSet<DocumentId>.GetInstance();

            try
            {
                foreach (var id in documentIds)
                {
                    var (newSolution, refDocumentIds) =
                        await ChangeNamespaceToMatchFoldersAsync(solutionAfterNamespaceChange, id, declaredNamespace, targetNamespace, cancellationToken)
                            .ConfigureAwait(false);
                    solutionAfterNamespaceChange = newSolution;
                    referenceDocuments.AddRange(refDocumentIds);
                }

                var solutionAfterFirstMerge = await MergeDiffAsync(solution, solutionAfterNamespaceChange, cancellationToken).ConfigureAwait(false);

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
                    documentIds,
                    CreateAllContainingNamespaces(declaredNamespace),
                    cancellationToken).ConfigureAwait(false);

                solutionAfterImportsRemoved = await RemoveUnnecessaryImportsAsync(
                    solutionAfterImportsRemoved,
                    referenceDocuments.ToImmutableArray(),
                    ImmutableArray.Create(declaredNamespace, targetNamespace),
                    cancellationToken).ConfigureAwait(false);

                return await MergeDiffAsync(solutionAfterFirstMerge, solutionAfterImportsRemoved, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                referenceDocuments.Free();
            }
        }

        private ImmutableArray<ISymbol> GetDeclaredSymbolsInContainer(
            SemanticModel semanticModel,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var declarations = GetMemberDeclarationsInContainer(node);
            var builder = ArrayBuilder<ISymbol>.GetInstance();
            foreach (var declaration in declarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
                builder.AddIfNotNull(symbol);
            }

            return builder.ToImmutableAndFree();
        }

        private static ImmutableArray<string> GetNamespaceParts(string @namespace)
        {
            return @namespace?.Split(s_dotSeparator).ToImmutableArray() ?? default;
        }

        private static ImmutableArray<string> CreateAllContainingNamespaces(string @namespace)
        {
            var parts = GetNamespaceParts(@namespace);
            var builder = ArrayBuilder<string>.GetInstance();
            for (var i = 1; i <= parts.Length; ++i)
            {
                builder.Add(string.Join(".", parts.Take(i)));
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

        private static SyntaxNode CreateImport(SyntaxGenerator syntaxGenerator, string name, bool withFormatterAnnotation)
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
        private async Task<(Solution, ImmutableArray<DocumentId>)> ChangeNamespaceToMatchFoldersAsync(
            Solution solution, 
            DocumentId id, 
            string oldNamespace, 
            string newNamespace, 
            CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(id);

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

            var documentWithNewNamespace = await FixDeclarationDocumentAsync(document, refLocationsInCurrentDocument, oldNamespace, newNamespace, cancellationToken)
                .ConfigureAwait(false);
            var solutionWithChangedNamespace = documentWithNewNamespace.Project.Solution;

            var refLocationGroups = refLocationsInOtherDocuments.GroupBy(loc => loc.Document.Id);

            var fixedDocuments = await Task.WhenAll(
                refLocationGroups.Select(refInOneDocument =>
                    FixReferencingDocumentAsync(
                        solutionWithChangedNamespace.GetDocument(refInOneDocument.Key),
                        refInOneDocument,
                        newNamespace,
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
            string oldNamespace,
            string newNamespace,
            CancellationToken cancellationToken)
        {
            Debug.Assert(newNamespace != null);

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

            var oldNamespaceParts = GetNamespaceParts(oldNamespace);
            var newNamespaceParts = GetNamespaceParts(newNamespace);

            if (refLocations.Count > 0)
            {
                (document, containers) = await FixReferencesAsync(document, this, addImportService, refLocations, newNamespaceParts, cancellationToken)
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
            var namesToImport = CreateAllContainingNamespaces(oldNamespace);

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
            root = ChangeNamespaceDeclaration((TCompilationUnitSyntax)root, oldNamespaceParts, newNamespaceParts)
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
            string newNamespace,
            CancellationToken cancellationToken)
        {
            // 1. Fully qualify all simple references (i.e. not via an alias) with new namespace.
            // 2. Add using of new namespace (for each reference's container).
            // 3. Try to simplify qualified names introduced from step(1).

            var addImportService = document.GetLanguageService<IAddImportsService>();
            var changeNamespaceService = document.GetLanguageService<IChangeNamespaceService>();

            var newNamespaceParts = GetNamespaceParts(newNamespace);

            var (documentWithRefFixed, containers) =
                await FixReferencesAsync(document, changeNamespaceService, addImportService, refLocations, newNamespaceParts, cancellationToken)
                    .ConfigureAwait(false);

            var optionSet = await documentWithRefFixed.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, documentWithRefFixed.Project.Language);

            var documentWithAdditionalImports = await AddImportsInContainersAsync(
                documentWithRefFixed,
                addImportService,
                containers,
                ImmutableArray.Create(newNamespace),
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
        /// If <paramref name="newNamespaceParts"/> is specified (not default), the fix would be:
        ///     1. qualify the reference with new namespace and mark it for simplification, or
        ///     2. find and mark the qualified reference for simplification.
        /// Otherwise, there would be no namespace replacement.
        /// </summary>
        private async Task<(Document, ImmutableArray<SyntaxNode>)> FixReferencesAsync(
            Document document,
            IChangeNamespaceService changeNamespaceService,
            IAddImportsService addImportService,
            IEnumerable<ReferenceLocation> refLocations,
            ImmutableArray<string> newNamespaceParts,
            CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var root = editor.OriginalRoot;
            var containers = PooledHashSet<SyntaxNode>.GetInstance();

            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            // We need a dummy import to figure out the container for given reference.
            var dummyImport = CreateImport(generator, "Dummy", withFormatterAnnotation: false);
            var abstractChangeNamespaceService = (AbstractChangeNamespaceService)changeNamespaceService;

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
                if (abstractChangeNamespaceService.TryGetReplacementReferenceSyntax(
                        refNode, newNamespaceParts, syntaxFacts, out var oldNode, out var newNode))
                {
                    editor.ReplaceNode(oldNode, newNode.WithAdditionalAnnotations(Simplifier.Annotation));
                }

                // Use a dummy import node to figure out which container the new import will be added to.
                var container = addImportService.GetImportContainer(root, refNode, dummyImport);
                containers.Add(container);
            }

            foreach (var container in containers)
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
