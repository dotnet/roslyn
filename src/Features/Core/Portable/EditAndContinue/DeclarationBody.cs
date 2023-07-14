// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Differencing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal abstract class DeclarationBody : IEquatable<DeclarationBody>
{
    public abstract SyntaxTree SyntaxTree { get; }

    /// <summary>
    /// Root nodes of the body. Descendant nodes of these roots include all nodes of the body and no nodes that do not belong to the body.
    /// Note: descendant nodes may include some tokens that are not part of the body.
    /// </summary>
    public abstract OneOrMany<SyntaxNode> RootNodes { get; }

    public abstract StateMachineInfo GetStateMachineInfo();

    /// <summary>
    /// Computes a statement-level syntax tree match of this body with <paramref name="newBody"/>.
    /// TODO: Consider returning <see cref="BidirectionalMap{T}"/> that includes matches of all roots of the body.
    /// <see cref="TryMatchActiveStatement"/> might not be needed if we do so.
    /// </summary>
    public abstract Match<SyntaxNode> ComputeMatch(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches);

    /// <summary>
    /// Matches old active statement node to new active statement node for nodes that are not accounted for in the body match or do not match but should be mapped
    /// (e.g. constructor initializers, opening brace of block body to expression body, etc.).
    /// </summary>
    public abstract bool TryMatchActiveStatement(DeclarationBody newBody, SyntaxNode oldStatement, int statementPart, [NotNullWhen(true)] out SyntaxNode? newStatement);

    public bool Equals(DeclarationBody? other)
        => ReferenceEquals(this, other) ||
           GetType() == other?.GetType() && SequenceEqual(RootNodes, other.RootNodes);

    public override bool Equals(object? obj)
        => Equals(obj as DeclarationBody);

    public override int GetHashCode()
        => RootNodes.First().GetHashCode();

    private static bool SequenceEqual(OneOrMany<SyntaxNode> left, OneOrMany<SyntaxNode> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }
}
