// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// Breakpoint spans:
/// 
/// [|public int a = expr;|]
/// [|public int a = expr|], [|b = expr|];
/// </summary>
internal sealed class FieldWithInitializerDeclarationBody(VariableDeclaratorSyntax variableDeclarator) : MemberBody
{
    public ExpressionSyntax InitializerExpression
        => variableDeclarator.Initializer!.Value;

    private BaseFieldDeclarationSyntax GetFieldDeclaration()
        => (BaseFieldDeclarationSyntax)variableDeclarator.Parent!.Parent!;

    public override SyntaxTree SyntaxTree
        => variableDeclarator.SyntaxTree;

    public override ImmutableArray<ISymbol> GetCapturedVariables(SemanticModel model)
        => model.AnalyzeDataFlow(InitializerExpression)!.CapturedInside;

    public override TextSpan Envelope
    {
        get
        {
            var fieldDeclaration = GetFieldDeclaration();
            return BreakpointSpans.CreateSpanForVariableDeclarator(variableDeclarator, fieldDeclaration.Modifiers, fieldDeclaration.SemicolonToken);
        }
    }

    public override SyntaxNode EncompassingAncestor
        => GetFieldDeclaration();

    public override IEnumerable<SyntaxToken> GetActiveTokens()
    {
        var fieldDeclaration = GetFieldDeclaration();
        return BreakpointSpans.GetActiveTokensForVariableDeclarator(variableDeclarator, fieldDeclaration.Modifiers, fieldDeclaration.SemicolonToken);
    }

    public override StateMachineInfo GetStateMachineInfo()
        => new(IsAsync: false, IsIterator: false, HasSuspensionPoints: false);

    public override OneOrMany<SyntaxNode> RootNodes
        => OneOrMany.Create<SyntaxNode>(InitializerExpression);

    public override Match<SyntaxNode>? ComputeSingleRootMatch(DeclarationBody newBody, IEnumerable<KeyValuePair<SyntaxNode, SyntaxNode>>? knownMatches)
        => CSharpEditAndContinueAnalyzer.ComputeBodyMatch(InitializerExpression, ((FieldWithInitializerDeclarationBody)newBody).InitializerExpression, knownMatches);

    public override bool TryMatchActiveStatement(DeclarationBody newBody, SyntaxNode oldStatement, ref int statementPart, [NotNullWhen(true)] out SyntaxNode? newStatement)
    {
        if (oldStatement == InitializerExpression)
        {
            newStatement = ((FieldWithInitializerDeclarationBody)newBody).InitializerExpression;
            return true;
        }

        newStatement = null;
        return false;
    }

    public override SyntaxNode FindStatementAndPartner(TextSpan span, MemberBody? partnerDeclarationBody, out SyntaxNode? partnerStatement, out int statementPart)
        => CSharpEditAndContinueAnalyzer.FindStatementAndPartner(
            span,
            body: InitializerExpression,
            partnerBody: ((FieldWithInitializerDeclarationBody?)partnerDeclarationBody)?.InitializerExpression,
            out partnerStatement,
            out statementPart);
}
