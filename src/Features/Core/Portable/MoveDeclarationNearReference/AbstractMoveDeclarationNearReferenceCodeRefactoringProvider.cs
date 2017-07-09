// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.MoveDeclarationNearReference
{
    internal abstract partial class AbstractMoveDeclarationNearReferenceCodeRefactoringProvider<
        TService,
        TStatementSyntax,
        TLocalDeclarationStatementSyntax,
        TVariableDeclaratorSyntax> : CodeRefactoringProvider
        where TService : AbstractMoveDeclarationNearReferenceCodeRefactoringProvider<TService, TStatementSyntax, TLocalDeclarationStatementSyntax, TVariableDeclaratorSyntax>
        where TStatementSyntax : SyntaxNode
        where TLocalDeclarationStatementSyntax : TStatementSyntax
        where TVariableDeclaratorSyntax : SyntaxNode
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (!textSpan.IsEmpty)
            {
                return;
            }

            var position = textSpan.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var statement = root.FindToken(position).GetAncestor<TLocalDeclarationStatementSyntax>();
            if (statement == null)
            {
                return;
            }

            var state = await State.GenerateAsync((TService)this, document, statement, cancellationToken).ConfigureAwait(false);
            if (state == null)
            {
                return;
            }

            // Only offer the refactoring when somewhere in the type+name of the local variable.
            var identifier = this.GetIdentifierOfVariableDeclarator(state.VariableDeclarator);
            if (position < statement.SpanStart || position > identifier.Span.End)
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(c => MoveDeclarationNearReferenceAsync(document, state, root, c)));
        }

        private async Task<Document> MoveDeclarationNearReferenceAsync(
            Document document, State state, SyntaxNode root, CancellationToken cancellationToken)
        {
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            var crossesMeaningfulBlock = CrossesMeaningfulBlock(state);
            var warningAnnotation = crossesMeaningfulBlock
                ? WarningAnnotation.Create(FeaturesResources.Warning_colon_Declaration_changes_scope_and_may_change_meaning)
                : null;

            editor.RemoveNode(state.DeclarationStatement);

            var canMergeDeclarationAndAssignment = await CanMergeDeclarationAndAssignmentAsync(document, state, cancellationToken).ConfigureAwait(false);
            if (canMergeDeclarationAndAssignment)
            {
                // Replace the first reference with a new declaration.
                var declarationStatement = CreateMergedDeclarationStatement(state.DeclarationStatement, state.FirstStatementAffectedInInnermostBlock);
                declarationStatement = warningAnnotation == null
                    ? declarationStatement
                    : declarationStatement.WithAdditionalAnnotations(warningAnnotation);

                editor.ReplaceNode(
                    state.FirstStatementAffectedInInnermostBlock,
                    declarationStatement.WithAdditionalAnnotations(Formatter.Annotation));
            }
            else
            {
                // If we're not merging with an existing declaration, make the declaration semantically
                // explicit to improve the chances that it won't break code.
                var explicitDeclarationStatement = await Simplifier.ExpandAsync(
                    state.DeclarationStatement, document, cancellationToken: cancellationToken).ConfigureAwait(false);

                // place the declaration above the first statement that references it.
                var declarationStatement = warningAnnotation == null
                    ? explicitDeclarationStatement
                    : explicitDeclarationStatement.WithAdditionalAnnotations(warningAnnotation);

                editor.InsertBefore(
                    state.FirstStatementAffectedInInnermostBlock,
                    declarationStatement.WithAdditionalAnnotations(Formatter.Annotation));
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private bool CrossesMeaningfulBlock(State state)
        {
            var blocks = state.InnermostBlock.GetAncestorsOrThis<SyntaxNode>();
            foreach (var block in blocks)
            {
                if (block == state.OutermostBlock)
                {
                    break;
                }

                if (IsMeaningfulBlock(block))
                {
                    return true;
                }
            }

            return false;
        }

        protected abstract bool IsMeaningfulBlock(SyntaxNode node);
        protected abstract SyntaxNode GetVariableDeclaratorSymbolNode(TVariableDeclaratorSyntax variableDeclarator);
        protected abstract bool IsValidVariableDeclarator(TVariableDeclaratorSyntax variableDeclarator);
        protected abstract SyntaxToken GetIdentifierOfVariableDeclarator(TVariableDeclaratorSyntax variableDeclarator);

        protected abstract TLocalDeclarationStatementSyntax CreateMergedDeclarationStatement(
            TLocalDeclarationStatementSyntax localDeclaration, TStatementSyntax statementSyntax);
        protected abstract Task<bool> TypesAreCompatibleAsync(Document document, ILocalSymbol localSymbol, TLocalDeclarationStatementSyntax declarationStatement, SyntaxNode right, CancellationToken cancellationToken);

        private async Task<bool> CanMergeDeclarationAndAssignmentAsync(
            Document document,
            State state,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(state.VariableDeclarator);
            if (initializer == null ||
                syntaxFacts.IsLiteralExpression(syntaxFacts.GetValueOfEqualsValueClause(initializer)))
            {
                var firstStatement = state.FirstStatementAffectedInInnermostBlock;
                if (syntaxFacts.IsSimpleAssignmentStatement(firstStatement))
                {
                    syntaxFacts.GetPartsOfAssignmentStatement(firstStatement, out var left, out var right);
                    if (syntaxFacts.IsIdentifierName(left))
                    {
                        var localSymbol = state.LocalSymbol;
                        var name = syntaxFacts.GetIdentifierOfSimpleName(left).ValueText;
                        if (syntaxFacts.StringComparer.Equals(name, localSymbol.Name))
                        {
                            return await TypesAreCompatibleAsync(
                                document, localSymbol, state.DeclarationStatement, right, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }

            return false;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Move_declaration_near_reference, createChangedDocument)
            {
            }

            internal override CodeActionPriority Priority => CodeActionPriority.Low;
        }
    }
}
