// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class BlockSyntaxExtensions
    {
        public static bool TryConvertToExpressionBody(
            this BlockSyntax? block,
            LanguageVersion languageVersion,
            ExpressionBodyPreference preference,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out ExpressionSyntax? expression,
            out SyntaxToken semicolonToken)
        {
            if (preference != ExpressionBodyPreference.Never &&
                block is { Statements: [var statement] } &&
                TryGetExpression(statement, languageVersion, out expression, out semicolonToken) &&
                MatchesPreference(expression, preference))
            {
                // If there are ifdef'ed sections of code below the single statement, then we can't convert.
                if (!block.CloseBraceToken.LeadingTrivia.Any(IsAnyCodeDirective))
                {
                    // We can have an ifdef'ed section around the statement, as long as each segment of the ifdef
                    // contains an expression-statement or throw-statement.
                    if (HasAcceptableDirectiveShape(statement))
                    {
                        // The close brace of the block may have important trivia on it (like 
                        // comments or directives).  Preserve them on the semicolon when we
                        // convert to an expression body.
                        semicolonToken = semicolonToken.WithAppendedTrailingTrivia(
                            block.CloseBraceToken.LeadingTrivia.Where(t => !t.IsWhitespaceOrEndOfLine()));
                        return true;
                    }
                }
            }

            expression = null;
            semicolonToken = default;
            return false;

            static bool IsAnyCodeDirective(SyntaxTrivia trivia)
                => trivia.Kind() is SyntaxKind.IfDirectiveTrivia or SyntaxKind.ElifDirectiveTrivia or SyntaxKind.ElseDirectiveTrivia or SyntaxKind.EndIfDirectiveTrivia;

            bool HasAcceptableDirectiveShape(StatementSyntax statement)
            {
                var leadingDirectives = statement.GetLeadingTrivia().Where(IsAnyCodeDirective).ToImmutableArray();
                if (leadingDirectives.Length == 0)
                    return true;

                // Ok, we have some if/elif/else/endif pp directives above us.  If we're one of hte branches, and all
                // the rest of the branches are ok as well, we can convert this.

                if (leadingDirectives.Any(t => t.Kind() == SyntaxKind.EndIfDirectiveTrivia))
                    return false;

                var firstDirective = (DirectiveTriviaSyntax)leadingDirectives.First().GetStructure()!;
                var conditionalDirectives = firstDirective.GetMatchingConditionalDirectives(cancellationToken);

                // The sequence of conditionals have to all be within the method body.
                if (conditionalDirectives.First().SpanStart <= block.OpenBraceToken.SpanStart ||
                    conditionalDirectives.Last().Span.End >= block.CloseBraceToken.Span.End)
                {
                    return false;
                }

                // Last directive has to come after our statement.
                if (conditionalDirectives.Last().Span.End <= statement.Span.Start)
                    return false;

                // Now, check each part of the conditional chain
                foreach (var conditionalDirective in conditionalDirectives)
                {
                    var parentTrivia = conditionalDirective.ParentTrivia;
                    var parentToken = parentTrivia.Token;
                    var triviaIndex = parentToken.LeadingTrivia.IndexOf(parentTrivia);
                    if (triviaIndex + 1 < parentToken.LeadingTrivia.Count)
                    {
                        var nextTrivia = parentToken.LeadingTrivia[triviaIndex + 1];
                        if (nextTrivia.Kind() == SyntaxKind.DisabledTextTrivia)
                        {
                            // This was a conditional before a disabled section.  Parse out the disabled section and make
                            // sure it can legally become the body of a expression-bodied member.
                            var parsed = SyntaxFactory.ParseStatement(nextTrivia.ToFullString());
                            if (parsed.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error))
                                return false;

                            if (parsed is not ExpressionStatementSyntax and not ThrowStatementSyntax { Expression: not null })
                                return false;
                        }
                    }
                }

                return true;
            }
        }

        public static bool TryConvertToArrowExpressionBody(
            this BlockSyntax block,
            SyntaxKind declarationKind,
            LanguageVersion languageVersion,
            ExpressionBodyPreference preference,
            CancellationToken cancellationToken,
            [NotNullWhen(true)] out ArrowExpressionClauseSyntax? arrowExpression,
            out SyntaxToken semicolonToken)
        {
            // We can always use arrow-expression bodies in C# 7 or above.
            // We can also use them in C# 6, but only a select set of member kinds.
            var acceptableVersion =
                languageVersion >= LanguageVersion.CSharp7 ||
                (languageVersion >= LanguageVersion.CSharp6 && IsSupportedInCSharp6(declarationKind));

            if (acceptableVersion &&
                block.TryConvertToExpressionBody(languageVersion, preference, cancellationToken, out var expression, out semicolonToken))
            {
                arrowExpression = SyntaxFactory.ArrowExpressionClause(expression);

                var parent = block.GetRequiredParent();

                if (parent.Kind() == SyntaxKind.GetAccessorDeclaration)
                {
                    var comments = parent.GetLeadingTrivia().Where(t => !t.IsWhitespaceOrEndOfLine());
                    if (!comments.IsEmpty())
                    {
                        arrowExpression = arrowExpression.WithLeadingTrivia(
                            parent.GetLeadingTrivia());
                    }
                }

                return true;
            }

            arrowExpression = null;
            semicolonToken = default;
            return false;
        }

        private static bool IsSupportedInCSharp6(SyntaxKind declarationKind)
        {
            switch (declarationKind)
            {
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                    return false;
            }

            return true;
        }

        public static bool MatchesPreference(
            ExpressionSyntax expression, ExpressionBodyPreference preference)
        {
            if (preference == ExpressionBodyPreference.WhenPossible)
            {
                return true;
            }

            Contract.ThrowIfFalse(preference == ExpressionBodyPreference.WhenOnSingleLine);
            return CSharpSyntaxFacts.Instance.IsOnSingleLine(expression, fullSpan: false);
        }

        private static bool TryGetExpression(StatementSyntax firstStatement, LanguageVersion languageVersion, [NotNullWhen(true)] out ExpressionSyntax? expression, out SyntaxToken semicolonToken)
        {
            if (firstStatement is ExpressionStatementSyntax exprStatement)
            {
                expression = exprStatement.Expression;
                semicolonToken = exprStatement.SemicolonToken;
                return true;
            }
            else if (firstStatement is ReturnStatementSyntax returnStatement)
            {
                if (returnStatement.Expression != null)
                {
                    // If there are any comments or directives on the return keyword, move them to
                    // the expression.
                    expression = firstStatement.GetLeadingTrivia().Any(t => t.IsDirective || t.IsSingleOrMultiLineComment())
                        ? returnStatement.Expression.WithLeadingTrivia(returnStatement.GetLeadingTrivia())
                        : returnStatement.Expression;
                    semicolonToken = returnStatement.SemicolonToken;
                    return true;
                }
            }
            else if (firstStatement is ThrowStatementSyntax throwStatement)
            {
                if (languageVersion >= LanguageVersion.CSharp7 && throwStatement.Expression != null)
                {
                    expression = SyntaxFactory.ThrowExpression(throwStatement.ThrowKeyword, throwStatement.Expression);
                    semicolonToken = throwStatement.SemicolonToken;
                    return true;
                }
            }

            expression = null;
            semicolonToken = default;
            return false;
        }
    }
}
