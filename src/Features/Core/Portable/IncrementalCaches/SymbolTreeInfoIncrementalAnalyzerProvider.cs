// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    /// <summary>
    /// Features like add-using want to be able to quickly search symbol indices for projects and
    /// metadata.  However, creating those indices can be expensive.  As such, we don't want to
    /// construct them during the add-using process itself.  Instead, we expose this type as an 
    /// Incremental-Analyzer to walk our projects/metadata in the background to keep the indices
    /// up to date.
    /// 
    /// We also then export this type as a service that can give back the index for a project or
    /// metadata dll on request.  If the index has been produced then it will be returned and 
    /// can be used by add-using.  Otherwise, nothing is returned and no results will be found.
    /// 
    /// This means that as the project is being indexed, partial results may be returned.  However
    /// once it is fully indexed, then total results will be returned.
    /// </summary>
    [Shared]
    [ExportIncrementalAnalyzerProvider(nameof(SymbolTreeInfoIncrementalAnalyzerProvider), new[] { WorkspaceKind.Host, WorkspaceKind.RemoteWorkspace })]
    [ExportWorkspaceServiceFactory(typeof(ISymbolTreeInfoCacheService))]
    internal class SymbolTreeInfoIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider, IWorkspaceServiceFactory
    {
        private struct MetadataInfo
        {
            /// <summary>
            /// Can't be null.  Even if we weren't able to read in metadata, we'll still create an empty
            /// index.
            /// </summary>
            public readonly SymbolTreeInfo SymbolTreeInfo;

            /// <summary>
            /// Note: the Incremental-Analyzer infrastructure guarantees that it will call all the methods
            /// on <see cref="IncrementalAnalyzer"/> in a serial fashion.  As that is the only type that
            /// reads/writes these <see cref="MetadataInfo"/> objects, we don't need to lock this.
            /// </summary>
            public readonly HashSet<ProjectId> ReferencingProjects;

            public MetadataInfo(SymbolTreeInfo info, HashSet<ProjectId> referencingProjects)
            {
                Contract.ThrowIfNull(info);
                SymbolTreeInfo = info;
                ReferencingProjects = referencingProjects;
            }
        }

        // Concurrent dictionaries so they can be read from the SymbolTreeInfoCacheService while 
        // they are being populated/updated by the IncrementalAnalyzer.
        private readonly ConcurrentDictionary<ProjectId, SymbolTreeInfo> _projectToInfo = new ConcurrentDictionary<ProjectId, SymbolTreeInfo>();
        private readonly ConcurrentDictionary<string, MetadataInfo> _metadataPathToInfo = new ConcurrentDictionary<string, MetadataInfo>();

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            var cacheService = workspace.Services.GetService<IWorkspaceCacheService>();
            if (cacheService != null)
            {
                cacheService.CacheFlushRequested += OnCacheFlushRequested;
            }

            return new IncrementalAnalyzer(_projectToInfo, _metadataPathToInfo);
        }

        private void OnCacheFlushRequested(object sender, EventArgs e)
        {
            // If we hear about low memory conditions, flush our caches.  This will degrade the 
            // experience a bit (as we will no longer offer to Add-Using for p2p refs/metadata),
            // but will be better than OOM'ing.  These caches will be regenerated in the future
            // when the incremental analyzer reanalyzers the projects in teh workspace.
            _projectToInfo.Clear();
            _metadataPathToInfo.Clear();
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new SymbolTreeInfoCacheService(_projectToInfo, _metadataPathToInfo);

        private static string GetReferenceKey(PortableExecutableReference reference)
            => reference.FilePath ?? reference.Display;

        private class SymbolTreeInfoCacheService : ISymbolTreeInfoCacheService
        {
            private readonly ConcurrentDictionary<ProjectId, SymbolTreeInfo> _projectToInfo;
            private readonly ConcurrentDictionary<string, MetadataInfo> _metadataPathToInfo;

            public SymbolTreeInfoCacheService(
                ConcurrentDictionary<ProjectId, SymbolTreeInfo> projectToInfo,
                ConcurrentDictionary<string, MetadataInfo> metadataPathToInfo)
            {
                _projectToInfo = projectToInfo;
                _metadataPathToInfo = metadataPathToInfo;
            }

            public async Task<SymbolTreeInfo> TryGetMetadataSymbolTreeInfoAsync(
                Solution solution,
                PortableExecutableReference reference,
                CancellationToken cancellationToken)
            {
                var checksum = SymbolTreeInfo.GetMetadataChecksum(solution, reference, cancellationToken);

                var key = GetReferenceKey(reference);
                if (key != null)
                {
                    if (_metadataPathToInfo.TryGetValue(key, out var metadataInfo) &&
                        metadataInfo.SymbolTreeInfo.Checksum == checksum)
                    {
                        return metadataInfo.SymbolTreeInfo;
                    }
                }

                // If we didn't have it in our cache, see if we can load it from disk.
                // Note: pass 'loadOnly' so we only attempt to load from disk, not to actually
                // try to create the metadata.
                var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                    solution, reference, checksum, loadOnly: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                return info;
            }

            public async Task<SymbolTreeInfo> TryGetSourceSymbolTreeInfoAsync(
                Project project, CancellationToken cancellationToken)
            {
                if (_projectToInfo.TryGetValue(project.Id, out var projectInfo) &&
                    projectInfo.Checksum == await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false))
                {
                    return projectInfo;
                }

                return null;
            }
        }

        private class IncrementalAnalyzer : IncrementalAnalyzerBase
        {
            private readonly ConcurrentDictionary<ProjectId, SymbolTreeInfo> _projectToInfo;
            private readonly ConcurrentDictionary<string, MetadataInfo> _metadataPathToInfo;

            public IncrementalAnalyzer(
                ConcurrentDictionary<ProjectId, SymbolTreeInfo> projectToInfo,
                ConcurrentDictionary<string, MetadataInfo> metadataPathToInfo)
            {
                _projectToInfo = projectToInfo;
                _metadataPathToInfo = metadataPathToInfo;
            }

            public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (!document.SupportsSyntaxTree)
                {
                    // Not a language we can produce indices for (i.e. TypeScript).  Bail immediately.
                    return SpecializedTasks.EmptyTask;
                }

                if (bodyOpt != null)
                {
                    // This was a method level edit.  This can't change the symbol tree info
                    // for this project.  Bail immediately.
                    return SpecializedTasks.EmptyTask;
                }

                return UpdateSymbolTreeInfoAsync(document.Project, cancellationToken);
            }

            public override Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return UpdateSymbolTreeInfoAsync(project, cancellationToken);
            }

            private async Task UpdateSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken)
            {
                if (!project.SupportsCompilation)
                {
                    // Not a language we can produce indices for (i.e. TypeScript).  Bail immediately.
                    return;
                }

                if (!RemoteFeatureOptions.ShouldComputeIndex(project.Solution.Workspace))
                {
                    return;
                }

                // Produce the indices for the source and metadata symbols in parallel.
                var projectTask = UpdateSourceSymbolTreeInfoAsync(project, cancellationToken);
                var referencesTask = UpdateReferencesAync(project, cancellationToken);

                await Task.WhenAll(projectTask, referencesTask).ConfigureAwait(false);
            }

            private async Task UpdateSourceSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken)
            {
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);
                if (!_projectToInfo.TryGetValue(project.Id, out var projectInfo) ||
                    projectInfo.Checksum != checksum)
                {
                    projectInfo = await SymbolTreeInfo.GetInfoForSourceAssemblyAsync(
                        project, checksum, cancellationToken).ConfigureAwait(false);

                    Contract.ThrowIfNull(projectInfo);
                    Contract.ThrowIfTrue(projectInfo.Checksum != checksum, "If we computed a SymbolTreeInfo, then its checksum much match our checksum.");

                    // Mark that we're up to date with this project.  Future calls with the same 
                    // semantic version can bail out immediately.
                    _projectToInfo.AddOrUpdate(project.Id, projectInfo, (_1, _2) => projectInfo);
                }
            }

            private Task UpdateReferencesAync(Project project, CancellationToken cancellationToken)
            {
                // Process all metadata references in parallel.
                var tasks = project.MetadataReferences.OfType<PortableExecutableReference>()
                                   .Select(r => UpdateReferenceAsync(project, r, cancellationToken))
                                   .ToArray();

                return Task.WhenAll(tasks);
            }

            private async Task UpdateReferenceAsync(
                Project project, PortableExecutableReference reference, CancellationToken cancellationToken)
            {
                var key = GetReferenceKey(reference);
                if (key == null)
                {
                    return;
                }

                var checksum = SymbolTreeInfo.GetMetadataChecksum(project.Solution, reference, cancellationToken);
                if (!_metadataPathToInfo.TryGetValue(key, out var metadataInfo) ||
                    metadataInfo.SymbolTreeInfo.Checksum != checksum)
                {
                    var info = await SymbolTreeInfo.GetInfoForMetadataReferenceAsync(
                        project.Solution, reference, checksum, loadOnly: false, cancellationToken: cancellationToken).ConfigureAwait(false);

                    Contract.ThrowIfNull(info);
                    Contract.ThrowIfTrue(info.Checksum != checksum, "If we computed a SymbolTreeInfo, then its checksum much match our checksum.");

                    // Note, getting the info may fail (for example, bogus metadata).  That's ok.  
                    // We still want to cache that result so that don't try to continuously produce
                    // this info over and over again.
                    metadataInfo = new MetadataInfo(info, metadataInfo.ReferencingProjects ?? new HashSet<ProjectId>());
                    _metadataPathToInfo.AddOrUpdate(key, metadataInfo, (_1, _2) => metadataInfo);
                }

                // Keep track that this dll is referenced by this project.
                metadataInfo.ReferencingProjects.Add(project.Id);
            }

            public override void RemoveProject(ProjectId projectId)
            {
                _projectToInfo.TryRemove(projectId, out var info);

                RemoveMetadataReferences(projectId);
            }

            private void RemoveMetadataReferences(ProjectId projectId)
            {
                foreach (var kvp in _metadataPathToInfo.ToArray())
                {
                    if (kvp.Value.ReferencingProjects.Remove(projectId))
                    {
                        if (kvp.Value.ReferencingProjects.Count == 0)
                        {
                            // This metadata dll isn't referenced by any project.  We can just dump it.
                            _metadataPathToInfo.TryRemove(kvp.Key, out var unneeded);
                        }
                    }
                }
            }
        }
    }
}