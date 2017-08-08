// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal sealed class VisualStudioAnalyzer : IDisposable
    {
        private readonly string _fullPath;
        private readonly FileChangeTracker _tracker;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
        private readonly ProjectId _projectId;
        private readonly Workspace _workspace;
        private readonly IAnalyzerAssemblyLoader _loader;
        private readonly string _language;

        private AnalyzerReference _analyzerReference;
        private List<DiagnosticData> _analyzerLoadErrors;

        public event EventHandler UpdatedOnDisk;

        public VisualStudioAnalyzer(string fullPath, IVsFileChangeEx fileChangeService, HostDiagnosticUpdateSource hostDiagnosticUpdateSource, ProjectId projectId, Workspace workspace, IAnalyzerAssemblyLoader loader, string language)
        {
            _fullPath = fullPath;
            _tracker = new FileChangeTracker(fileChangeService, fullPath);
            _tracker.UpdatedOnDisk += OnUpdatedOnDisk;
            _tracker.StartFileChangeListeningAsync();
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            _projectId = projectId;
            _workspace = workspace;
            _loader = loader;
            _language = language;
        }

        public string FullPath
        {
            get { return _fullPath; }
        }

        public bool HasLoadErrors
        {
            get { return _analyzerLoadErrors != null && _analyzerLoadErrors.Count > 0; }
        }

        public AnalyzerReference GetReference()
        {
            if (_analyzerReference == null)
            {
                if (File.Exists(_fullPath))
                {
                    // Pass down a custom loader that will ensure we are watching for file changes once we actually load the assembly.
                    var assemblyLoaderForFileTracker = new AnalyzerAssemblyLoaderThatEnsuresFileBeingWatched(this);
                    _analyzerReference = new AnalyzerFileReference(_fullPath, assemblyLoaderForFileTracker);
                    ((AnalyzerFileReference)_analyzerReference).AnalyzerLoadFailed += OnAnalyzerLoadError;
                }
                else
                {
                    _analyzerReference = new VisualStudioUnresolvedAnalyzerReference(_fullPath, this);
                }
            }

            return _analyzerReference;
        }

        private void OnAnalyzerLoadError(object sender, AnalyzerLoadFailureEventArgs e)
        {
            var data = AnalyzerHelper.CreateAnalyzerLoadFailureDiagnostic(_workspace, _projectId, _language, _fullPath, e);

            _analyzerLoadErrors = _analyzerLoadErrors ?? new List<DiagnosticData>();
            _analyzerLoadErrors.Add(data);

            _hostDiagnosticUpdateSource.UpdateDiagnosticsForProject(_projectId, this, _analyzerLoadErrors);
        }

        public void Dispose()
        {
            Reset();

            _tracker.Dispose();
            _tracker.UpdatedOnDisk -= OnUpdatedOnDisk;
        }

        public void Reset()
        {
            var analyzerFileReference = _analyzerReference as AnalyzerFileReference;
            if (analyzerFileReference != null)
            {
                analyzerFileReference.AnalyzerLoadFailed -= OnAnalyzerLoadError;

                if (_analyzerLoadErrors != null && _analyzerLoadErrors.Count > 0)
                {
                    _hostDiagnosticUpdateSource.ClearDiagnosticsForProject(_projectId, this);
                }

                _hostDiagnosticUpdateSource.ClearAnalyzerReferenceDiagnostics(analyzerFileReference, _language, _projectId);
            }

            _analyzerLoadErrors = null;
            _analyzerReference = null;
        }

        private void OnUpdatedOnDisk(object sender, EventArgs e)
        {
            UpdatedOnDisk?.Invoke(this, EventArgs.Empty);
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
                _analyzer._loader.AddDependencyLocation(fullPath);
            }

            public Assembly LoadFromPath(string fullPath)
            {
                _analyzer._tracker.EnsureSubscription();
                return _analyzer._loader.LoadFromPath(fullPath);
            }
        }

        /// <summary>
        /// This custom <see cref="AnalyzerReference"/>, just wraps an existing <see cref="UnresolvedAnalyzerReference"/>,
        /// but ensure that we start listening to the file for changes once we've actually observed it, so that if the
        /// file then gets created on disk, we are notified.
        /// </summary>
        private class VisualStudioUnresolvedAnalyzerReference : AnalyzerReference
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

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            {
                _visualStudioAnalyzer._tracker.EnsureSubscription();
                return _underlying.GetAnalyzers(language);
            }

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
                => _underlying.GetAnalyzersForAllLanguages();
        }
    }
}
