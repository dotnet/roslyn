// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    internal partial class CSharpIntroduceVariableService
    {
        protected override async Task<Document> IntroduceLocalAsync(
            SemanticDocument document,
            ExpressionSyntax expression,
            bool allOccurrences,
            bool isConstant,
            CancellationToken cancellationToken)
        {
            var containerToGenerateInto = expression.Ancestors().FirstOrDefault(s =>
                s is BlockSyntax || s is ArrowExpressionClauseSyntax || s is LambdaExpressionSyntax);

            var newLocalNameToken = GenerateUniqueLocalName(
                document, expression, isConstant, containerToGenerateInto, cancellationToken);
            var newLocalName = SyntaxFactory.IdentifierName(newLocalNameToken);

            var modifiers = isConstant
                ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword))
                : default;

            var options = await document.Document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var declarationStatement = SyntaxFactory.LocalDeclarationStatement(
                modifiers,
                SyntaxFactory.VariableDeclaration(
                    this.GetTypeSyntax(document, options, expression, isConstant, cancellationToken),
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(
                        newLocalNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()),
                        null,
                        SyntaxFactory.EqualsValueClause(expression.WithoutTrivia())))));

            switch (containerToGenerateInto)
            {
                case BlockSyntax block:
                    return await IntroduceLocalDeclarationIntoBlockAsync(
                        document, block, expression, newLocalName, declarationStatement, allOccurrences, cancellationToken).ConfigureAwait(false);

                case ArrowExpressionClauseSyntax arrowExpression:
                    // this will be null for expression-bodied properties & indexer (not for individual getters & setters, those do have a symbol),
                    // both of which are a shorthand for the getter and always return a value
                    var method = document.SemanticModel.GetDeclaredSymbol(arrowExpression.Parent) as IMethodSymbol;
                    var createReturnStatement = !method?.ReturnsVoid ?? true;

                    return RewriteExpressionBodiedMemberAndIntroduceLocalDeclaration(
                        document, arrowExpression, expression, newLocalName,
                        declarationStatement, allOccurrences, createReturnStatement, cancellationToken);

                case LambdaExpressionSyntax lambda:
                    return IntroduceLocalDeclarationIntoLambda(
                        document, lambda, expression, newLocalName, declarationStatement,
                        allOccurrences, cancellationToken);
            }

            throw new InvalidOperationException();
        }

        private Document IntroduceLocalDeclarationIntoLambda(
            SemanticDocument document,
            LambdaExpressionSyntax oldLambda,
            ExpressionSyntax expression,
            IdentifierNameSyntax newLocalName,
            LocalDeclarationStatementSyntax declarationStatement,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            var oldBody = (ExpressionSyntax)oldLambda.Body;

            var rewrittenBody = Rewrite(
                document, expression, newLocalName, document, oldBody, allOccurrences, cancellationToken);

            var delegateType = document.SemanticModel.GetTypeInfo(oldLambda, cancellationToken).ConvertedType as INamedTypeSymbol;

            var newBody = delegateType != null && delegateType.DelegateInvokeMethod != null && delegateType.DelegateInvokeMethod.ReturnsVoid
                ? SyntaxFactory.Block(declarationStatement)
                : SyntaxFactory.Block(declarationStatement, SyntaxFactory.ReturnStatement(rewrittenBody));

            // Add an elastic newline so that the formatter will place this new lambda body across multiple lines.
            newBody = newBody.WithOpenBraceToken(newBody.OpenBraceToken.WithAppendedTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed))
                             .WithAdditionalAnnotations(Formatter.Annotation);

            var newLambda = oldLambda.WithBody(newBody);

            var newRoot = document.Root.ReplaceNode(oldLambda, newLambda);
            return document.Document.WithSyntaxRoot(newRoot);
        }

        private TypeSyntax GetTypeSyntax(SemanticDocument document, DocumentOptionSet options, ExpressionSyntax expression, bool isConstant, CancellationToken cancellationToken)
        {
            var typeSymbol = GetTypeSymbol(document, expression, cancellationToken);
            return typeSymbol.GenerateTypeSyntax();
        }

        private bool CanUseVar(ITypeSymbol typeSymbol)
        {
            return typeSymbol.TypeKind != TypeKind.Delegate
                && !typeSymbol.IsErrorType()
                && !typeSymbol.IsFormattableString();
        }

        private Document RewriteExpressionBodiedMemberAndIntroduceLocalDeclaration(
            SemanticDocument document,
            ArrowExpressionClauseSyntax arrowExpression,
            ExpressionSyntax expression,
            NameSyntax newLocalName,
            LocalDeclarationStatementSyntax declarationStatement,
            bool allOccurrences,
            bool createReturnStatement,
            CancellationToken cancellationToken)
        {
            var oldBody = arrowExpression;
            var oldParentingNode = oldBody.Parent;
            var leadingTrivia = oldBody.GetLeadingTrivia()
                                       .AddRange(oldBody.ArrowToken.TrailingTrivia);

            var newExpression = Rewrite(document, expression, newLocalName, document, oldBody.Expression, allOccurrences, cancellationToken);

            var convertedStatement = createReturnStatement
                ? SyntaxFactory.ReturnStatement(newExpression)
                : (StatementSyntax)SyntaxFactory.ExpressionStatement(newExpression);

            var newBody = SyntaxFactory.Block(declarationStatement, convertedStatement)
                                       .WithLeadingTrivia(leadingTrivia)
                                       .WithTrailingTrivia(oldBody.GetTrailingTrivia());

            // Add an elastic newline so that the formatter will place this new block across multiple lines.
            newBody = newBody.WithOpenBraceToken(newBody.OpenBraceToken.WithAppendedTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed))
                             .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = document.Root.ReplaceNode(oldParentingNode, WithBlockBody(oldParentingNode, newBody));
            return document.Document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode WithBlockBody(SyntaxNode node, BlockSyntax body)
        {
            switch (node)
            {
                case BasePropertyDeclarationSyntax baseProperty:
                    var accessorList = SyntaxFactory.AccessorList(SyntaxFactory.SingletonList(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, body)));
                    return baseProperty
                        .TryWithExpressionBody(null)
                        .WithAccessorList(accessorList)
                        .TryWithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithTriviaFrom(baseProperty);
                case AccessorDeclarationSyntax accessor:
                    return accessor
                        .WithExpressionBody(null)
                        .WithBody(body)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithTriviaFrom(accessor);
                case BaseMethodDeclarationSyntax baseMethod:
                    return baseMethod
                        .WithExpressionBody(null)
                        .WithBody(body)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithTriviaFrom(baseMethod);
                case LocalFunctionStatementSyntax localFunction:
                    return localFunction
                        .WithExpressionBody(null)
                        .WithBody(body)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithTriviaFrom(localFunction);
                default:
                    throw ExceptionUtilities.UnexpectedValue(node);
            }
        }

        private async Task<Document> IntroduceLocalDeclarationIntoBlockAsync(
            SemanticDocument document,
            BlockSyntax block,
            ExpressionSyntax expression,
            NameSyntax newLocalName,
            LocalDeclarationStatementSyntax declarationStatement,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            declarationStatement = declarationStatement.WithAdditionalAnnotations(Formatter.Annotation);

            var oldOutermostBlock = block;
            var matches = FindMatches(document, expression, document, oldOutermostBlock, allOccurrences, cancellationToken);
            Debug.Assert(matches.Contains(expression));

            (document, matches) = await ComplexifyParentingStatements(document, matches, cancellationToken).ConfigureAwait(false);

            // Our original expression should have been one of the matches, which were tracked as part
            // of complexification, so we can retrieve the latest version of the expression here.
            expression = document.Root.GetCurrentNode(expression);

            var root = document.Root;
            ISet<StatementSyntax> allAffectedStatements = new HashSet<StatementSyntax>(matches.SelectMany(expr => expr.GetAncestorsOrThis<StatementSyntax>()));

            SyntaxNode innermostCommonBlock;

            var innermostStatements = new HashSet<StatementSyntax>(matches.Select(expr => expr.GetAncestorOrThis<StatementSyntax>()));
            if (innermostStatements.Count == 1)
            {
                // if there was only one match, or all the matches came from the same statement
                var statement = innermostStatements.Single();

                // and the statement is an embedded statement without a block, we want to generate one
                // around this statement rather than continue going up to find an actual block
                if (!IsBlockLike(statement.Parent))
                {
                    root = root.TrackNodes(allAffectedStatements.Concat(new SyntaxNode[] { expression, statement }));
                    root = root.ReplaceNode(root.GetCurrentNode(statement),
                        SyntaxFactory.Block(root.GetCurrentNode(statement)).WithAdditionalAnnotations(Formatter.Annotation));

                    expression = root.GetCurrentNode(expression);
                    allAffectedStatements = allAffectedStatements.Select(root.GetCurrentNode).ToSet();

                    statement = root.GetCurrentNode(statement);
                }

                innermostCommonBlock = statement.Parent;
            }
            else
            {
                innermostCommonBlock = innermostStatements.FindInnermostCommonNode(IsBlockLike);
            }

            var firstStatementAffectedIndex = GetStatements(innermostCommonBlock).IndexOf(allAffectedStatements.Contains);

            var newInnerMostBlock = Rewrite(
                document, expression, newLocalName, document, innermostCommonBlock, allOccurrences, cancellationToken);

            var statements = InsertWithinTriviaOfNext(GetStatements(newInnerMostBlock), declarationStatement, firstStatementAffectedIndex);
            var finalInnerMostBlock = WithStatements(newInnerMostBlock, statements);

            var newRoot = root.ReplaceNode(innermostCommonBlock, finalInnerMostBlock);
            return document.Document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxList<StatementSyntax> InsertWithinTriviaOfNext(
            SyntaxList<StatementSyntax> oldStatements,
            StatementSyntax newStatement,
            int statementIndex)
        {
            var nextStatement = oldStatements.ElementAtOrDefault(statementIndex);
            return nextStatement == null
                ? oldStatements.Insert(statementIndex, newStatement)
                : oldStatements.ReplaceRange(nextStatement, new[] {
                    newStatement.WithLeadingTrivia(nextStatement.GetLeadingTrivia()),
                    nextStatement.WithoutLeadingTrivia() });
        }

        private static bool IsBlockLike(SyntaxNode node) => node is BlockSyntax || node is SwitchSectionSyntax;

        private static SyntaxList<StatementSyntax> GetStatements(SyntaxNode blockLike) =>
            blockLike is BlockSyntax block ? block.Statements :
            blockLike is SwitchSectionSyntax switchSection ? switchSection.Statements :
            throw ExceptionUtilities.UnexpectedValue(blockLike);

        private static SyntaxNode WithStatements(SyntaxNode blockLike, SyntaxList<StatementSyntax> statements) =>
            blockLike is BlockSyntax block ? block.WithStatements(statements) as SyntaxNode :
            blockLike is SwitchSectionSyntax switchSection ? switchSection.WithStatements(statements) :
            throw ExceptionUtilities.UnexpectedValue(blockLike);
    }
}
