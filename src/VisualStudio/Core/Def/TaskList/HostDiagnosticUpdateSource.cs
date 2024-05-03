// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;

internal sealed class HostDiagnosticUpdateSource : IProjectSystemDiagnosticSource
{
    public static readonly HostDiagnosticUpdateSource Instance = new();

    private HostDiagnosticUpdateSource()
    {
    }

    public DiagnosticData CreateAnalyzerLoadFailureDiagnostic(AnalyzerLoadFailureEventArgs e, string fullPath, ProjectId projectId, string language)
    {
        return DocumentAnalysisExecutor.CreateAnalyzerLoadFailureDiagnostic(e, fullPath, projectId, language);
    }
}
