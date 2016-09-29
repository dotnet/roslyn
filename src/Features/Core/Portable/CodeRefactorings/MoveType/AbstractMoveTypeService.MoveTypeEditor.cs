// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                string fileName,
                CancellationToken cancellationToken) : base(service, state, fileName, cancellationToken)
            {
            }

            /// <summary>
            /// Given a document and a type contained in it, moves the type
            /// out to its own document. The new document's name typically
            /// is the type name, or is atleast based on the type name.
            /// </summary>
            /// <remarks>
            /// The algorithm for this, is as follows:
            /// 1. Fork the original document that contains the type to be moved.
            /// 2. Keep the type, required namespace containers and using statements.
            ///    remove everything else from the forked document.
            /// 3. Add this forked document to the solution.
            /// 4. Finally, update the original document and remove the type from it.
            /// </remarks>
            internal override async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync()
            {
                var solution = SemanticDocument.Document.Project.Solution;

                // Fork, update and add as new document.
                var projectToBeUpdated = SemanticDocument.Document.Project;
                var newDocumentId = DocumentId.CreateNewId(projectToBeUpdated.Id, FileName);

                var solutionWithNewDocument = await AddNewDocumentWithSingleTypeDeclarationAndImportsAsync(newDocumentId).ConfigureAwait(false);

                // Get the original source document again, from the latest forked solution.
                var sourceDocument = solutionWithNewDocument.GetDocument(SemanticDocument.Document.Id);

                // update source document to add partial modifiers to type chain
                // and/or remove type declaration from original source document.
                var solutionWithBothDocumentsUpdated = await RemoveTypeFromSourceDocumentAsync(
                      sourceDocument).ConfigureAwait(false);

                return ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(solutionWithBothDocumentsUpdated));
            }

            /// <summary>
            /// Forks the source document, keeps required type, namespace containers
            /// and adds it the solution.
            /// </summary>
            /// <param name="newDocumentId">id for the new document to be added</param>
            /// <returns>the new solution which contains a new document with the type being moved</returns>
            private async Task<Solution> AddNewDocumentWithSingleTypeDeclarationAndImportsAsync(
                DocumentId newDocumentId)
            {
                var document = SemanticDocument.Document;
                Debug.Assert(document.Name != FileName,
                             $"New document name is same as old document name:{FileName}");

                var root = SemanticDocument.Root;
                var projectToBeUpdated = document.Project;
                var documentEditor = await DocumentEditor.CreateAsync(document, CancellationToken).ConfigureAwait(false);

                // Make the type chain above this new type partial.  Also, remove any 
                // attributes from the containing partial types.  We don't want to create
                // duplicate attributes on things.
                AddPartialModifiersToTypeChain(documentEditor, removeAttributes: true);

                // remove things that are not being moved, from the forked document.
                var membersToRemove = GetMembersToRemove(root);
                foreach (var member in membersToRemove)
                {
                    documentEditor.RemoveNode(member, SyntaxRemoveOptions.KeepNoTrivia);
                }

                var modifiedRoot = documentEditor.GetChangedRoot();

                // add an empty document to solution, so that we'll have options from the right context.
                var solutionWithNewDocument = projectToBeUpdated.Solution.AddDocument(
                    newDocumentId, FileName, text: string.Empty, folders: document.Folders);

                // update the text for the new document
                solutionWithNewDocument = solutionWithNewDocument.WithDocumentSyntaxRoot(newDocumentId, modifiedRoot, PreservationMode.PreserveIdentity);

                // get the updated document, perform clean up like remove unused usings.
                var newDocument = solutionWithNewDocument.GetDocument(newDocumentId);
                newDocument = await CleanUpDocumentAsync(newDocument).ConfigureAwait(false);

                return newDocument.Project.Solution;
            }

            /// <summary>
            /// update the original document and remove the type that was moved.
            /// perform other fix ups as necessary.
            /// </summary>
            /// <param name="sourceDocument">original document</param>
            /// <returns>an updated solution with the original document fixed up as appropriate.</returns>
            private async Task<Solution> RemoveTypeFromSourceDocumentAsync(Document sourceDocument)
            {
                var documentEditor = await DocumentEditor.CreateAsync(sourceDocument, CancellationToken).ConfigureAwait(false);

                // Make the type chain above the type we're moving 'partial'.  
                // However, keep all the attributes on these types as theses are the 
                // original attributes and we don't want to mess with them. 
                AddPartialModifiersToTypeChain(documentEditor, removeAttributes: false);
                documentEditor.RemoveNode(State.TypeNode, SyntaxRemoveOptions.KeepNoTrivia);

                var updatedDocument = documentEditor.GetChangedDocument();

                updatedDocument = await CleanUpDocumentAsync(updatedDocument).ConfigureAwait(false);

                return updatedDocument.Project.Solution;
            }

            /// <summary>
            /// Traverses the syntax tree of the forked document and
            /// collects a list of nodes that are not being moved.
            /// This list of nodes are then removed from the forked copy.
            /// </summary>
            /// <param name="root">root, of the syntax tree of forked document</param>
            /// <returns>list of syntax nodes, to be removed from the forked copy.</returns>
            private ISet<SyntaxNode> GetMembersToRemove(SyntaxNode root)
            {
                var spine = new HashSet<SyntaxNode>();

                // collect the parent chain of declarations to keep.
                spine.AddRange(State.TypeNode.GetAncestors());

                // get potential namespace, types and members to remove.
                var removableCandidates = root
                    .DescendantNodes(n => spine.Contains(n))
                    .Where(n => FilterToTopLevelMembers(n, State.TypeNode)).ToSet();

                // diff candidates with items we want to keep.
                removableCandidates.ExceptWith(spine);

#if DEBUG
                // None of the nodes we're removing should also have any of their parent
                // nodes removed.  If that happened we could get a crash by first trying to remove
                // the parent, then trying to remove the child.
                foreach (var node in removableCandidates)
                {
                    foreach (var ancestor in node.GetAncestors())
                    {
                        Debug.Assert(!removableCandidates.Contains(ancestor));
                    }
                }
#endif

                return removableCandidates;
            }

            private static bool FilterToTopLevelMembers(SyntaxNode node, SyntaxNode typeNode)
            {
                // We never filter out the actual node we're trying to keep around.
                if (node == typeNode)
                {
                    return false;
                }

                return node is TTypeDeclarationSyntax ||
                       node is TMemberDeclarationSyntax ||
                       node is TNamespaceDeclarationSyntax;
            }

            /// <summary>
            /// if a nested type is being moved, this ensures its containing type is partial.
            /// </summary>
            private void AddPartialModifiersToTypeChain(
                DocumentEditor documentEditor, bool removeAttributes)
            {
                var semanticFacts = State.SemanticDocument.Document.GetLanguageService<ISemanticFactsService>();
                var typeChain = State.TypeNode.Ancestors().OfType<TTypeDeclarationSyntax>();

                foreach (var node in typeChain)
                {
                    var symbol = (ITypeSymbol)State.SemanticDocument.SemanticModel.GetDeclaredSymbol(node, CancellationToken);
                    if (!semanticFacts.IsPartial(symbol, CancellationToken))
                    {
                        documentEditor.SetModifiers(node, DeclarationModifiers.Partial);
                    }

                    if (removeAttributes)
                    {
                        documentEditor.RemoveAllAttributes(node);
                    }
                }
            }

            /// <summary>
            /// Perform clean ups on a given document.
            /// </summary>
            private Task<Document> CleanUpDocumentAsync(Document document)
            {
                return document
                    .GetLanguageService<IRemoveUnnecessaryImportsService>()
                    .RemoveUnnecessaryImportsAsync(document, CancellationToken);
            }
        }
    }
}
