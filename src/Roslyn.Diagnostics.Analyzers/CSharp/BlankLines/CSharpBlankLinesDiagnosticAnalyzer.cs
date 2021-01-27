// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Diagnostics.Analyzers.BlankLines;

namespace Roslyn.Diagnostics.CSharp.Analyzers.BlankLines
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpBlankLinesDiagnosticAnalyzer : AbstractBlankLinesDiagnosticAnalyzer
    {
        protected override bool IsEndOfLine(SyntaxTrivia trivia)
            => trivia.IsKind(SyntaxKind.EndOfLineTrivia);

        public static bool StatementNeedsWrapping(StatementSyntax statement)
        {
            // Statement has to be parented by another statement (or an else-clause) to count.
            var parent = statement.Parent;
            var parentIsElseClause = parent.IsKind(SyntaxKind.ElseClause);

            if (!(parent is StatementSyntax || parentIsElseClause))
                return false;

            // `else if` is always allowed.
            if (statement.IsKind(SyntaxKind.IfStatement) && parentIsElseClause)
                return false;

            if (parent.IsKind(SyntaxKind.Block))
            {
                // Blocks can be on a single line if parented by a member/accessor/lambda.
                var blockParent = parent.Parent;
                if (blockParent is MemberDeclarationSyntax or
                    AccessorDeclarationSyntax or
                    AnonymousFunctionExpressionSyntax)
                {
                    return false;
                }
            }

            var statementStartToken = statement.GetFirstToken();
            var previousToken = statementStartToken.GetPreviousToken();

            // we have to have a newline between the start of this statement and the previous statement.
            if (ContainsEndOfLine(previousToken.TrailingTrivia) ||
                ContainsEndOfLine(statementStartToken.LeadingTrivia))
            {
                return false;
            }

            return true;
        }

        private static bool ContainsEndOfLine(SyntaxTriviaList triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    return true;
            }

            return false;
        }
    }
}
