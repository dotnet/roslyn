// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// Property or indexer accessor with explicit body:
///   T P { get => [|expr;|] }
///   T P { set => [|expr;|] }
///   T P { init => [|expr;|] }
///   T P { get { ... } }
///   T P { set { ... } }
///   T P { init { ... } }
///   T this[...] { get => [|expr;|] }
///   T this[...] { set => [|expr;|] }
///   T this[...] { get { ... } }
///   T this[...] { set { ... } }
///
/// Property or indexer with explicit body:
///   T P => [|expr;|]
///   T this[...] => [|expr;|]
///   
/// Auto-property accessor:
///   T P { [|get;|] }
///   T P { [|set;|] }
///   T P { [|init;|] }
///   
/// Primary record auto-property: 
///   record R([|T P|])
/// </summary>
internal abstract class PropertyOrIndexerAccessorDeclarationBody : MemberBody
{
    /// <summary>
    /// <see cref="ExpressionSyntax"/> or <see cref="BlockSyntax"/> or <see langword="null"/>.
    /// </summary>
    public abstract SyntaxNode? ExplicitBody { get; }

    /// <summary>
    /// Active statement outside of <see cref="ExplicitBody"/>.
    /// </summary>
    public abstract SyntaxNode? HeaderActiveStatement { get; }

    public abstract TextSpan HeaderActiveStatementSpan { get; }

    public abstract SyntaxNode? MatchRoot { get; }

    public SyntaxNode RootNode
        => ExplicitBody ?? HeaderActiveStatement!;

    public sealed override SyntaxTree SyntaxTree
        => RootNode.SyntaxTree;

    public sealed override TextSpan Envelope
        => ExplicitBody?.Span ?? HeaderActiveStatementSpan;

    public sealed override OneOrMany<SyntaxNode> RootNodes
        => new(RootNode);

    public sealed override SyntaxNode EncompassingAncestor
        => RootNode;

    public sealed override StateMachineInfo GetStateMachineInfo()
        => StateMachineInfo.None;

    public sealed override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => (ExplicitBody != null) ? model.AnalyzeDataFlow(ExplicitBody).CapturedInside : [];

    public sealed override SyntaxNode FindStatementAndPartner(TextSpan span, MemberBody? partnerDeclarationBody, out SyntaxNode? partnerStatement, out int statementPart)
    {
        if (HeaderActiveStatement != null)
        {
            Debug.Assert(partnerDeclarationBody is null or PropertyOrIndexerAccessorDeclarationBody { HeaderActiveStatement: not null });

            statementPart = AbstractEditAndContinueAnalyzer.DefaultStatementPart;
            partnerStatement = ((PropertyOrIndexerAccessorDeclarationBody?)partnerDeclarationBody)?.HeaderActiveStatement;
            return HeaderActiveStatement;
        }

        Debug.Assert(ExplicitBody != null);
        Debug.Assert(partnerDeclarationBody is null or PropertyOrIndexerAccessorDeclarationBody { ExplicitBody: not null });

        return CSharpEditAndContinueAnalyzer.FindStatementAndPartner(
            span,
            body: ExplicitBody,
            partnerBody: ((PropertyOrIndexerAccessorDeclarationBody?)partnerDeclarationBody)?.ExplicitBody,
            out partnerStatement,
            out statementPart);
    }

    public sealed override bool TryMatchActiveStatement(DeclarationBody newBody, SyntaxNode oldStatement, ref int statementPart, [NotNullWhen(true)] out SyntaxNode? newStatement)
    {
        var newPropertyBody = (PropertyOrIndexerAccessorDeclarationBody)newBody;

        if (HeaderActiveStatement != null)
        {
            if (oldStatement != HeaderActiveStatement)
            {
                newStatement = null;
                return false;
            }

            if (newPropertyBody.HeaderActiveStatement != null)
            {
                newStatement = newPropertyBody.HeaderActiveStatement;
                return true;
            }

            Contract.ThrowIfNull(newPropertyBody.ExplicitBody);
            (newStatement, statementPart) = CSharpEditAndContinueAnalyzer.GetFirstBodyActiveStatement(newPropertyBody.ExplicitBody);
            return true;
        }

        if (newPropertyBody.HeaderActiveStatement != null)
        {
            newStatement = newPropertyBody.HeaderActiveStatement;
            statementPart = AbstractEditAndContinueAnalyzer.DefaultStatementPart;
            return true;
        }

        Contract.ThrowIfNull(ExplicitBody);
        Contract.ThrowIfNull(newPropertyBody.ExplicitBody);
        Contract.ThrowIfNull(MatchRoot);
        Contract.ThrowIfNull(newPropertyBody.MatchRoot);

        // Statements in explicit bodies will be mapped using a match.
        // We need to special case active statements in root nodes though because the root kinds differ (block vs arrow expr)
        if (oldStatement == ExplicitBody)
        {
            newStatement = newPropertyBody.ExplicitBody;
            statementPart = AbstractEditAndContinueAnalyzer.DefaultStatementPart;
            return true;
        }

        newStatement = null;
        return false;
    }

    public sealed override Match<SyntaxNode>? ComputeSingleRootMatch(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
        => MatchRoot is { } oldRoot && ((PropertyOrIndexerAccessorDeclarationBody)newBody).MatchRoot is { } newRoot
            ? SyntaxComparer.Statement.ComputeMatch(oldRoot, newRoot, knownMatches)
            : null;
}
