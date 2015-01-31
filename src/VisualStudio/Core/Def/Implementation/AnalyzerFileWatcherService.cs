// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
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
        private static object s_analyzerChangedErrorId = new object();

        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly HostDiagnosticUpdateSource _updateSource;
        private readonly IVsFileChangeEx _fileChangeService;

        private readonly Dictionary<string, FileChangeTracker> _fileChangeTrackers = new Dictionary<string, FileChangeTracker>(StringComparer.OrdinalIgnoreCase);

        private readonly object _fileChangeTrackersLock = new object();

        [ImportingConstructor]
        public AnalyzerFileWatcherService(
            VisualStudioWorkspaceImpl workspace,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            SVsServiceProvider serviceProvider)
        {
            _workspace = workspace;
            _updateSource = hostDiagnosticUpdateSource;
            _fileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));

            AnalyzerFileReference.AssemblyLoad += AnalyzerFileReference_AssemblyLoad;
        }

        private void AnalyzerFileReference_AssemblyLoad(object sender, AnalyzerAssemblyLoadEventArgs e)
        {
            lock (_fileChangeTrackersLock)
            {
                FileChangeTracker tracker;
                if (!_fileChangeTrackers.TryGetValue(e.Path, out tracker))
                {
                    tracker = new FileChangeTracker(_fileChangeService, e.Path);
                    tracker.UpdatedOnDisk += Tracker_UpdatedOnDisk;
                    tracker.StartFileChangeListeningAsync();

                    _fileChangeTrackers.Add(e.Path, tracker);
                }
            }
        }

        private void Tracker_UpdatedOnDisk(object sender, EventArgs e)
        {
            FileChangeTracker tracker = (FileChangeTracker)sender;
            var filePath = tracker.FilePath;

            lock (_fileChangeTrackersLock)
            {
                // Once we've created a diagnostic for a given analyzer file, there's
                // no need to keep watching it.
                _fileChangeTrackers.Remove(filePath);
            }

            tracker.Dispose();
            tracker.UpdatedOnDisk -= Tracker_UpdatedOnDisk;

            string id = ServicesVSResources.WRN_AnalyzerChangedId;
            string category = ServicesVSResources.ErrorCategory;
            string message = string.Format(ServicesVSResources.WRN_AnalyzerChangedMessage, filePath);

            // Traverse the chain of requesting assemblies to get back to the original analyzer
            // assembly.
            var assemblyPath = filePath;
            var requestingAssemblyPath = AnalyzerFileReference.TryGetRequestingAssemblyPath(filePath);
            while (requestingAssemblyPath != null)
            {
                assemblyPath = requestingAssemblyPath;
                requestingAssemblyPath = AnalyzerFileReference.TryGetRequestingAssemblyPath(assemblyPath);
            }

            var projectsWithAnalyzer = _workspace.ProjectTracker.Projects.Where(p => p.CurrentProjectAnalyzersContains(assemblyPath)).ToArray();
            foreach (var project in projectsWithAnalyzer)
            {
                DiagnosticData data = new DiagnosticData(
                    id,
                    category,
                    message,
                    ServicesVSResources.WRN_AnalyzerChangedMessage,
                    severity: DiagnosticSeverity.Warning,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    warningLevel: 0,
                    customTags: ImmutableArray<string>.Empty,
                    workspace: _workspace,
                    projectId: project.Id);

                _updateSource.UpdateDiagnosticsForProject(project.Id, Tuple.Create(s_analyzerChangedErrorId, filePath), SpecializedCollections.SingletonEnumerable(data));
            }
        }
    }
}
