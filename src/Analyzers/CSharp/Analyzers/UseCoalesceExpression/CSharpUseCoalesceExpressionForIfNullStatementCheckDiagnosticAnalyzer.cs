// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Analyzers.UseCoalesceExpression;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UseCoalesceExpression;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseCoalesceExpressionForIfNullStatementCheckDiagnosticAnalyzer :
    AbstractUseCoalesceExpressionForIfNullStatementCheckDiagnosticAnalyzer<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        VariableDeclaratorSyntax,
        IfStatementSyntax>
{
    protected override SyntaxKind IfStatementKind
        => SyntaxKind.IfStatement;

    protected override ISyntaxFacts SyntaxFacts
        => CSharpSyntaxFacts.Instance;

    protected override bool IsSingle(VariableDeclaratorSyntax declarator)
        => true;

    protected override SyntaxNode GetDeclarationNode(VariableDeclaratorSyntax declarator)
        => declarator;

    protected override ExpressionSyntax GetConditionOfIfStatement(IfStatementSyntax ifStatement)
        => ifStatement.Condition;

    protected override bool IsNullCheck(ExpressionSyntax condition, [NotNullWhen(true)] out ExpressionSyntax? checkedExpression)
    {
        if (condition is BinaryExpressionSyntax(SyntaxKind.EqualsExpression) { Right: LiteralExpressionSyntax(SyntaxKind.NullLiteralExpression) } binary)
        {
            checkedExpression = binary.Left;
            return true;
        }
        else if (condition is IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax(SyntaxKind.NullLiteralExpression) } } isPattern)
        {
            checkedExpression = isPattern.Expression;
            return true;
        }

        checkedExpression = null;
        return false;
    }

    protected override bool TryGetEmbeddedStatement(IfStatementSyntax ifStatement, [NotNullWhen(true)] out StatementSyntax? whenTrueStatement)
    {
        whenTrueStatement = ifStatement.Statement is BlockSyntax { Statements.Count: 1 } block
            ? block.Statements[0]
            : ifStatement.Statement;

        return true;
    }

    protected override bool HasElseBlock(IfStatementSyntax ifStatement)
        => ifStatement.Else != null;

    protected override StatementSyntax? TryGetPreviousStatement(IfStatementSyntax ifStatement)
        => ifStatement.GetPreviousStatement();
}
