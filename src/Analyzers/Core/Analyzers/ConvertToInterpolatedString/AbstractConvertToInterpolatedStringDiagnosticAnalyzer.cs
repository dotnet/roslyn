// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString;

internal abstract class AbstractConvertToInterpolatedStringDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    protected AbstractConvertToInterpolatedStringDiagnosticAnalyzer(LocalizableString title)
        : base(diagnosticId: IDEDiagnosticIds.ConvertToInterpolatedStringDiagnosticId,
              EnforceOnBuildValues.ConvertToInterpolatedString,
              option: null,
              title: title)
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
    }
}
