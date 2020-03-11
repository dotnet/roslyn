﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.AddAccessibilityModifiers
{
    internal abstract class AbstractAddAccessibilityModifiersDiagnosticAnalyzer<TCompilationUnitSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TCompilationUnitSyntax : SyntaxNode
    {
        protected AbstractAddAccessibilityModifiersDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId,
                   CodeStyleOptions.RequireAccessibilityModifiers,
                   new LocalizableResourceString(nameof(FeaturesResources.Add_accessibility_modifiers), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Accessibility_modifiers_required), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var syntaxTree = context.Tree;

            if (!(context.Options is WorkspaceAnalyzerOptions workspaceAnalyzerOptions))
            {
                return;
            }

            var language = syntaxTree.Options.Language;
            var option = context.GetOption(CodeStyleOptions.RequireAccessibilityModifiers, language);
            if (option.Value == AccessibilityModifiersRequired.Never)
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(workspaceAnalyzerOptions.Services.Workspace, language);
            ProcessCompilationUnit(context, generator, option, (TCompilationUnitSyntax)syntaxTree.GetRoot(cancellationToken));
        }

        protected abstract void ProcessCompilationUnit(SyntaxTreeAnalysisContext context, SyntaxGenerator generator, CodeStyleOption<AccessibilityModifiersRequired> option, TCompilationUnitSyntax compilationUnitSyntax);
    }
}
