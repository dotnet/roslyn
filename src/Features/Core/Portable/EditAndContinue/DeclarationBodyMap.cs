// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Differencing;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly struct DeclarationBodyMap
{
    public static readonly DeclarationBodyMap Empty = new(
        SpecializedCollections.EmptyReadOnlyDictionary<SyntaxNode, SyntaxNode>(),
        SpecializedCollections.EmptyReadOnlyDictionary<SyntaxNode, SyntaxNode>(),
        ImmutableDictionary<SyntaxNode, SyntaxNode>.Empty);

    public IReadOnlyDictionary<SyntaxNode, SyntaxNode> Forward { get; }
    public IReadOnlyDictionary<SyntaxNode, SyntaxNode> Reverse { get; }

    public ImmutableDictionary<SyntaxNode, SyntaxNode> AdditionalReverseMapping { get; }

    public DeclarationBodyMap(
        IReadOnlyDictionary<SyntaxNode, SyntaxNode> forwardMatch,
        IReadOnlyDictionary<SyntaxNode, SyntaxNode> reverseMatch,
        ImmutableDictionary<SyntaxNode, SyntaxNode> additionalReverseMapping)
    {
        Contract.ThrowIfFalse(forwardMatch.Count == reverseMatch.Count);

        Forward = forwardMatch;
        Reverse = reverseMatch;
        AdditionalReverseMapping = additionalReverseMapping;
    }

    public static DeclarationBodyMap FromMatch(Match<SyntaxNode> match)
        => new(match.Matches, match.ReverseMatches, ImmutableDictionary<SyntaxNode, SyntaxNode>.Empty);

    public DeclarationBodyMap WithAdditionalMapping(SyntaxNode oldNode, SyntaxNode newNode)
        => new(Forward, Reverse, AdditionalReverseMapping.Add(newNode, oldNode));
}
