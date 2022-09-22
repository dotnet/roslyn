// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestResources.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class DoNothingSuppressor : DiagnosticSuppressor
{
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray<SuppressionDescriptor>.Empty;

    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
    }
}
