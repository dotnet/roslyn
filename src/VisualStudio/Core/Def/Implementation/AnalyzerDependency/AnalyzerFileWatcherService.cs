// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(AnalyzerFileWatcherService))]
    internal sealed class AnalyzerFileWatcherService
    {
        private static readonly object s_analyzerChangedErrorId = new object();

        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly HostDiagnosticUpdateSource _updateSource;
        private readonly IVsFileChangeEx _fileChangeService;

        private readonly Dictionary<string, FileChangeTracker> _fileChangeTrackers = new Dictionary<string, FileChangeTracker>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _assemblyUpdatedTimesUtc = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

        private readonly object _guard = new object();

        [ImportingConstructor]
        public AnalyzerFileWatcherService(
            VisualStudioWorkspaceImpl workspace,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            SVsServiceProvider serviceProvider)
        {
            _workspace = workspace;
            _updateSource = hostDiagnosticUpdateSource;
            _fileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));
        }

        internal void ErrorIfAnalyzerAlreadyLoaded(ProjectId projectId, string analyzerPath)
        {
            DateTime loadedAssemblyUpdateTimeUtc;
            lock (_guard)
            {
                if (!_assemblyUpdatedTimesUtc.TryGetValue(analyzerPath, out loadedAssemblyUpdateTimeUtc))
                {
                    return;
                }
            }

            DateTime? fileUpdateTimeUtc = GetLastUpdateTimeUtc(analyzerPath);

            if (fileUpdateTimeUtc != null &&
                loadedAssemblyUpdateTimeUtc != fileUpdateTimeUtc)
            {
                RaiseAnalyzerChangedWarning(projectId, analyzerPath);
            }
        }

        internal void RemoveAnalyzerAlreadyLoadedDiagnostics(ProjectId projectId, string analyzerPath)
        {
            _updateSource.ClearDiagnosticsForProject(projectId, Tuple.Create(s_analyzerChangedErrorId, analyzerPath));
        }

        private void RaiseAnalyzerChangedWarning(ProjectId projectId, string analyzerPath)
        {
            string message = string.Format(ServicesVSResources.WRN_AnalyzerChangedMessage, analyzerPath);

            DiagnosticData data = new DiagnosticData(
                IDEDiagnosticIds.AnalyzerChangedId,
                FeaturesResources.ErrorCategory,
                message,
                ServicesVSResources.WRN_AnalyzerChangedMessage,
                severity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 0,
                workspace: _workspace,
                projectId: projectId,
                title: ServicesVSResources.WRN_AnalyzerChangedTitle);

            _updateSource.UpdateDiagnosticsForProject(projectId, Tuple.Create(s_analyzerChangedErrorId, analyzerPath), SpecializedCollections.SingletonEnumerable(data));
        }

        private DateTime? GetLastUpdateTimeUtc(string fullPath)
        {
            try
            {
                DateTime creationTimeUtc = File.GetCreationTimeUtc(fullPath);
                DateTime writeTimeUtc = File.GetLastWriteTimeUtc(fullPath);

                return writeTimeUtc > creationTimeUtc ? writeTimeUtc : creationTimeUtc;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        internal void AddPath(string filePath)
        {
            lock (_guard)
            {
                FileChangeTracker tracker;
                if (!_fileChangeTrackers.TryGetValue(filePath, out tracker))
                {
                    tracker = new FileChangeTracker(_fileChangeService, filePath);
                    tracker.UpdatedOnDisk += Tracker_UpdatedOnDisk;
                    tracker.StartFileChangeListeningAsync();

                    _fileChangeTrackers.Add(filePath, tracker);
                }

                DateTime? fileUpdateTime = GetLastUpdateTimeUtc(filePath);

                if (fileUpdateTime.HasValue)
                {
                    _assemblyUpdatedTimesUtc[filePath] = fileUpdateTime.Value;
                }
            }
        }

        private void Tracker_UpdatedOnDisk(object sender, EventArgs e)
        {
            FileChangeTracker tracker = (FileChangeTracker)sender;
            var filePath = tracker.FilePath;

            lock (_guard)
            {
                // Once we've created a diagnostic for a given analyzer file, there's
                // no need to keep watching it.
                _fileChangeTrackers.Remove(filePath);
            }

            tracker.Dispose();
            tracker.UpdatedOnDisk -= Tracker_UpdatedOnDisk;

            // Traverse the chain of requesting assemblies to get back to the original analyzer
            // assembly.
            var projectsWithAnalyzer = _workspace.ProjectTracker.Projects.Where(p => p.CurrentProjectAnalyzersContains(filePath)).ToArray();
            foreach (var project in projectsWithAnalyzer)
            {
                RaiseAnalyzerChangedWarning(project.Id, filePath);
            }
        }
    }
}
