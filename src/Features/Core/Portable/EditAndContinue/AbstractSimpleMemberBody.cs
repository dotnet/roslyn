// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal abstract class AbstractSimpleMemberBody(SyntaxNode node) : MemberBody
{
    public SyntaxNode Node
        => node;

    public sealed override SyntaxTree SyntaxTree
        => node.SyntaxTree;

    public sealed override OneOrMany<SyntaxNode> RootNodes
        => OneOrMany.Create(node);

    public sealed override TextSpan Envelope
        => node.Span;

    public sealed override SyntaxNode EncompassingAncestor
        => node;

    public sealed override IEnumerable<SyntaxToken>? GetActiveTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => getDescendantTokens(node);

    public override IEnumerable<SyntaxToken> GetUserCodeTokens(Func<SyntaxNode, IEnumerable<SyntaxToken>> getDescendantTokens)
        => getDescendantTokens(node);

    public override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => model.AnalyzeDataFlow(Node).CapturedInside;
}
