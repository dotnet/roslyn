// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.AddOrRemoveAccessibilityModifiers;

internal abstract class AbstractAddOrRemoveAccessibilityModifiersDiagnosticAnalyzer<TCompilationUnitSyntax>()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.AddOrRemoveAccessibilityModifiersDiagnosticId,
        EnforceOnBuildValues.AddOrRemoveAccessibilityModifiers,
        CodeStyleOptions2.AccessibilityModifiersRequired,
        new LocalizableResourceString(nameof(AnalyzersResources.Add_accessibility_modifiers), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Accessibility_modifiers_required), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    where TCompilationUnitSyntax : SyntaxNode
{
    protected abstract IAccessibilityFacts AccessibilityFacts { get; }
    protected abstract IAddOrRemoveAccessibilityModifiers AddOrRemoveAccessibilityModifiers { get; }

    protected abstract void ProcessCompilationUnit(SyntaxTreeAnalysisContext context, CodeStyleOption2<AccessibilityModifiersRequired> option, TCompilationUnitSyntax compilationUnitSyntax);

    protected readonly DiagnosticDescriptor ModifierRemovedDescriptor = CreateDescriptorWithId(
        IDEDiagnosticIds.AddOrRemoveAccessibilityModifiersDiagnosticId,
        EnforceOnBuildValues.AddOrRemoveAccessibilityModifiers,
        hasAnyCodeStyleOption: true,
        new LocalizableResourceString(nameof(AnalyzersResources.Remove_accessibility_modifiers), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Accessibility_modifiers_unnecessary), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)));

    protected static readonly ImmutableDictionary<string, string?> ModifiersAddedProperties = ImmutableDictionary<string, string?>.Empty.Add(
        AddOrRemoveAccessibilityModifiersConstants.ModifiersAdded, AddOrRemoveAccessibilityModifiersConstants.ModifiersAdded);

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
            context.RegisterSyntaxTreeAction(treeContext => AnalyzeTree(treeContext, context.Compilation.Options)));

    private void AnalyzeTree(SyntaxTreeAnalysisContext context, CompilationOptions compilationOptions)
    {
        var option = context.GetAnalyzerOptions().RequireAccessibilityModifiers;
        if (option.Value == AccessibilityModifiersRequired.Never
            || ShouldSkipAnalysis(context, compilationOptions, option.Notification))
        {
            return;
        }

        ProcessCompilationUnit(context, option, (TCompilationUnitSyntax)context.Tree.GetRoot(context.CancellationToken));
    }

    protected void CheckMemberAndReportDiagnostic(
        SyntaxTreeAnalysisContext context,
        CodeStyleOption2<AccessibilityModifiersRequired> option,
        SyntaxNode member)
    {
        if (!this.AddOrRemoveAccessibilityModifiers.ShouldUpdateAccessibilityModifier(
                this.AccessibilityFacts, member, option.Value, out var name, out var modifiersAdded))
        {
            return;
        }

        // Have an issue to flag, either add or remove. Report issue to user.
        var additionalLocations = ImmutableArray.Create(member.GetLocation());
        context.ReportDiagnostic(DiagnosticHelper.Create(
            modifiersAdded ? Descriptor : ModifierRemovedDescriptor,
            name.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: additionalLocations,
            modifiersAdded ? ModifiersAddedProperties : null));
    }
}
