// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using Roslyn.Utilities;

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
                    var generic = SyntaxFactory.GenericName(
                        SyntaxFactory.IdentifierName(
                            nameof(Enumerable.Cast)).Identifier,
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(fromClause.Type)));
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
                            expression = CreateInvocationExpression(expression, nameof(Enumerable.Where), CreateLambdaExpressionWithReplacedIdentifiers(((WhereClauseSyntax)queryClause).Condition));
                            break;
                        case SyntaxKind.OrderByClause:
                            expression = ProcessOrderByClause(expression, (OrderByClauseSyntax)queryClause);
                            break;
                        case SyntaxKind.FromClause:
                            expression = ProcessNestedFromClause(expression, ((FromClauseSyntax)queryClause).Expression);
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
                        var groupClause = (GroupClauseSyntax)selectOrGroupClause;
                        expression = CreateInvocationExpression(
                            expression,
                            nameof(Enumerable.GroupBy),
                            new[] { CreateLambdaExpressionWithReplacedIdentifiers(groupClause.GroupExpression), CreateLambdaExpressionWithReplacedIdentifiers(groupClause.ByExpression) });
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

            private ExpressionSyntax ProcessNestedFromClause(ExpressionSyntax expression, ExpressionSyntax bodyExpression)
            {
                var secondArgumentAnonymousFunction = FindAnonymousFunctionsFromParentInvocationOperation(bodyExpression).Last();
                var returnOperation = secondArgumentAnonymousFunction.Body.Operations.First() as IReturnOperation;
                var returnedValue = returnOperation.ReturnedValue;

                CSharpSyntaxNode tupleExpression;
                if (returnedValue.Kind == OperationKind.ObjectCreation)
                {
                    tupleExpression = CreateTupleExpression(returnedValue as IObjectCreationOperation);
                }
                else
                {
                    tupleExpression = ReplaceIdentifierNames(returnOperation.Syntax);
                }

                var secondLambda = CreateLambdaExpression(tupleExpression, secondArgumentAnonymousFunction.Symbol);

                return CreateInvocationExpression(expression, nameof(Enumerable.SelectMany), new[] { CreateLambdaExpressionWithReplacedIdentifiers(bodyExpression), secondLambda });
            }

            private static ExpressionSyntax CreateInvocationExpression(ExpressionSyntax parentExpression, string keyword, ExpressionSyntax argumentExpression)
                => CreateInvocationExpression(parentExpression, keyword, new[] { argumentExpression });

            private static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax parentExpression, string keyword, IEnumerable<ExpressionSyntax> argumentExpressions)
            {
                var argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentExpressions.WhereNotNull().Select(argumentExpression => SyntaxFactory.Argument(argumentExpression))));
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

                return CreateInvocationExpression(expression, nameof(Enumerable.Select), CreateLambdaExpressionWithReplacedIdentifiers(selectClause.Expression));
            }

            private LambdaExpressionSyntax CreateLambdaExpressionWithReplacedIdentifiers(CSharpSyntaxNode body)
            {
                var anonymousFunction = FindParentAnonymousFunction(body);
                if (anonymousFunction == null)
                {
                    return null;
                }

                return CreateLambdaExpression(ReplaceIdentifierNames(body), anonymousFunction.Symbol);
            }

            private LambdaExpressionSyntax CreateLambdaExpression(CSharpSyntaxNode body, IMethodSymbol methodSymbol)
            {
                var parameters = methodSymbol.Parameters.Select(p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(BeautifyName(p.Name))));
                if (parameters.Count() == 1)
                {
                    return SyntaxFactory.SimpleLambdaExpression(parameters.First(), body);
                }
                else
                {
                    return SyntaxFactory.ParenthesizedLambdaExpression(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters)), body);
                }
            }

            private CSharpSyntaxNode CreateTupleExpression(IObjectCreationOperation objectCreation)
            {
                var identifiers = objectCreation.Arguments.Select(arg => SyntaxFactory.IdentifierName(BeautifyName(arg.Parameter.Name)));
                if (identifiers.Count() == 1)
                {
                    return identifiers.First();
                }
                else
                {
                    return SyntaxFactory.TupleExpression(SyntaxFactory.SeparatedList(identifiers.Select(identifier => SyntaxFactory.Argument(identifier))));
                }
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

                    expression = CreateInvocationExpression(expression, orderingKeyword, CreateLambdaExpressionWithReplacedIdentifiers(ordering.Expression));
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

            private CSharpSyntaxNode ReplaceIdentifierNames(SyntaxNode node)
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
