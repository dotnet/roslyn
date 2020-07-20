// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class StatementSyntaxExtensions
    {
        public static StatementSyntax WithoutLeadingBlankLinesInTrivia(this StatementSyntax statement)
            => statement.WithLeadingTrivia(statement.GetLeadingTrivia().WithoutLeadingBlankLines());

        public static StatementSyntax GetPreviousStatement(this StatementSyntax statement)
        {
            if (statement != null)
            {
                var previousToken = statement.GetFirstToken().GetPreviousToken();
                return previousToken.GetAncestors<StatementSyntax>().FirstOrDefault(s => s.Parent == statement.Parent);
            }

            return null;
        }

        public static StatementSyntax GetNextStatement(this StatementSyntax statement)
        {
            if (statement != null)
            {
                var nextToken = statement.GetLastToken().GetNextToken();
                return nextToken.GetAncestors<StatementSyntax>().FirstOrDefault(s => s.Parent == statement.Parent || AreInSiblingTopLevelStatements(s, statement));
            }

            return null;

            static bool AreInSiblingTopLevelStatements(StatementSyntax one, StatementSyntax other)
            {
                return one.IsParentKind(SyntaxKind.GlobalStatement) &&
                    other.IsParentKind(SyntaxKind.GlobalStatement);
            }
        }
    }
}
