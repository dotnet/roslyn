// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

internal sealed class TopLevelCodeDeclarationBody(CompilationUnitSyntax unit) : MemberBody
{
    public CompilationUnitSyntax Unit
        => unit;

    private IEnumerable<GlobalStatementSyntax> GlobalStatements
        => unit.Members.OfType<GlobalStatementSyntax>();

    public override SyntaxTree SyntaxTree
        => unit.SyntaxTree;

    public override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => model.AnalyzeDataFlow(((GlobalStatementSyntax)unit.Members[0]).Statement, GlobalStatements.Last().Statement)!.Captured;

    public override ActiveStatementEnvelope Envelope
        => TextSpan.FromBounds(unit.Members[0].SpanStart, GlobalStatements.Last().Span.End);

    public override SyntaxNode EncompassingAncestor
        => unit;

    public override IEnumerable<SyntaxToken>? GetActiveTokens()
        => GlobalStatements.SelectMany(globalStatement => globalStatement.DescendantTokens());

    public override StateMachineInfo GetStateMachineInfo()
    {
        var isAsync = GlobalStatements.Any(static s => SyntaxUtilities.GetSuspensionPoints(s).Any());
        return new StateMachineInfo(IsAsync: isAsync, IsIterator: false, HasSuspensionPoints: isAsync);
    }

    public override OneOrMany<SyntaxNode> RootNodes
        => OneOrMany.Create(GlobalStatements.ToImmutableArray<SyntaxNode>());

    public override Match<SyntaxNode> ComputeMatch(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
        => CSharpEditAndContinueAnalyzer.ComputeBodyMatch(Unit, ((TopLevelCodeDeclarationBody)newBody).Unit, knownMatches);

    public override SyntaxNode FindStatementAndPartner(TextSpan span, MemberBody? partnerDeclarationBody, out SyntaxNode? partnerStatement, out int statementPart)
        => CSharpEditAndContinueAnalyzer.FindStatementAndPartner(
            span,
            body: Unit,
            partnerBody: ((TopLevelCodeDeclarationBody?)partnerDeclarationBody)?.Unit,
            out partnerStatement,
            out statementPart);

    public override bool TryMatchActiveStatement(DeclarationBody newBody, SyntaxNode oldStatement, int statementPart, [NotNullWhen(true)] out SyntaxNode? newStatement)
    {
        newStatement = null;
        return false;
    }
}
