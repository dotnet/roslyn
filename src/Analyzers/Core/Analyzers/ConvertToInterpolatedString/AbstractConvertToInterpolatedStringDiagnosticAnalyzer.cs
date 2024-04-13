// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ConvertToInterpolatedString;

internal abstract class AbstractConvertToInterpolatedStringDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
	protected AbstractConvertToInterpolatedStringDiagnosticAnalyzer()
		: base(IDEDiagnosticIds.ConvertToInterpolatedStringDiagnosticId,
			  EnforceOnBuildValues.ConvertToInterpolatedString,
			  options: null,
			  new LocalizableResourceString(nameof(AnalyzersResources.Convert_to_interpolated_string), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
			  new LocalizableResourceString(nameof(AnalyzersResources.Interpolated_string_should_be_used_for_performance), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
	{
	}

	public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
		=> DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

	protected override void InitializeWorker(AnalysisContext context)
	{
	}
}
