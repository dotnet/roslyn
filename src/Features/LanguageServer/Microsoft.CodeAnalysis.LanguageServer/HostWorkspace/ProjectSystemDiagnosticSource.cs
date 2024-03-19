// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
internal class ProjectSystemDiagnosticSource : IProjectSystemDiagnosticSource
{
    public void ClearAllDiagnosticsForProject(ProjectId projectId)
    {
    }

    public void ClearAnalyzerReferenceDiagnostics(AnalyzerFileReference fileReference, string language, ProjectId projectId)
    {
    }

    public void ClearDiagnosticsForProject(ProjectId projectId, object key)
    {
    }

    public DiagnosticData CreateAnalyzerLoadFailureDiagnostic(AnalyzerLoadFailureEventArgs e, string fullPath, ProjectId projectId, string language)
    {
        return DocumentAnalysisExecutor.CreateAnalyzerLoadFailureDiagnostic(e, fullPath, projectId, language);
    }

    public void UpdateDiagnosticsForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
    {
        // TODO: actually store the diagnostics
    }
}
