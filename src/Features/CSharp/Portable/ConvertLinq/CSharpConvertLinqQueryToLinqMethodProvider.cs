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
                if (fromClause.Type != null)
                {
                    // TODO consider 'from _string_ s in <>'
                    return null;
                }

                var identifier = fromClause.Identifier;
                var context = new ConversionContext(identifier);
                var expression = fromClause.Expression;

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
                if (queryBody.Continuation != null)
                {
                    // TODO
                    return null;
                }

                var expression = parentExpression;
                foreach (var queryClause in queryBody.Clauses)
                {
                    expression = ProcessQueryClause(context, expression, queryClause);
                    if (expression == null)
                    {
                        return null;
                    }
                }

                switch (queryBody.SelectOrGroup.Kind())
                {
                    case SyntaxKind.SelectClause: return ProcesSelectClause(context, expression, (SelectClauseSyntax)queryBody.SelectOrGroup);
                    case SyntaxKind.GroupClause: return null; // TODO
                    case SyntaxKind.LetClause: return null; // Skip this because the linq method will be more complicated than the query.
                    default: throw new NotImplementedException(queryBody.SelectOrGroup.Kind().ToString()); // TODO better exception type
                }
            }

            private ExpressionSyntax ProcesSelectClause(ConversionContext context, ExpressionSyntax parentExpression, SelectClauseSyntax selectClause)
            {
                var expression = selectClause.Expression;
                // Avoid trivial Select(x => x)
                if (expression is IdentifierNameSyntax identifierName)
                {
                    // TODO consider better condition to compare
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
                // TODO why cannot create the list immediately?
                var arguments = new SeparatedSyntaxList<ArgumentSyntax>();
                var arguments1 = arguments.Add(argument);

                var argumentList = SyntaxFactory.ArgumentList(arguments1);

                var selectKeyword = SyntaxFactory.IdentifierName(keyword);
                var memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentExpression, selectKeyword);
                return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList);
            }

            private InvocationExpressionSyntax ProcessQueryClause(ConversionContext context, ExpressionSyntax parentExpression, QueryClauseSyntax queryClause)
            {
                switch (queryClause.Kind())
                {
                    case SyntaxKind.WhereClause: return ProcesQueryClause(context, parentExpression, ((WhereClauseSyntax)queryClause).Condition, "Where");
                    case SyntaxKind.OrderByClause: return ProcessOrderByClause(context, parentExpression, (OrderByClauseSyntax)queryClause);
                    default: throw new NotImplementedException(queryClause.Kind().ToString()); // TODO better exception type
                }
            }

            private InvocationExpressionSyntax ProcessOrderByClause(ConversionContext context, ExpressionSyntax parentExpression, OrderByClauseSyntax orderByClause)
            {
                InvocationExpressionSyntax result = null;
                var expression = parentExpression;
                bool isFirst = true;
                foreach(var ordering in orderByClause.Orderings)
                {
                    // TODO refactor
                    result = ProcesQueryClause(context, parentExpression, ordering.Expression, GetOrderingKeyword(ordering, isFirst));
                    isFirst = false;
                    expression = result;
                }

                return result;
            }

            private string GetOrderingKeyword(OrderingSyntax ordering, bool isFirst)
            {
                switch (ordering.Kind())
                {
                    case SyntaxKind.AscendingOrdering: return isFirst ? "OrderBy" : "ThenBy";
                    case SyntaxKind.DescendingOrdering: return isFirst ? "OrderByDescending" : "ThenByDescending";
                    default: throw new NotImplementedException(ordering.Kind().ToString()); // TODO better exception type
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
