// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseNullConditionalAwait;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseNullConditionalAwaitDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.UseNullConditionalAwait,
        EnforceOnBuildValues.UseNullConditionalAwait,
        CSharpCodeStyleOptions.PreferNullConditionalAwait,
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_null_conditional_await), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Null_conditional_await_can_be_used), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            // `await?` is a preview feature; only offer the conversion where it is available.
            if (!context.Compilation.LanguageVersion().IsCSharp15OrAbove())
                return;

            context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
        });
    }

    private void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetCSharpAnalyzerOptions().PreferNullConditionalAwait;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var ifStatement = (IfStatementSyntax)context.Node;

        // Only the `if (a != null) await E;` shape (no else; the result of the await is discarded).
        if (ifStatement.Else != null)
            return;

        if (ifStatement.ContainsDirectives)
            return;

        // The body has to be a single `await E;` (directly, or as the sole statement in a block).
        var statement = ifStatement.Statement is BlockSyntax { Statements: [var single] } ? single : ifStatement.Statement;
        if (statement is not ExpressionStatementSyntax { Expression: AwaitExpressionSyntax { QuestionToken.RawKind: 0 } awaitExpression })
            return;

        // The condition has to be a non-null check whose operand is the receiver of the awaited expression.
        if (!UseNullConditionalAwaitHelpers.TryGetNotNullCheckOperand(ifStatement.Condition, out var conditionOperand))
            return;

        var match = UseNullConditionalAwaitHelpers.GetReceiverMatch(
            context.SemanticModel, conditionOperand, awaitExpression.Expression, context.CancellationToken);
        if (match is null)
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            ifStatement.IfKeyword.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: [ifStatement.GetLocation()],
            properties: null));
    }
}
