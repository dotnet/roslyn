﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertForEachToFor;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertForEachToFor
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertForEachToForCodeRefactoringProvider)), Shared]
    internal sealed class CSharpConvertForEachToForCodeRefactoringProvider :
        AbstractConvertForEachToForCodeRefactoringProvider<ForEachStatementSyntax>
    {
        protected override string Title => CSharpFeaturesResources.Convert_to_for;

        protected override ForEachStatementSyntax GetForEachStatement(TextSpan selection, SyntaxToken token)
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

        protected override (SyntaxNode start, SyntaxNode end) GetForEachBody(ForEachStatementSyntax foreachStatement)
            => (foreachStatement.Statement, foreachStatement.Statement);

        protected override void ConvertToForStatement(
            SemanticModel model, ForEachInfo foreachInfo, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var generator = editor.Generator;
            var foreachStatement = foreachInfo.ForEachStatement;

            var foreachCollectionExpression = foreachStatement.Expression;
            var collectionVariable = GetCollectionVariableName(
                model, generator, foreachInfo, foreachCollectionExpression, cancellationToken);

            var collectionStatementType = foreachInfo.RequireExplicitCastInterface
                ? generator.GetTypeExpression(foreachInfo.Options, foreachInfo.ExplicitCastInterface) :
                  generator.GetTypeExpression(foreachInfo.Options,
                     model.GetTypeInfo(foreachCollectionExpression).Type ?? model.Compilation.GetSpecialType(SpecialType.System_Object));

            // first, see whether we need to introduce new statement to capture collection
            IntroduceCollectionStatement(
                model, foreachInfo, editor, collectionStatementType, foreachCollectionExpression, collectionVariable);

            var indexVariable = CreateUniqueName(foreachInfo.SemanticFacts, model, foreachStatement.Statement, "i", cancellationToken);

            // put variable statement in body
            var bodyStatement = GetForLoopBody(generator, foreachInfo, collectionVariable, indexVariable);

            // create for statement from foreach statement
            var forStatement = SyntaxFactory.ForStatement(
                SyntaxFactory.VariableDeclaration(
                    generator.GetTypeExpression(
                        foreachInfo.Options, model.Compilation.GetSpecialType(SpecialType.System_Int32)),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(
                            indexVariable.WithAdditionalAnnotations(RenameAnnotation.Create()),
                            argumentList: default,
                            SyntaxFactory.EqualsValueClause((ExpressionSyntax)generator.LiteralExpression(0))))),
                SyntaxFactory.SeparatedList<ExpressionSyntax>(),
                (ExpressionSyntax)generator.LessThanExpression(
                    generator.IdentifierName(indexVariable),
                    generator.MemberAccessExpression(collectionVariable, foreachInfo.CountName)),
                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.PostfixUnaryExpression(
                        SyntaxKind.PostIncrementExpression, SyntaxFactory.IdentifierName(indexVariable))),
                bodyStatement);

            if (!foreachInfo.RequireCollectionStatement)
            {
                // move comments before "foreach" keyword to "for". if collection statement is introduced,
                // it should have attached to the new collection statement, so no need to do it here.
                forStatement = forStatement.WithLeadingTrivia(foreachStatement.GetLeadingTrivia());
            }

            // replace close parenthese from "foreach" statement
            forStatement = forStatement.WithCloseParenToken(foreachStatement.CloseParenToken);

            editor.ReplaceNode(foreachStatement, forStatement);
        }

        private StatementSyntax GetForLoopBody(
            SyntaxGenerator generator, ForEachInfo foreachInfo, SyntaxNode collectionVariableName, SyntaxToken indexVariable)
        {
            var foreachStatement = foreachInfo.ForEachStatement;
            if (foreachStatement.Statement is EmptyStatementSyntax)
            {
                return foreachStatement.Statement;
            }

            var bodyBlock = foreachStatement.Statement is BlockSyntax block ? block : SyntaxFactory.Block(foreachStatement.Statement);
            if (bodyBlock.Statements.Count > 0)
            {
                // create variable statement
                var variableStatement = AddItemVariableDeclaration(
                    generator, generator.GetTypeExpression(foreachInfo.Options, foreachInfo.ForEachElementType),
                    foreachStatement.Identifier, foreachInfo.ForEachElementType, collectionVariableName, indexVariable);

                bodyBlock = bodyBlock.InsertNodesBefore(
                    bodyBlock.Statements[0], SpecializedCollections.SingletonEnumerable(
                        variableStatement.WithAdditionalAnnotations(Formatter.Annotation)));
            }

            return bodyBlock;
        }
    }
}
