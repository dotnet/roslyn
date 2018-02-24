// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
{
    internal sealed class CSharpConvertLinqQueryToLinqMethodProvider : AbstractConvertLinqQueryToLinqMethodProvider
    {
        protected override IAnalyzer CreateAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken) => new CSharpAnalyzer(semanticModel, cancellationToken);

        private sealed class CSharpAnalyzer : Analyzer<QueryExpressionSyntax, ExpressionSyntax>
        {
            public CSharpAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken)
                : base(semanticModel, cancellationToken)
            {
            }

            protected override string Title => CSharpFeaturesResources.Convert_linq_query_to_linq_method;

            protected override ExpressionSyntax TryConvert(QueryExpressionSyntax source)
            {
                var fromClause = source.FromClause;
                var expression = fromClause.Expression.WithoutTrailingTrivia();
                if (fromClause.Type != null)
                { 
                    var identifier = SyntaxFactory.IdentifierName(nameof(Enumerable.Cast));
                    var generic = SyntaxFactory.GenericName(identifier.Identifier, SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(fromClause.Type)));
                    var memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, generic);
                    return SyntaxFactory.InvocationExpression(memberAccessExpression, SyntaxFactory.ArgumentList());
                }

                return ProcessQueryBody(expression, source.Body);
            }

            private ExpressionSyntax ProcessQueryBody(ExpressionSyntax expression, QueryBodySyntax queryBody)
            {
                foreach (var queryClause in queryBody.Clauses)
                {
                    switch (queryClause.Kind())
                    {
                        case SyntaxKind.WhereClause:
                            expression = CreateInvocationExpression(expression, ((WhereClauseSyntax)queryClause).Condition, nameof(Enumerable.Where));
                            break;
                        case SyntaxKind.OrderByClause:
                            expression = ProcessOrderByClause(expression, (OrderByClauseSyntax)queryClause);
                            break;
                        case SyntaxKind.FromClause:
                            expression = CreateInvocationExpression(expression, ((FromClauseSyntax)queryClause).Expression, nameof(Enumerable.SelectMany));
                            break;
                        case SyntaxKind.JoinClause: // Not supported because linq queries seem to provide essentially simpler syntax for the same than query methods.
                        default:
                            return null;
                    }
                }

                var selectOrGroupClause = queryBody.SelectOrGroup;
                switch (selectOrGroupClause.Kind())
                {
                    case SyntaxKind.SelectClause:
                        expression = ProcesSelectClause(expression, (SelectClauseSyntax)selectOrGroupClause);
                        break;
                    case SyntaxKind.GroupClause:
                        expression = CreateInvocationExpression(expression, ((GroupClauseSyntax)selectOrGroupClause).ByExpression, nameof(Enumerable.GroupBy));
                        break;
                    case SyntaxKind.LetClause: // Skip this because the linq method for the let will be more complicated than the query.
                    default:
                        return null;
                }

                if (queryBody.Continuation != null)
                {
                    return ProcessQueryBody(expression, queryBody.Continuation.Body);
                }

                return expression;
            }

            private ExpressionSyntax ProcesSelectClause(ExpressionSyntax expression, SelectClauseSyntax selectClause)
            {
                var selectExpression = selectClause.Expression;
                // Avoid trivial Select(x => x)
                if (selectExpression is IdentifierNameSyntax identifierName)
                {
                    var identifierNames = GetIdentifierNames(_semanticModel, identifierName, _cancellationToken);
                    if (identifierNames == default || identifierNames.Length == 1)
                    {
                        return expression;
                    }
                }

                return CreateInvocationExpression(expression, selectClause.Expression, nameof(Enumerable.Select));
            }

            private ExpressionSyntax CreateInvocationExpression(ExpressionSyntax expression, ExpressionSyntax bodyExpression, string keyword)
            {
                var expressionOperation = _semanticModel.GetOperation(bodyExpression, _cancellationToken);
                IEnumerable<ArgumentSyntax> arguments;
                if (expressionOperation != null)
                {
                    var invocationOperation = FindParentInvocationOperation(expressionOperation);
                    arguments = invocationOperation.Arguments.Where(a => a.Value.Kind == OperationKind.DelegateCreation).Select(argument => CreateArgument(argument));
                }
                else
                {
                    arguments = Enumerable.Empty<ArgumentSyntax>();
                }

                var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments));
                var identifierName = SyntaxFactory.IdentifierName(keyword);
                var memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, identifierName);
                return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList);
            }

            private ArgumentSyntax CreateArgument(IArgumentOperation argumentOperation)
            {
                // TODO do we need to check for cast?
                var anonymousFunction = (argumentOperation.Value as IDelegateCreationOperation).Target as IAnonymousFunctionOperation;
                var parameters = SyntaxFactory.SeparatedList(anonymousFunction.Symbol.Parameters.Select(p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(BeautifyName(p.Name)))));
                var body = MakeIdentifierNameReplacements(anonymousFunction.Body.Operations.First().Syntax);

                LambdaExpressionSyntax lambdaExpression;
                if (parameters.Count == 1)
                {
                    lambdaExpression = SyntaxFactory.SimpleLambdaExpression(parameters.First(), body);
                }
                else
                {
                    lambdaExpression = SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.ParameterList(parameters), body);
                }

                return SyntaxFactory.Argument(lambdaExpression);
            }

            private ExpressionSyntax ProcessOrderByClause(ExpressionSyntax expression, OrderByClauseSyntax orderByClause)
            {
                bool isFirst = true;
                foreach (var ordering in orderByClause.Orderings)
                {
                    string orderingKeyword;
                    switch (ordering.Kind())
                    {
                        case SyntaxKind.AscendingOrdering:
                            orderingKeyword = isFirst ? nameof(Enumerable.OrderBy) : nameof(Enumerable.ThenBy);
                            break;
                        case SyntaxKind.DescendingOrdering:
                            orderingKeyword = isFirst ? nameof(Enumerable.OrderByDescending) : nameof(Enumerable.ThenByDescending);
                            break;
                        default:
                            return null;
                    }

                    expression = CreateInvocationExpression(expression, ordering.Expression, orderingKeyword);
                    isFirst = false;
                }

                return expression;
            }

            private static string BeautifyName(string name)
            {
                return name.Replace("$", "");
            }

            private string GetIdentifierName(IdentifierNameSyntax node)
            {
                var names = GetIdentifierNames(_semanticModel, node, _cancellationToken);
                if (names.IsDefault)
                {
                    return string.Empty;
                }
                else
                {
                    return BeautifyName(string.Join(".", names));
                }
            }

            private CSharpSyntaxNode MakeIdentifierNameReplacements(SyntaxNode node)
            {
                return new LambdaRewriter(GetIdentifierName).Visit(node) as CSharpSyntaxNode;
            }

            private class LambdaRewriter : CSharpSyntaxRewriter
            {
                private readonly Func<IdentifierNameSyntax, string> _getNameMethod;

                public LambdaRewriter(Func<IdentifierNameSyntax, string> getNameMethod)
                {
                    _getNameMethod = getNameMethod;
                }

                public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
                {
                    var name = _getNameMethod(node);
                    return string.IsNullOrEmpty(name) ? base.VisitIdentifierName(node) : SyntaxFactory.IdentifierName(name);
                }
            }
        }
    }
}
