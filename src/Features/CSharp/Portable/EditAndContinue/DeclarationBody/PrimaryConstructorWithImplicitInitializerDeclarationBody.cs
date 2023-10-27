// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// Breakpoint spans:
/// 
/// class [|C(int a, int b)|] : B;
/// </summary>
internal sealed class PrimaryConstructorWithImplicitInitializerDeclarationBody(TypeDeclarationSyntax typeDeclaration)
    : PrimaryConstructorDeclarationBody(typeDeclaration)
{
    public ParameterListSyntax ParameterList
        => TypeDeclaration.ParameterList!;

    public override bool HasExplicitInitializer
        => false;

    public override SyntaxNode InitializerActiveStatement
        => ParameterList;

    public override TextSpan InitializerActiveStatementSpan
        => BreakpointSpans.CreateSpanForImplicitPrimaryConstructorInitializer(TypeDeclaration);

    public override SyntaxNode? MatchRoot
        => null;

    public override IEnumerable<SyntaxToken>? GetActiveTokens()
        => BreakpointSpans.GetActiveTokensForImplicitPrimaryConstructorInitializer(TypeDeclaration);

    public sealed override SyntaxNode EncompassingAncestor
        => TypeDeclaration;

    public override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => ImmutableArray<ISymbol>.Empty;

    // Active spans of copy-constructor and primary record properties overlap with the primary constructor initializer span,
    // but do not belong to the primary constructor body. The only active span that belongs is the initializer span itself.
    public override bool IsExcludedActiveStatementSpanWithinEnvelope(TextSpan span)
       => span != InitializerActiveStatementSpan;
}
