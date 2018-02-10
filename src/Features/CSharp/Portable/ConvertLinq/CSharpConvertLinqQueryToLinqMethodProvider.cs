// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
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
                var expression = fromClause.Expression.WithoutTrailingTrivia();
                if (fromClause.Type != null)
                {
                    var lambda = SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, SyntaxFactory.IdentifierName(identifier), SyntaxFactory.Token(SyntaxKind.IsKeyword), fromClause.Type);
                    expression = CreateInvocationExpression(expression, identifier, lambda, "Select");
                }

                var context = new ConversionContext(identifier, expression);
                return ProcessQueryBody(context, source.Body);
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

            private static ExpressionSyntax ProcessQueryBody(ConversionContext context, QueryBodySyntax queryBody)
            {
                var expression = context.Expression;
                foreach (var queryClause in queryBody.Clauses)
                {
                    switch (queryClause.Kind())
                    {
                        case SyntaxKind.WhereClause: expression = CreateInvocationExpression(expression, context.Identifier, ((WhereClauseSyntax)queryClause).Condition, "Where"); break;
                        case SyntaxKind.OrderByClause: expression = ProcessOrderByClause(expression, context.Identifier, (OrderByClauseSyntax)queryClause); break;
                        case SyntaxKind.JoinClause: // Not supported because linq queries seem to provide essentially simpler syntax for the same than query methods.
                        case SyntaxKind.FromClause: // More than one fromClause is not supported. The linq method seems to be more complicated for this.
                        default: return null;
                    }
                }

                var selectOrGroupClause = queryBody.SelectOrGroup;
                switch (selectOrGroupClause.Kind())
                {
                    case SyntaxKind.SelectClause: expression = ProcesSelectClause(expression, context.Identifier, (SelectClauseSyntax)selectOrGroupClause); break;
                    case SyntaxKind.GroupClause: expression = ProcessGroupClause(expression, context.Identifier, (GroupClauseSyntax)selectOrGroupClause); break;
                    case SyntaxKind.LetClause: return null; // Skip this because the linq method for the let will be more complicated than the query.
                    default: return null;
                }

                if (queryBody.Continuation != null)
                {
                    context = new ConversionContext(queryBody.Continuation.Identifier, expression);
                    return ProcessQueryBody(context, queryBody.Continuation.Body);
                }

                return expression;
            }

            private static ExpressionSyntax ProcessGroupClause(ExpressionSyntax expression, SyntaxToken identifier, GroupClauseSyntax groupClause)
            {
                var parameter = SyntaxFactory.Parameter(identifier);
                var groupExpressionLambda = SyntaxFactory.SimpleLambdaExpression(parameter, groupClause.GroupExpression.WithoutTrailingTrivia());
                var byExpressionLambda = SyntaxFactory.SimpleLambdaExpression(parameter, groupClause.ByExpression.WithoutTrailingTrivia());
                var groupExpressionArgument = SyntaxFactory.Argument(groupExpressionLambda);
                var byExpressionArgument = SyntaxFactory.Argument(byExpressionLambda);
                var arguments = new SeparatedSyntaxList<ArgumentSyntax>().Add(byExpressionArgument).Add(groupExpressionArgument);

                return CreateInvocationExpression(expression, arguments, "GroupBy");
            }

            private static ExpressionSyntax ProcesSelectClause(ExpressionSyntax expression, SyntaxToken identifier, SelectClauseSyntax selectClause)
            {
                var selectExpression = selectClause.Expression;
                // Avoid trivial Select(x => x)
                if (selectExpression is IdentifierNameSyntax identifierName)
                {
                    // TODO consider a better condition to compare
                    if (identifierName.Identifier.Text == identifier.Text)
                    {
                        return expression;
                    }
                }

                return CreateInvocationExpression(expression, identifier, selectClause.Expression, "Select");
            }

            private static ExpressionSyntax CreateInvocationExpression(ExpressionSyntax expression, SyntaxToken identifier, ExpressionSyntax bodyExpression, string keyword)
            {
                var parameter = SyntaxFactory.Parameter(identifier);
                var lambda = SyntaxFactory.SimpleLambdaExpression(parameter, bodyExpression.WithoutTrailingTrivia());
                var argument = SyntaxFactory.Argument(lambda);
                var arguments = new SeparatedSyntaxList<ArgumentSyntax>().Add(argument);

                return CreateInvocationExpression(expression, arguments, keyword);
            }

            private static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax expression, SeparatedSyntaxList<ArgumentSyntax> arguments, string keyword)
            {
                var argumentList = SyntaxFactory.ArgumentList(arguments);
                var identifierName = SyntaxFactory.IdentifierName(keyword);
                var memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, identifierName);
                return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList);
            }

            private static ExpressionSyntax ProcessOrderByClause(ExpressionSyntax expression, SyntaxToken identifier, OrderByClauseSyntax orderByClause)
            {
                bool isFirst = true;
                foreach (var ordering in orderByClause.Orderings)
                {
                    string orderingKeyword;
                    switch (ordering.Kind())
                    {
                        case SyntaxKind.AscendingOrdering: orderingKeyword = isFirst ? "OrderBy" : "ThenBy"; break;
                        case SyntaxKind.DescendingOrdering: orderingKeyword = isFirst ? "OrderByDescending" : "ThenByDescending"; break;
                        default: return null;
                    }
                    expression = CreateInvocationExpression(expression, identifier, ordering.Expression, orderingKeyword);
                    isFirst = false;
                }

                return expression;
            }

            protected override QueryExpressionSyntax FindNodeToRefactor(SyntaxNode root, CodeRefactoringContext context)
            {
                return root.FindNode(context.Span).FirstAncestorOrSelf<QueryExpressionSyntax>();
            }

            private class ConversionContext
            {
                public SyntaxToken Identifier { get; }
                public ExpressionSyntax Expression { get; }

                public ConversionContext(SyntaxToken identifier, ExpressionSyntax expression)
                {
                    Identifier = identifier;
                    Expression = expression;
                }
            }
        }
    }
}
