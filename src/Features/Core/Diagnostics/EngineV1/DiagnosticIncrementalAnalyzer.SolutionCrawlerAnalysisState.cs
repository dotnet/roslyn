// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        // PERF: Keep track of the current solution crawler analysis state for each project, so that we can reduce memory pressure by disposing off the per-project CompilationWithAnalyzers instances when appropriate.
        private class SolutionCrawlerAnalysisState
        {
            private readonly ConditionalWeakTable<Project, ProjectAnalysisState> _projectAnalysisStateCache;
            private readonly HostAnalyzerManager _hostAnalyzerManager;
            private DateTime _lastCacheCleanupTime;

            // Only keep hold CompilationWithAnalyzers instances for projects whose documents were analyzed within the last minute.
            // TODO: Tune this cache cleanup interval based on performance measurements.
            private const int cacheCleanupIntervalInMinutes = 1;

            public SolutionCrawlerAnalysisState(HostAnalyzerManager hostAnalyzerManager)
            {
                _projectAnalysisStateCache = new ConditionalWeakTable<Project, ProjectAnalysisState>();
                _hostAnalyzerManager = hostAnalyzerManager;
                _lastCacheCleanupTime = DateTime.UtcNow;
            }

            private class ProjectAnalysisState
            {
                public HashSet<Guid> PendingDocumentsOrProject { get; set; }
                public DateTime LastAccessTime { get; set; }
            }

            private ProjectAnalysisState CreateProjectAnalysisState(Project project)
            {
                return new ProjectAnalysisState
                {
                    PendingDocumentsOrProject = new HashSet<Guid>(project.Documents.Select(d => d.Id.Id).Concat(project.Id.Id)),
                    LastAccessTime = DateTime.UtcNow
                };
            }

            public void OnDocumentAnalyzed(Document document)
            {
                OnDocumentOrProjectAnalyzed(document.Id.Id, document.Project);
            }

            public void OnProjectAnalyzed(Project project)
            {
                OnDocumentOrProjectAnalyzed(project.Id.Id, project);
            }

            private void OnDocumentOrProjectAnalyzed(Guid documentOrProjectGuid, Project project)
            {
                if (!project.SupportsCompilation)
                {
                    return;
                }

                var currentTime = DateTime.UtcNow;
                var projectAnalysisState = _projectAnalysisStateCache.GetValue(project, CreateProjectAnalysisState);

                projectAnalysisState.PendingDocumentsOrProject.Remove(documentOrProjectGuid);
                projectAnalysisState.LastAccessTime = currentTime;

                if (projectAnalysisState.PendingDocumentsOrProject.Count == 0)
                {
                    // PERF: We have computed and cached all documents and project diagnostics for the given project, so drop the CompilationWithAnalyzers instance that also caches all these diagnostics.
                    _projectAnalysisStateCache.Remove(project);
                    _hostAnalyzerManager.DisposeCompilationWithAnalyzers(project);
                }

                var minutesSinceCleanup = (currentTime - _lastCacheCleanupTime).TotalMinutes;
                if (minutesSinceCleanup >= cacheCleanupIntervalInMinutes)
                {
                    // PERF: For projects which haven't been analyzed recently, drop the CompilationWithAnalyzers instance to reduce memory pressure.
                    //       Subsequent diagnostic request with instantiate a new CompilationWithAnalyzers for these projects.
                    foreach (var p in project.Solution.Projects)
                    {
                        ProjectAnalysisState state;
                        if (_projectAnalysisStateCache.TryGetValue(p, out state))
                        {
                            var timeSinceLastAccess = currentTime - state.LastAccessTime;
                            if (timeSinceLastAccess.TotalMinutes >= cacheCleanupIntervalInMinutes)
                            {
                                _hostAnalyzerManager.DisposeCompilationWithAnalyzers(p);
                                state.LastAccessTime = currentTime;
                            }
                        }
                    }

                    _lastCacheCleanupTime = currentTime;
                }
            }
        }
    }
}
