// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Lightup;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class AnalysisContextExtensions
{
    private static readonly Func<AnalysisContext, DiagnosticSeverity> s_minimumReportedSeverity
        = LightupHelpers.CreatePropertyAccessor<AnalysisContext, DiagnosticSeverity>(typeof(AnalysisContext), nameof(MinimumReportedSeverity), DiagnosticSeverity.Hidden);

    public static DiagnosticSeverity MinimumReportedSeverity(this AnalysisContext context)
        => s_minimumReportedSeverity(context);
}
