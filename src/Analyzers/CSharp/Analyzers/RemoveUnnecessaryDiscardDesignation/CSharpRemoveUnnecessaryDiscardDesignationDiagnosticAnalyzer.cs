// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryDiscardDesignation
{
    /// <summary>
    /// Supports code like <c>o switch { int _ => ... }</c> to just <c>o switch { int => ... }</c> in C# 9 and above.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpRemoveUnnecessaryDiscardDesignationDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpRemoveUnnecessaryDiscardDesignationDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveUnnecessaryDiscardDesignationDiagnosticId,
                   EnforceOnBuildValues.RemoveUnnecessaryDiscardDesignation,
                   option: null,
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Remove_unnessary_discard), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   new LocalizableResourceString(nameof(CSharpAnalyzersResources.Discard_can_be_removed), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                   isUnnecessary: true)
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(context =>
            {
                if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp9)
                    return;

                context.RegisterSyntaxNodeAction(AnalyzeDiscardDesignation, SyntaxKind.DiscardDesignation);
            });
        }

        private void AnalyzeDiscardDesignation(SyntaxNodeAnalysisContext context)
        {
            var discard = (DiscardDesignationSyntax)context.Node;
            if (discard.Parent is DeclarationPatternSyntax)
            {
                Report(discard);
            }
            else if (discard.Parent is RecursivePatternSyntax recursivePattern)
            {
                // can't remove from `(int i) _` as `(int i)` is not a legal pattern itself.
                if (recursivePattern.PositionalPatternClause != null &&
                    recursivePattern.PositionalPatternClause.Subpatterns.Count == 1)
                {
                    return;
                }

                Report(discard);
            }

            return;

            void Report(DiscardDesignationSyntax discard)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    this.Descriptor,
                    discard.GetLocation()));
            }
        }
    }
}
