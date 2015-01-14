// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class QueryExpressionSyntaxExtensions
    {
        public static IList<SyntaxNode> GetAllClauses(this QueryExpressionSyntax query)
        {
            var result = new List<SyntaxNode>();
            result.Add(query.FromClause);
            result.AddRange(query.Body.Clauses);
            result.Add(query.Body.SelectOrGroup);
            return result;
        }

        public static IList<SyntaxNode> GetAllClauses(this QueryBodySyntax body)
        {
            var result = new List<SyntaxNode>();
            result.AddRange(body.Clauses);
            result.Add(body.SelectOrGroup);
            return result;
        }

        public static QueryExpressionSyntax WithAllClauses(
            this QueryExpressionSyntax query,
            IList<SyntaxNode> allClauses)
        {
            var fromClause = (FromClauseSyntax)allClauses.First();
            return query.WithFromClause(fromClause).WithBody(query.Body.WithAllClauses(allClauses.Skip(1)));
        }

        public static QueryBodySyntax WithAllClauses(
            this QueryBodySyntax body,
            IEnumerable<SyntaxNode> allClauses)
        {
            var clauses = SyntaxFactory.List(allClauses.Take(allClauses.Count() - 1).Cast<QueryClauseSyntax>());
            var selectOrGroup = (SelectOrGroupClauseSyntax)allClauses.Last();
            return body.WithClauses(clauses).WithSelectOrGroup(selectOrGroup);
        }
    }
}
