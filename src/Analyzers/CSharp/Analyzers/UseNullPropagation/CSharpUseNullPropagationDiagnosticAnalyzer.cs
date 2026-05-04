// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
    public static readonly CSharpUseNullPropagationDiagnosticAnalyzer Instance = new();

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
        out ImmutableArray<StatementSyntax> trueStatements)
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
        //
        // or
        //
        //   if (...)
        //   {
        //       statement
        //       val = null;
        //   }

        condition = ifStatement.Condition;

        trueStatements = [];
        if (ifStatement.Else == null)
        {
            trueStatements = ifStatement.Statement is BlockSyntax block
                ? [.. block.Statements]
                : [ifStatement.Statement];
        }

        return true;
    }

    public override IfStatementAnalysisResult? AnalyzeIfStatement(
        SemanticModel semanticModel,
        IMethodSymbol? referenceEqualsMethod,
        IfStatementSyntax ifStatement,
        CancellationToken cancellationToken)
    {
        if (ifStatement.Statement.ContainsDirectives)
            return null;

        return base.AnalyzeIfStatement(semanticModel, referenceEqualsMethod, ifStatement, cancellationToken);
    }
}
