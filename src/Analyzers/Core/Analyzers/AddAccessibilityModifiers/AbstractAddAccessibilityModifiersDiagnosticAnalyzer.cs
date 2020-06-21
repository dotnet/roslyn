// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.AddAccessibilityModifiers
{
    internal abstract class AbstractAddAccessibilityModifiersDiagnosticAnalyzer<TCompilationUnitSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TCompilationUnitSyntax : SyntaxNode
    {
        protected AbstractAddAccessibilityModifiersDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddAccessibilityModifiersDiagnosticId,
                   CodeStyleOptions2.RequireAccessibilityModifiers,
                   new LocalizableResourceString(nameof(AnalyzersResources.Add_accessibility_modifiers), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Accessibility_modifiers_required), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
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

            var language = syntaxTree.Options.Language;
            var option = context.GetOption(CodeStyleOptions2.RequireAccessibilityModifiers, language);
            if (option.Value == AccessibilityModifiersRequired.Never)
            {
                return;
            }

            ProcessCompilationUnit(context, option, (TCompilationUnitSyntax)syntaxTree.GetRoot(cancellationToken));
        }

        protected abstract void ProcessCompilationUnit(SyntaxTreeAnalysisContext context, CodeStyleOption2<AccessibilityModifiersRequired> option, TCompilationUnitSyntax compilationUnitSyntax);
    }
}
