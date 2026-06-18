// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseNullConditionalAwait;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseNullConditionalAwaitDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpUseNullConditionalAwaitDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseNullConditionalAwait,
               EnforceOnBuildValues.UseNullConditionalAwait,
               CSharpCodeStyleOptions.PreferNullConditionalAwait,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_null_conditional_await), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Null_conditional_await_can_be_used), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            // `await?` is a preview feature; only offer the conversion where it is available.
            if (context.Compilation.LanguageVersion() < LanguageVersion.Preview)
                return;

            // Pattern detection (the `if (x != null) await x;` and ternary shapes) lands in the
            // following commits.
        });
    }
}
