// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;

// exporting both Abstract and HostDiagnosticUpdateSource is just to make testing easier.
// use HostDiagnosticUpdateSource when abstract one is not needed for testing purpose
[Export(typeof(AbstractHostDiagnosticUpdateSource))]
[Export(typeof(HostDiagnosticUpdateSource))]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class HostDiagnosticUpdateSource(Lazy<VisualStudioWorkspace> workspace) : AbstractHostDiagnosticUpdateSource, IProjectSystemDiagnosticSource
{
    private readonly Lazy<VisualStudioWorkspace> _workspace = workspace;

    public override Workspace Workspace => _workspace.Value;

    void IProjectSystemDiagnosticSource.UpdateDiagnosticsForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
    {
    }

    void IProjectSystemDiagnosticSource.ClearAllDiagnosticsForProject(ProjectId projectId)
    {
        Contract.ThrowIfNull(projectId);

        AddArgsToClearAnalyzerDiagnostics(projectId);
    }

    void IProjectSystemDiagnosticSource.ClearDiagnosticsForProject(ProjectId projectId, object key)
    {
    }

    public DiagnosticData CreateAnalyzerLoadFailureDiagnostic(AnalyzerLoadFailureEventArgs e, string fullPath, ProjectId projectId, string language)
    {
        return DocumentAnalysisExecutor.CreateAnalyzerLoadFailureDiagnostic(e, fullPath, projectId, language);
    }
}
