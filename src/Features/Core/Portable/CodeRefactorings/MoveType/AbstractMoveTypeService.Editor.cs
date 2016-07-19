// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        /// <summary>
        /// We perform 3 different kinds of edits for Move Type Code Action
        /// 1. Rename file to match type
        /// 2. Rename type to match file
        /// 3. Move type to a new file
        /// </summary>
        private partial class Editor
        {
            private readonly CancellationToken _cancellationToken;
            private readonly State _state;
            private readonly TService _service;

            private readonly bool _renameFile;
            private readonly bool _renameType;

            public Editor(
                TService service,
                State state,
                bool renameFile,
                bool renameType,
                CancellationToken cancellationToken)
            {
                _renameFile = renameFile;
                _renameType = renameType;
                _state = state;
                _service = service;
                _cancellationToken = cancellationToken;
            }

            private SemanticDocument SemanticDocument => _state.SemanticDocument;

            /// <summary>
            /// operations performed by Move Type CodeAction.
            /// </summary>
            internal async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync()
            {
                var solution = SemanticDocument.Document.Project.Solution;

                if (_renameFile)
                {
                    return RenameFileToMatchTypeName(solution);
                }
                else if (_renameType)
                {
                    return await RenameTypeToMatchFileAsync(solution).ConfigureAwait(false);
                }

                return await MoveTypeToNewFileAsync(_state.TargetFileNameCandidate).ConfigureAwait(false);
            }

            /// <summary>
            /// Given a document and a type contained in it, moves the type
            /// out to its own document. The new document's name typically
            /// is the type name, or is atleast based on the type name.
            /// </summary>
            /// <param name="documentName">name for the new document being added</param>
            /// <remarks>
            /// The algorithm for this, is as follows:
            /// 1. Fork the original document that contains the type to be moved.
            /// 2. Keep the type and required namespace containers, using statements
            ///     and remove everything else from the forked document.
            /// 3. Add this forked document to the solution.
            /// 4. Finally, update the original document and remove the type from it.
            /// </remarks>
            private async Task<IEnumerable<CodeActionOperation>> MoveTypeToNewFileAsync(string documentName)
            {
                // Fork, update and add as new document.
                var projectToBeUpdated = SemanticDocument.Document.Project;
                var newDocumentId = DocumentId.CreateNewId(projectToBeUpdated.Id, documentName);

                var solutionWithNewDocument = await AddNewDocumentWithTypeDeclarationAsync(
                    SemanticDocument, documentName, newDocumentId, _state.TypeNode, _cancellationToken).ConfigureAwait(false);

                // Get the original source document again, from the latest forked solution.
                var sourceDocument = solutionWithNewDocument.GetDocument(SemanticDocument.Document.Id);

                // update source document to add partial modifiers to type chain
                // and/or remove type declaration from original source document.
                var solutionWithBothDocumentsUpdated = await UpdateSourceDocumentAsync(
                      sourceDocument, _state.TypeNode, _cancellationToken).ConfigureAwait(false);

                return new CodeActionOperation[] { new ApplyChangesOperation(solutionWithBothDocumentsUpdated) };
            }

            /// <summary>
            /// Forks the source document, keeps required type, namespace containers
            /// and adds it the solution.
            /// </summary>
            /// <param name="sourceDocument">original document</param>
            /// <param name="newDocumentName">name of the new document to be added</param>
            /// <param name="newDocumentId">id for the new document to be added</param>
            /// <param name="typeNode">type to move from original document to new document</param>
            /// <param name="cancellationToken">a cancellation token</param>
            /// <returns>the new solution which contains a new document with the type being moved</returns>
            private async Task<Solution> AddNewDocumentWithTypeDeclarationAsync(
                SemanticDocument sourceDocument,
                string newDocumentName,
                DocumentId newDocumentId,
                TTypeDeclarationSyntax typeNode,
                CancellationToken cancellationToken)
            {
                var root = sourceDocument.Root;
                var projectToBeUpdated = sourceDocument.Document.Project;
                var documentEditor = await DocumentEditor.CreateAsync(sourceDocument.Document, cancellationToken).ConfigureAwait(false);

                AddPartialModifiersToTypeChain(documentEditor, typeNode);

                // remove things that are not being moved, from the forked document.
                var membersToRemove = GetMembersToRemove(root, typeNode);
                foreach (var member in membersToRemove)
                {
                    documentEditor.RemoveNode(member, SyntaxRemoveOptions.KeepNoTrivia);
                }

                var modifiedRoot = documentEditor.GetChangedRoot();

                // add an empty document to solution, so that we'll have options from the right context.
                var solutionWithNewDocument = projectToBeUpdated.Solution.AddDocument(newDocumentId, newDocumentName, text: string.Empty);

                // update the text for the new document
                solutionWithNewDocument = solutionWithNewDocument.WithDocumentSyntaxRoot(newDocumentId, modifiedRoot, PreservationMode.PreserveIdentity);

                // get the updated document, perform clean up like remove unused usings.
                var newDocument = solutionWithNewDocument.GetDocument(newDocumentId);
                return await CleanUpDocumentAsync(newDocument, cancellationToken).ConfigureAwait(false);
            }

            /// <summary>
            /// update the original document and remove the type that was moved.
            /// perform other fix ups as necessary.
            /// </summary>
            /// <param name="sourceDocument">original document</param>
            /// <param name="typeNode">type that was moved to new document</param>
            /// <param name="cancellationToken">a cancellation token</param>
            /// <returns>an updated solution with the original document fixed up as appropriate.</returns>
            private async Task<Solution> UpdateSourceDocumentAsync(
                Document sourceDocument, TTypeDeclarationSyntax typeNode, CancellationToken cancellationToken)
            {
                var documentEditor = await DocumentEditor.CreateAsync(sourceDocument, cancellationToken).ConfigureAwait(false);

                AddPartialModifiersToTypeChain(documentEditor, typeNode);
                documentEditor.RemoveNode(typeNode, SyntaxRemoveOptions.KeepNoTrivia);

                var updatedDocument = documentEditor.GetChangedDocument();
                return await CleanUpDocumentAsync(updatedDocument, cancellationToken).ConfigureAwait(false);
            }

            /// <summary>
            /// Traverses the syntax tree of the forked document and
            /// collects a list of nodes that are not being moved.
            /// This list of nodes are then removed from the forked copy.
            /// </summary>
            /// <param name="root">root, of the syntax tree of forked document</param>
            /// <param name="typeNode">node being moved to new document</param>
            /// <returns>list of syntax nodes, to be removed from the forked copy.</returns>
            private static IEnumerable<SyntaxNode> GetMembersToRemove(
                SyntaxNode root, TTypeDeclarationSyntax typeNode)
            {
                // the type node being moved and its container declarations should
                // be kept.
                var ancestorsAndSelfToKeep = typeNode
                    .AncestorsAndSelf()
                    .Where(n => n is TNamespaceDeclarationSyntax || n is TTypeDeclarationSyntax);

                // while we need the ancestor container declarations,
                // we do not need other declarations inside of these containers,
                // that are not the type being moved.
                var ancestorsKept = ancestorsAndSelfToKeep
                    .OfType<TTypeDeclarationSyntax>()
                    .Except(new[] { typeNode });

                var membersOfAncestorsKept = ancestorsKept
                    .SelectMany(a => a.DescendantNodes()
                    .OfType<TMemberDeclarationSyntax>()
                    .Where(t => !t.Equals(typeNode) && t.Parent.Equals(a)));

                // top level nodes other that are not in the ancestor chain 
                // of the type being moved should be removed.
                var topLevelMembersToRemove = root
                    .DescendantNodesAndSelf(descendIntoChildren: _ => true, descendIntoTrivia: false)
                    .Where(n => IsTopLevelNamespaceOrTypeNode(n) && !ancestorsAndSelfToKeep.Contains(n));

                return topLevelMembersToRemove.Concat(membersOfAncestorsKept);
            }

            private static bool IsTopLevelNamespaceOrTypeNode(SyntaxNode node)
            {
                // check only for top level namespaces and types in the file.
                // top level types are parented by either a namespace or compilation unit
                // top level namespaces are parented by compilation unit.
                return node is TNamespaceDeclarationSyntax
                    ? node.Parent is TCompilationUnitSyntax
                    : node is TTypeDeclarationSyntax
                        ? node.Parent is TNamespaceDeclarationSyntax || node.Parent is TCompilationUnitSyntax
                        : false;
            }

            /// <summary>
            /// if a nested type is being moved, this ensures its containing type is partial.
            /// </summary>
            /// <param name="documentEditor">document editor for the new document being created</param>
            /// <param name="typeNode">type being moved</param>
            private void AddPartialModifiersToTypeChain(DocumentEditor documentEditor, TTypeDeclarationSyntax typeNode)
            {
                var semanticFacts = _state.SemanticDocument.Document.GetLanguageService<ISemanticFactsService>();

                if (typeNode.Parent is TTypeDeclarationSyntax)
                {
                    var typeChain = typeNode.Ancestors().OfType<TTypeDeclarationSyntax>();

                    foreach (var node in typeChain)
                    {
                        var symbol = (ITypeSymbol)_state.SemanticDocument.SemanticModel.GetDeclaredSymbol(node, _cancellationToken);
                        if (!semanticFacts.IsPartial(symbol))
                        {
                            documentEditor.SetModifiers(node, DeclarationModifiers.Partial);
                        }
                    }
                }
            }

            /// <summary>
            /// Perform clean ups on a given document.
            /// </summary>
            private async Task<Solution> CleanUpDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                document = await document
                    .GetLanguageService<IRemoveUnnecessaryImportsService>()
                    .RemoveUnnecessaryImportsAsync(document, cancellationToken)
                    .ConfigureAwait(false);

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, root, PreservationMode.PreserveIdentity);
            }
        }
    }
}
