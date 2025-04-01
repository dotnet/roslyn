// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.NewLines.ConstructorInitializerPlacement;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class ConstructorInitializerPlacementDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public ConstructorInitializerPlacementDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.ConstructorInitializerPlacementDiagnosticId,
               EnforceOnBuildValues.ConstructorInitializerPlacement,
               CSharpCodeStyleOptions.AllowBlankLineAfterColonInConstructorInitializer,
               new LocalizableResourceString(
                   nameof(CSharpAnalyzersResources.Blank_line_not_allowed_after_constructor_initializer_colon), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
            context.RegisterSyntaxTreeAction(treeContext => AnalyzeTree(treeContext, context.Compilation.Options)));

    private void AnalyzeTree(SyntaxTreeAnalysisContext context, CompilationOptions compilationOptions)
    {
        var option = context.GetCSharpAnalyzerOptions().AllowBlankLineAfterColonInConstructorInitializer;
        if (option.Value || ShouldSkipAnalysis(context, compilationOptions, option.Notification))
            return;

        Recurse(context, option.Notification, context.GetAnalysisRoot(findInTrivia: false));
    }

    private void Recurse(SyntaxTreeAnalysisContext context, NotificationOption2 notificationOption, SyntaxNode node)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        // Don't bother analyzing nodes that have syntax errors in them.
        if (node.ContainsDiagnostics)
            return;

        if (node is ConstructorInitializerSyntax initializer)
            ProcessConstructorInitializer(context, notificationOption, initializer);

        foreach (var child in node.ChildNodesAndTokens())
        {
            if (!context.ShouldAnalyzeSpan(child.Span))
                continue;

            if (child.AsNode(out var childNode))
                Recurse(context, notificationOption, childNode);
        }
    }

    private void ProcessConstructorInitializer(
        SyntaxTreeAnalysisContext context, NotificationOption2 notificationOption, ConstructorInitializerSyntax initializer)
    {
        var sourceText = context.Tree.GetText(context.CancellationToken);

        var colonToken = initializer.ColonToken;
        var thisOrBaseKeyword = initializer.ThisOrBaseKeyword;

        var colonLine = sourceText.Lines.GetLineFromPosition(colonToken.SpanStart);
        var thisBaseLine = sourceText.Lines.GetLineFromPosition(thisOrBaseKeyword.SpanStart);
        if (colonLine == thisBaseLine)
            return;

        if (colonToken.TrailingTrivia.Count == 0)
            return;

        if (colonToken.TrailingTrivia.Last().Kind() != SyntaxKind.EndOfLineTrivia)
            return;

        if (colonToken.TrailingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine()))
            return;

        if (thisOrBaseKeyword.LeadingTrivia.Any(t => !t.IsWhitespaceOrEndOfLine() && !t.IsSingleOrMultiLineComment()))
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            this.Descriptor,
            colonToken.GetLocation(),
            notificationOption,
            context.Options,
            additionalLocations: [initializer.GetLocation()],
            properties: null));
    }
}
