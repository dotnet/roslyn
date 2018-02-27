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
                            expression = ProcessNestedFromClause(expression, ((FromClauseSyntax)queryClause).Expression, nameof(Enumerable.SelectMany));
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

            private ExpressionSyntax ProcessNestedFromClause(ExpressionSyntax expression, ExpressionSyntax bodyExpression, string keyword)
            {
                var expressionOperation = GetOperation(bodyExpression);

                var invocationOperation = FindParentInvocationOperation(expressionOperation);
                var argumentOperations = invocationOperation.Arguments.Where(a => a.Value.Kind == OperationKind.DelegateCreation);
                var firstLambda = CreateLambdaExpression(argumentOperations.First());

                var secondArgumentOperation = argumentOperations.Last();
                var secondArgumentAnonymousFunction = ((argumentOperations.Last()).Value as IDelegateCreationOperation).Target as IAnonymousFunctionOperation;
                var returnedValue = ((secondArgumentAnonymousFunction.Body as IBlockOperation).Operations.First() as IReturnOperation).ReturnedValue;
                LambdaExpressionSyntax secondLambda;
                if (returnedValue.Kind == OperationKind.ObjectCreation)
                {
                    secondLambda = CreateTupleExpression(secondArgumentAnonymousFunction, returnedValue as IObjectCreationOperation);
                }
                else
                {
                    secondLambda = CreateLambdaExpression(secondArgumentOperation);
                }

                return CreateInvocationExpression(expression, new[] { firstLambda, secondLambda }, keyword);
            }

            private static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax parentExpression, IEnumerable<ExpressionSyntax> expressions, string keyword)
            {
                var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(expressions.Select(expression => SyntaxFactory.Argument(expression))));
                var identifierName = SyntaxFactory.IdentifierName(keyword);
                var memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentExpression, identifierName);
                return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList);
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
                var expressionOperation = GetOperation(bodyExpression);
                IEnumerable<ExpressionSyntax> expressions;
                if (expressionOperation != null)
                {
                    var invocationOperation = FindParentInvocationOperation(expressionOperation);
                    expressions = invocationOperation.Arguments.Where(a => a.Value.Kind == OperationKind.DelegateCreation).Select(argument => CreateLambdaExpression(argument));
                }
                else
                {
                    expressions = Enumerable.Empty<ExpressionSyntax>();
                }

                return CreateInvocationExpression(expression, expressions, keyword);
            }

            private LambdaExpressionSyntax CreateLambdaExpression(IArgumentOperation argumentOperation)
            {
                // TODO do we need to check for cast?
                var anonymousFunction = (argumentOperation.Value as IDelegateCreationOperation).Target as IAnonymousFunctionOperation;
                var parameters = CreateParameters(anonymousFunction);
                var body = MakeIdentifierNameReplacements(anonymousFunction.Body.Operations.First().Syntax);
                return CreateLambdaExpression(parameters, body);
            }

            private LambdaExpressionSyntax CreateLambdaExpression(IEnumerable<ParameterSyntax> parameters, CSharpSyntaxNode body)
            {
                if (parameters.Count() == 1)
                {
                    return SyntaxFactory.SimpleLambdaExpression(parameters.First(), body);
                }
                else
                {
                    return SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)), body);
                }
            }

            private LambdaExpressionSyntax CreateTupleExpression(IAnonymousFunctionOperation anonymousFunction, IObjectCreationOperation objectCreation)
            {
                var parameters = CreateParameters(anonymousFunction);
                var identifiers = (objectCreation as IObjectCreationOperation).Arguments.Select(arg => SyntaxFactory.IdentifierName(BeautifyName(arg.Parameter.Name)));

                if (identifiers.Count() == 1)
                {
                    return CreateLambdaExpression(parameters, identifiers.First());
                }
                else
                {
                    var tuple = SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(identifiers.Select(identifier => SyntaxFactory.Argument(identifier))));
                    return CreateLambdaExpression(parameters, tuple);
                }
            }

            private static IEnumerable<ParameterSyntax> CreateParameters(IAnonymousFunctionOperation anonymousFunction)
            {
                return anonymousFunction.Symbol.Parameters.Select(p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(BeautifyName(p.Name))));
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
                return name.Replace("<>h__TransparentIdentifier", "__queryIdentifier");
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
