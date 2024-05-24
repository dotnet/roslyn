// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

internal sealed class SimpleMemberBody(SyntaxNode node) : AbstractSimpleMemberBody(node)
{
    public override SyntaxNode FindStatementAndPartner(TextSpan span, MemberBody? partnerDeclarationBody, out SyntaxNode? partnerStatement, out int statementPart)
        => CSharpEditAndContinueAnalyzer.FindStatementAndPartner(
            span,
            body: Node,
            partnerBody: ((SimpleMemberBody?)partnerDeclarationBody)?.Node,
            out partnerStatement,
            out statementPart);

    public override StateMachineInfo GetStateMachineInfo()
        => new(
            IsAsync: SyntaxUtilities.IsAsyncDeclaration(Node.Parent!),
            IsIterator: SyntaxUtilities.IsIterator(Node),
            HasSuspensionPoints: SyntaxUtilities.GetSuspensionPoints(Node).Any());

    public override Match<SyntaxNode>? ComputeSingleRootMatch(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
        => CSharpEditAndContinueAnalyzer.ComputeBodyMatch(Node, ((SimpleMemberBody)newBody).Node, knownMatches);

    public override bool TryMatchActiveStatement(DeclarationBody newBody, SyntaxNode oldStatement, ref int statementPart, [NotNullWhen(true)] out SyntaxNode? newStatement)
        => CSharpEditAndContinueAnalyzer.TryMatchActiveStatement(Node, ((SimpleMemberBody)newBody).Node, oldStatement, out newStatement);
}
