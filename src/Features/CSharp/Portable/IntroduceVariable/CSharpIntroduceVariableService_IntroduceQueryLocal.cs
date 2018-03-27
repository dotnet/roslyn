// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.IntroduceVariable
{
    internal partial class CSharpIntroduceVariableService
    {
        private static bool IsAnyQueryClause(SyntaxNode node)
        {
            return node is QueryClauseSyntax || node is SelectOrGroupClauseSyntax;
        }

        protected override Task<Document> IntroduceQueryLocalAsync(
            SemanticDocument document, ExpressionSyntax expression, bool allOccurrences, CancellationToken cancellationToken)
        {
            var oldOutermostQuery = expression.GetAncestorsOrThis<QueryExpressionSyntax>().LastOrDefault();

            var newLocalNameToken = GenerateUniqueLocalName(
                document, expression, isConstant: false,
                containerOpt: oldOutermostQuery, cancellationToken: cancellationToken);
            var newLocalName = SyntaxFactory.IdentifierName(newLocalNameToken);

            var letClause = SyntaxFactory.LetClause(
                newLocalNameToken.WithAdditionalAnnotations(RenameAnnotation.Create()),
                expression).WithAdditionalAnnotations(Formatter.Annotation);

            var matches = FindMatches(document, expression, document, oldOutermostQuery, allOccurrences, cancellationToken);
            var innermostClauses = new HashSet<SyntaxNode>(
                matches.Select(expr => expr.GetAncestorsOrThis<SyntaxNode>().First(IsAnyQueryClause)));

            if (innermostClauses.Count == 1)
            {
                // If there was only one match, or all the matches came from the same
                // statement, then we want to place the declaration right above that
                // statement. Note: we special case this because the statement we are going
                // to go above might not be in a block and we may have to generate it
                return Task.FromResult(IntroduceQueryLocalForSingleOccurrence(
                    document, expression, newLocalName, letClause, allOccurrences, cancellationToken));
            }

            var oldInnerMostCommonQuery = matches.FindInnermostCommonNode<QueryExpressionSyntax>();
            var newInnerMostQuery = Rewrite(
                document, expression, newLocalName, document, oldInnerMostCommonQuery, allOccurrences, cancellationToken);

            var allAffectedClauses = new HashSet<SyntaxNode>(matches.SelectMany(expr => expr.GetAncestorsOrThis<SyntaxNode>().Where(IsAnyQueryClause)));

            var oldClauses = oldInnerMostCommonQuery.GetAllClauses();
            var newClauses = newInnerMostQuery.GetAllClauses();

            var firstClauseAffectedInQuery = oldClauses.First(allAffectedClauses.Contains);
            var firstClauseAffectedIndex = oldClauses.IndexOf(firstClauseAffectedInQuery);

            var finalClauses = newClauses.Take(firstClauseAffectedIndex)
                                         .Concat(letClause)
                                         .Concat(newClauses.Skip(firstClauseAffectedIndex)).ToList();

            var finalQuery = newInnerMostQuery.WithAllClauses(finalClauses);
            var newRoot = document.Root.ReplaceNode(oldInnerMostCommonQuery, finalQuery);

            return Task.FromResult(document.Document.WithSyntaxRoot(newRoot));
        }

        private Document IntroduceQueryLocalForSingleOccurrence(
            SemanticDocument document,
            ExpressionSyntax expression,
            NameSyntax newLocalName,
            LetClauseSyntax letClause,
            bool allOccurrences,
            CancellationToken cancellationToken)
        {
            var oldClause = expression.GetAncestors<SyntaxNode>().First(IsAnyQueryClause);
            var newClause = Rewrite(
                document, expression, newLocalName, document, oldClause, allOccurrences, cancellationToken);

            var oldQuery = (QueryBodySyntax)oldClause.Parent;
            var newQuery = GetNewQuery(oldQuery, oldClause, newClause, letClause);

            var newRoot = document.Root.ReplaceNode(oldQuery, newQuery);
            return document.Document.WithSyntaxRoot(newRoot);
        }

        private static QueryBodySyntax GetNewQuery(
            QueryBodySyntax oldQuery,
            SyntaxNode oldClause,
            SyntaxNode newClause,
            LetClauseSyntax letClause)
        {
            var oldClauses = oldQuery.GetAllClauses();
            var oldClauseIndex = oldClauses.IndexOf(oldClause);

            var newClauses = oldClauses.Take(oldClauseIndex)
                                       .Concat(letClause)
                                       .Concat(newClause)
                                       .Concat(oldClauses.Skip(oldClauseIndex + 1)).ToList();
            return oldQuery.WithAllClauses(newClauses);
        }
    }
}
