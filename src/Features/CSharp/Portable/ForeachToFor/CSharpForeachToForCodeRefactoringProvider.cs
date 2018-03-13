// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ForeachToFor;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ForeachToFor
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpForeachToForCodeRefactoringProvider)), Shared]
    internal sealed class CSharpForeachToForCodeRefactoringProvider : AbstractForeachToForCodeRefactoringProvider
    {
        public CSharpForeachToForCodeRefactoringProvider()
        {
        }

        protected override SyntaxNode GetForeachStatement(SyntaxToken token)
        {
            var foreachStatement = token.Parent.FirstAncestorOrSelf<ForEachStatementSyntax>();
            if (foreachStatement == null)
            {
                return null;
            }

            // support refactoring only if caret is in between "foreach" and ")"
            var scope = TextSpan.FromBounds(foreachStatement.ForEachKeyword.Span.Start, foreachStatement.CloseParenToken.Span.End);
            if (!scope.IntersectsWith(token.Span))
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

        protected override (SyntaxNode start, SyntaxNode end) GetForeachBody(SyntaxNode node)
        {
            var foreachStatement = (ForEachStatementSyntax)node;
            return (foreachStatement.Statement, foreachStatement.Statement);
        }

        protected override void ConvertToForStatement(SemanticModel model, ForeachInfo foreachInfo, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var parseOption = model.SyntaxTree.Options;
            var foreachStatement = (ForEachStatementSyntax)foreachInfo.ForeachStatement;

            // first, see whether we need to introduce new statement to capture collection
            var foreachCollectionExpression = GetForeachCollection(foreachStatement);
            var collectionVariableName = foreachCollectionExpression.ToString();
            if (foreachInfo.RequireCollectionStatement)
            {
                collectionVariableName = CreateUniqueMethodName(model, foreachStatement, "list");

                var collectionStatement = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(collectionVariableName).WithAdditionalAnnotations(RenameAnnotation.Create()),
                                default,
                                SyntaxFactory.EqualsValueClause(
                                    foreachInfo.RequireExplicitCast ?
                                        SyntaxFactory.CastExpression(
                                            SyntaxFactory.ParseTypeName(foreachInfo.ExplicitCastInterface).WithAdditionalAnnotations(Simplifier.Annotation),
                                            SyntaxFactory.ParenthesizedExpression(foreachCollectionExpression)) : 
                                        foreachCollectionExpression)))));

                collectionStatement =
                    collectionStatement.ReplaceToken(collectionStatement.GetFirstToken(), collectionStatement.GetFirstToken().WithLeadingTrivia(foreachStatement.ForEachKeyword.LeadingTrivia));

                editor.InsertBefore(foreachStatement, collectionStatement);
            }

            // create new index varialbe name
            var indexString = CreateUniqueMethodName(model, foreachStatement.Statement, "i");

            // put variable statement in body
            var bodyStatement = GetForLoopBody(foreachInfo, parseOption, collectionVariableName, indexString);

            // create for statement from foreach statement
            var forStatement = SyntaxFactory.ForStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                        SyntaxFactory.Identifier(indexString).WithAdditionalAnnotations(RenameAnnotation.Create()),
                        default,
                        SyntaxFactory.EqualsValueClause(
                            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)))))),
                SyntaxFactory.SeparatedList<ExpressionSyntax>(),
                SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression, SyntaxFactory.IdentifierName(indexString), SyntaxFactory.ParseExpression($"{collectionVariableName}.{foreachInfo.CountName}")),
                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, SyntaxFactory.IdentifierName(indexString))),
                bodyStatement);

            // let leading and trailing trivia set
            if (!foreachInfo.RequireCollectionStatement)
            {
                forStatement = forStatement.ReplaceToken(forStatement.ForKeyword, forStatement.ForKeyword.WithLeadingTrivia(foreachStatement.ForEachKeyword.LeadingTrivia));
            }

            forStatement = forStatement.ReplaceToken(forStatement.CloseParenToken, forStatement.CloseParenToken.WithTrailingTrivia(foreachStatement.CloseParenToken.TrailingTrivia));

            editor.ReplaceNode(foreachStatement, forStatement);
        }

        private ExpressionSyntax GetForeachCollection(SyntaxNode node)
        {
            var foreachStatement = (ForEachStatementSyntax)node;

            // use original text, not value text
            return foreachStatement.Expression;
        }

        private StatementSyntax GetForLoopBody(ForeachInfo foreachInfo, ParseOptions parseOption, string collectionVariableName, string indexString)
        {
            var foreachStatement = (ForEachStatementSyntax)foreachInfo.ForeachStatement;
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
                var variableStatement = AddElasticAnnotation(SyntaxFactory.ParseStatement($"var {foreachVariableString} = {collectionVariableName}[{indexString}];", options: parseOption), SyntaxFactory.ElasticMarker);
                bodyBlock = bodyBlock.InsertNodesBefore(bodyBlock.Statements[0], new[] { variableStatement });
            }

            return bodyBlock;
        }
    }
}
