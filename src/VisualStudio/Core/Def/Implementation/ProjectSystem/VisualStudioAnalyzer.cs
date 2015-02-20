﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly string _language;

        private AnalyzerReference _analyzerReference;
        private List<DiagnosticData> _analyzerLoadErrors;

        // These are the error codes of the compiler warnings. Keep the ids the same so that de-duplication against compiler errors
        // works in the error list (after a build).
        private const string WRN_AnalyzerCannotBeCreatedIdCS = "CS8032";
        private const string WRN_AnalyzerCannotBeCreatedIdVB = "BC42376";
        private const string WRN_NoAnalyzerInAssemblyIdCS = "CS8033";
        private const string WRN_NoAnalyzerInAssemblyIdVB = "BC42377";
        private const string WRN_UnableToLoadAnalyzerIdCS = "CS8034";
        private const string WRN_UnableToLoadAnalyzerIdVB = "BC42378";

        public event EventHandler UpdatedOnDisk;

        public VisualStudioAnalyzer(string fullPath, IVsFileChangeEx fileChangeService, HostDiagnosticUpdateSource hostDiagnosticUpdateSource, ProjectId projectId, Workspace workspace, string language)
        {
            _fullPath = fullPath;
            _tracker = new FileChangeTracker(fileChangeService, fullPath);
            _tracker.UpdatedOnDisk += OnUpdatedOnDisk;
            _tracker.StartFileChangeListeningAsync();
            _tracker.EnsureSubscription();
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            _projectId = projectId;
            _workspace = workspace;
            _language = language;
        }

        public string FullPath
        {
            get { return _fullPath; }
        }

        public AnalyzerReference GetReference()
        {
            if (_analyzerReference == null)
            {
                if (File.Exists(_fullPath))
                {
                    _analyzerReference = new AnalyzerFileReference(_fullPath);
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
            string id;
            string message;
            string messageFormat;

            switch (e.ErrorCode)
            {
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer:
                    id = _language == LanguageNames.CSharp ? WRN_UnableToLoadAnalyzerIdCS : WRN_UnableToLoadAnalyzerIdVB;
                    messageFormat = ServicesVSResources.WRN_UnableToLoadAnalyzer;
                    message = string.Format(ServicesVSResources.WRN_UnableToLoadAnalyzer, _fullPath, e.Exception.Message);
                    break;
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer:
                    id = _language == LanguageNames.CSharp ? WRN_AnalyzerCannotBeCreatedIdCS : WRN_AnalyzerCannotBeCreatedIdVB;
                    messageFormat = ServicesVSResources.WRN_AnalyzerCannotBeCreated;
                    message = string.Format(ServicesVSResources.WRN_AnalyzerCannotBeCreated, e.TypeName, _fullPath, e.Exception.Message);
                    break;
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers:
                    id = _language == LanguageNames.CSharp ? WRN_NoAnalyzerInAssemblyIdCS : WRN_NoAnalyzerInAssemblyIdVB;
                    messageFormat = ServicesVSResources.WRN_NoAnalyzerInAssembly;
                    message = string.Format(ServicesVSResources.WRN_NoAnalyzerInAssembly, _fullPath);
                    break;
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.None:
                default:
                    return;
            }

            DiagnosticData data = new DiagnosticData(
                id,
                ServicesVSResources.ErrorCategory,
                message,
                messageFormat,
                severity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 0,
                workspace: _workspace,
                projectId: _projectId);

            _analyzerLoadErrors = _analyzerLoadErrors ?? new List<DiagnosticData>();
            _analyzerLoadErrors.Add(data);

            _hostDiagnosticUpdateSource.UpdateDiagnosticsForProject(_projectId, this, _analyzerLoadErrors);
        }

        public void Dispose()
        {
            if (_analyzerReference is AnalyzerFileReference)
            {
                ((AnalyzerFileReference)_analyzerReference).AnalyzerLoadFailed -= OnAnalyzerLoadError;

                if (_analyzerLoadErrors != null && _analyzerLoadErrors.Count > 0)
                {
                    _hostDiagnosticUpdateSource.ClearDiagnosticsForProject(_projectId, this);
                }
            }

            _analyzerLoadErrors = null;

            _tracker.Dispose();
            _tracker.UpdatedOnDisk -= OnUpdatedOnDisk;
        }

        private void OnUpdatedOnDisk(object sender, EventArgs e)
        {
            var handler = UpdatedOnDisk;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
