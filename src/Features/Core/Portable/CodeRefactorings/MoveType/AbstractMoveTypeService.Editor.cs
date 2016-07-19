// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
    {
        private class Editor
        {
            private readonly CancellationToken _cancellationToken;
            private readonly MoveTypeOptionsResult _moveTypeOptions;
            private readonly State _state;
            private readonly TService _service;

            private readonly bool _fromDialog;
            private readonly bool _makeOuterTypesPartial;
            private readonly bool _makeTypePartial;
            private readonly bool _renameFile;
            private readonly bool _renameType;

            public Editor(
                TService service,
                State state,
                bool renameFile,
                bool renameType,
                bool makeTypePartial,
                bool makeOuterTypePartial,
                MoveTypeOptionsResult moveTypeOptions,
                bool fromDialog,
                CancellationToken cancellationToken)
            {
                _renameFile = renameFile;
                _makeTypePartial = makeTypePartial;
                _makeOuterTypesPartial = makeOuterTypePartial || makeTypePartial;
                _renameType = renameType;
                _state = state;
                _service = service;
                this._moveTypeOptions = moveTypeOptions;
                this._fromDialog = fromDialog;
                this._cancellationToken = cancellationToken;
            }

            private SemanticDocument SemanticDocument => _state.SemanticDocument;

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

                var documentName = _fromDialog
                    ? _moveTypeOptions.NewFileName
                    : _state.TargetFileNameCandidate + _state.TargetFileExtension;

                return await MoveTypeToNewFileAsync(documentName).ConfigureAwait(false);
            }

            private async Task<IEnumerable<CodeActionOperation>> MoveTypeToNewFileAsync(string documentName)
            {
                // fork source document, keep required type/namespace hierarchy and add it to a new document
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

            private async Task<IEnumerable<CodeActionOperation>> RenameTypeToMatchFileAsync(Solution solution)
            {
                var symbol = _state.SemanticDocument.SemanticModel.GetDeclaredSymbol(_state.TypeNode, _cancellationToken) as INamedTypeSymbol;
                var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, _state.DocumentName, SemanticDocument.Document.Options, _cancellationToken).ConfigureAwait(false);
                return new CodeActionOperation[] { new ApplyChangesOperation(newSolution) };
            }

            private IEnumerable<CodeActionOperation> RenameFileToMatchTypeName(Solution solution)
            {
                var text = SemanticDocument.Text;
                var oldDocumentId = SemanticDocument.Document.Id;
                var newDocumentId = DocumentId.CreateNewId(SemanticDocument.Document.Project.Id, _state.TargetFileNameCandidate);

                var newSolution = solution.RemoveDocument(oldDocumentId);
                newSolution = newSolution.AddDocument(newDocumentId, _state.TargetFileNameCandidate, text);

                return new CodeActionOperation[]
                {
                    new ApplyChangesOperation(newSolution),
                    new OpenDocumentOperation(newDocumentId)
                };
            }

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

                var membersToRemove = GetMembersToRemove(root, typeNode, removeDescendentsOfSelf: _makeTypePartial);
                foreach (var member in membersToRemove)
                {
                    documentEditor.RemoveNode(member, SyntaxRemoveOptions.KeepNoTrivia);
                }

                var memberToAdd = GetMemberToAdd(documentEditor.Generator, newDocumentName);
                if (memberToAdd != null)
                {
                    documentEditor.AddMember(typeNode, memberToAdd);
                }

                var modifiedRoot = documentEditor.GetChangedRoot();

                // add an empty document to solution, so that we'll have options from the right context.
                var solutionWithNewDocument = projectToBeUpdated.Solution.AddDocument(newDocumentId, newDocumentName, string.Empty/*, folders, fullFilePath*/);

                // update the text for the new document
                solutionWithNewDocument = solutionWithNewDocument.WithDocumentSyntaxRoot(newDocumentId, modifiedRoot, PreservationMode.PreserveIdentity);

                // get the updated document, perform clean up like remove unused usings.
                var newDocument = solutionWithNewDocument.GetDocument(newDocumentId);
                return await CleanUpDocumentAsync(newDocument, cancellationToken).ConfigureAwait(false);
            }

            private async Task<Solution> UpdateSourceDocumentAsync(
                Document sourceDocument, TTypeDeclarationSyntax typeNode, CancellationToken cancellationToken)
            {
                var documentEditor = await DocumentEditor.CreateAsync(sourceDocument, cancellationToken).ConfigureAwait(false);

                AddPartialModifiersToTypeChain(documentEditor, typeNode);

                if (!_makeTypePartial)
                {
                    documentEditor.RemoveNode(typeNode, SyntaxRemoveOptions.KeepNoTrivia);
                }

                var updatedDocument = documentEditor.GetChangedDocument();
                // TODO: make this optional -- shouldn't touch other parts of source document unless asked to. 
                return await CleanUpDocumentAsync(updatedDocument, cancellationToken).ConfigureAwait(false);
            }

            private SyntaxNode GetMemberToAdd(SyntaxGenerator generator, string newDocumentName)
            {
                if (!_makeOuterTypesPartial)
                {
                    return null;
                }

                // we recognize filenames of that follow the pattern "OuterClass.InnerClass.cs" 
                // and generate code for this file, which will look like so:
                // a partial definition of OuterClass and a nested private InnerClass.
                var nameParts = Path.GetFileNameWithoutExtension(newDocumentName).Split(new char[] { '.' });
                if (nameParts == null || nameParts.Count() != 2)
                {
                    return null;
                }

                var innerTypeName = nameParts[1];
                return generator.ClassDeclaration(innerTypeName, accessibility: Accessibility.Private);
            }

            // TODO: look to optimize the number of Ancestors and Descendants calls here.
            private static IEnumerable<SyntaxNode> GetMembersToRemove(
                SyntaxNode root, TTypeDeclarationSyntax typeNode, bool removeDescendentsOfSelf)
            {
                var ancestorsAndSelfToKeep = typeNode
                    .AncestorsAndSelf()
                    .Where(n => n is TNamespaceDeclarationSyntax || n is TTypeDeclarationSyntax);

                var topLevelMembersToRemove = root
                    .DescendantNodesAndSelf(descendIntoChildren: _ => true, descendIntoTrivia: false)
                    .Where(n => IsTopLevelNamespaceOrTypeNode(n)
                                ? !ancestorsAndSelfToKeep.Contains(n)
                                : false);

                var ancestorsKept = ancestorsAndSelfToKeep
                    .OfType<TTypeDeclarationSyntax>()
                    .Except(new[] { typeNode });

                var membersOfAncestorsKept = ancestorsKept
                    .SelectMany(a => a.DescendantNodes().OfType<TMemberDeclarationSyntax>().Where(t => !t.Equals(typeNode) && t.Parent.Equals(a)));

                var membersToRemove = topLevelMembersToRemove.Concat(membersOfAncestorsKept);

                if (removeDescendentsOfSelf)
                {
                    var descendentsOfSelf = typeNode
                        .DescendantNodes(descendIntoChildren: _ => true, descendIntoTrivia: false)
                        .OfType<TMemberDeclarationSyntax>();

                    membersToRemove = membersToRemove.Concat(descendentsOfSelf);
                }

                return membersToRemove;
            }

            private static bool IsTopLevelNamespaceOrTypeNode(SyntaxNode node)
            {
                return ((node is TNamespaceDeclarationSyntax && node.Parent is TCompilationUnitSyntax)
                     || (node is TTypeDeclarationSyntax && (node.Parent is TNamespaceDeclarationSyntax
                                                            || node.Parent is TCompilationUnitSyntax)));
            }

            private void AddPartialModifiersToTypeChain(DocumentEditor documentEditor, TTypeDeclarationSyntax typeNode)
            {
                if (_makeOuterTypesPartial)
                {
                    var typeChain = typeNode.Ancestors().OfType<TTypeDeclarationSyntax>();

                    if (_makeTypePartial)
                    {
                        typeChain = typeChain.Concat(typeNode);
                    }

                    foreach (var node in typeChain)
                    {
                        if (!_service.IsPartial(node))
                        {
                            documentEditor.SetModifiers(node, DeclarationModifiers.Partial);
                        }
                    }
                }
            }

            private async Task<Solution> CleanUpDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                document = await document
                    .GetLanguageService<IRemoveUnnecessaryImportsService>()
                    .RemoveUnnecessaryImportsAsync(document, cancellationToken)
                    .ConfigureAwait(false);

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, root, PreservationMode.PreserveIdentity);

                //TODO: if documentWithTypeRemoved is empty (without any types left, should we remove the document from solution?
            }
        }
    }
}
