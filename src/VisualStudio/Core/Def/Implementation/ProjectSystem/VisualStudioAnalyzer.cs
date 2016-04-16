// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
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
            _tracker.EnsureSubscription();
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
                    _analyzerReference = new AnalyzerFileReference(_fullPath, _loader);
                    ((AnalyzerFileReference)_analyzerReference).AnalyzerLoadFailed += OnAnalyzerLoadError;
                }
                else
                {
                    _analyzerReference = new UnresolvedAnalyzerReference(_fullPath);
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
    }
}
