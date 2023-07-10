// Licensed to the .NET Foundation under one or more agreements.
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

internal sealed class InstanceConstructorWithExplicitInitializerDeclarationBody(ConstructorDeclarationSyntax constructor)
    : InstanceConstructorDeclarationBody(constructor)
{
    private ConstructorInitializerSyntax Initializer
        => Constructor.Initializer!;

    public override SyntaxNode InitializerActiveStatement
        => Initializer;

    public override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => model.AnalyzeDataFlow(Initializer)!.Captured.AddRange(model.AnalyzeDataFlow(Body).Captured).Distinct();

    public override ActiveStatementEnvelope Envelope
        => TextSpan.FromBounds(BreakpointSpans.CreateSpanForExplicitConstructorInitializer(Initializer).Start, Body.Span.End);

    public override IEnumerable<SyntaxToken> GetActiveTokens()
        => BreakpointSpans.GetActiveTokensForExplicitConstructorInitializer(Initializer).Concat(Body.DescendantTokens());
}
