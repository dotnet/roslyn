// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

internal abstract class InstanceConstructorDeclarationBody : MemberBody
{
    public abstract bool HasExplicitInitializer { get; }
    public abstract TextSpan InitializerActiveStatementSpan { get; }
    public abstract SyntaxNode InitializerActiveStatement { get; }
    public abstract SyntaxNode? MatchRoot { get; }

    /// <summary>
    /// Expression or block body.
    /// </summary>
    public abstract SyntaxNode? ExplicitBody { get; }

    /// <summary>
    /// Node that represents the closure that constructor parameters captured within its body are lifted to.
    /// </summary>
    public abstract SyntaxNode? ParameterClosure { get; }

    public sealed override SyntaxTree SyntaxTree
        => InitializerActiveStatement.SyntaxTree;

    public sealed override StateMachineInfo GetStateMachineInfo()
        => StateMachineInfo.None;

    public sealed override SyntaxNode FindStatementAndPartner(TextSpan span, MemberBody? partnerDeclarationBody, out SyntaxNode? partnerStatement, out int statementPart)
    {
        var partnerCtorBody = (InstanceConstructorDeclarationBody?)partnerDeclarationBody;

        if (span == InitializerActiveStatementSpan)
        {
            statementPart = AbstractEditAndContinueAnalyzer.DefaultStatementPart;
            partnerStatement = partnerCtorBody?.InitializerActiveStatement;
            return InitializerActiveStatement;
        }

        if (HasExplicitInitializer && InitializerActiveStatementSpan.Contains(span))
        {
            // If present, partner body does not have any non-trivial changes and thus the initializer is also present.
            Debug.Assert(partnerCtorBody == null || partnerCtorBody.HasExplicitInitializer);

            return CSharpEditAndContinueAnalyzer.FindStatementAndPartner(
                span,
                body: InitializerActiveStatement,
                partnerBody: partnerCtorBody?.InitializerActiveStatement,
                out partnerStatement,
                out statementPart);
        }

        Debug.Assert(ExplicitBody != null);

        // If present, partner body does not have any non-trivial changes and thus the explicit body is also present.
        Debug.Assert(partnerCtorBody == null || partnerCtorBody.ExplicitBody != null);

        return CSharpEditAndContinueAnalyzer.FindStatementAndPartner(
                span,
                body: ExplicitBody,
                partnerBody: partnerCtorBody?.ExplicitBody,
                out partnerStatement,
                out statementPart);
    }

    public sealed override bool TryMatchActiveStatement(DeclarationBody newBody, SyntaxNode oldStatement, ref int statementPart, [NotNullWhen(true)] out SyntaxNode? newStatement)
    {
        var newCtorBody = (InstanceConstructorDeclarationBody)newBody;

        if (oldStatement == InitializerActiveStatement)
        {
            newStatement = newCtorBody.InitializerActiveStatement;
            return true;
        }

        if (ExplicitBody != null && newCtorBody.ExplicitBody != null &&
            CSharpEditAndContinueAnalyzer.TryMatchActiveStatement(ExplicitBody, newCtorBody.ExplicitBody, oldStatement, out newStatement))
        {
            return true;
        }

        if (MatchRoot == null || newCtorBody.MatchRoot == null)
        {
            // General body mapping is not available, so we can't do better then
            // mapping any active statement in this body to the initializer of the other body.
            newStatement = newCtorBody.InitializerActiveStatement;
            return true;
        }

        newStatement = null;
        return false;
    }

    public sealed override Match<SyntaxNode>? ComputeSingleRootMatch(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
        => MatchRoot is { } oldRoot && ((InstanceConstructorDeclarationBody)newBody).MatchRoot is { } newRoot
            ? SyntaxComparer.Statement.ComputeMatch(oldRoot, newRoot, knownMatches)
            : null;

    public override DeclarationBodyMap ComputeMap(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
    {
        var map = base.ComputeMap(newBody, knownMatches);

        // parameter closures are represented by the constructor or type declaration node, which may not be included in the match:
        return ParameterClosure is { } parameterClosure &&
               ((InstanceConstructorDeclarationBody)newBody).ParameterClosure is { } newParameterClosure &&
               !map.Forward.ContainsKey(parameterClosure)
               ? map.WithAdditionalMapping(parameterClosure, newParameterClosure) : map;
    }
}
