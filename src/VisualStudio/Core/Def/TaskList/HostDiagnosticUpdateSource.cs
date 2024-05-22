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

    private void AddDiagnosticsCreatedArgsForProject(ref TemporaryArray<DiagnosticsUpdatedArgs> builder, ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
    {
        var args = DiagnosticsUpdatedArgs.DiagnosticsCreated(
            CreateId(projectId, key),
            solution: null,
            projectId: projectId,
            documentId: null,
            diagnostics: items.AsImmutableOrEmpty());

        builder.Add(args);
    }

    private void AddDiagnosticsRemovedArgsForProject(ref TemporaryArray<DiagnosticsUpdatedArgs> builder, ProjectId projectId, object key)
    {
        var args = DiagnosticsUpdatedArgs.DiagnosticsRemoved(
            CreateId(projectId, key),
            solution: null,
            projectId: projectId,
            documentId: null);

        builder.Add(args);
    }

    private object CreateId(ProjectId projectId, object key) => Tuple.Create(this, projectId, key);

    public void UpdateAndAddDiagnosticsArgsForProject(ref TemporaryArray<DiagnosticsUpdatedArgs> builder, ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
    {
        Contract.ThrowIfNull(projectId);
        Contract.ThrowIfNull(key);
        Contract.ThrowIfNull(items);

        lock (_gate)
        {
            _diagnosticMap.GetOrAdd(projectId, id => new HashSet<object>()).Add(key);
        }

        AddDiagnosticsCreatedArgsForProject(ref builder, projectId, key, items);
    }

    void IProjectSystemDiagnosticSource.UpdateDiagnosticsForProject(ProjectId projectId, object key, IEnumerable<DiagnosticData> items)
    {
        using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;
        UpdateAndAddDiagnosticsArgsForProject(ref argsBuilder.AsRef(), projectId, key, items);
        RaiseDiagnosticsUpdated(argsBuilder.ToImmutableAndClear());
    }

    void IProjectSystemDiagnosticSource.ClearAllDiagnosticsForProject(ProjectId projectId)
    {
        Contract.ThrowIfNull(projectId);

        HashSet<object> projectDiagnosticKeys;
        lock (_gate)
        {
            if (_diagnosticMap.TryGetValue(projectId, out projectDiagnosticKeys))
            {
                _diagnosticMap.Remove(projectId);
            }
        }

        using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;
        if (projectDiagnosticKeys != null)
        {
            foreach (var key in projectDiagnosticKeys)
            {
                AddDiagnosticsRemovedArgsForProject(ref argsBuilder.AsRef(), projectId, key);
            }
        }

        AddArgsToClearAnalyzerDiagnostics(ref argsBuilder.AsRef(), projectId);
        RaiseDiagnosticsUpdated(argsBuilder.ToImmutableAndClear());
    }

    internal void ClearAndAddDiagnosticsArgsForProject(ref TemporaryArray<DiagnosticsUpdatedArgs> builder, ProjectId projectId, object key)
    {
        Contract.ThrowIfNull(projectId);
        Contract.ThrowIfNull(key);

        var raiseEvent = false;
        lock (_gate)
        {
            if (_diagnosticMap.TryGetValue(projectId, out var projectDiagnosticKeys))
            {
                raiseEvent = projectDiagnosticKeys.Remove(key);
            }
        }

        if (raiseEvent)
        {
            AddDiagnosticsRemovedArgsForProject(ref builder, projectId, key);
        }
    }

    void IProjectSystemDiagnosticSource.ClearDiagnosticsForProject(ProjectId projectId, object key)
    {
        using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;
        ClearAndAddDiagnosticsArgsForProject(ref argsBuilder.AsRef(), projectId, key);
        RaiseDiagnosticsUpdated(argsBuilder.ToImmutableAndClear());
    }

    public DiagnosticData CreateAnalyzerLoadFailureDiagnostic(AnalyzerLoadFailureEventArgs e, string fullPath, ProjectId projectId, string language)
    {
        return DocumentAnalysisExecutor.CreateAnalyzerLoadFailureDiagnostic(e, fullPath, projectId, language);
    }
}
