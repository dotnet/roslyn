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
internal sealed class HostDiagnosticUpdateSource : AbstractHostDiagnosticUpdateSource, IProjectSystemDiagnosticSource
{
    private readonly Lazy<VisualStudioWorkspace> _workspace;

    private readonly object _gate = new();
    private readonly Dictionary<ProjectId, HashSet<object>> _diagnosticMap = [];

    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public HostDiagnosticUpdateSource(Lazy<VisualStudioWorkspace> workspace)
    {
        _workspace = workspace;
    }

    public override Workspace Workspace
    {
        get
        {
            return _workspace.Value;
        }
    }

    public void UpdateAndAddDiagnosticsArgsForProject(ProjectId projectId, object key)
    {
        Contract.ThrowIfNull(projectId);
        Contract.ThrowIfNull(key);

        lock (_gate)
        {
            _diagnosticMap.GetOrAdd(projectId, id => new HashSet<object>()).Add(key);
        }
    }

    void IProjectSystemDiagnosticSource.UpdateDiagnosticsForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
        => UpdateAndAddDiagnosticsArgsForProject(projectId, key);

    void IProjectSystemDiagnosticSource.ClearAllDiagnosticsForProject(ProjectId projectId)
    {
        Contract.ThrowIfNull(projectId);

        lock (_gate)
        {
            if (_diagnosticMap.TryGetValue(projectId, out _))
            {
                _diagnosticMap.Remove(projectId);
            }
        }

        AddArgsToClearAnalyzerDiagnostics(projectId);
    }

    internal void ClearAndAddDiagnosticsArgsForProject(ProjectId projectId, object key)
    {
        Contract.ThrowIfNull(projectId);
        Contract.ThrowIfNull(key);

        lock (_gate)
        {
            if (_diagnosticMap.TryGetValue(projectId, out var projectDiagnosticKeys))
                projectDiagnosticKeys.Remove(key);
        }
    }

    void IProjectSystemDiagnosticSource.ClearDiagnosticsForProject(ProjectId projectId, object key)
        => ClearAndAddDiagnosticsArgsForProject(projectId, key);

    public DiagnosticData CreateAnalyzerLoadFailureDiagnostic(AnalyzerLoadFailureEventArgs e, string fullPath, ProjectId projectId, string language)
    {
        return DocumentAnalysisExecutor.CreateAnalyzerLoadFailureDiagnostic(e, fullPath, projectId, language);
    }
}
