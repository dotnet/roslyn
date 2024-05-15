// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// record [|C{T}|](int a, int b);
/// </summary>
internal sealed class CopyConstructorDeclarationBody(RecordDeclarationSyntax recordDeclaration) : InstanceConstructorDeclarationBody
{
    public override bool HasExplicitInitializer
        => false;

    public override SyntaxNode? ExplicitBody
        => null;

    public override SyntaxNode? ParameterClosure
        => null;

    public override SyntaxNode? MatchRoot
        => null;

    public override SyntaxNode InitializerActiveStatement
        => recordDeclaration;

    public override TextSpan InitializerActiveStatementSpan
        => BreakpointSpans.CreateSpanForCopyConstructor(recordDeclaration);

    public override OneOrMany<SyntaxNode> RootNodes
        => new(recordDeclaration);

    public override SyntaxNode EncompassingAncestor
        => recordDeclaration;

    public override TextSpan Envelope
        => InitializerActiveStatementSpan;

    public override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => [];

    public override IEnumerable<SyntaxToken>? GetActiveTokens()
        => BreakpointSpans.GetActiveTokensForCopyConstructor(recordDeclaration);
}
