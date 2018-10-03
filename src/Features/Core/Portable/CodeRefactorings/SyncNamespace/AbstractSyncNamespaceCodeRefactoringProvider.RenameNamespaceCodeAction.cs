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
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace
{
    internal abstract partial class AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TService : AbstractSyncNamespaceCodeRefactoringProvider<TService, TNamespaceDeclarationSyntax, TCompilationUnitSyntax>
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TCompilationUnitSyntax : SyntaxNode 
    {
        private class RenameNamespaceCodeAction : CodeAction
        {
            private readonly State _state;
            private readonly TService _service;

            public override string Title => $"Change namespace to \"{_state.TargetNamespace}\" to match folder hierarchy.";

            public RenameNamespaceCodeAction(TService service, State state)
            {
                _service = service;
                _state = state;
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                => await ChangeNamespaceToMatchFoldersAsync(cancellationToken).ConfigureAwait(false);            

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

            private async Task<ImmutableArray<CodeActionOperation>> ChangeNamespaceToMatchFoldersAsync(CancellationToken cancellationToken)
            {
                var document = _state.Document;
                var targetNamespace = _state.TargetNamespace;

                var declarationRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var container = declarationRoot.DescendantNodes().FirstOrDefault(node => node is TNamespaceDeclarationSyntax) ?? declarationRoot;

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var declaredSymbols = _service.GetDeclaredSymbolsInContainer(semanticModel, container, cancellationToken);

                var documentSet = ImmutableHashSet.Create(document);
                var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var refLocationsInCurrentDocument = SpecializedCollections.EmptyEnumerable<ReferenceLocation>();
                var refLocationsInOtherFilePath = SpecializedCollections.EmptyEnumerable<ReferenceLocation>();

                var solution = document.Project.Solution;

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
                    var refSymbols = await SymbolFinder.FindReferencesAsync(
                      declaredSymbol,
                      solution,
                      cancellationToken).ConfigureAwait(false);

                    var refLocationsForSymbol = refSymbols.Where(refSymbol => refSymbol.Definition == declaredSymbol).SelectMany(refSymbol => refSymbol.Locations);

                    // Ignore other documents with identical file path as triggering document (i.e. other TFM in a MTFM project)
                    refLocationsInCurrentDocument = refLocationsInCurrentDocument.Concat(refLocationsForSymbol.Where(loc => loc.Document.Id == document.Id));
                    refLocationsInOtherFilePath = refLocationsInOtherFilePath.Concat(refLocationsForSymbol.Where(loc => !PathUtilities.PathsEqual(loc.Document.FilePath, document.FilePath)));
                }   

                // Transform current document(s) first
                solution = (await FixDeclarationDocumentAsync(document, refLocationsInCurrentDocument, cancellationToken).ConfigureAwait(false)).Project.Solution;

                // Then fix all referencing documents.
                var oldImport = _service.CreateUsingDirective(OldNamespaceParts);
                var newImport = _service.CreateUsingDirective(NewNamespaceParts);

                var refLocationGroups = refLocationsInOtherFilePath.GroupBy(loc => loc.Document.Id);
                foreach (var refInOneDocument in refLocationGroups)
                {
                    var refDocument = solution.GetDocument(refInOneDocument.Key);
                    solution = (await FixReferencingDocumentAsync(refDocument, oldImport, newImport, refInOneDocument, cancellationToken).ConfigureAwait(false)).Project.Solution;
                }

                return ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(solution));
            }            

            private async Task<Document> FixDeclarationDocumentAsync(Document document, IEnumerable<ReferenceLocation> refLocations, CancellationToken cancellationToken)
            {
                Debug.Assert(!NewNamespaceParts.IsDefault);

                // 1. Reduce all references.
                // 2. Add usings for containing namespaces, in case we have references relying on old namespace declaration for resolution. 
                // 3. Change namespace declaration.
                // 4. Remove unnecessary usings.              

                var addImportService = document.GetLanguageService<IAddImportsService>();

                ImmutableHashSet<SyntaxNode> containers;
                (document, containers) = await FixReferences(document, addImportService, refLocations, namespaceParts: default, cancellationToken);
                var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);

                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                document = await Simplifier.ReduceAsync(document, optionSet, cancellationToken).ConfigureAwait(false);                

                // Create import for all levels of old namespace and add them to the document (if it's not global namespace) 
                var imports = new List<SyntaxNode>();
                for (var i = 1; i <= OldNamespaceParts.Length; ++i)
                {
                    imports.Add(_service.CreateUsingDirective(ImmutableArray.Create(OldNamespaceParts, 0, i)));
                }
                
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
                //TODO: might have to add imports to namespace declaration instead of root.
                root = addImportService.AddImports(await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false), root, null, imports, placeSystemNamespaceFirst);

                // Change namespace declaration name to new namespace.
                document = document.WithSyntaxRoot(_service.ChangeNamespaceDeclaration(root, OldNamespaceParts, NewNamespaceParts));
                return await RemoveUnnecessaryImportsAsync(document, imports, optionSet, cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> FixReferencingDocumentAsync(
                Document document,
                SyntaxNode oldImport,
                SyntaxNode newImport,
                IEnumerable<ReferenceLocation> refLocations,    
                CancellationToken cancellationToken)
            {
                // 1. Fully qualify all simple references (i.e. not via an alias) with new namespace.
                // 2. Add using of new namespace (for each reference's container).
                // 3. Reduce fully qualified names from step(1).
                // 4. Remove unnecessary usings related to old and new namespace.

                var addImportService = document.GetLanguageService<IAddImportsService>();

                ImmutableHashSet<SyntaxNode> containers;
                (document, containers) = await FixReferences(document, addImportService, refLocations, NewNamespaceParts, cancellationToken);

                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);

                document = await AddImportInContainersAsync(
                    document, 
                    addImportService,
                    containers,
                    newImport, 
                    placeSystemNamespaceFirst, 
                    cancellationToken);

                document = await Simplifier.ReduceAsync(document, optionSet, cancellationToken).ConfigureAwait(false);
                return await RemoveUnnecessaryImportsAsync(document, ImmutableArray.Create(oldImport, newImport), optionSet, cancellationToken).ConfigureAwait(false);
            }


            /// <summary>
            /// Fix each reference and return a collection of proper containers 
            /// that new import should be added to based on refrence locations.
            /// Depends on whether <paramref name="namespaceParts"/> is specified,
            /// the fix would be:
            ///     1. qualify the reference with new namespace and mark it for simplification, or
            ///     2. find and mark the qualified reference for simplification.
            /// </summary>
            private async Task<(Document, ImmutableHashSet<SyntaxNode>)> FixReferences(
                Document document, 
                IAddImportsService addImportService, 
                IEnumerable<ReferenceLocation> refLocations, 
                ImmutableArray<string> namespaceParts,          // No namespace replacement if this is default.  
                CancellationToken cancellationToken)
            {
                var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                var root = editor.OriginalRoot;
                var containers = new HashSet<SyntaxNode>();

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
                    if (_service.TryGetReplacementSyntax(refNode, namespaceParts, out var oldNode, out var newNode))
                    {
                        newNode = newNode.WithAdditionalAnnotations(Simplifier.Annotation);
                        editor.ReplaceNode(oldNode, newNode);
                    }

                    var container = addImportService.GetImportContainer(root, refNode, _service.CreateUsingDirective(ImmutableArray.Create("Dummy")));
                    containers.Add(container);
                }
                return (editor.GetChangedDocument(), containers.Select(c => root.GetCurrentNode(c)).ToImmutableHashSet());
            }            

            private async Task<Document> RemoveUnnecessaryImportsAsync(
                Document document, 
                IEnumerable<SyntaxNode> importsToRemove, 
                DocumentOptionSet optionSet, 
                CancellationToken cancellationToken)
            {
                var removeImportService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
                return await removeImportService.RemoveUnnecessaryImportsFromCurrentContextAsync(
                    document,
                    import => importsToRemove.Any(importToRemove => importToRemove.IsEquivalentTo(import, topLevel: false)),
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            private async Task<Document> AddImportInContainersAsync(
                Document document,
                IAddImportsService addImportService, 
                IEnumerable<SyntaxNode> containers, 
                SyntaxNode import,
                bool placeSystemNamespaceFirst, 
                CancellationToken cancellationToken)
            {
                foreach (var container in containers)
                {
                    var contextLocation = container is TNamespaceDeclarationSyntax 
                        ? container.DescendantNodes().First() 
                        : container;

                    var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    if (!addImportService.HasExistingImport(compilation, root, contextLocation, import))
                    {
                        root = addImportService.AddImport(compilation, root, contextLocation, import, placeSystemNamespaceFirst);
                        document = document.WithSyntaxRoot(root);
                    }
                }
                return document;
            }
        }
    }
}
