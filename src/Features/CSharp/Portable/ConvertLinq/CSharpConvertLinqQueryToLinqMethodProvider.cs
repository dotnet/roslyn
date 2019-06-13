// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.ConvertLinq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
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

            protected override string Title => CSharpFeaturesResources.Convert_to_method;

            protected override bool TryConvert(QueryExpressionSyntax source, out ExpressionSyntax result)
            {
                // Do not try refactoring queries with comments or conditional compilation in them.
                // We can consider supporting queries with comments in the future.
                if (source.DescendantTrivia().Any(trivia => trivia.MatchesKind(
                        SyntaxKind.SingleLineCommentTrivia,
                        SyntaxKind.MultiLineCommentTrivia,
                        SyntaxKind.MultiLineDocumentationCommentTrivia) ||
                    source.ContainsDirectives))
                {
                    result = default;
                    return false;
                }

                var fromClause = source.FromClause;
                var expression = fromClause.Expression.WithoutTrailingTrivia();
                if (fromClause.Type != null)
                {
                    var generic = SyntaxFactory.GenericName(
                        SyntaxFactory.IdentifierName(
                            nameof(Enumerable.Cast)).Identifier,
                        SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(fromClause.Type)));
                    result = CreateInvocationExpression(expression, generic);
                    return true;
                }

                // GetDiagnostics is expensive. Move it to the end if there were no bail outs from the algorithm.
                // TODO likely adding more semantic checks will perform checks we expect from GetDiagnostics
                // We may consider removing GetDiagnostics.
                // https://github.com/dotnet/roslyn/issues/25639
                return TryProcessQueryBody(expression, source.Body, out result) &&
                    !_semanticModel.GetDiagnostics(source.Span, _cancellationToken).Any(diagnostic => diagnostic.DefaultSeverity == DiagnosticSeverity.Error);
            }

            private bool TryProcessQueryBody(ExpressionSyntax expression, QueryBodySyntax queryBody, out ExpressionSyntax result)
            {
                result = default;
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
                        case SyntaxKind.JoinClause: // TODO: https://github.com/dotnet/roslyn/issues/25112
                        default:
                            return false;
                    }
                }

                var selectOrGroupClause = queryBody.SelectOrGroup;
                switch (selectOrGroupClause.Kind())
                {
                    case SyntaxKind.SelectClause:
                        var lambda = CreateLambdaExpressionWithReplacedIdentifiers(((SelectClauseSyntax)selectOrGroupClause).Expression);
                        // No need to create select x => x
                        if (lambda != null)
                        {
                            expression = CreateInvocationExpression(expression, nameof(Enumerable.Select), lambda);
                        }

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
                        return false;
                }

                if (queryBody.Continuation != null)
                {
                    return TryProcessQueryBody(expression, queryBody.Continuation.Body, out result);
                }

                result = expression;
                return true;
            }

            private ExpressionSyntax ProcessNestedFromClause(ExpressionSyntax expression, ExpressionSyntax bodyExpression)
            {
                var argumentAnonymousFunctions = FindAnonymousFunctionsFromParentInvocationOperation(bodyExpression);
                Debug.Assert(argumentAnonymousFunctions.Length == 2);
                var secondArgumentAnonymousFunction = argumentAnonymousFunctions.Last();

                Debug.Assert(secondArgumentAnonymousFunction.Body.Operations.Length == 1);
                var returnOperation = (IReturnOperation)secondArgumentAnonymousFunction.Body.Operations.First();
                var returnedValue = returnOperation.ReturnedValue;

                CSharpSyntaxNode tupleExpression;
                if (returnedValue.Kind == OperationKind.ObjectCreation)
                {
                    tupleExpression = CreateTupleExpression((IObjectCreationOperation)returnedValue);
                }
                else
                {
                    tupleExpression = ReplaceIdentifierNames(returnOperation.Syntax);
                }

                var secondLambda = CreateLambdaExpression(tupleExpression, secondArgumentAnonymousFunction.Symbol);
                return CreateInvocationExpression(expression, nameof(Enumerable.SelectMany), new[] { CreateLambdaExpressionWithReplacedIdentifiers(bodyExpression), secondLambda });
            }

            private static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax parentExpression, string keyword, ExpressionSyntax argumentExpression)
                => CreateInvocationExpression(parentExpression, keyword, new[] { argumentExpression });

            private static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax parentExpression, string keyword, IEnumerable<ExpressionSyntax> argumentExpressions)
                => CreateInvocationExpression(parentExpression, SyntaxFactory.IdentifierName(keyword), argumentExpressions);

            private static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax parentExpression, SimpleNameSyntax simpleName, IEnumerable<ExpressionSyntax> argumentExpressions = null)
            {
                var argumentList = argumentExpressions == null ? SyntaxFactory.ArgumentList() : SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(argumentExpressions.WhereNotNull().Select(argumentExpression => SyntaxFactory.Argument(argumentExpression))));
                var memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentExpression, simpleName);
                return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList);
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
                => name.Replace("<>h__TransparentIdentifier", "__queryIdentifier");

            private string GetIdentifierName(IdentifierNameSyntax node)
            {
                var names = GetIdentifierNames(node);
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
                => (CSharpSyntaxNode)new LambdaRewriter(GetIdentifierName).Visit(node);

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
