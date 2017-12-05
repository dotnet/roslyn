// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

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
        protected abstract bool IsMeaningfulBlock(SyntaxNode node);
        protected abstract bool CanMoveToBlock(ILocalSymbol localSymbol, SyntaxNode currentBlock, SyntaxNode destinationBlock);
        protected abstract SyntaxNode GetVariableDeclaratorSymbolNode(TVariableDeclaratorSyntax variableDeclarator);
        protected abstract bool IsValidVariableDeclarator(TVariableDeclaratorSyntax variableDeclarator);
        protected abstract SyntaxToken GetIdentifierOfVariableDeclarator(TVariableDeclaratorSyntax variableDeclarator);
        protected abstract Task<bool> TypesAreCompatibleAsync(Document document, ILocalSymbol localSymbol, TLocalDeclarationStatementSyntax declarationStatement, SyntaxNode right, CancellationToken cancellationToken);

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

            if (!CanMoveToBlock(state.LocalSymbol, state.OutermostBlock, state.InnermostBlock))
            {
                return;
            }

            // Don't offer the refactoring inside the initializer for the variable.
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var initializer = syntaxFacts.GetInitializerOfVariableDeclarator(state.VariableDeclarator);
            var applicableSpan = initializer == null
                ? statement.Span
                : TextSpan.FromBounds(statement.SpanStart, initializer.SpanStart);

            if (!applicableSpan.IntersectsWith(position))
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
                MergeDeclarationAndAssignment(
                    document, state, editor, warningAnnotation);
            }
            else
            {
                await MoveDeclarationToFirstReferenceAsync(
                    document, state, editor, warningAnnotation, cancellationToken).ConfigureAwait(false);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task MoveDeclarationToFirstReferenceAsync(Document document, State state, SyntaxEditor editor, SyntaxAnnotation warningAnnotation, CancellationToken cancellationToken)
        {
            // If we're not merging with an existing declaration, make the declaration semantically
            // explicit to improve the chances that it won't break code.
            var explicitDeclarationStatement = await Simplifier.ExpandAsync(
                state.DeclarationStatement, document, cancellationToken: cancellationToken).ConfigureAwait(false);

            // place the declaration above the first statement that references it.
            var declarationStatement = warningAnnotation == null
                ? explicitDeclarationStatement
                : explicitDeclarationStatement.WithAdditionalAnnotations(warningAnnotation);
            declarationStatement = declarationStatement.WithAdditionalAnnotations(Formatter.Annotation);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var newNextStatement = state.FirstStatementAffectedInInnermostBlock;
            declarationStatement = declarationStatement.WithPrependedLeadingTrivia(
                syntaxFacts.GetLeadingBlankLines(newNextStatement));

            editor.InsertBefore(
                state.FirstStatementAffectedInInnermostBlock,
                declarationStatement);

            editor.ReplaceNode(
                newNextStatement,
                newNextStatement.WithAdditionalAnnotations(Formatter.Annotation).WithLeadingTrivia(
                    syntaxFacts.GetTriviaAfterLeadingBlankLines(newNextStatement)));

            // Move leading whitespace from the declaration statement to the next statement.
            var statementIndex = state.OutermostBlockStatements.IndexOf(state.DeclarationStatement);
            if (statementIndex + 1 < state.OutermostBlockStatements.Count)
            {
                var originalNextStatement = state.OutermostBlockStatements[statementIndex + 1];
                editor.ReplaceNode(
                    originalNextStatement,
                    (current, generator) => current.WithAdditionalAnnotations(Formatter.Annotation).WithPrependedLeadingTrivia(
                        syntaxFacts.GetLeadingBlankLines(state.DeclarationStatement)));
            }
        }

        private void MergeDeclarationAndAssignment(
            Document document, State state, SyntaxEditor editor, SyntaxAnnotation warningAnnotation)
        {
            // Replace the first reference with a new declaration.
            var declarationStatement = CreateMergedDeclarationStatement(document, state);
            declarationStatement = warningAnnotation == null
                ? declarationStatement
                : declarationStatement.WithAdditionalAnnotations(warningAnnotation);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            declarationStatement = declarationStatement.WithLeadingTrivia(
                GetMergedTrivia(syntaxFacts, state.DeclarationStatement, state.FirstStatementAffectedInInnermostBlock));

            editor.ReplaceNode(
                state.FirstStatementAffectedInInnermostBlock,
                declarationStatement.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private ImmutableArray<SyntaxTrivia> GetMergedTrivia(
            ISyntaxFactsService syntaxFacts, TStatementSyntax statement1, TStatementSyntax statement2)
        {
            return syntaxFacts.GetLeadingBlankLines(statement2).Concat(
                   syntaxFacts.GetTriviaAfterLeadingBlankLines(statement1)).Concat(
                   syntaxFacts.GetTriviaAfterLeadingBlankLines(statement2));
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

        private TLocalDeclarationStatementSyntax CreateMergedDeclarationStatement(
            Document document, State state)
        {
            var generator = SyntaxGenerator.GetGenerator(document);

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            syntaxFacts.GetPartsOfAssignmentStatement(
                state.FirstStatementAffectedInInnermostBlock, 
                out var left, out var operatorToken, out var right);

            return state.DeclarationStatement.ReplaceNode(
                state.VariableDeclarator,
                generator.WithInitializer(
                    state.VariableDeclarator,
                    generator.EqualsValueClause(operatorToken, right)));
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
