// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.CSharp;
using System;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    internal sealed class CSharpConvertLinqQueryToLinqMethodProvider : AbstractConvertLinqProvider
    {
        protected override IAnalyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
            => new CSharpAnalyzer(syntaxFacts, semanticModel);

        private sealed class CSharpAnalyzer : Analyzer<QueryExpressionSyntax, ExpressionSyntax>
        {
            public CSharpAnalyzer(ISyntaxFactsService syntaxFacts, SemanticModel semanticModel)
                : base(syntaxFacts, semanticModel)
            {
            }

            protected override string Title => CSharpFeaturesResources.Convert_linq_query_to_linq_method;

            protected override ExpressionSyntax Convert(QueryExpressionSyntax source)
            {
                var fromClause = source.FromClause;

                var identifier = fromClause.Identifier;
                var context = new ConversionContext(identifier);
                var expression = fromClause.Expression;
                if (fromClause.Type != null)
                {
                    var lambda = SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, SyntaxFactory.IdentifierName(identifier), SyntaxFactory.Token(SyntaxKind.IsKeyword), fromClause.Type);
                    expression = ProcesQueryClause(context, expression, lambda, "Select");
                }

                return ProcessQueryBody(context, expression.WithoutTrailingTrivia(), source.Body);
            }

            protected override bool Validate(QueryExpressionSyntax originalMethod, ExpressionSyntax convertedMethod, CancellationToken cancellationToken)
            {
                var speculationAnalyzer = new SpeculationAnalyzer(originalMethod, convertedMethod, _semanticModel, cancellationToken);
                if (speculationAnalyzer.ReplacementChangesSemantics())
                {
                    return false;
                }

                // TODO add more checks
                return true;
            }

            private ExpressionSyntax ProcessQueryBody(ConversionContext context, ExpressionSyntax parentExpression, QueryBodySyntax queryBody)
            {
                var expression = parentExpression;
                foreach (var queryClause in queryBody.Clauses)
                {
                    expression = ProcessQueryClause(context, expression, queryClause);
                    if (expression == null)
                    {
                        return null;
                    }
                }

                expression = ProcessSelectOrGroup(context, expression, queryBody.SelectOrGroup);

                if (queryBody.Continuation != null)
                {
                    var newContext = new ConversionContext(queryBody.Continuation.Identifier);
                    return ProcessQueryBody(newContext, expression, queryBody.Continuation.Body);
                }

                return expression;
            }

            private ExpressionSyntax ProcessSelectOrGroup(ConversionContext context, ExpressionSyntax parentExpression, SelectOrGroupClauseSyntax selectOrGroupClause)
            {
                switch (selectOrGroupClause.Kind())
                {
                    case SyntaxKind.SelectClause: return ProcesSelectClause(context, parentExpression, (SelectClauseSyntax)selectOrGroupClause);
                    case SyntaxKind.GroupClause: return ProcessGroupClause(context, parentExpression, (GroupClauseSyntax)selectOrGroupClause);
                    case SyntaxKind.LetClause: return null; // Skip this because the linq method for the let will be more complicated than the query.
                    default: return null;
                }
            }

            private ExpressionSyntax ProcessGroupClause(ConversionContext context, ExpressionSyntax parentExpression, GroupClauseSyntax groupClause)
            {
                var parameter = SyntaxFactory.Parameter(context.Identifier);
                var groupExpressionLambda = SyntaxFactory.SimpleLambdaExpression(parameter, groupClause.GroupExpression.WithoutTrailingTrivia());
                var byExpressionLambda = SyntaxFactory.SimpleLambdaExpression(parameter, groupClause.ByExpression.WithoutTrailingTrivia());
                var groupExpressionArgument = SyntaxFactory.Argument(groupExpressionLambda);
                var byExpressionArgument = SyntaxFactory.Argument(byExpressionLambda);
                var arguments = new SeparatedSyntaxList<ArgumentSyntax>().Add(byExpressionArgument).Add(groupExpressionArgument);
                var argumentList = SyntaxFactory.ArgumentList(arguments);

                var groupKeyword = SyntaxFactory.IdentifierName("GroupBy");
                var memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentExpression, groupKeyword);
                return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList);
            }

            private ExpressionSyntax ProcesSelectClause(ConversionContext context, ExpressionSyntax parentExpression, SelectClauseSyntax selectClause)
            {
                var expression = selectClause.Expression;
                // Avoid trivial Select(x => x)
                if (expression is IdentifierNameSyntax identifierName)
                {
                    // TODO consider a better condition to compare
                    if (identifierName.Identifier.Text == context.Identifier.Text)
                    {
                        return parentExpression;
                    }
                }

                return ProcesQueryClause(context, parentExpression, selectClause.Expression, "Select");
            }

            private InvocationExpressionSyntax ProcesQueryClause(ConversionContext context, ExpressionSyntax parentExpression, ExpressionSyntax expression, string keyword)
            {
                var parameter = SyntaxFactory.Parameter(context.Identifier);
                var lambda = SyntaxFactory.SimpleLambdaExpression(parameter, expression.WithoutTrailingTrivia());
                var argument = SyntaxFactory.Argument(lambda);
                var arguments = new SeparatedSyntaxList<ArgumentSyntax>().Add(argument);
                var argumentList = SyntaxFactory.ArgumentList(arguments);

                var selectKeyword = SyntaxFactory.IdentifierName(keyword);
                var memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentExpression, selectKeyword);
                return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList);
            }

            private ExpressionSyntax ProcessQueryClause(ConversionContext context, ExpressionSyntax parentExpression, QueryClauseSyntax queryClause)
            {
                switch (queryClause.Kind())
                {
                    case SyntaxKind.WhereClause: return ProcesQueryClause(context, parentExpression, ((WhereClauseSyntax)queryClause).Condition, "Where");
                    case SyntaxKind.OrderByClause: return ProcessOrderByClause(context, parentExpression, (OrderByClauseSyntax)queryClause);
                    case SyntaxKind.JoinClause: // Not supported because linq queries seem to provide essentially simpler syntax for the same than query methods.
                    case SyntaxKind.FromClause: // More than one fromClause is not supported. The linq method seems to be more complicated for this.
                    default: return null;
                }
            }

            private ExpressionSyntax ProcessOrderByClause(ConversionContext context, ExpressionSyntax parentExpression, OrderByClauseSyntax orderByClause)
            {
                bool isFirst = true;
                foreach(var ordering in orderByClause.Orderings)
                {
                    parentExpression = ProcesQueryClause(context, parentExpression, ordering.Expression, GetOrderingKeyword(ordering, isFirst));
                    isFirst = false;
                }

                return parentExpression;
            }

            private string GetOrderingKeyword(OrderingSyntax ordering, bool isFirst)
            {
                switch (ordering.Kind())
                {
                    case SyntaxKind.AscendingOrdering: return isFirst ? "OrderBy" : "ThenBy";
                    case SyntaxKind.DescendingOrdering: return isFirst ? "OrderByDescending" : "ThenByDescending";
                    default: return null;
                }
            }

            protected override QueryExpressionSyntax FindNodeToRefactor(SyntaxNode root, CodeRefactoringContext context)
            {
                return root.FindNode(context.Span).FirstAncestorOrSelf<QueryExpressionSyntax>();
            }

            private struct ConversionContext
            {
                public SyntaxToken Identifier { get; }

                public ConversionContext(SyntaxToken identifier)
                {
                    Identifier = identifier;
                }
            }
        }
    }
}
