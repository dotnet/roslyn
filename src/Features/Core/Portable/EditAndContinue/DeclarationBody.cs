// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Differencing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal abstract class DeclarationBody : IEquatable<DeclarationBody>
{
    public abstract SyntaxTree SyntaxTree { get; }

    /// <summary>
    /// Root nodes of the body. Descendant nodes of these roots include all nodes of the body and no nodes that do not belong to the body.
    /// Note: descendant nodes may include some tokens that are not part of the body and exclude some tokens that are.
    /// </summary>
    public abstract OneOrMany<SyntaxNode> RootNodes { get; }

    /// <summary>
    /// Returns all nodes of the body.
    /// </summary>
    /// <remarks>
    /// Note that VB lambda bodies are represented by a lambda header and that some lambda bodies share 
    /// their parent nodes with other bodies (e.g. join clause expressions).
    /// </remarks>
    public virtual IEnumerable<SyntaxNode> GetExpressionsAndStatements()
    {
        foreach (var root in RootNodes)
        {
            yield return root;
        }
    }

    public IEnumerable<SyntaxNode> GetDescendantNodes(Func<SyntaxNode, bool> descendIntoChildren)
    {
        foreach (var root in GetExpressionsAndStatements())
        {
            foreach (var node in root.DescendantNodesAndSelf(descendIntoChildren))
            {
                yield return node;
            }
        }
    }

    /// <summary>
    /// <see cref="SyntaxNode"/> that includes all active tokens (<see cref="MemberBody.GetActiveTokens"/>)
    /// and its span covers the entire <see cref="MemberBody.Envelope"/>.
    /// May include descendant nodes or tokens that do not belong to the body.
    /// </summary>
    public abstract SyntaxNode EncompassingAncestor { get; }

    public abstract StateMachineInfo GetStateMachineInfo();

    /// <summary>
    /// Analyzes data flow in the member body represented by the specified node and returns all captured variables and parameters (including "this").
    /// If the body is a field/property initializer analyzes the initializer expression only.
    /// </summary>
    public abstract ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model);

    /// <summary>
    /// Computes a statement-level syntax tree match of this body with <paramref name="newBody"/>.
    /// </summary>
    public virtual DeclarationBodyMap ComputeMap(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
    {
        var primaryMatch = ComputeSingleRootMatch(newBody, knownMatches);
        return (primaryMatch != null) ? DeclarationBodyMap.FromMatch(primaryMatch) : DeclarationBodyMap.Empty;
    }

    /// <summary>
    /// If both this body and <paramref name="newBody"/> have single roots, computes a statement-level syntax tree match rooted in these roots.
    /// Otherwise, returns null (e.g. a primary constructor with implicit initializer does not have any body to match).
    /// </summary>
    public abstract Match<SyntaxNode>? ComputeSingleRootMatch(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches);

    /// <summary>
    /// Matches old active statement node to new active statement node for nodes that are not accounted for in the body match or do not match but should be mapped
    /// (e.g. constructor initializers, opening brace of block body to expression body, etc.).
    /// </summary>
    public abstract bool TryMatchActiveStatement(DeclarationBody newBody, SyntaxNode oldStatement, ref int statementPart, [NotNullWhen(true)] out SyntaxNode? newStatement);

    public bool Equals(DeclarationBody? other)
        => ReferenceEquals(this, other) ||
           GetType() == other?.GetType() && RootNodes.SequenceEqual(other.RootNodes);

    public override bool Equals(object? obj)
        => Equals(obj as DeclarationBody);

    public override int GetHashCode()
        => RootNodes.First().GetHashCode();
}
