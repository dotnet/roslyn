// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// Property accessor with explicit body:
///   T P { get => [|expr;|] }
///   T P { set => [|expr;|] }
///   T P { init => [|expr;|] }
///   T P { get { ... } }
///   T P { set { ... } }
///   T P { init { ... } }
///   T this[...] { get => [|expr;|] }
///   T this[...] { set => [|expr;|] }
///   T this[...] { get { ... } }
///   T this[...] { set { ... } }
/// </summary>
internal sealed class PropertyOrIndexerAccessorWithExplicitBodyDeclarationBody(AccessorDeclarationSyntax accessor) : PropertyOrIndexerAccessorDeclarationBody
{
    public SyntaxNode Body
        => (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody!.Expression;

    public override SyntaxNode? ExplicitBody
        => Body;

    public override SyntaxNode? HeaderActiveStatement
        => null;

    public override TextSpan HeaderActiveStatementSpan
        => default;

    public override SyntaxNode? MatchRoot
        => (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody;

    public sealed override IEnumerable<SyntaxToken>? GetActiveTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => getDescendantTokens(Body);

    public override IEnumerable<SyntaxToken> GetUserCodeTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => getDescendantTokens(Body);
}
