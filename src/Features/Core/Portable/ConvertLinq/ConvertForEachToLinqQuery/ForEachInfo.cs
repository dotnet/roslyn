// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery;

internal readonly struct ForEachInfo<TForEachStatement, TStatement>(
    TForEachStatement forEachStatement,
    SemanticModel semanticModel,
    ImmutableArray<ExtendedSyntaxNode> convertingExtendedNodes,
    ImmutableArray<SyntaxToken> identifiers,
    ImmutableArray<TStatement> statements,
    ImmutableArray<SyntaxToken> leadingTokens,
    ImmutableArray<SyntaxToken> trailingTokens)
{
    public TForEachStatement ForEachStatement { get; } = forEachStatement;

    public SemanticModel SemanticModel { get; } = semanticModel;

    public ImmutableArray<ExtendedSyntaxNode> ConvertingExtendedNodes { get; } = convertingExtendedNodes;

    public ImmutableArray<SyntaxToken> Identifiers { get; } = identifiers;

    public ImmutableArray<TStatement> Statements { get; } = statements;

    public ImmutableArray<SyntaxToken> LeadingTokens { get; } = leadingTokens;

    public ImmutableArray<SyntaxToken> TrailingTokens { get; } = trailingTokens;
}
