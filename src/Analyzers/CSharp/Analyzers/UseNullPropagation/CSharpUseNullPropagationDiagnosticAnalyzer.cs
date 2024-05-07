// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.UseNullPropagation;

namespace Microsoft.CodeAnalysis.CSharp.UseNullPropagation;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseNullPropagationDiagnosticAnalyzer :
    AbstractUseNullPropagationDiagnosticAnalyzer<
        SyntaxKind,
        ExpressionSyntax,
        StatementSyntax,
        ConditionalExpressionSyntax,
        BinaryExpressionSyntax,
        InvocationExpressionSyntax,
        ConditionalAccessExpressionSyntax,
        ElementAccessExpressionSyntax,
        MemberAccessExpressionSyntax,
        IfStatementSyntax,
        ExpressionStatementSyntax>
{
    protected override SyntaxKind IfStatementSyntaxKind
        => SyntaxKind.IfStatement;

    protected override bool ShouldAnalyze(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp6;

    protected override ISemanticFacts SemanticFacts
        => CSharpSemanticFacts.Instance;

    protected override bool TryAnalyzePatternCondition(
        ISyntaxFacts syntaxFacts, ExpressionSyntax conditionNode,
        [NotNullWhen(true)] out ExpressionSyntax? conditionPartToCheck, out bool isEquals)
    {
        conditionPartToCheck = null;
        isEquals = true;

        if (conditionNode is not IsPatternExpressionSyntax patternExpression)
            return false;

        var pattern = patternExpression.Pattern;
        if (pattern is UnaryPatternSyntax(SyntaxKind.NotPattern) notPattern)
        {
            isEquals = false;
            pattern = notPattern.Pattern;
        }

        if (pattern is not ConstantPatternSyntax constantPattern)
            return false;

        if (!syntaxFacts.IsNullLiteralExpression(constantPattern.Expression))
            return false;

        conditionPartToCheck = patternExpression.Expression;
        return true;
    }

    protected override bool TryGetPartsOfIfStatement(
        IfStatementSyntax ifStatement,
        [NotNullWhen(true)] out ExpressionSyntax? condition,
        [NotNullWhen(true)] out StatementSyntax? trueStatement)
    {
        // has to be of the form:
        //
        //   if (...)
        //      statement
        //
        // or
        //
        //   if (...)
        //   {
        //       statement
        //   }

        condition = ifStatement.Condition;

        trueStatement = null;
        if (ifStatement.Else == null)
        {
            if (ifStatement.Statement is BlockSyntax block)
            {
                if (block.Statements.Count == 1)
                    trueStatement = block.Statements[0];
            }
            else
            {
                trueStatement = ifStatement.Statement;
            }
        }

        return trueStatement != null;
    }
}
