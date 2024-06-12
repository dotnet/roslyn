// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// Auto-property accessor:
///   T P { [|get;|] }
///   T P { [|set;|] }
///   T P { [|init;|] }
/// </summary>
internal sealed class ExplicitAutoPropertyAccessorDeclarationBody(AccessorDeclarationSyntax accessor) : PropertyOrIndexerAccessorDeclarationBody
{
    public override SyntaxNode? ExplicitBody
        => null;

    public override SyntaxNode? HeaderActiveStatement
        => accessor;

    public override TextSpan HeaderActiveStatementSpan
        => BreakpointSpans.CreateSpanForAutoPropertyAccessor(accessor);

    public override IEnumerable<SyntaxToken>? GetActiveTokens()
        => BreakpointSpans.GetActiveTokensForAutoPropertyAccessor(accessor);

    public override SyntaxNode? MatchRoot
        => null;
}
