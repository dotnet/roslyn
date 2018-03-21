// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ForeachToFor;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ForeachToFor
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpForEachToForCodeRefactoringProvider)), Shared]
    internal sealed class CSharpForEachToForCodeRefactoringProvider : AbstractForEachToForCodeRefactoringProvider
    {
        protected override SyntaxNode GetForEachStatement(TextSpan selection, SyntaxToken token)
        {
            var foreachStatement = token.Parent.FirstAncestorOrSelf<ForEachStatementSyntax>();
            if (foreachStatement == null)
            {
                return null;
            }

            // support refactoring only if caret is in between "foreach" and ")"
            var scope = TextSpan.FromBounds(foreachStatement.ForEachKeyword.Span.Start, foreachStatement.CloseParenToken.Span.End);
            if (!scope.IntersectsWith(selection))
            {
                return null;
            }

            // check whether there is any comments between foreach and ) tokens
            // if they do, we don't support conversion.
            foreach (var trivia in foreachStatement.DescendantTrivia(n => n == foreachStatement || scope.Contains(n.FullSpan)))
            {
                if (trivia.Span.End <= scope.Start ||
                    scope.End <= trivia.Span.Start)
                {
                    continue;
                }

                if (trivia.Kind() != SyntaxKind.WhitespaceTrivia &&
                    trivia.Kind() != SyntaxKind.EndOfLineTrivia)
                {
                    // we don't know what to do with these comments
                    return null;
                }
            }

            return foreachStatement;
        }

        protected override bool ValidLocation(ForEachInfo foreachInfo)
        {
            if (!foreachInfo.RequireCollectionStatement)
            {
                return true;
            }

            // for now, we don't support converting in embeded statement if 
            // new local declaration for collection is required.
            // we can support this by using Introduce local variable service
            // but the service is not currently written in a way that can be
            // easily reused here.
            return foreachInfo.ForEachStatement.Parent.IsKind(SyntaxKind.Block);
        }

        protected override (SyntaxNode start, SyntaxNode end) GetForEachBody(SyntaxNode node)
        {
            var foreachStatement = (ForEachStatementSyntax)node;
            return (foreachStatement.Statement, foreachStatement.Statement);
        }

        protected override void ConvertToForStatement(SemanticModel model, ForEachInfo foreachInfo, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var generator = editor.Generator;
            var foreachStatement = (ForEachStatementSyntax)foreachInfo.ForEachStatement;

            var foreachCollectionExpression = foreachStatement.Expression;
            var collectionVariableName = GetCollectionVariableName(model, foreachInfo, foreachCollectionExpression);

            // first, see whether we need to introduce new statement to capture collection
            IntroduceCollectionStatement(model, foreachInfo, editor, foreachCollectionExpression, collectionVariableName);

            // create new index varialbe name
            var indexString = CreateUniqueName(model, foreachStatement.Statement, "i");

            // put variable statement in body
            var bodyStatement = GetForLoopBody(generator, foreachInfo, collectionVariableName, indexString);

            // create for statement from foreach statement
            var forStatement = SyntaxFactory.ForStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            generator.Identifier(indexString).WithAdditionalAnnotations(RenameAnnotation.Create()),
                            default,
                            SyntaxFactory.EqualsValueClause((ExpressionSyntax)generator.LiteralExpression(0))))),
                SyntaxFactory.SeparatedList<ExpressionSyntax>(),
                (ExpressionSyntax)generator.LessThanExpression(
                    generator.IdentifierName(indexString),
                    generator.MemberAccessExpression(
                        generator.IdentifierName(collectionVariableName), foreachInfo.CountName)),
                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, SyntaxFactory.IdentifierName(indexString))),
                bodyStatement);

            // let leading and trailing trivia set
            if (!foreachInfo.RequireCollectionStatement)
            {
                forStatement = forStatement.WithLeadingTrivia(foreachStatement.ForEachKeyword.LeadingTrivia);
            }

            forStatement = forStatement.ReplaceToken(forStatement.CloseParenToken, forStatement.CloseParenToken.WithTrailingTrivia(foreachStatement.CloseParenToken.TrailingTrivia));

            editor.ReplaceNode(foreachStatement, forStatement);
        }

        private StatementSyntax GetForLoopBody(SyntaxGenerator generator, ForEachInfo foreachInfo, string collectionVariableName, string indexString)
        {
            var foreachStatement = (ForEachStatementSyntax)foreachInfo.ForEachStatement;
            if (foreachStatement.Statement is EmptyStatementSyntax)
            {
                return foreachStatement.Statement;
            }

            var bodyBlock = foreachStatement.Statement is BlockSyntax block ? block : SyntaxFactory.Block(foreachStatement.Statement);
            if (bodyBlock.Statements.Count > 0)
            {
                // use original text
                var foreachVariableString = foreachStatement.Identifier.ToString();

                // create varialbe statement
                var variableStatement = AddItemVariableDeclaration(generator, foreachVariableString, collectionVariableName, indexString);

                bodyBlock = bodyBlock.InsertNodesBefore(
                    bodyBlock.Statements[0], SpecializedCollections.SingletonEnumerable(variableStatement));
            }

            return bodyBlock;
        }
    }
}
