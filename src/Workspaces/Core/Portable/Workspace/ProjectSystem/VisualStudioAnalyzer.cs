// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem
{
    // TODO: Remove. This is only needed to support Solution Explorer Analyzer node population. 
    // Analyzers should not be loaded in devenv process (see https://github.com/dotnet/roslyn/issues/43008).
    internal sealed class ProjectAnalyzerReference(string fullPath, IProjectSystemDiagnosticSource projectSystemDiagnosticSource, ProjectId projectId, string language) : IDisposable
    {
        // Shadow copy analyzer files coming from packages to avoid locking the files in NuGet cache.
        // NOTE: It is important that we share the same shadow copy assembly loader for all VisualStudioAnalyzer instances.
        // This is required to ensure that shadow copied analyzer dependencies are correctly loaded.
        private static readonly IAnalyzerAssemblyLoader s_analyzerAssemblyLoader =
            new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"));

        // these 2 are mutable states that must be guarded under the _gate.
        private readonly object _gate = new();
        private AnalyzerReference? _analyzerReference;
        private ImmutableArray<DiagnosticData> _analyzerLoadErrors = ImmutableArray<DiagnosticData>.Empty;

        public string FullPath { get; } = fullPath;

        public AnalyzerReference GetReference()
        {
            lock (_gate)
            {
                if (_analyzerReference == null)
                {
                    // TODO: ensure the file watcher is subscribed
                    // (tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/661546)

                    var analyzerFileReference = new AnalyzerFileReference(FullPath, s_analyzerAssemblyLoader);
                    analyzerFileReference.AnalyzerLoadFailed += OnAnalyzerLoadError;
                    _analyzerReference = analyzerFileReference;
                }

                return _analyzerReference;
            }
        }

        private void OnAnalyzerLoadError(object? sender, AnalyzerLoadFailureEventArgs e)
        {
            var data = projectSystemDiagnosticSource.CreateAnalyzerLoadFailureDiagnostic(e, FullPath, projectId, language);

            lock (_gate)
            {
                _analyzerLoadErrors = _analyzerLoadErrors.Add(data);
                projectSystemDiagnosticSource.UpdateDiagnosticsForProject(projectId, this, _analyzerLoadErrors);
            }
        }

        public void Dispose()
        {
            ResetReferenceAndErrors(out var reference, out var loadErrors);

            if (reference is AnalyzerFileReference fileReference)
            {
                fileReference.AnalyzerLoadFailed -= OnAnalyzerLoadError;

                if (!loadErrors.IsEmpty)
                {
                    projectSystemDiagnosticSource.ClearDiagnosticsForProject(projectId, this);
                }

                projectSystemDiagnosticSource.ClearAnalyzerReferenceDiagnostics(fileReference, language, projectId);
            }
        }

        private void ResetReferenceAndErrors(out AnalyzerReference? reference, out ImmutableArray<DiagnosticData> loadErrors)
        {
            lock (_gate)
            {
                loadErrors = _analyzerLoadErrors;
                reference = _analyzerReference;

                _analyzerLoadErrors = ImmutableArray<DiagnosticData>.Empty;
                _analyzerReference = null;
            }
        }
    }
}
