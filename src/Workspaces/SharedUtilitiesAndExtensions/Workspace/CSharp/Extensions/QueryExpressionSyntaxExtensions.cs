// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class QueryExpressionSyntaxExtensions
{
    extension(QueryExpressionSyntax query)
    {
        public IList<SyntaxNode> GetAllClauses()
        {
            var result = new List<SyntaxNode>
        {
            query.FromClause
        };
            result.AddRange(query.Body.Clauses);
            result.Add(query.Body.SelectOrGroup);
            return result;
        }

        public QueryExpressionSyntax WithAllClauses(
            IList<SyntaxNode> allClauses)
        {
            var fromClause = (FromClauseSyntax)allClauses.First();
            return query.WithFromClause(fromClause).WithBody(query.Body.WithAllClauses(allClauses.Skip(1)));
        }
    }

    extension(QueryBodySyntax body)
    {
        public IList<SyntaxNode> GetAllClauses()
        {
            var result = new List<SyntaxNode>();
            result.AddRange(body.Clauses);
            result.Add(body.SelectOrGroup);
            return result;
        }

        public QueryBodySyntax WithAllClauses(
            IEnumerable<SyntaxNode> allClauses)
        {
            var clauses = SyntaxFactory.List(allClauses.Take(allClauses.Count() - 1).Cast<QueryClauseSyntax>());
            var selectOrGroup = (SelectOrGroupClauseSyntax)allClauses.Last();
            return body.WithClauses(clauses).WithSelectOrGroup(selectOrGroup);
        }
    }
}
