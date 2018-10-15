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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode 
    {
        /// <summary>
        /// This code action tries to change the name of the namespace declaration to 
        /// match the folder hierarchy of the document. The new namespace is constructed 
        /// by concatenate the default namespace of the project and all the folders in 
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
            private readonly AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax> _service;

            public override string Title => _state.TargetNamespace.Length == 0 
                ? FeaturesResources.Change_to_global_namespace
                : string.Format(FeaturesResources.Change_namespace_to_0, _state.TargetNamespace);

            public ChangeNamespaceCodeAction(
                AbstractSyncNamespaceCodeRefactoringProvider<TNamespaceDeclarationSyntax, TCompilationUnitSyntax> service,
                State state)
            {
                _service = service;
                _state = state;
            }

            private ImmutableArray<string> _oldNamespaceParts;
            private ImmutableArray<string> OldNamespaceParts
            {
                get
                {
                    if (_oldNamespaceParts.IsDefault)
                    {
                        _oldNamespaceParts = _state.DeclaredNamespace.Split(new[] { '.' }).ToImmutableArray();
                    }
                    return _oldNamespaceParts;
                }
            }

            private ImmutableArray<string> _newNamespaceParts;
            private ImmutableArray<string> NewNamespaceParts
            {
                get
                {
                    Debug.Assert(_state.TargetNamespace != null);

                    if (_newNamespaceParts.IsDefault)
                    {
                        _newNamespaceParts = _state.TargetNamespace.Split(new[] { '.' }).ToImmutableArray();
                    }
                    Debug.Assert(_newNamespaceParts.Length > 0);
                    return _newNamespaceParts;
                }
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

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                cancellationToken = default;
                var solution = _state.Solution;
                var referenceDocuments = new HashSet<DocumentId>();

                foreach (var id in _state.DocumentIds)
                {
                    var result = await ChangeNamespaceToMatchFoldersAsync(solution, id, cancellationToken).ConfigureAwait(false);
                    solution = result.Item1;
                    referenceDocuments.AddRange(result.Item2);
                }

                // After changing documents, we still need to remove unnecessary imports related to our change.
                // We don't try to remove all imports that might become unnecessary/invalid after the namespace change, 
                // just ones that fully matche the old or new namespace.
                // For example, if we are changing namespace `Foo.Bar` to `A.B`, the using of name `Bar` below would remain 
                // untouched:
                //
                //      namespace Foo
                //      {
                //          using Bar;
                //          ~~~~~~~~~
                //      }
                //

                solution = await RemoveUnnecessaryImportsAsync(solution, _state.DocumentIds, CreateAllContainingNamespaces(OldNamespaceParts), cancellationToken)
                    .ConfigureAwait(false);

                solution = await RemoveUnnecessaryImportsAsync(
                    solution, referenceDocuments.ToImmutableArray(), ImmutableArray.Create(_state.DeclaredNamespace, _state.TargetNamespace), cancellationToken)
                    .ConfigureAwait(false);

                return ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(solution));
            }

            private ImmutableArray<string> CreateAllContainingNamespaces(ImmutableArray<string> parts)
            {
                var builder = ArrayBuilder<string>.GetInstance();
                for (var i = 1; i <= OldNamespaceParts.Length; ++i)
                {
                    builder.Add(string.Join(".", OldNamespaceParts.Take(i)));
                }
                return builder.ToImmutableAndFree();
            }

            private ImmutableArray<SyntaxNode> CreateImports(Document document, ImmutableArray<string> names)
            {
                var generator = SyntaxGenerator.GetGenerator(document);
                var builder = ArrayBuilder<SyntaxNode>.GetInstance(names.Length);
                for (var i = 0; i < names.Length; ++i)
                {
                    builder.Add(generator.NamespaceImportDeclaration(names[i]));
                }
                return builder.ToImmutableAndFree();
            }

            private async Task<(Solution, ImmutableArray<DocumentId>)> ChangeNamespaceToMatchFoldersAsync(Solution solution, DocumentId id, CancellationToken cancellationToken)
            {
                //todo
                var document = solution.GetDocument(id);
                var targetNamespace = _state.TargetNamespace;

                var declarationRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var container = declarationRoot.DescendantNodes().FirstOrDefault(node => node is TNamespaceDeclarationSyntax) ?? declarationRoot;

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var declaredSymbols = GetDeclaredSymbolsInContainer(semanticModel, container, cancellationToken);

                var documentSet = ImmutableHashSet.Create(document);
                var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var refLocationsInCurrentDocument = new List<ReferenceLocation>();
                var refLocationsInOtherDocuments = new List<ReferenceLocation>();

                // TODO: 
                // Need to figure out how to handle MTFM projects and linked files properly.
                // 
                // For the document triggered the refactoring, we try to change the namespace 
                // declaration *only in current context*, the "find reference" is invoked only 
                // on symbols from current context as well.
                //
                // And for documents referencing those symbols, they can be linked/MTFM documents themselves.
                // In which case, we treat them as independent documents and fix them individually, and at the 
                // end rely on existing conflict resolving mechanism to merge those fixes in the same file together.

                foreach (var declaredSymbol in declaredSymbols)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var refSymbols = await SymbolFinder.FindReferencesAsync(
                      declaredSymbol,
                      solution,
                      cancellationToken).ConfigureAwait(false);

                    var refLocationsForSymbol = refSymbols.Where(refSymbol => refSymbol.Definition == declaredSymbol)
                        .SelectMany(refSymbol => refSymbol.Locations);

                    foreach (var refLocation in refLocationsForSymbol)
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
                }

                // Transform current document(s) first
                document = await FixDeclarationDocumentAsync(document, refLocationsInCurrentDocument, cancellationToken)
                    .ConfigureAwait(false);
                solution = document.Project.Solution;

                var refLocationGroups = refLocationsInOtherDocuments.GroupBy(loc => loc.Document.Id);
                foreach (var refInOneDocument in refLocationGroups)
                {
                    var refDocument = solution.GetDocument(refInOneDocument.Key);
                    document = await FixReferencingDocumentAsync(refDocument, refInOneDocument, cancellationToken)
                        .ConfigureAwait(false);
                    solution = document.Project.Solution;
                }

                return (solution, refLocationGroups.SelectAsArray(g => g.Key));
            }            

            private async Task<Document> FixDeclarationDocumentAsync(
                Document document, 
                IReadOnlyList<ReferenceLocation> refLocations, 
                CancellationToken cancellationToken)
            {
                Debug.Assert(!NewNamespaceParts.IsDefault);

                // 1. Add usings for containing namespaces, in case we have references 
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
                // 2. Simplify the document, so any qualified reference to types 
                //    defined in containing namespaces will be simplied. 
                // 3. Change the name of namespace declaration.
                // 4. Remove unnecessary usings.              

                var addImportService = document.GetLanguageService<IAddImportsService>();
                ImmutableHashSet<SyntaxNode> containers;
                SyntaxNode root;

                if (refLocations.Count > 0)
                {
                    (document, containers) = await FixReferencesAsync(document, addImportService, refLocations, NewNamespaceParts, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    // If there's no reference to types declared in this document,
                    // we will use root node as import container.
                    // TODO: find the inner most container
                    root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    containers = ImmutableHashSet.Create(root);
                }
                Debug.Assert(containers.Count > 0);

                // Need to import all containing namespaces of old namespace and add them to the document (if it's not global namespace)
                var namesToImport = CreateAllContainingNamespaces(OldNamespaceParts);

                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
                document = await AddImportsInContainersAsync(
                    document, 
                    addImportService, 
                    containers, 
                    namesToImport, 
                    placeSystemNamespaceFirst, 
                    cancellationToken).ConfigureAwait(false);

                root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                root = _service.ChangeNamespaceDeclaration(root, OldNamespaceParts, NewNamespaceParts);

                root = root.WithAdditionalAnnotations(Simplifier.Annotation);
                document = document.WithSyntaxRoot(root);
                return await Simplifier.ReduceAsync(document, optionSet, cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> FixReferencingDocumentAsync(
                Document document,
                IEnumerable<ReferenceLocation> refLocations,    
                CancellationToken cancellationToken)
            {
                // 1. Fully qualify all simple references (i.e. not via an alias) with new namespace.
                // 2. Add using of new namespace (for each reference's container).
                // 3. Try to simplify qualified names introduced from step(1).
                // 4. Remove unnecessary usings related to old and new namespace.

                var addImportService = document.GetLanguageService<IAddImportsService>();

                ImmutableHashSet<SyntaxNode> containers;
                (document, containers) = await FixReferencesAsync(document, addImportService, refLocations, NewNamespaceParts, cancellationToken);

                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);

                document = await AddImportsInContainersAsync(
                    document, 
                    addImportService,
                    containers,
                    ImmutableArray.Create(_state.TargetNamespace), 
                    placeSystemNamespaceFirst, 
                    cancellationToken).ConfigureAwait(false);

                return await Simplifier.ReduceAsync(document, optionSet, cancellationToken).ConfigureAwait(false);
            }

            /// <summary>
            /// Fix each reference and return a collection of proper containers 
            /// that new import should be added to based on reference locations.
            /// Depends on whether <paramref name="namespaceParts"/> is specified,
            /// the fix would be:
            ///     1. qualify the reference with new namespace and mark it for simplification, or
            ///     2. find and mark the qualified reference for simplification.
            /// </summary>
            private async Task<(Document, ImmutableHashSet<SyntaxNode>)> FixReferencesAsync(
                Document document, 
                IAddImportsService addImportService, 
                IEnumerable<ReferenceLocation> refLocations, 
                ImmutableArray<string> namespaceParts,          // No namespace replacement if this is default.  
                CancellationToken cancellationToken)
            {
                var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                var root = editor.OriginalRoot;
                var containers = new HashSet<SyntaxNode>();
                var dummyImport = _service.CreateUsingDirective(document, "Dummy");

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

                    var refNode = root.FindNode(refLoc.Location.SourceSpan, getInnermostNodeForTie: true);
                    if (_service.TryGetReplacementReferenceSyntax(refNode, namespaceParts, out var oldNode, out var newNode))
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

                document = editor.GetChangedDocument();
                root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                return (document, containers.Select(c => root.GetCurrentNode(c)).ToImmutableHashSet());
            }

            private async Task<Solution> RemoveUnnecessaryImportsAsync(
                Solution solution, 
                ImmutableArray<DocumentId> ids, 
                ImmutableArray<string> names, 
                CancellationToken cancellationToken)
            {
                foreach (var id in ids)
                {
                    var document = solution.GetDocument(id);
                    document = await RemoveUnnecessaryImportsAsync(document, CreateImports(document, names), cancellationToken)
                        .ConfigureAwait(false);
                    solution = document.Project.Solution;
                }
                return solution;
            }

            private async Task<Document> RemoveUnnecessaryImportsAsync(
                Document document, 
                IEnumerable<SyntaxNode> importsToRemove,
                CancellationToken cancellationToken)
            {
                var removeImportService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
                return await removeImportService.RemoveUnnecessaryImportsFromCurrentContextAsync(
                    document,
                    import => importsToRemove.Any(importToRemove => importToRemove.IsEquivalentTo(import, topLevel: false)),
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            private async Task<Document> AddImportsInContainersAsync(
                Document document,
                IAddImportsService addImportService,
                ImmutableHashSet<SyntaxNode> containers, 
                ImmutableArray<string> names,
                bool placeSystemNamespaceFirst, 
                CancellationToken cancellationToken)
            {
                var imports = CreateImports(document, names);
                foreach (var container in containers)
                {
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
        }
    }
}
