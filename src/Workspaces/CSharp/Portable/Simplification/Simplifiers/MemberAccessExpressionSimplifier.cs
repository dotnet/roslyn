// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification.Simplifiers;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal class MemberAccessExpressionSimplifier : AbstractMemberAccessExpressionSimplifier<
        ExpressionSyntax,
        MemberAccessExpressionSyntax,
        ThisExpressionSyntax>
    {
        public static readonly MemberAccessExpressionSimplifier Instance = new();

        private MemberAccessExpressionSimplifier()
        {
        }

        protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

        protected override ISpeculationAnalyzer GetSpeculationAnalyzer(
            SemanticModel semanticModel, MemberAccessExpressionSyntax memberAccessExpression, CancellationToken cancellationToken)
        {
            return new SpeculationAnalyzer(memberAccessExpression, memberAccessExpression.Name, semanticModel, cancellationToken);
        }

        protected override bool MayCauseParseDifference(MemberAccessExpressionSyntax memberAccessExpression)
            => ParserWouldTreatReplacementWithNameAsCast(memberAccessExpression);

        public static bool ParserWouldTreatReplacementWithNameAsCast(
            MemberAccessExpressionSyntax memberAccessExpression)
        {
            SyntaxNode parent = memberAccessExpression;
            while (true)
            {
                if (parent.IsParentKind(SyntaxKind.SimpleMemberAccessExpression))
                {
                    parent = parent.GetRequiredParent();
                    continue;
                }

                if (!parent.IsParentKind(SyntaxKind.ParenthesizedExpression))
                    return false;

                break;
            }

            // To resolve cast_expression ambiguities, the following rule exists: A sequence of one or more tokens
            // (§6.4) enclosed in parentheses is considered the start of a cast_expression only if at least one of the
            // following are true:

            // The sequence of tokens is correct grammar for a type, but not for an expression.
            //
            // The sequence of tokens is correct grammar for a type, and the token immediately following the closing
            // parentheses is the token “~”, the token “!”, the token “(”, an identifier(§6.4.3), a literal(§6.4.5), or
            // any keyword(§6.4.4) except as and is.

            // Note: the first cannot be true here.  Because we started with a MemberAccessExpression that we are
            // replacing with it's 'name' portion, this will always be valid as an expression.  So what matters is the
            // second statement.

            var parenthesizedExpression = parent.GetRequiredParent();
            var nextToken = parenthesizedExpression.GetLastToken().GetNextToken();

            if ((nextToken.Kind() is SyntaxKind.TildeToken or SyntaxKind.ExclamationToken or SyntaxKind.OpenParenToken) ||
                (CSharp.SyntaxFacts.IsKeywordKind(nextToken.Kind()) && nextToken.Kind() is not SyntaxKind.AsKeyword and not SyntaxKind.IsKeyword))
            {
                // This could definitely end up looking like a cast.  See if `The sequence of tokens is correct grammar
                // for a type` holds true here. Note: this check is likely not super accurate.  It probably is missing
                // cases with things like array-syntax or alias-syntax.  But it's likely sufficient for the common case
                // of `this.A.B.C` becoming `A.B.C` which then looks like a type name.
                return IsEntirelySimpleNames(parent.ReplaceNode(memberAccessExpression, memberAccessExpression.Name));
            }

            return false;
        }

        private static bool IsEntirelySimpleNames(SyntaxNode node)
        {
            return node is MemberAccessExpressionSyntax(SyntaxKind.SimpleMemberAccessExpression) memberAccess
                ? IsEntirelySimpleNames(memberAccess.Expression)
                : node is SimpleNameSyntax;
        }
    }
}
