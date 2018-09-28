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

            public override string Title => $"Change namespace to {_state.TargetNamespace} to match folder hierarchy.";

            public RenameNamespaceCodeAction(TService service, State state)
            {
                _service = service;
                _state = state;
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                => await ChangeNamespaceToMatchFoldersAsync(cancellationToken).ConfigureAwait(false);

            private string OldNamespaceName => _state.DeclaredNamespace;
            private string NewNamespaceName => _state.TargetNamespace;

            private ImmutableArray<string> _oldNamespaceParts;
            private ImmutableArray<string> OldNamespaceParts
            {
                get
                {
                    if (_oldNamespaceParts.IsDefault)
                    {
                        _oldNamespaceParts = OldNamespaceName.Split(new[] { '.' }).ToImmutableArray();
                    }
                    return _oldNamespaceParts;
                }
            }

            private ImmutableArray<string> _newNamespaceParts;
            private ImmutableArray<string> NewNamespaceParts
            {
                get
                {
                    Debug.Assert(NewNamespaceName != null);

                    if (_newNamespaceParts.IsDefault)
                    {
                        _newNamespaceParts = NewNamespaceName.Split(new[] { '.' }).ToImmutableArray();
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
                var namespaceDecl = declarationRoot.DescendantNodes().FirstOrDefault(node => node is TNamespaceDeclarationSyntax) as TNamespaceDeclarationSyntax;

                Debug.Assert(namespaceDecl != null);

                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var declaredSymbols = _service.GetDeclaredSymbols(semanticModel, namespaceDecl, cancellationToken);

                var documentSet = ImmutableHashSet.Create(document);
                var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

                var refLocationsInCurrentDocument = SpecializedCollections.EmptyEnumerable<ReferenceLocation>();
                var refLocationsInOtherDocument = SpecializedCollections.EmptyEnumerable<ReferenceLocation>();

                foreach (var declaredSymbol in declaredSymbols)
                {
                    var refSymbols = await SymbolFinder.FindReferencesAsync(
                      declaredSymbol,
                      document.Project.Solution,
                      cancellationToken).ConfigureAwait(false);

                    var refLocationsForSymbol = refSymbols.Where(refSymbol => refSymbol.Definition == declaredSymbol).SelectMany(refSymbol => refSymbol.Locations);

                    refLocationsInCurrentDocument = refLocationsInCurrentDocument.Concat(refLocationsForSymbol.Where(loc => loc.Document.Id == document.Id));
                    refLocationsInOtherDocument = refLocationsInOtherDocument.Concat(refLocationsForSymbol.Where(loc => loc.Document.Id != document.Id));
                }

                var refLocationGroups = refLocationsInOtherDocument.GroupBy(loc => loc.Document.Id);

                // transform current document first
                var newSolution = (await FixDeclarationDocumentAsync(document, refLocationsInCurrentDocument, cancellationToken).ConfigureAwait(false)).Project.Solution;
                var oldImport = _service.CreateUsingDirective(OldNamespaceParts);
                var newImport = _service.CreateUsingDirective(NewNamespaceParts);

                // then fix all referencing documents
                foreach (var refInOneDocument in refLocationGroups)
                {
                    var refDocument = newSolution.GetDocument(refInOneDocument.Key);
                    newSolution = (await FixReferencingDocumentAsync(refDocument, oldImport, newImport, refInOneDocument, cancellationToken).ConfigureAwait(false)).Project.Solution;
                }

                return ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(newSolution));
            }

            // TODO: refactor these methods

            private async Task<Document> FixDeclarationDocumentAsync(Document document, IEnumerable<ReferenceLocation> refLocations, CancellationToken cancellationToken)
            {
                Debug.Assert(!NewNamespaceParts.IsDefault);

                // 1. Add usings for containing namespaces, in case we have references relying on old namespace declaration for resolution. 
                // 2. Reduce all references.
                // 3. Rename namespace declaration.
                // 4. Remove unnecessary usings.              

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                root = root.WithAdditionalAnnotations(Simplifier.Annotation);

                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                document = await Simplifier.ReduceAsync(document.WithSyntaxRoot(root), optionSet, cancellationToken).ConfigureAwait(false);
                root = await document.GetSyntaxRootAsync().ConfigureAwait(false);

                //TODO: handle change from and to global namespace
                var declaredInGlobalNamespace = OldNamespaceName.Length == 0;
                Debug.Assert(!declaredInGlobalNamespace);

                // Create import for all levels of old namespace and add them to the document (if it's not global namespace) 
                var imports = new List<SyntaxNode>();
                for (var i = 1; i <= OldNamespaceParts.Length; ++i)
                {
                    imports.Add(_service.CreateUsingDirective(ImmutableArray.Create(OldNamespaceParts, 0, i)));
                }

                var addImportService = document.GetLanguageService<IAddImportsService>();
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);
                root = addImportService.AddImports(await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false), root, null, imports, placeSystemNamespaceFirst);
                document = document.WithSyntaxRoot(root);

                // change namespace declaration to new namespace
                var oldDecl = root.DescendantNodes().First(n => n is TNamespaceDeclarationSyntax) as TNamespaceDeclarationSyntax;
                var newDecl = _service.ChangeNamespace(oldDecl, NewNamespaceParts);
                root = root.ReplaceNode(oldDecl, newDecl);
                document = document.WithSyntaxRoot(root);

                return await RemoveImportsIfUnnecessaryAsync(document, imports, optionSet, cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> FixReferencingDocumentAsync(
                Document document,
                SyntaxNode oldImport,
                SyntaxNode newImport,
                IEnumerable<ReferenceLocation> refLocations,    // The solution has changed after we got those ReferenceLocation 
                CancellationToken cancellationToken)
            {
                // 1. Fully qualify all simple references (i.e. not via an alias) with new namespace.
                // 2. Add using of new namespace (for each reference's container).
                // 3. Reduce fully qualified names from step(1).
                // 4. Remove unnecessary usings related to old and new namespace.

                var addImportService = document.GetLanguageService<IAddImportsService>();
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

                    // Get inner most node in case of type used as a base type. e.g.
                    //
                    //      public class Foo {}
                    //      public class Bar : Foo {}
                    //
                    // For the reference to Foo where it is used the base class of Bar, the BaseTypeSyntax and the TypeSyntax
                    // have exact same span

                    var refNode = root.FindNode(refLoc.Location.SourceSpan, getInnermostNodeForTie: true);
                    if (_service.TryGetReplacementSyntax(refNode, NewNamespaceParts, out var oldNode, out var newNode))
                    {
                        newNode = newNode.WithAdditionalAnnotations(Simplifier.Annotation);
                        editor.ReplaceNode(oldNode, newNode);
                    }

                    var container = addImportService.GetImportContainer(root, refNode, newImport);
                    containers.Add(container);
                }

                document = editor.GetChangedDocument();
                root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var commonContainer = FindCommonContainer(containers.Select(c => root.GetCurrentNode(c)));

                var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
                var placeSystemNamespaceFirst = optionSet.GetOption(GenerationOptions.PlaceSystemNamespaceFirst, document.Project.Language);

                root = addImportService.AddImport(await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false), root, commonContainer, newImport, placeSystemNamespaceFirst);

                document = await Simplifier.ReduceAsync(document.WithSyntaxRoot(root), optionSet, cancellationToken).ConfigureAwait(false);
                root = await document.GetSyntaxRootAsync().ConfigureAwait(false);

                return await RemoveImportsIfUnnecessaryAsync(document, ImmutableArray.Create(oldImport, newImport), optionSet, cancellationToken).ConfigureAwait(false);
            }

            private async Task<Document> RemoveImportsIfUnnecessaryAsync(Document document, IEnumerable<SyntaxNode> importsToRemove, DocumentOptionSet optionSet, CancellationToken cancellationToken)
            {
                var removeImportService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
                return await removeImportService.RemoveUnnecessaryImportsAsync(
                    document,
                    import => importsToRemove.Any(importToRemove => importToRemove.IsEquivalentTo(import, topLevel: false)),
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            private static SyntaxNode FindCommonContainer(IEnumerable<SyntaxNode> containers)
            {
                // TODO
                return containers.First();
            }
        }
    }
}
