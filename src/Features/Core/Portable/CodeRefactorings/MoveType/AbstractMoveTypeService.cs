// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.MoveType
{
    internal abstract partial class AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax> :
        IMoveTypeService
        where TService : AbstractMoveTypeService<TService, TTypeDeclarationSyntax, TNamespaceDeclarationSyntax, TMemberDeclarationSyntax, TCompilationUnitSyntax>
        where TTypeDeclarationSyntax : SyntaxNode
        where TNamespaceDeclarationSyntax : SyntaxNode
        where TMemberDeclarationSyntax : SyntaxNode
    {
        protected abstract bool IsPartial(TTypeDeclarationSyntax typeDeclaration);

        protected virtual SyntaxNode GetNodetoAnalyze(SyntaxNode root, TextSpan span)
        {
            return root.FindNode(span);
        }

        private bool IsNestedType(TTypeDeclarationSyntax typeNode)
        {
            return typeNode.Parent is TTypeDeclarationSyntax;
        }

        private bool IsSingleTypeDeclarationInSourceDocument(SyntaxNode root)
        {
            return root.DescendantNodes().OfType<TTypeDeclarationSyntax>().Count() == 1;
        }

        private bool ProjectContainsTargetFile(Project project, string targetFileName, string typeName, string sourceDocumentName)
        {
            // currently we generate into the same project. 
            // so, if type name matches file name, target file name is already present.
            return TypeNameMatchesDocumentName(typeName, sourceDocumentName) ||
                project.ContainsDocument(DocumentId.CreateNewId(project.Id, targetFileName));
        }

        private bool TypeNameMatchesDocumentName(string fileName, string typeName)
        {
            return string.Equals(fileName, typeName, StringComparison.CurrentCultureIgnoreCase);
        }

        public async Task<CodeRefactoring> GetRefactoringAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var state = State.Generate((TService)this, semanticDocument, textSpan, cancellationToken);
            if (state == null)
            {
                return null;
            }

            var actions = CreateActions(semanticDocument, state, cancellationToken);
            if (actions.Count == 0)
            {
                return null;
            }

            return new CodeRefactoring(null, actions);
        }

        private List<CodeAction> CreateActions(SemanticDocument document, State state, CancellationToken cancellationToken)
        {
            var actions = new List<CodeAction>();
            var targetFileName = state.TargetFileNameCandidate + state.TargetFileExtension;
            var uiRequired = ProjectContainsTargetFile(state.SemanticDocument.Project, targetFileName, state.DocumentName, state.TypeName);
            var isPartial = IsPartial(state.TypeNode);
            var isNested = IsNestedType(state.TypeNode);
            var singleType = IsSingleTypeDeclarationInSourceDocument(state.SemanticDocument.Root);
            var typeSymbol = state.SemanticDocument.SemanticModel.GetDeclaredSymbol(state.TypeNode, cancellationToken) as INamedTypeSymbol;

            // BALAJIK: make this clear, should also check if TypeNameMatchesFileName?
            if (isNested)
            {
                // nested type, make outer type partial and move type into a new file inside a partial part.
                if (!uiRequired)
                {
                    actions.Add(GetSimpleCodeAction(
                        document, state, renameFile: false, renameType: false, makeTypePartial: false, makeOuterTypePartial: true));
                }

                actions.Add(GetCodeActionWithUI(
                    document, state, renameFile: false, renameType: false, makeTypePartial: false, makeOuterTypePartial: true));
            }
            else
            {
                if (singleType)
                {
                    //Todo: clean up this, showDialog means can't move or rename etc.. feels weird.
                    if (!uiRequired)
                    {
                        // rename file.
                        actions.Add(GetSimpleCodeAction(
                            document, state, renameFile: true, renameType: false, makeTypePartial: false, makeOuterTypePartial: false));

                        // rename type.
                        actions.Add(GetSimpleCodeAction(
                            document, state, renameFile: false, renameType: true, makeTypePartial: false, makeOuterTypePartial: false));

                        // make partial and create a partial decl in a new file
                        //actions.Add(GetSimpleCodeAction(
                        //    document, state, renameFile: false, renameType:false, moveToNewFile: false, makeTypePartial: true, makeOuterTypePartial: false));
                    }

                    if (!isPartial)
                    {
                        // create a partial part in a file name that user inputs.
                        actions.Add(GetCodeActionWithUI(
                            document, state, renameFile: false, renameType: false, makeTypePartial: true, makeOuterTypePartial: false));
                    }
                }
                else
                {
                    // straight forward case, not the only type in this file, move type to a new file.
                    if (!uiRequired)
                    {
                        // move to file name that is precomputed
                        actions.Add(GetSimpleCodeAction(
                            document, renameFile: false, renameType: false, makeTypePartial: false, makeOuterTypePartial: false, state: state));
                    }

                    // move to a file name that user inputs.
                    actions.Add(GetCodeActionWithUI(
                        document, renameFile: false, renameType: false, makeTypePartial: false, makeOuterTypePartial: false, state: state));

                    if (!isPartial)
                    {
                        // create a partial part in a file name that user inputs.
                        actions.Add(GetCodeActionWithUI(
                            document, renameFile: false, renameType: false, makeTypePartial: true, makeOuterTypePartial: false, state: state));
                    }
                }
            }

            return actions;
        }

        private CodeAction GetCodeActionWithUI(SemanticDocument document, State state, bool renameFile, bool renameType, bool makeTypePartial, bool makeOuterTypePartial)
        {
            return new MoveTypeCodeActionWithOption((TService)this, document, renameFile, renameType, makeTypePartial, makeOuterTypePartial, state);
        }

        private MoveTypeCodeAction GetSimpleCodeAction(SemanticDocument document, State state, bool renameFile, bool renameType, bool makeTypePartial, bool makeOuterTypePartial)
        {
            return new MoveTypeCodeAction((TService)this, document, renameFile, renameType, makeTypePartial, makeOuterTypePartial, state);
        }
    }
}
