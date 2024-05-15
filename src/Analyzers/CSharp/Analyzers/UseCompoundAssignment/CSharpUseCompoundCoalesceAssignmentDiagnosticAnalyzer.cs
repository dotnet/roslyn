// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCompoundAssignment;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment;

/// <summary>
/// Looks for expressions of the form:
/// <list type="number">
///     <item><c>expr ?? (expr = value)</c> and converts it to <c>expr ??= value</c>.</item>
///     <item><c>if (expr is null) expr = value</c> and converts it to <c>expr ??= value</c>.</item>
/// </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpUseCompoundCoalesceAssignmentDiagnosticAnalyzer
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpUseCompoundCoalesceAssignmentDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId,
               EnforceOnBuildValues.UseCoalesceCompoundAssignment,
               CodeStyleOptions2.PreferCompoundAssignment,
               new LocalizableResourceString(nameof(AnalyzersResources.Use_compound_assignment), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp8)
                return;

            context.RegisterSyntaxNodeAction(AnalyzeCoalesceExpression, SyntaxKind.CoalesceExpression);
            context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        });
    }

    private void AnalyzeCoalesceExpression(SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var semanticModel = context.SemanticModel;

        var coalesceExpression = (BinaryExpressionSyntax)context.Node;

        var option = context.GetAnalyzerOptions().PreferCompoundAssignment;

        // Bail immediately if the user has disabled this feature.
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var coalesceLeft = coalesceExpression.Left;
        var coalesceRight = coalesceExpression.Right;

        if (coalesceRight is not ParenthesizedExpressionSyntax parenthesizedExpr)
            return;

        if (parenthesizedExpr.Expression is not AssignmentExpressionSyntax assignment)
            return;

        if (assignment.Kind() != SyntaxKind.SimpleAssignmentExpression)
            return;

        // have    x ?? (y = z)
        // ensure that 'x' and 'y' are suitably equivalent.
        var syntaxFacts = CSharpSyntaxFacts.Instance;
        if (!syntaxFacts.AreEquivalent(coalesceLeft, assignment.Left))
            return;

        // Syntactically looks promising.  But we can only safely do this if 'expr'
        // is side-effect-free since we will be changing the number of times it is
        // executed from twice to once.
        if (!UseCompoundAssignmentUtilities.IsSideEffectFree(
                syntaxFacts, coalesceLeft, semanticModel, cancellationToken))
        {
            return;
        }

        // Good match.
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            coalesceExpression.OperatorToken.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: ImmutableArray.Create(coalesceExpression.GetLocation()),
            properties: null));
    }

    public static bool GetWhenTrueAssignment(
        IfStatementSyntax ifStatement,
        [NotNullWhen(true)] out StatementSyntax? whenTrue,
        [NotNullWhen(true)] out AssignmentExpressionSyntax? assignment)
    {
        whenTrue = ifStatement.Statement is BlockSyntax block
            ? block.Statements.Count == 1 ? block.Statements[0] : null
            : ifStatement.Statement;

        assignment = whenTrue is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax(SyntaxKind.SimpleAssignmentExpression) assignmentTemp }
            ? assignmentTemp
            : null;

        return whenTrue != null && assignment != null;
    }

    private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var semanticModel = context.SemanticModel;

        var ifStatement = (IfStatementSyntax)context.Node;

        var option = context.GetAnalyzerOptions().PreferCompoundAssignment;

        // Bail immediately if the user has disabled this feature.
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        if (ifStatement.Else != null)
            return;

        if (!GetWhenTrueAssignment(ifStatement, out var whenTrue, out var assignment))
            return;

        if (!IsReferenceEqualsNullCheck(semanticModel, ifStatement.Condition, cancellationToken, out var testedExpression))
            return;

        // have    if (x == null) x = y;
        // ensure that 'x' in both locations are suitably equivalent.
        var syntaxFacts = CSharpSyntaxFacts.Instance;
        if (!syntaxFacts.AreEquivalent(testedExpression, assignment.Left))
            return;

        // Syntactically looks promising.  But we can only safely do this if 'expr'
        // is side-effect-free since we will be changing the number of times it is
        // executed from twice to once.
        if (!UseCompoundAssignmentUtilities.IsSideEffectFree(
                syntaxFacts, testedExpression, semanticModel, cancellationToken))
        {
            return;
        }

        // Don't want to offer anything if our if-statement body has any conditional directives in it.  That
        // means there's some other code that may run under some other conditions, that we do not want to now
        // run conditionally outside of the 'if' statement itself.
        if (whenTrue.GetLeadingTrivia().Any(static t => t.GetStructure() is ConditionalDirectiveTriviaSyntax))
            return;

        // pointers cannot use ??=
        if (semanticModel.GetTypeInfo(testedExpression, cancellationToken).Type is IPointerTypeSymbol or IFunctionPointerTypeSymbol)
            return;

        // Good match.
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            ifStatement.IfKeyword.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: ImmutableArray.Create(ifStatement.GetLocation()),
            properties: null));
    }

    private bool IsReferenceEqualsNullCheck(
        SemanticModel semanticModel,
        ExpressionSyntax condition,
        CancellationToken cancellationToken,
        [NotNullWhen(true)] out ExpressionSyntax? testedExpression)
    {
        testedExpression = null;
        if (condition is BinaryExpressionSyntax(SyntaxKind.EqualsExpression) { Right: LiteralExpressionSyntax(SyntaxKind.NullLiteralExpression) } binaryExpression)
        {
            // Ensure that if we are using `==` that it's not an overloaded operator.  One known exception is
            // System.String.  Even though `==` is overloaded, it has the same semantics as ReferenceEquals(null) so
            // it's safe to convert.
            var symbol = semanticModel.GetSymbolInfo(binaryExpression, cancellationToken).Symbol;
            if (symbol is null || !symbol.IsUserDefinedOperator() || symbol.ContainingType.SpecialType == SpecialType.System_String)
                testedExpression = binaryExpression.Left;
        }
        else if (condition is IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax(SyntaxKind.NullLiteralExpression) } } isPattern)
        {
            // x is null.  always a valid null check.
            testedExpression = isPattern.Expression;
        }
        else if (condition is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 2 } invocation)
        {
            var arg0 = invocation.ArgumentList.Arguments[0].Expression;
            var arg1 = invocation.ArgumentList.Arguments[1].Expression;

            if (arg0.Kind() == SyntaxKind.NullLiteralExpression ||
                arg1.Kind() == SyntaxKind.NullLiteralExpression)
            {
                var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol;
                if (symbol?.Name == nameof(ReferenceEquals) &&
                    symbol.ContainingType?.SpecialType == SpecialType.System_Object)
                {
                    testedExpression = arg0.Kind() == SyntaxKind.NullLiteralExpression ? arg1 : arg0;
                }
            }
        }

        return testedExpression != null;
    }
}
