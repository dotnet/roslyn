﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

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
        => model.AnalyzeDataFlow(Body).Captured;

    public override TextSpan Envelope
        => TextSpan.FromBounds(InitializerActiveStatementSpan.Start, Body.Span.End);

    public override IEnumerable<SyntaxToken> GetActiveTokens()
        => BreakpointSpans.GetActiveTokensForImplicitConstructorInitializer(Constructor).Concat(Body.DescendantTokens());
}
