// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.MoveDeclarationNearReference
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.MoveDeclarationNearReference), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddImport)]
    internal partial class MoveDeclarationNearReferenceCodeRefactoringProvider : CodeRefactoringProvider
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

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var position = textSpan.Start;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var statement = root.FindToken(position).GetAncestor<LocalDeclarationStatementSyntax>();
            if (statement == null)
            {
                return;
            }

            if (statement.Declaration.Variables.Count != 1)
            {
                return;
            }

            // Only offer the refactoring when somewhere in the type+name of the local variable.
            if (position < statement.SpanStart || position > statement.Declaration.Variables[0].Identifier.Span.End)
            {
                return;
            }

            var state = await State.GenerateAsync(document, statement, cancellationToken).ConfigureAwait(false);
            if (state == null)
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

            //var innermostStatements =
            //    state.InnermostBlock.Statements.Where(s => s != state.DeclarationStatement).ToList();
            //var innermostAffectedIndex = innermostStatements.IndexOf(state.FirstStatementAffectedInInnermostBlock);

            var crossesMeaningfulBlock = CrossesMeaningfulBlock(state);
            var warningAnnotation = crossesMeaningfulBlock
                ? WarningAnnotation.Create(CSharpFeaturesResources.Warning_colon_Declaration_changes_scope_and_may_change_meaning)
                : null;

            editor.RemoveNode(state.DeclarationStatement);

            var canMergeDeclarationAndAssignment = await CanMergeDeclarationAndAssignmentAsync(document, state, cancellationToken).ConfigureAwait(false);
            if (canMergeDeclarationAndAssignment)
            {
                // Replace the first reference with a new declaration.
                var declarationStatement = CreateMergedDeclarationStatement(state, state.FirstStatementAffectedInInnermostBlock);
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
            var blocks = state.InnermostBlock.GetAncestorsOrThis<BlockSyntax>();
            foreach (var block in blocks)
            {
                if (block == state.OutermostBlock)
                {
                    break;
                }

                if (block.Parent is ParenthesizedLambdaExpressionSyntax ||
                    block.Parent is SimpleLambdaExpressionSyntax ||
                    block.Parent is AnonymousMethodExpressionSyntax ||
                    block.Parent is CommonForEachStatementSyntax ||
                    block.Parent is ForStatementSyntax ||
                    block.Parent is WhileStatementSyntax ||
                    block.Parent is DoStatementSyntax)
                {
                    return true;
                }
            }

            return false;
        }

        private StatementSyntax CreateMergedDeclarationStatement(State state, StatementSyntax statementSyntax)
        {
            var assignExpression = (AssignmentExpressionSyntax)((ExpressionStatementSyntax)statementSyntax).Expression;
            return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(state.VariableDeclaration.Type,
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(state.VariableDeclarator.Identifier).WithInitializer(SyntaxFactory.EqualsValueClause(assignExpression.Right)))));
        }

        private async Task<bool> CanMergeDeclarationAndAssignmentAsync(
            Document document,
            State state,
            CancellationToken cancellationToken)
        {
            var firstStatement = state.FirstStatementAffectedInInnermostBlock;
            var localSymbol = state.LocalSymbol;
            if (firstStatement.Kind() != SyntaxKind.ExpressionStatement)
            {
                return false;
            }

            var expressionStatement = (ExpressionStatementSyntax)firstStatement;
            if (expressionStatement.Expression.Kind() != SyntaxKind.SimpleAssignmentExpression)
            {
                return false;
            }

            var expression = (AssignmentExpressionSyntax)expressionStatement.Expression;
            if (expression.Left.Kind() != SyntaxKind.IdentifierName)
            {
                return false;
            }

            var identifierName = (IdentifierNameSyntax)expression.Left;
            if (identifierName.Identifier.ValueText != localSymbol.Name)
            {
                return false;
            }

            // Can only merge if the declaration had a non-var type, or if it was 'var' and the
            // types match.
            var type = state.VariableDeclaration.Type;
            if (type.IsVar)
            {
                // Type inference.  Only merge if types match.
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var rightType = semanticModel.GetTypeInfo(expression.Right, cancellationToken);
                return (localSymbol.Type == null && rightType.Type == null) || localSymbol.Type.Equals(rightType.Type);
            }
            else
            {
                // No type inference, so we can definitely merge these
                return true;
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(CSharpFeaturesResources.Move_declaration_near_reference, createChangedDocument)
            {
            }

            internal override CodeActionPriority Priority => CodeActionPriority.Low;
        }
    }
}
