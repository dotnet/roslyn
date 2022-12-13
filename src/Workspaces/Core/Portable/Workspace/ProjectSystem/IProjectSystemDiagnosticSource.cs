// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem
{
    // TODO: see if we can get rid of this interface by appropriately rewriting HostDiagnosticUpdateSource to live at the workspaces layer.
    internal interface IProjectSystemDiagnosticSource
    {
        void ClearAllDiagnosticsForProject(ProjectId projectId);
        void ClearAnalyzerReferenceDiagnostics(AnalyzerFileReference fileReference, string language, ProjectId projectId);
        void ClearDiagnosticsForProject(ProjectId projectId, object key);
        DiagnosticData CreateAnalyzerLoadFailureDiagnostic(AnalyzerLoadFailureEventArgs e, string fullPath, ProjectId projectId, string language);
        void UpdateDiagnosticsForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items);
    }
}
