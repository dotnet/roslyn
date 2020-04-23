﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    // TODO: Remove. This is only needed to support Solution Explorer Analyzer node population. 
    // Analyzers should not be loaded in devenv process (see https://github.com/dotnet/roslyn/issues/43008).
    internal sealed class VisualStudioAnalyzer : IDisposable
    {
        private readonly ProjectId _projectId;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
        private readonly string _language;
        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader;

        // these 2 are mutable states that must be guarded under the _gate.
        private readonly object _gate = new object();
        private AnalyzerReference? _analyzerReference;
        private ImmutableArray<DiagnosticData> _analyzerLoadErrors = ImmutableArray<DiagnosticData>.Empty;

        public VisualStudioAnalyzer(string fullPath, HostDiagnosticUpdateSource hostDiagnosticUpdateSource, ProjectId projectId, string language)
        {
            FullPath = fullPath;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            _projectId = projectId;
            _language = language;

            // Shadow copy analyzer files coming from packages to avoid locking the files in NuGet cache.
            _analyzerAssemblyLoader = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"));
        }

        public string FullPath { get; }

        public AnalyzerReference GetReference()
        {
            lock (_gate)
            {
                if (_analyzerReference == null)
                {
                    // TODO: ensure the file watcher is subscribed
                    // (tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/661546)

                    var analyzerFileReference = new AnalyzerFileReference(FullPath, _analyzerAssemblyLoader);
                    analyzerFileReference.AnalyzerLoadFailed += OnAnalyzerLoadError;
                    _analyzerReference = analyzerFileReference;
                }

                return _analyzerReference;
            }
        }

        private void OnAnalyzerLoadError(object sender, AnalyzerLoadFailureEventArgs e)
        {
            var data = AnalyzerHelper.CreateAnalyzerLoadFailureDiagnostic(e, FullPath, _projectId, _language);

            lock (_gate)
            {
                _analyzerLoadErrors = _analyzerLoadErrors.Add(data);
                _hostDiagnosticUpdateSource.UpdateDiagnosticsForProject(_projectId, this, _analyzerLoadErrors);
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
                    _hostDiagnosticUpdateSource.ClearDiagnosticsForProject(_projectId, this);
                }

                _hostDiagnosticUpdateSource.ClearAnalyzerReferenceDiagnostics(fileReference, _language, _projectId);
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
