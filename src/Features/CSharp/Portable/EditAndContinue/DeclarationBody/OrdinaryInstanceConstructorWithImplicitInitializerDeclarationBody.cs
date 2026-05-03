// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// C() { ... }
/// C() => ...
/// </summary>
internal sealed class OrdinaryInstanceConstructorWithImplicitInitializerDeclarationBody(ConstructorDeclarationSyntax constructor)
    : OrdinaryInstanceConstructorDeclarationBody(constructor)
{
    public override SyntaxNode InitializerActiveStatement
        => Constructor;

    public override bool HasExplicitInitializer
        => false;

    public override TextSpan InitializerActiveStatementSpan
        => BreakpointSpans.CreateSpanForImplicitConstructorInitializer(Constructor);

    public override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => model.AnalyzeDataFlow(Body).CapturedInside;

    public override TextSpan Envelope
        => TextSpan.FromBounds(InitializerActiveStatementSpan.Start, Body.Span.End);

    public override IEnumerable<SyntaxToken> GetActiveTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => BreakpointSpans.GetActiveTokensForImplicitConstructorInitializer(Constructor, getDescendantTokens).Concat(getDescendantTokens(Body));

    public override IEnumerable<SyntaxToken> GetUserCodeTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => getDescendantTokens(Body);
}
