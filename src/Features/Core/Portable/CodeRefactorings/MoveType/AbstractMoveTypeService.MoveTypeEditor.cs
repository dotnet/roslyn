// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private class MoveTypeEditor : Editor
        {
            public MoveTypeEditor(
                TService service,
                State state,
                CancellationToken cancellationToken) : base(service, state, cancellationToken)
            {
            }

            internal override async Task<IEnumerable<CodeActionOperation>> GetOperationsAsync()
            {
                var solution = SemanticDocument.Document.Project.Solution;
                return await MoveTypeToNewFileAsync(State.TargetFileNameCandidate).ConfigureAwait(false);
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
            /// 2. Keep the type, required namespace containers and using statements.
            ///    remove everything else from the forked document.
            /// 3. Add this forked document to the solution.
            /// 4. Finally, update the original document and remove the type from it.
            /// </remarks>
            private async Task<IEnumerable<CodeActionOperation>> MoveTypeToNewFileAsync(string documentName)
            {
                // Fork, update and add as new document.
                var projectToBeUpdated = SemanticDocument.Document.Project;
                var newDocumentId = DocumentId.CreateNewId(projectToBeUpdated.Id, documentName);

                var solutionWithNewDocument = await AddNewDocumentWithSingleTypeDeclarationAndImportsAsync(
                    SemanticDocument, documentName, newDocumentId, State.TypeNode, CancellationToken).ConfigureAwait(false);

                // Get the original source document again, from the latest forked solution.
                var sourceDocument = solutionWithNewDocument.GetDocument(SemanticDocument.Document.Id);

                // update source document to add partial modifiers to type chain
                // and/or remove type declaration from original source document.
                var solutionWithBothDocumentsUpdated = await RemoveTypeFromSourceDocumentAsync(
                      sourceDocument, State.TypeNode, CancellationToken).ConfigureAwait(false);

                return SpecializedCollections.SingletonEnumerable(new ApplyChangesOperation(solutionWithBothDocumentsUpdated));
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
            private async Task<Solution> AddNewDocumentWithSingleTypeDeclarationAndImportsAsync(
                SemanticDocument sourceDocument,
                string newDocumentName,
                DocumentId newDocumentId,
                TTypeDeclarationSyntax typeNode,
                CancellationToken cancellationToken)
            {
                Debug.Assert(sourceDocument.Document.Name != newDocumentName,
                             $"New document name is same as old document name:{newDocumentName}");

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
                newDocument = await CleanUpDocumentAsync(newDocument, cancellationToken).ConfigureAwait(false);

                return newDocument.Project.Solution;
            }

            /// <summary>
            /// update the original document and remove the type that was moved.
            /// perform other fix ups as necessary.
            /// </summary>
            /// <param name="sourceDocument">original document</param>
            /// <param name="typeNode">type that was moved to new document</param>
            /// <param name="cancellationToken">a cancellation token</param>
            /// <returns>an updated solution with the original document fixed up as appropriate.</returns>
            private async Task<Solution> RemoveTypeFromSourceDocumentAsync(
                Document sourceDocument, TTypeDeclarationSyntax typeNode, CancellationToken cancellationToken)
            {
                var documentEditor = await DocumentEditor.CreateAsync(sourceDocument, cancellationToken).ConfigureAwait(false);

                AddPartialModifiersToTypeChain(documentEditor, typeNode);
                documentEditor.RemoveNode(typeNode, SyntaxRemoveOptions.KeepNoTrivia);

                var updatedDocument = documentEditor.GetChangedDocument();

                updatedDocument = await CleanUpDocumentAsync(updatedDocument, cancellationToken).ConfigureAwait(false);

                return updatedDocument.Project.Solution;
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
                HashSet<SyntaxNode> spine = new HashSet<SyntaxNode>();

                // collect the parent chain of declarations to keep.
                SyntaxNode node = typeNode;
                while (node.Parent != null && !(node.Parent is TCompilationUnitSyntax))
                {
                    spine.Add(node.Parent);
                    node = node.Parent;
                }

                // get potential namespace, types and members to remove.
                var removableCandidates = root
                    .DescendantNodes(n => DescendIntoChildren(n, spine.Contains(n)))
                    .Where(n => FilterToTopLevelMembers(n, typeNode));

                // diff candidates with items we want to keep.
                return removableCandidates.Except(spine);
            }

            private static bool DescendIntoChildren(SyntaxNode node, bool shouldDescendIntoType)
            {
                // 1. get top level types and namespaces to remove.
                // 2. descend into types and get members to remove, only if type is part of spine, which means
                //    we'll be keeping the type declaration but not other members, in the new file.
                return node is TCompilationUnitSyntax
                    || node is TNamespaceDeclarationSyntax
                    || (node is TTypeDeclarationSyntax && shouldDescendIntoType);
            }

            private static bool FilterToTopLevelMembers(SyntaxNode node, SyntaxNode typeNode)
            {
                // It is a type declaration that is not the node we've moving
                // or its a container namespace, or a member declaration that is not a type,
                // thereby ignoring other stuff like statements and identifiers.
                return node is TTypeDeclarationSyntax
                    ? !node.Equals(typeNode)
                    : (node is TNamespaceDeclarationSyntax || node is TMemberDeclarationSyntax);
            }

            /// <summary>
            /// if a nested type is being moved, this ensures its containing type is partial.
            /// </summary>
            /// <param name="documentEditor">document editor for the new document being created</param>
            /// <param name="typeNode">type being moved</param>
            private void AddPartialModifiersToTypeChain(DocumentEditor documentEditor, TTypeDeclarationSyntax typeNode)
            {
                var semanticFacts = State.SemanticDocument.Document.GetLanguageService<ISemanticFactsService>();
                var typeChain = typeNode.Ancestors().OfType<TTypeDeclarationSyntax>();

                foreach (var node in typeChain)
                {
                    var symbol = (ITypeSymbol)State.SemanticDocument.SemanticModel.GetDeclaredSymbol(node, CancellationToken);
                    if (!semanticFacts.IsPartial(symbol))
                    {
                        documentEditor.SetModifiers(node, DeclarationModifiers.Partial);
                    }
                }
            }

            /// <summary>
            /// Perform clean ups on a given document.
            /// </summary>
            private async Task<Document> CleanUpDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                document = await document
                    .GetLanguageService<IRemoveUnnecessaryImportsService>()
                    .RemoveUnnecessaryImportsAsync(document, cancellationToken)
                    .ConfigureAwait(false);

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                return document.WithSyntaxRoot(root);
            }
        }
    }
}
