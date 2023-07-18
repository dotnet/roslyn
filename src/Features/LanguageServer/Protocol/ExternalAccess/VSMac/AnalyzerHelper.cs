// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.VSMac;

internal static class AnalyzerHelper
{
    public static DiagnosticData CreateAnalyzerLoadFailureDiagnostic(AnalyzerLoadFailureEventArgs e, string fullPath, ProjectId? projectId, string? language)
        => DocumentAnalysisExecutor.CreateAnalyzerLoadFailureDiagnostic(e, fullPath, projectId, language);
}

