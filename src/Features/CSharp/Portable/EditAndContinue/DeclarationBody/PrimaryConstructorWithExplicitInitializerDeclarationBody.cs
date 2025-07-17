// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// Breakpoint spans:
/// 
/// class C(int a, int b) : [|B(expr)|];
/// </summary>
internal sealed class PrimaryConstructorWithExplicitInitializerDeclarationBody(TypeDeclarationSyntax typeDeclaration)
    : PrimaryConstructorDeclarationBody(typeDeclaration)
{
    public PrimaryConstructorBaseTypeSyntax Initializer
        => (PrimaryConstructorBaseTypeSyntax)TypeDeclaration.BaseList!.Types[0];

    public override bool HasExplicitInitializer
        => true;

    public override SyntaxNode InitializerActiveStatement
        => Initializer;

    public override TextSpan InitializerActiveStatementSpan
        => BreakpointSpans.CreateSpanForExplicitPrimaryConstructorInitializer(Initializer);

    public override SyntaxNode? MatchRoot
        => Initializer;

    public override IEnumerable<SyntaxToken>? GetActiveTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => BreakpointSpans.GetActiveTokensForExplicitPrimaryConstructorInitializer(Initializer, getDescendantTokens);

    public override IEnumerable<SyntaxToken> GetUserCodeTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => getDescendantTokens(Initializer);

    public sealed override SyntaxNode EncompassingAncestor
        => Initializer;

    public override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => model.AnalyzeDataFlow(Initializer)!.CapturedInside;
}
