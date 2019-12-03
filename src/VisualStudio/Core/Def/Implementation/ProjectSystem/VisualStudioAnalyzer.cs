// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed class VisualStudioAnalyzer : IDisposable
    {
        private readonly Workspace _workspace;
        private readonly ProjectId _projectId;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
        private readonly string _language;
        private readonly IAnalyzerAssemblyLoader _analyzerAssemblyLoader;

        // these 2 are mutable states that must be guarded under the _gate.
        private readonly object _gate = new object();
        private AnalyzerReference _analyzerReference = null;
        private ImmutableArray<DiagnosticData> _analyzerLoadErrors = ImmutableArray<DiagnosticData>.Empty;

        public VisualStudioAnalyzer(string fullPath, HostDiagnosticUpdateSource hostDiagnosticUpdateSource, ProjectId projectId, Workspace workspace, string language)
        {
            FullPath = fullPath;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            _projectId = projectId;
            _workspace = workspace;
            _language = language;
            _analyzerAssemblyLoader = _workspace.Services.GetRequiredService<IAnalyzerService>().GetLoader();
        }

        public string FullPath { get; }

        public AnalyzerReference GetReference()
        {
            lock (_gate)
            {
                if (_analyzerReference == null)
                {
                    if (File.Exists(FullPath))
                    {
                        // Pass down a custom loader that will ensure we are watching for file changes once we actually load the assembly.
                        var assemblyLoaderForFileTracker = new AnalyzerAssemblyLoaderThatEnsuresFileBeingWatched(this);
                        var analyzerFileReference = new AnalyzerFileReference(FullPath, assemblyLoaderForFileTracker);
                        analyzerFileReference.AnalyzerLoadFailed += OnAnalyzerLoadError;

                        _analyzerReference = analyzerFileReference;
                    }
                    else
                    {
                        _analyzerReference = new VisualStudioUnresolvedAnalyzerReference(FullPath, this);
                    }
                }

                return _analyzerReference;
            }
        }

        private void OnAnalyzerLoadError(object sender, AnalyzerLoadFailureEventArgs e)
        {
            var data = AnalyzerHelper.CreateAnalyzerLoadFailureDiagnostic(_projectId, _language, FullPath, e);

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

        private void ResetReferenceAndErrors(out AnalyzerReference reference, out ImmutableArray<DiagnosticData> loadErrors)
        {
            lock (_gate)
            {
                loadErrors = _analyzerLoadErrors;
                reference = _analyzerReference;

                _analyzerLoadErrors = ImmutableArray<DiagnosticData>.Empty;
                _analyzerReference = null;
            }
        }

        /// <summary>
        /// This custom loader just wraps an existing loader, but ensures that we start listening to the file
        /// for changes once we've actually looked at the file.
        /// </summary>
        private class AnalyzerAssemblyLoaderThatEnsuresFileBeingWatched : IAnalyzerAssemblyLoader
        {
            private readonly VisualStudioAnalyzer _analyzer;

            public AnalyzerAssemblyLoaderThatEnsuresFileBeingWatched(VisualStudioAnalyzer analyzer)
            {
                _analyzer = analyzer;
            }

            public void AddDependencyLocation(string fullPath)
            {
                _analyzer._analyzerAssemblyLoader.AddDependencyLocation(fullPath);
            }

            public Assembly LoadFromPath(string fullPath)
            {
                // TODO: ensure the file watcher is subscribed
                // (tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/661546)
                return _analyzer._analyzerAssemblyLoader.LoadFromPath(fullPath);
            }
        }

        /// <summary>
        /// This custom <see cref="AnalyzerReference"/>, just wraps an existing <see cref="UnresolvedAnalyzerReference"/>,
        /// but ensure that we start listening to the file for changes once we've actually observed it, so that if the
        /// file then gets created on disk, we are notified.
        /// </summary>
        private sealed class VisualStudioUnresolvedAnalyzerReference : AnalyzerReference
        {
            private readonly UnresolvedAnalyzerReference _underlying;
            private readonly VisualStudioAnalyzer _visualStudioAnalyzer;

            public VisualStudioUnresolvedAnalyzerReference(string fullPath, VisualStudioAnalyzer visualStudioAnalyzer)
            {
                _underlying = new UnresolvedAnalyzerReference(fullPath);
                _visualStudioAnalyzer = visualStudioAnalyzer;
            }

            public override string FullPath
                => _underlying.FullPath;

            public override object Id
                => _underlying.Id;

            public override string Display
                => _underlying.Display;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            {
                // TODO: ensure the file watcher is subscribed
                // (tracked by https://devdiv.visualstudio.com/DevDiv/_workitems/edit/661546)
                return _underlying.GetAnalyzers(language);
            }

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
                => _underlying.GetAnalyzersForAllLanguages();
        }
    }
}
