// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class MemberAccessExpressionSyntaxExtensions
{
    public static SimpleNameSyntax GetNameWithTriviaMoved(this MemberAccessExpressionSyntax memberAccess)
        => memberAccess.Name
            .WithLeadingTrivia(GetLeadingTriviaForSimplifiedMemberAccess(memberAccess))
            .WithTrailingTrivia(memberAccess.GetTrailingTrivia());

    private static SyntaxTriviaList GetLeadingTriviaForSimplifiedMemberAccess(MemberAccessExpressionSyntax memberAccess)
    {
        // We want to include any user-typed trivia that may be present between the 'Expression', 'OperatorToken' and 'Identifier' of the MemberAccessExpression.
        // However, we don't want to include any elastic trivia that may have been introduced by the expander in these locations. This is to avoid triggering
        // aggressive formatting. Otherwise, formatter will see this elastic trivia added by the expander and use that as a cue to introduce unnecessary blank lines
        // etc. around the user's original code.
        return [.. WithoutElasticTrivia(
            memberAccess.GetLeadingTrivia()
                .AddRange(memberAccess.Expression.GetTrailingTrivia())
                .AddRange(memberAccess.OperatorToken.LeadingTrivia)
                .AddRange(memberAccess.OperatorToken.TrailingTrivia)
                .AddRange(memberAccess.Name.GetLeadingTrivia()))];
    }

    private static IEnumerable<SyntaxTrivia> WithoutElasticTrivia(IEnumerable<SyntaxTrivia> list)
        => list.Where(t => !t.IsElastic());
}
