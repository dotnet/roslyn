// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

internal sealed class CSharpLambdaBody(SyntaxNode node) : LambdaBody
{
    public SyntaxNode Node
        => node;

    public sealed override SyntaxTree SyntaxTree
        => node.SyntaxTree;

    public override OneOrMany<SyntaxNode> RootNodes
        => OneOrMany.Create(node);

    public override SyntaxNode EncompassingAncestor
        => node;

    public override StateMachineInfo GetStateMachineInfo()
        => new(
            IsAsync: SyntaxUtilities.IsAsyncDeclaration(node.Parent!),
            IsIterator: SyntaxUtilities.IsIterator(node),
            HasSuspensionPoints: SyntaxUtilities.GetSuspensionPoints(node).Any());

    public override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => model.AnalyzeDataFlow(node).CapturedInside;

    public override Match<SyntaxNode>? ComputeSingleRootMatch(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
        => CSharpEditAndContinueAnalyzer.ComputeBodyMatch(node, ((CSharpLambdaBody)newBody).Node, knownMatches);

    public override DeclarationBodyMap ComputeMap(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
    {
        var map = base.ComputeMap(newBody, knownMatches);

        // Include the lambda body mapping if it hasn't been matched.
        // This happens with the lambda body is an expression body.
        // The expression body may be a closure scope and thus needs to be included in the map.
        return map.Forward.ContainsKey(node) ? map : map.WithAdditionalMapping(node, ((CSharpLambdaBody)newBody).Node);
    }

    public override bool TryMatchActiveStatement(DeclarationBody newBody, SyntaxNode oldStatement, ref int statementPart, [NotNullWhen(true)] out SyntaxNode? newStatement)
        => CSharpEditAndContinueAnalyzer.TryMatchActiveStatement(Node, ((CSharpLambdaBody)newBody).Node, oldStatement, out newStatement);

    public override LambdaBody? TryGetPartnerLambdaBody(SyntaxNode newLambda)
        => LambdaUtilities.TryGetCorrespondingLambdaBody(node, newLambda) is { } newNode ? new CSharpLambdaBody(newNode) : null;

    public override IEnumerable<SyntaxNode> GetExpressionsAndStatements()
        => [node];

    public override SyntaxNode GetLambda()
        => LambdaUtilities.GetLambda(node);

    public override bool IsSyntaxEquivalentTo(LambdaBody other)
        => SyntaxFactory.AreEquivalent(node, ((CSharpLambdaBody)other).Node);
}
