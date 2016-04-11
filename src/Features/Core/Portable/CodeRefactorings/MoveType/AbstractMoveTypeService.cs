// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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

        public async Task<CodeRefactoring> GetRefactoringAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var semanticDocument = await SemanticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var state = State.Generate(semanticDocument, textSpan, cancellationToken);
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
            var uiRequired = state.TargetFileNameAlreadyExists;
            var isPartial = IsPartial(state.TypeNode);

            // BALAJIK: make this clear, should also check if TypeNameMatchesFileName?
            if (state.IsNestedType)
            {
                // nested type, make outer type partial and move type into a new file inside a partial part.
                if (!uiRequired)
                {
                    actions.Add(GetSimpleCodeAction(
                        document, state, renameFile: false, renameType:false, makeTypePartial: false, makeOuterTypePartial: true));
                }

                actions.Add(GetCodeActionWithUI(
                    document, state, renameFile: false, renameType:false, makeTypePartial: false, makeOuterTypePartial: true));
            }
            else
            {
                if (state.OnlyTypeInFile)
                {
                    //Todo: clean up this, showDialog means can't move or rename etc.. feels weird.
                    if (!uiRequired)
                    {
                        // rename file.
                        actions.Add(GetSimpleCodeAction(
                            document, state, renameFile: true, renameType:false, makeTypePartial: false, makeOuterTypePartial: false));

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
