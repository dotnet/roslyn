// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;
using static Roslyn.Utilities.PortableShim;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    [Shared]
    [ExportIncrementalAnalyzerProvider(WorkspaceKind.Host)]
    [ExportWorkspaceServiceFactory(typeof(ISymbolTreeInfoCacheService))]
    internal class SymbolTreeInfoIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider, IWorkspaceServiceFactory
    {
        private class ProjectInfo
        {
            public readonly VersionStamp VersionStamp;
            public readonly SymbolTreeInfo SymbolTreeInfo;

            public ProjectInfo(VersionStamp versionStamp, SymbolTreeInfo info)
            {
                VersionStamp = versionStamp;
                SymbolTreeInfo = info;
            }
        }

        private class MetadataInfo
        {
            public readonly DateTime TimeStamp;
            public readonly SymbolTreeInfo SymbolTreeInfo;
            public readonly HashSet<ProjectId> ReferencingProjects;

            public MetadataInfo(DateTime timeStamp, SymbolTreeInfo info, HashSet<ProjectId> referencingProjects)
            {
                TimeStamp = timeStamp;
                SymbolTreeInfo = info;
                ReferencingProjects = referencingProjects;
            }
        }

        // Concurrent dictionaries so they can be read from the SymbolTreeInfoCacheService while 
        // they are being populated/updated by the IncrementalAnalyzer.
        private readonly ConcurrentDictionary<ProjectId, ProjectInfo> _projectToInfo = new ConcurrentDictionary<ProjectId, ProjectInfo>();
        private readonly ConcurrentDictionary<string, MetadataInfo> _metadataPathToInfo = new ConcurrentDictionary<string, MetadataInfo>();

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new IncrementalAnalyzer(_projectToInfo, _metadataPathToInfo);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new SymbolTreeInfoCacheService(_projectToInfo, _metadataPathToInfo);
        }

        private static string GetReferenceKey(PortableExecutableReference reference)
        {
            return reference.FilePath ?? reference.Display;
        }

        private static bool TryGetLastWriteTime(string path, out DateTime time)
        {
            var succeeded = false;
            time = IOUtilities.PerformIO(
                () =>
                {
                    var result = File.GetLastWriteTimeUtc(path);
                    succeeded = true;
                    return result;
                },
                default(DateTime));

            return succeeded;
        }

        private class SymbolTreeInfoCacheService : ISymbolTreeInfoCacheService
        {
            private readonly ConcurrentDictionary<ProjectId, ProjectInfo> _projectToInfo;
            private readonly ConcurrentDictionary<string, MetadataInfo> _metadataPathToInfo;

            public SymbolTreeInfoCacheService(
                ConcurrentDictionary<ProjectId, ProjectInfo> projectToInfo,
                ConcurrentDictionary<string, MetadataInfo> metadataPathToInfo)
            {
                _projectToInfo = projectToInfo;
                _metadataPathToInfo = metadataPathToInfo;
            }

            public Task<ValueTuple<bool, SymbolTreeInfo>> TryGetSymbolTreeInfoAsync(PortableExecutableReference reference, CancellationToken cancellationToken)
            {
                var key = GetReferenceKey(reference);
                if (key != null)
                {
                    MetadataInfo metadataInfo;
                    if (_metadataPathToInfo.TryGetValue(key, out metadataInfo))
                    {
                        DateTime writeTime;
                        if (TryGetLastWriteTime(key, out writeTime) && writeTime == metadataInfo.TimeStamp)
                        {
                            return Task.FromResult(ValueTuple.Create(true, metadataInfo.SymbolTreeInfo));
                        }
                    }
                }

                return Task.FromResult(default(ValueTuple<bool, SymbolTreeInfo>));
            }

            public async Task<ValueTuple<bool, SymbolTreeInfo>> TryGetSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken)
            {
                ProjectInfo projectInfo;
                if (_projectToInfo.TryGetValue(project.Id, out projectInfo))
                {
                    var version = await project.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                    if (version == projectInfo.VersionStamp)
                    {
                        return ValueTuple.Create(true, projectInfo.SymbolTreeInfo);
                    }
                }

                return default(ValueTuple<bool, SymbolTreeInfo>);
            }
        }

        private class IncrementalAnalyzer : IncrementalAnalyzerBase
        {
            private readonly ConcurrentDictionary<ProjectId, ProjectInfo> _projectToInfo;

            // Note: the Incremental-Analyzer infrastructure guarantees that it will call all the methods
            // on this type in a serial fashion.  As such, we don't need explicit locking, or threadsafe
            // collections (if they're only used by this type).  So, for example, the map we populate
            // needs to be a ConcurrentDictionary as it will be read and written from multiple types.
            // However, the HashSet<ProjectId> is ok as it will only be used by this type and there is
            // no concurrency in this type on its own.
            private readonly ConcurrentDictionary<string, MetadataInfo> _metadataPathToInfo;

            public IncrementalAnalyzer(
                ConcurrentDictionary<ProjectId, ProjectInfo> projectToInfo,
                ConcurrentDictionary<string, MetadataInfo> metadataPathToInfo)
            {
                _projectToInfo = projectToInfo;
                _metadataPathToInfo = metadataPathToInfo;
            }

            public override async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
            {
                if (!project.SupportsCompilation)
                {
                    return;
                }

                await UpdateReferencesAync(project, cancellationToken).ConfigureAwait(false);

                var version = await project.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                ProjectInfo projectInfo;
                if (_projectToInfo.TryGetValue(project.Id, out projectInfo) && projectInfo.VersionStamp == version)
                {
                    return;
                }

                var info = await SymbolTreeInfo.GetInfoForSourceAssemblyAsync(project, cancellationToken).ConfigureAwait(false);
                projectInfo = new ProjectInfo(version, info);
                _projectToInfo.AddOrUpdate(project.Id, projectInfo, (_1, _2) => projectInfo);
            }

            private async Task UpdateReferencesAync(Project project, CancellationToken cancellationToken)
            {
                Compilation compilation = null;
                foreach (var reference in project.MetadataReferences.OfType<PortableExecutableReference>())
                {
                    compilation = await UpdateReferenceAsync(project, reference, compilation, cancellationToken).ConfigureAwait(false);
                }
            }

            private async Task<Compilation> UpdateReferenceAsync(
                Project project, PortableExecutableReference reference, Compilation compilation, CancellationToken cancellationToken)
            {
                var key = GetReferenceKey(reference);
                if (key != null)
                {
                    DateTime lastWriteTime;
                    if (!TryGetLastWriteTime(key, out lastWriteTime))
                    {
                        // Couldn't get the write time.  Just ignore this reference.
                        return compilation;
                    }

                    MetadataInfo metadataInfo;
                    if (_metadataPathToInfo.TryGetValue(key, out metadataInfo) && metadataInfo.TimeStamp == lastWriteTime) 
                    {
                        // We've already computed and cached the info for this reference.
                        return compilation;
                    }

                    compilation = compilation ?? await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var assembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                    if (assembly != null)
                    {
                        var info = await SymbolTreeInfo.TryGetInfoForMetadataAssemblyAsync(project.Solution, assembly, reference, cancellationToken).ConfigureAwait(false);
                        metadataInfo = metadataInfo ?? new MetadataInfo(lastWriteTime, info, new HashSet<ProjectId>());

                        // Keep track that this dll is referenced by this project.
                        metadataInfo.ReferencingProjects.Add(project.Id);

                        _metadataPathToInfo.AddOrUpdate(key, metadataInfo, (_1, _2) => metadataInfo);
                    }
                }

                return compilation;
            }

            public override void RemoveProject(ProjectId projectId)
            {
                ProjectInfo tuple;
                _projectToInfo.TryRemove(projectId, out tuple);

                RemoveMetadataReferences(projectId);
            }

            private void RemoveMetadataReferences(ProjectId projectId)
            {
                foreach (var kvp in _metadataPathToInfo.ToArray())
                {
                    var tuple = kvp.Value;
                    if (kvp.Value.ReferencingProjects.Remove(projectId))
                    {
                        if (kvp.Value.ReferencingProjects.Count == 0)
                        {
                            // This metadata dll isn't referenced by any project.  We can just dump it.
                            MetadataInfo unneeded;
                            _metadataPathToInfo.TryRemove(kvp.Key, out unneeded);
                        }
                    }
                }
            }
        }
    }
}
