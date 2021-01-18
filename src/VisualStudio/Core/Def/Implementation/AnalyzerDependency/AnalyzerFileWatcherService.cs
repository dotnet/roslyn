// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
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
        private static readonly object s_analyzerChangedErrorId = new();

        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly HostDiagnosticUpdateSource _updateSource;
        private readonly IVsFileChangeEx _fileChangeService;

        private readonly Dictionary<string, FileChangeTracker> _fileChangeTrackers = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Holds a list of assembly modified times that we can use to detect a file change prior to the <see cref="FileChangeTracker"/> being in place.
        /// Once it's in place and subscribed, we'll remove the entry because any further changes will be detected that way.
        /// </summary>
        private readonly Dictionary<string, DateTime> _assemblyUpdatedTimesUtc = new(StringComparer.OrdinalIgnoreCase);

        private readonly object _guard = new();

        private readonly DiagnosticDescriptor _analyzerChangedRule = new(
            id: IDEDiagnosticIds.AnalyzerChangedId,
            title: ServicesVSResources.AnalyzerChangedOnDisk,
            messageFormat: ServicesVSResources.The_analyzer_assembly_0_has_changed_Diagnostics_may_be_incorrect_until_Visual_Studio_is_restarted,
            category: FeaturesResources.Roslyn_HostError,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AnalyzerFileWatcherService(
            VisualStudioWorkspaceImpl workspace,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            SVsServiceProvider serviceProvider)
        {
            _workspace = workspace;
            _updateSource = hostDiagnosticUpdateSource;
            _fileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));
        }
        internal void RemoveAnalyzerAlreadyLoadedDiagnostics(ProjectId projectId, string analyzerPath)
            => _updateSource.ClearDiagnosticsForProject(projectId, Tuple.Create(s_analyzerChangedErrorId, analyzerPath));

        private void RaiseAnalyzerChangedWarning(ProjectId projectId, string analyzerPath)
        {
            var messageArguments = new string[] { analyzerPath };

            var project = _workspace.CurrentSolution.GetProject(projectId);
            if (project != null && DiagnosticData.TryCreate(_analyzerChangedRule, messageArguments, project, out var diagnostic))
            {
                _updateSource.UpdateDiagnosticsForProject(projectId, Tuple.Create(s_analyzerChangedErrorId, analyzerPath), SpecializedCollections.SingletonEnumerable(diagnostic));
            }
        }

        private static DateTime? GetLastUpdateTimeUtc(string fullPath)
        {
            try
            {
                var creationTimeUtc = File.GetCreationTimeUtc(fullPath);
                var writeTimeUtc = File.GetLastWriteTimeUtc(fullPath);

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

        internal void TrackFilePathAndReportErrorIfChanged(string filePath, ProjectId projectId)
        {
            lock (_guard)
            {
                if (!_fileChangeTrackers.TryGetValue(filePath, out var tracker))
                {
                    tracker = new FileChangeTracker(_fileChangeService, filePath);
                    tracker.UpdatedOnDisk += Tracker_UpdatedOnDisk;
                    _ = tracker.StartFileChangeListeningAsync();

                    _fileChangeTrackers.Add(filePath, tracker);
                }

                if (_assemblyUpdatedTimesUtc.TryGetValue(filePath, out var assemblyUpdatedTime))
                {
                    var currentFileUpdateTime = GetLastUpdateTimeUtc(filePath);

                    if (currentFileUpdateTime != null)
                    {
                        if (currentFileUpdateTime != assemblyUpdatedTime)
                        {
                            RaiseAnalyzerChangedWarning(projectId, filePath);
                        }

                        // If the the tracker is in place, at this point we can stop checking any further for this assembly
                        if (tracker.PreviousCallToStartFileChangeHasAsynchronouslyCompleted)
                        {
                            _assemblyUpdatedTimesUtc.Remove(filePath);
                        }
                    }
                }
                else
                {
                    // We don't have an assembly updated time. This means we either haven't ever checked it, or we have a file watcher in place.
                    // If the file watcher is in place, then nothing further to do. Otherwise we'll add the update time to the map for future checking
                    if (!tracker.PreviousCallToStartFileChangeHasAsynchronouslyCompleted)
                    {
                        var currentFileUpdateTime = GetLastUpdateTimeUtc(filePath);

                        if (currentFileUpdateTime != null)
                        {
                            _assemblyUpdatedTimesUtc[filePath] = currentFileUpdateTime.Value;
                        }
                    }
                }
            }
        }

        private void Tracker_UpdatedOnDisk(object sender, EventArgs e)
        {
            var tracker = (FileChangeTracker)sender;
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
            foreach (var project in _workspace.CurrentSolution.Projects)
            {
                var analyzerFileReferences = project.AnalyzerReferences.OfType<AnalyzerFileReference>();

                if (analyzerFileReferences.Any(a => a.FullPath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
                {
                    RaiseAnalyzerChangedWarning(project.Id, filePath);
                }
            }
        }
    }
}
