// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;

/// <summary>
/// Looks for the workarounds people write to emulate labeled <see langword="break"/>/<see langword="continue"/>
/// (jumping out of/continuing an outer loop) and offers to rewrite them to the real labeled jump statements:
/// <list type="bullet">
/// <item><c>goto</c> to a label placed immediately after an enclosing loop/switch (an emulated multi-level
/// <see langword="break"/>).</item>
/// <item><c>goto</c> to a label placed at the end of an enclosing loop body (an emulated multi-level
/// <see langword="continue"/>).</item>
/// <item>A <see langword="bool"/> flag set inside an inner loop and then checked in an outer loop to propagate a
/// <see langword="break"/>/<see langword="continue"/>.</item>
/// </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseLabeledJumpStatementsDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.UseLabeledJumpStatementDiagnosticId,
        EnforceOnBuildValues.UseLabeledJumpStatement,
        CSharpCodeStyleOptions.PreferLabeledJumpStatements,
        new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Use_labeled_break_or_continue), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            // Labeled 'break'/'continue' is only available in C# preview (and above), so only offer to rewrite
            // to it when the project can actually use the feature.
            if (!context.Compilation.LanguageVersion().IsCSharp15OrAbove())
                return;

            context.RegisterSyntaxNodeAction(AnalyzeGotoStatement, SyntaxKind.GotoStatement);
            context.RegisterSyntaxNodeAction(AnalyzeBreakStatement, SyntaxKind.BreakStatement);
        });
    }

    private void AnalyzeGotoStatement(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetCSharpAnalyzerOptions().PreferLabeledJumpStatements;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var gotoStatement = (GotoStatementSyntax)context.Node;

        // 'goto' to a label placed right after an enclosing loop is an emulated (multi-level) 'break'; 'goto' to a
        // label at the end of an enclosing loop's body is an emulated (multi-level) 'continue'.
        if (CSharpUseLabeledJumpStatementsHelpers.TryGetGotoBreakPattern(
                gotoStatement, context.SemanticModel, context.CancellationToken, out _, out _, out _) ||
            CSharpUseLabeledJumpStatementsHelpers.TryGetGotoContinuePattern(
                gotoStatement, context.SemanticModel, context.CancellationToken, out _, out _, out _))
        {
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                gotoStatement.GotoKeyword.GetLocation(),
                option.Notification,
                context.Options,
                additionalLocations: null,
                properties: null));
        }
    }

    private void AnalyzeBreakStatement(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetCSharpAnalyzerOptions().PreferLabeledJumpStatements;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var breakStatement = (BreakStatementSyntax)context.Node;

        // An inner 'break;' preceded by 'flag = true;' that, together with an outer 'if (flag) break/continue;', only
        // exists to emulate a multi-level break/continue.  Report on the inner 'break;' (the actionable jump),
        // pointing back at the flag declaration so the fix can reconstruct the whole pattern.
        if (CSharpUseLabeledJumpStatementsHelpers.TryGetFlagPatternFromInnerBreak(
                breakStatement, context.SemanticModel, context.CancellationToken, out var pattern))
        {
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                breakStatement.GetLocation(),
                option.Notification,
                context.Options,
                additionalLocations: [pattern!.Declaration.GetLocation()],
                properties: null));
        }
    }
}
