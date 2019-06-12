// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using Roslyn.Utilities;
using System.Collections.Immutable;
using System.Diagnostics;

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

            protected override string Title => CSharpFeaturesResources.Convert_to_method;

            protected override ExpressionSyntax TryConvert(QueryExpressionSyntax source)
            {
                FromClauseSyntax fromClause = source.FromClause;
                ExpressionSyntax expression = fromClause.Expression.WithoutTrailingTrivia();
                if (fromClause.Type != null)
                {
                    var generic = SyntaxFactory.GenericName(
                        SyntaxFactory.IdentifierName(
                            nameof(Enumerable.Cast)).Identifier,
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(fromClause.Type)));
                    return CreateInvocationExpression(expression, generic);
                }

                return ProcessQueryBody(expression, source.Body);
            }

            private ExpressionSyntax ProcessQueryBody(ExpressionSyntax expression, QueryBodySyntax queryBody)
            {
                foreach (QueryClauseSyntax queryClause in queryBody.Clauses)
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
                        case SyntaxKind.JoinClause: // TODO: https://github.com/dotnet/roslyn/issues/25112
                        default:
                            return null;
                    }
                }

                var selectOrGroupClause = queryBody.SelectOrGroup;
                switch (selectOrGroupClause.Kind())
                {
                    case SyntaxKind.SelectClause:
                        expression = CreateInvocationExpression(expression, nameof(Enumerable.Select), CreateLambdaExpressionWithReplacedIdentifiers(((SelectClauseSyntax)selectOrGroupClause).Expression));
                        break;
                    case SyntaxKind.GroupClause:
                        var groupClause = (GroupClauseSyntax)selectOrGroupClause;
                        expression = CreateInvocationExpression(
                            expression,
                            nameof(Enumerable.GroupBy),
                            new[] { CreateLambdaExpressionWithReplacedIdentifiers(groupClause.GroupExpression), CreateLambdaExpressionWithReplacedIdentifiers(groupClause.ByExpression) });
                        break;
                    case SyntaxKind.LetClause: // TODO: https://github.com/dotnet/roslyn/issues/25112
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
                ImmutableArray<IAnonymousFunctionOperation> argumentAnonymousFunctions = FindAnonymousFunctionsFromParentInvocationOperation(bodyExpression);
                Debug.Assert(argumentAnonymousFunctions.Length == 2);
                IAnonymousFunctionOperation secondArgumentAnonymousFunction = argumentAnonymousFunctions.Last();

                Debug.Assert(secondArgumentAnonymousFunction.Body.Operations.Length == 1);
                IReturnOperation returnOperation = (IReturnOperation)secondArgumentAnonymousFunction.Body.Operations.First();
                IOperation returnedValue = returnOperation.ReturnedValue;

                CSharpSyntaxNode tupleExpression;
                if (returnedValue.Kind == OperationKind.ObjectCreation)
                {
                    tupleExpression = CreateTupleExpression((IObjectCreationOperation)returnedValue);
                }
                else
                {
                    tupleExpression = ReplaceIdentifierNames(returnOperation.Syntax);
                }

                LambdaExpressionSyntax secondLambda = CreateLambdaExpression(tupleExpression, secondArgumentAnonymousFunction.Symbol);
                return CreateInvocationExpression(expression, nameof(Enumerable.SelectMany), new[] { CreateLambdaExpressionWithReplacedIdentifiers(bodyExpression), secondLambda });
            }

            private static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax parentExpression, string keyword, ExpressionSyntax argumentExpression)
                => CreateInvocationExpression(parentExpression, keyword, new[] { argumentExpression });

            private static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax parentExpression, string keyword, IEnumerable<ExpressionSyntax> argumentExpressions)
                => CreateInvocationExpression(parentExpression, SyntaxFactory.IdentifierName(keyword), argumentExpressions);

            private static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax parentExpression, SimpleNameSyntax simpleName, IEnumerable<ExpressionSyntax> argumentExpressions = null)
            {
                ArgumentListSyntax argumentList = argumentExpressions == null ? SyntaxFactory.ArgumentList() : SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentExpressions.WhereNotNull().Select(argumentExpression => SyntaxFactory.Argument(argumentExpression))));
                var memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentExpression, simpleName);
                return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList);
            }

            private ExpressionSyntax ProcesSelectClause(ExpressionSyntax expression, SelectClauseSyntax selectClause)
            {
                return CreateInvocationExpression(expression, nameof(Enumerable.Select), CreateLambdaExpressionWithReplacedIdentifiers(selectClause.Expression));
            }

            private LambdaExpressionSyntax CreateLambdaExpressionWithReplacedIdentifiers(CSharpSyntaxNode body)
            {
                IAnonymousFunctionOperation anonymousFunction = FindParentAnonymousFunction(body);
                if (anonymousFunction == null)
                {
                    return null;
                }

                return CreateLambdaExpression(ReplaceIdentifierNames(body), anonymousFunction.Symbol);
            }

            private LambdaExpressionSyntax CreateLambdaExpression(CSharpSyntaxNode body, IMethodSymbol methodSymbol)
            {
                ParameterSyntax[] parameters = methodSymbol.Parameters.Select(p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(BeautifyName(p.Name)))).ToArray();
                if (parameters.Length == 1)
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
                IdentifierNameSyntax[] identifiers = objectCreation.Arguments.Select(arg => SyntaxFactory.IdentifierName(BeautifyName(arg.Parameter.Name))).ToArray();
                if (identifiers.Length == 1)
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
                foreach (OrderingSyntax ordering in orderByClause.Orderings)
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
                ImmutableArray<string> names = GetIdentifierNames(node);
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
                return (CSharpSyntaxNode)new LambdaRewriter(GetIdentifierName).Visit(node);
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
                    string name = _getNameMethod(node);
                    if (!string.IsNullOrEmpty(name))
                    {
                        node = node.WithIdentifier(SyntaxFactory.Identifier(name));
                    }

                    return base.VisitIdentifierName(node);
                }
            }
        }
    }
}
