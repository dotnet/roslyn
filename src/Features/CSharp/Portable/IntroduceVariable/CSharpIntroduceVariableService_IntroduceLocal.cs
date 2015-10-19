// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    internal partial class CSharpIntroduceVariableService
    {
        protected override Task<Document> IntroduceLocalAsync(
            SemanticDocument document,
            ExpressionSyntax expression,
            bool allOccurrences,
            bool isConstant,
            CancellationToken cancellationToken)
        {
            var options = document.Project.Solution.Workspace.Options;

            var newLocalNameToken = GenerateUniqueLocalName(document, expression, isConstant, cancellationToken);
            var newLocalName = SyntaxFactory.IdentifierName(newLocalNameToken);

            var modifiers = isConstant
                ? SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword))
                : default(SyntaxTokenList);

            var declarationStatement = SyntaxFactory.LocalDeclarationStatement(
                modifiers,
                SyntaxFactory.VariableDeclaration(
                    this.GetTypeSyntax(document, expression, isConstant, options, cancellationToken),
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(
                        newLocalNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()),
                        null,
                        SyntaxFactory.EqualsValueClause(expression.WithoutTrailingTrivia().WithoutLeadingTrivia())))));

            var anonymousMethodParameters = GetAnonymousMethodParameters(document, expression, cancellationToken);
            var lambdas = anonymousMethodParameters.SelectMany(p => p.ContainingSymbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax(cancellationToken)).AsEnumerable())
                                                   .Where(n => n is ParenthesizedLambdaExpressionSyntax || n is SimpleLambdaExpressionSyntax)
                                                   .ToSet();

            var parentLambda = GetParentLambda(expression, lambdas);

            if (parentLambda != null)
            {
                return Task.FromResult(IntroduceLocalDeclarationIntoLambda(
                    document, expression, newLocalName, declarationStatement, parentLambda, allOccurrences, cancellationToken));
            }
            else if (IsInExpressionBodiedMember(expression))
            {
                return Task.FromResult(RewriteExpressionBodiedMemberAndIntroduceLocalDeclaration(
                    document, expression, newLocalName, declarationStatement, allOccurrences, cancellationToken));
            }
            else
            {
                return IntroduceLocalDeclarationIntoBlockAsync(
                    document, expression, newLocalName, declarationStatement, allOccurrences, cancellationToken);
            }
        }

        private Document IntroduceLocalDeclarationIntoLambda(
            SemanticDocument document,
            ExpressionSyntax expression,
            IdentifierNameSyntax newLocalName,
            LocalDeclarationStatementSyntax declarationStatement,
            SyntaxNode oldLambda,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            var oldBody = oldLambda is ParenthesizedLambdaExpressionSyntax
                ? (ExpressionSyntax)((ParenthesizedLambdaExpressionSyntax)oldLambda).Body
                : (ExpressionSyntax)((SimpleLambdaExpressionSyntax)oldLambda).Body;

            var rewrittenBody = Rewrite(
                document, expression, newLocalName, document, oldBody, allOccurrences, cancellationToken);

            var delegateType = document.SemanticModel.GetTypeInfo(oldLambda, cancellationToken).ConvertedType as INamedTypeSymbol;

            var newBody = delegateType != null && delegateType.DelegateInvokeMethod != null && delegateType.DelegateInvokeMethod.ReturnsVoid
                ? SyntaxFactory.Block(declarationStatement)
                : SyntaxFactory.Block(declarationStatement, SyntaxFactory.ReturnStatement(rewrittenBody));

            newBody = newBody.WithAdditionalAnnotations(Formatter.Annotation);

            var newLambda = oldLambda is ParenthesizedLambdaExpressionSyntax
                ? ((ParenthesizedLambdaExpressionSyntax)oldLambda).WithBody(newBody)
                : (SyntaxNode)((SimpleLambdaExpressionSyntax)oldLambda).WithBody(newBody);

            var newRoot = document.Root.ReplaceNode(oldLambda, newLambda);
            return document.Document.WithSyntaxRoot(newRoot);
        }

        private SyntaxNode GetParentLambda(ExpressionSyntax expression, ISet<SyntaxNode> lambdas)
        {
            var current = expression;
            while (current != null)
            {
                if (lambdas.Contains(current.Parent))
                {
                    return current.Parent;
                }

                current = current.Parent as ExpressionSyntax;
            }

            return null;
        }

        private TypeSyntax GetTypeSyntax(SemanticDocument document, ExpressionSyntax expression, bool isConstant, OptionSet options, CancellationToken cancellationToken)
        {
            var typeSymbol = GetTypeSymbol(document, expression, cancellationToken);
            if (typeSymbol.ContainsAnonymousType())
            {
                return SyntaxFactory.IdentifierName("var");
            }

            if (!isConstant && options.GetOption(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals) && CanUseVar(typeSymbol))
            {
                return SyntaxFactory.IdentifierName("var");
            }

            return typeSymbol.GenerateTypeSyntax();
        }

        private bool CanUseVar(ITypeSymbol typeSymbol)
        {
            return typeSymbol.TypeKind != TypeKind.Delegate
                && !typeSymbol.IsErrorType()
                && !typeSymbol.IsFormattableString();
        }

        private static async Task<Tuple<SemanticDocument, ISet<ExpressionSyntax>>> ComplexifyParentingStatements(
            SemanticDocument semanticDocument,
            ISet<ExpressionSyntax> matches,
            CancellationToken cancellationToken)
        {
            // First, track the matches so that we can get back to them later.
            var newRoot = semanticDocument.Root.TrackNodes(matches);
            var newDocument = semanticDocument.Document.WithSyntaxRoot(newRoot);
            var newSemanticDocument = await SemanticDocument.CreateAsync(newDocument, cancellationToken).ConfigureAwait(false);
            var newMatches = newSemanticDocument.Root.GetCurrentNodes(matches.AsEnumerable()).ToSet();

            // Next, expand the topmost parenting expression of each match, being careful
            // not to expand the matches themselves.
            var topMostExpressions = newMatches
                .Select(m => m.AncestorsAndSelf().OfType<ExpressionSyntax>().Last())
                .Distinct();

            newRoot = await newSemanticDocument.Root
                .ReplaceNodesAsync(
                    topMostExpressions,
                    computeReplacementAsync: async (oldNode, newNode, ct) =>
                    {
                        return await Simplifier
                            .ExpandAsync(
                                oldNode,
                                newSemanticDocument.Document,
                                expandInsideNode: node =>
                                {
                                    var expression = node as ExpressionSyntax;
                                    return expression == null
                                        || !newMatches.Contains(expression);
                                },
                                cancellationToken: ct)
                            .ConfigureAwait(false);
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            newDocument = newSemanticDocument.Document.WithSyntaxRoot(newRoot);
            newSemanticDocument = await SemanticDocument.CreateAsync(newDocument, cancellationToken).ConfigureAwait(false);
            newMatches = newSemanticDocument.Root.GetCurrentNodes(matches.AsEnumerable()).ToSet();

            return Tuple.Create(newSemanticDocument, newMatches);
        }

        private Document RewriteExpressionBodiedMemberAndIntroduceLocalDeclaration(
            SemanticDocument document,
            ExpressionSyntax expression,
            NameSyntax newLocalName,
            LocalDeclarationStatementSyntax declarationStatement,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            var oldBody = expression.GetAncestorOrThis<ArrowExpressionClauseSyntax>();
            var oldParentingNode = oldBody.Parent;
            var leadingTrivia = oldBody.GetLeadingTrivia()
                                       .AddRange(oldBody.ArrowToken.TrailingTrivia);

            var newStatement = Rewrite(document, expression, newLocalName, document, oldBody.Expression, allOccurrences, cancellationToken);
            var newBody = SyntaxFactory.Block(declarationStatement, SyntaxFactory.ReturnStatement(newStatement))
                                       .WithLeadingTrivia(leadingTrivia)
                                       .WithTrailingTrivia(oldBody.GetTrailingTrivia())
                                       .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newParentingNode = null;
            if (oldParentingNode is BasePropertyDeclarationSyntax)
            {
                var getAccessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, newBody);
                var accessorList = SyntaxFactory.AccessorList(SyntaxFactory.List(new[] { getAccessor }));

                newParentingNode = ((BasePropertyDeclarationSyntax)oldParentingNode).RemoveNode(oldBody, SyntaxRemoveOptions.KeepNoTrivia);

                if (newParentingNode.IsKind(SyntaxKind.PropertyDeclaration))
                {
                    var propertyDeclaration = ((PropertyDeclarationSyntax)newParentingNode);
                    newParentingNode = propertyDeclaration
                        .WithAccessorList(accessorList)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithTrailingTrivia(propertyDeclaration.SemicolonToken.TrailingTrivia);
                }
                else if (newParentingNode.IsKind(SyntaxKind.IndexerDeclaration))
                {
                    var indexerDeclaration = ((IndexerDeclarationSyntax)newParentingNode);
                    newParentingNode = indexerDeclaration
                        .WithAccessorList(accessorList)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithTrailingTrivia(indexerDeclaration.SemicolonToken.TrailingTrivia);
                }
            }
            else if (oldParentingNode is BaseMethodDeclarationSyntax)
            {
                newParentingNode = ((BaseMethodDeclarationSyntax)oldParentingNode)
                    .RemoveNode(oldBody, SyntaxRemoveOptions.KeepNoTrivia)
                    .WithBody(newBody);

                if (newParentingNode.IsKind(SyntaxKind.MethodDeclaration))
                {
                    var methodDeclaration = ((MethodDeclarationSyntax)newParentingNode);
                    newParentingNode = methodDeclaration
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithTrailingTrivia(methodDeclaration.SemicolonToken.TrailingTrivia);
                }
                else if (newParentingNode.IsKind(SyntaxKind.OperatorDeclaration))
                {
                    var operatorDeclaration = ((OperatorDeclarationSyntax)newParentingNode);
                    newParentingNode = operatorDeclaration
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithTrailingTrivia(operatorDeclaration.SemicolonToken.TrailingTrivia);
                }
                else if (newParentingNode.IsKind(SyntaxKind.ConversionOperatorDeclaration))
                {
                    var conversionOperatorDeclaration = ((ConversionOperatorDeclarationSyntax)newParentingNode);
                    newParentingNode = conversionOperatorDeclaration
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.None))
                        .WithTrailingTrivia(conversionOperatorDeclaration.SemicolonToken.TrailingTrivia);
                }
            }

            var newRoot = document.Root.ReplaceNode(oldParentingNode, newParentingNode);
            return document.Document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> IntroduceLocalDeclarationIntoBlockAsync(
            SemanticDocument document,
            ExpressionSyntax expression,
            NameSyntax newLocalName,
            LocalDeclarationStatementSyntax declarationStatement,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            declarationStatement = declarationStatement.WithAdditionalAnnotations(Formatter.Annotation);

            var oldOutermostBlock = expression.GetAncestorsOrThis<BlockSyntax>().LastOrDefault();
            var matches = FindMatches(document, expression, document, oldOutermostBlock, allOccurrences, cancellationToken);
            Debug.Assert(matches.Contains(expression));

            var complexified = await ComplexifyParentingStatements(document, matches, cancellationToken).ConfigureAwait(false);
            document = complexified.Item1;
            matches = complexified.Item2;

            // Our original expression should have been one of the matches, which were tracked as part
            // of complexification, so we can retrieve the latest version of the expression here.
            expression = document.Root.GetCurrentNodes(expression).First();

            var innermostStatements = new HashSet<StatementSyntax>(
                matches.Select(expr => expr.GetAncestorOrThis<StatementSyntax>()));

            if (innermostStatements.Count == 1)
            {
                // If there was only one match, or all the matches came from the same
                // statement, then we want to place the declaration right above that
                // statement. Note: we special case this because the statement we are going
                // to go above might not be in a block and we may have to generate it
                return IntroduceLocalForSingleOccurrenceIntoBlock(
                    document, expression, newLocalName, declarationStatement, allOccurrences, cancellationToken);
            }

            var oldInnerMostCommonBlock = matches.FindInnermostCommonBlock();
            var allAffectedStatements = new HashSet<StatementSyntax>(matches.SelectMany(expr => expr.GetAncestorsOrThis<StatementSyntax>()));
            var firstStatementAffectedInBlock = oldInnerMostCommonBlock.Statements.First(allAffectedStatements.Contains);

            var firstStatementAffectedIndex = oldInnerMostCommonBlock.Statements.IndexOf(firstStatementAffectedInBlock);

            var newInnerMostBlock = Rewrite(
                document, expression, newLocalName, document, oldInnerMostCommonBlock, allOccurrences, cancellationToken);

            var statements = new List<StatementSyntax>();
            statements.AddRange(newInnerMostBlock.Statements.Take(firstStatementAffectedIndex));
            statements.Add(declarationStatement);
            statements.AddRange(newInnerMostBlock.Statements.Skip(firstStatementAffectedIndex));

            var finalInnerMostBlock = newInnerMostBlock.WithStatements(
                SyntaxFactory.List<StatementSyntax>(statements));

            var newRoot = document.Root.ReplaceNode(oldInnerMostCommonBlock, finalInnerMostBlock);
            return document.Document.WithSyntaxRoot(newRoot);
        }

        private Document IntroduceLocalForSingleOccurrenceIntoBlock(
            SemanticDocument document,
            ExpressionSyntax expression,
            NameSyntax localName,
            LocalDeclarationStatementSyntax localDeclaration,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            var oldStatement = expression.GetAncestorOrThis<StatementSyntax>();
            var newStatement = Rewrite(
                document, expression, localName, document, oldStatement, allOccurrences, cancellationToken);

            if (oldStatement.IsParentKind(SyntaxKind.Block))
            {
                var oldBlock = oldStatement.Parent as BlockSyntax;
                var statementIndex = oldBlock.Statements.IndexOf(oldStatement);

                var newBlock = oldBlock.WithStatements(CreateNewStatementList(
                    oldBlock.Statements, localDeclaration, newStatement, statementIndex));

                var newRoot = document.Root.ReplaceNode(oldBlock, newBlock);
                return document.Document.WithSyntaxRoot(newRoot);
            }
            else if (oldStatement.IsParentKind(SyntaxKind.SwitchSection))
            {
                var oldSwitchSection = oldStatement.Parent as SwitchSectionSyntax;
                var statementIndex = oldSwitchSection.Statements.IndexOf(oldStatement);

                var newSwitchSection = oldSwitchSection.WithStatements(CreateNewStatementList(
                    oldSwitchSection.Statements, localDeclaration, newStatement, statementIndex));

                var newRoot = document.Root.ReplaceNode(oldSwitchSection, newSwitchSection);
                return document.Document.WithSyntaxRoot(newRoot);
            }
            else
            {
                // we need to introduce a block to put the original statement, along with
                // the statement we're generating
                var newBlock = SyntaxFactory.Block(localDeclaration, newStatement).WithAdditionalAnnotations(Formatter.Annotation);

                var newRoot = document.Root.ReplaceNode(oldStatement, newBlock);
                return document.Document.WithSyntaxRoot(newRoot);
            }
        }

        private static SyntaxList<StatementSyntax> CreateNewStatementList(
            SyntaxList<StatementSyntax> oldStatements,
            LocalDeclarationStatementSyntax localDeclaration,
            StatementSyntax newStatement,
            int statementIndex)
        {
            return oldStatements.Take(statementIndex)
                                .Concat(localDeclaration.WithLeadingTrivia(oldStatements.Skip(statementIndex).First().GetLeadingTrivia()))
                                .Concat(newStatement.WithoutLeadingTrivia())
                                .Concat(oldStatements.Skip(statementIndex + 1))
                                .ToSyntaxList();
        }
    }
}
