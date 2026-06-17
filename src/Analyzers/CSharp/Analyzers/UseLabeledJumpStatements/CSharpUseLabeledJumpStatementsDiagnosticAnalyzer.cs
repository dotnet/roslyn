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
/// Offers to replace the workarounds people write to emulate labeled <see langword="break"/>/<see
/// langword="continue"/> with the real labeled jump statements. See <see
/// cref="CSharpUseLabeledJumpStatementsHelpers"/> for the patterns that are detected.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseLabeledJumpStatementsDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.UseLabeledJumpStatementDiagnosticId,
        EnforceOnBuildValues.UseLabeledJumpStatement,
        CSharpCodeStyleOptions.PreferLabeledJumpStatements,
        new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Use_labeled_jump_statement), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
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
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;

        if (CSharpUseLabeledJumpStatementsHelpers.TryGetGotoBreakPattern(gotoStatement, semanticModel, cancellationToken, out _, out _, out _) ||
            CSharpUseLabeledJumpStatementsHelpers.TryGetGotoContinuePattern(gotoStatement, semanticModel, cancellationToken, out _, out _, out _))
        {
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                gotoStatement.GotoKeyword.GetLocation(),
                option.Notification,
                context.Options,
                additionalLocations: [gotoStatement.GetLocation()],
                properties: null));
        }
    }

    private void AnalyzeBreakStatement(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetCSharpAnalyzerOptions().PreferLabeledJumpStatements;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var breakStatement = (BreakStatementSyntax)context.Node;

        // We register on 'break' (not 'continue') because the flag pattern's inner jump is always a 'break': it has to
        // exit the inner loop so control returns to the outer loop where the flag is checked.  Whether that emulates a
        // break or a continue of the outer loop is decided by the guard ('if (flag) break/continue;'), not by this
        // inner jump, so this single registration covers both.
        if (CSharpUseLabeledJumpStatementsHelpers.TryGetFlagPatternFromInnerBreak(
                breakStatement, context.SemanticModel, context.CancellationToken, out _))
        {
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                breakStatement.BreakKeyword.GetLocation(),
                option.Notification,
                context.Options,
                additionalLocations: [breakStatement.GetLocation()],
                properties: null));
        }
    }
}
