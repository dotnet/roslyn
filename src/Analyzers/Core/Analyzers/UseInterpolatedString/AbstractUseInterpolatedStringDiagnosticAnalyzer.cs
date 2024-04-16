// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.UseInterpolatedString;

internal abstract class AbstractUseInterpolatedStringDiagnosticAnalyzer<TLanguageKindEnum>
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TLanguageKindEnum : struct
{
    protected AbstractUseInterpolatedStringDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseInterpolatedStringDiagnosticId,
              EnforceOnBuildValues.UseInterpolatedString,
              options: [CodeStyleOptions2.PreferInterpolatedString],
              new LocalizableResourceString(nameof(AnalyzersResources.Use_interpolated_string), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
              new LocalizableResourceString(nameof(AnalyzersResources.Interpolated_string_should_be_used_for_performance), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected abstract ImmutableArray<TLanguageKindEnum> GetSyntaxKinds();

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxKinds());

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context) => throw new NotImplementedException();
}
