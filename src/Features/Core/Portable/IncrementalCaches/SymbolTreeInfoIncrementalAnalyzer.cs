// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    internal partial class SymbolTreeInfoIncrementalAnalyzerProvider
    {
        private class SymbolTreeInfoIncrementalAnalyzer : IncrementalAnalyzerBase
        {
            // Shared with SymbolTreeInfoCacheService.  We populate the values, they read from them.

            private readonly ConcurrentDictionary<ProjectId, SymbolTreeInfo> _projectIdToInfo;
            private readonly ConcurrentDictionary<MetadataId, MetadataInfo> _metadataIdToInfo;

            public SymbolTreeInfoIncrementalAnalyzer(
                ConcurrentDictionary<ProjectId, SymbolTreeInfo> projectToInfo,
                ConcurrentDictionary<MetadataId, MetadataInfo> metadataIdToInfo)
            {
                _projectIdToInfo = projectToInfo;
                _metadataIdToInfo = metadataIdToInfo;
            }

            private static bool SupportAnalysis(Project project)
                => project.SupportsCompilation;

            public override async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (!SupportAnalysis(document.Project))
                    return;

                if (bodyOpt != null)
                {
                    // This was a method body edit.  We can reuse the existing SymbolTreeInfo if
                    // we have one.  We can't just bail out here as the change in the document means
                    // we'll have a new checksum.  We need to get that new checksum so that our
                    // cached information is valid.
                    if (_projectIdToInfo.TryGetValue(document.Project.Id, out var cachedInfo))
                    {
                        var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(
                            document.Project, cancellationToken).ConfigureAwait(false);

                        var newInfo = cachedInfo.WithChecksum(checksum);
                        _projectIdToInfo[document.Project.Id] = newInfo;
                        return;
                    }
                }

                await UpdateSymbolTreeInfoAsync(document.Project, cancellationToken).ConfigureAwait(false);
            }

            public override Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (!SupportAnalysis(project))
                    return Task.CompletedTask;

                return UpdateSymbolTreeInfoAsync(project, cancellationToken);
            }

            private async Task UpdateSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken)
            {
                Debug.Assert(SupportAnalysis(project));

                // Produce the indices for the source and metadata symbols in parallel.
                using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

                tasks.Add(Task.Run(() => this.UpdateSourceSymbolTreeInfoAsync(project, cancellationToken), cancellationToken));
                tasks.Add(Task.Run(() => this.UpdateReferencesAsync(project, cancellationToken), cancellationToken));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            private async Task UpdateSourceSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken)
            {
                var checksum = await SymbolTreeInfo.GetSourceSymbolsChecksumAsync(project, cancellationToken).ConfigureAwait(false);
                if (!_projectIdToInfo.TryGetValue(project.Id, out var projectInfo) ||
                    projectInfo.Checksum != checksum)
                {
                    projectInfo = await SymbolTreeInfo.GetInfoForSourceAssemblyAsync(
                        project, checksum, loadOnly: false, cancellationToken).ConfigureAwait(false);

                    Contract.ThrowIfNull(projectInfo);
                    Contract.ThrowIfTrue(projectInfo.Checksum != checksum, "If we computed a SymbolTreeInfo, then its checksum much match our checksum.");

                    // Mark that we're up to date with this project.  Future calls with the same 
                    // semantic version can bail out immediately.
                    _projectIdToInfo[project.Id] = projectInfo;
                }
            }

            private async Task UpdateReferencesAsync(Project project, CancellationToken cancellationToken)
            {
                // Process all metadata references. If it remote workspace, do this in parallel.
                using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

                foreach (var reference in project.MetadataReferences.OfType<PortableExecutableReference>())
                    tasks.Add(Task.Run(() => this.UpdateReferenceAsync(project, reference, cancellationToken), cancellationToken));

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            private async Task UpdateReferenceAsync(
                Project project, PortableExecutableReference reference, CancellationToken cancellationToken)
            {
                var metadataId = SymbolTreeInfo.GetMetadataIdNoThrow(reference);
                if (metadataId == null)
                    return;

                var checksum = SymbolTreeInfo.GetMetadataChecksum(project.Solution, reference, cancellationToken);
                if (!_metadataIdToInfo.TryGetValue(metadataId, out var metadataInfo) ||
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
                    _metadataIdToInfo[metadataId] = metadataInfo;
                }

                // Keep track that this dll is referenced by this project.
                metadataInfo.ReferencingProjects.Add(project.Id);
            }

            public override Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
            {
                _projectIdToInfo.TryRemove(projectId, out _);
                RemoveMetadataReferences(projectId);

                return Task.CompletedTask;
            }

            private void RemoveMetadataReferences(ProjectId projectId)
            {
                foreach (var (id, info) in _metadataIdToInfo.ToArray())
                {
                    info.ReferencingProjects.Remove(projectId);

                    // If this metadata dll isn't referenced by any project.  We can just dump it.
                    if (info.ReferencingProjects.Count == 0)
                        _metadataIdToInfo.TryRemove(id, out _);
                }
            }
        }
    }
}
