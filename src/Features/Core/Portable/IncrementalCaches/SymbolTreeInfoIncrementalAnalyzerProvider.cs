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
        // Concurrent dictionaries so they can be read from the SymbolTreeInfoCacheService while 
        // they are being populated/updated by the IncrementalAnalyzer.
        private readonly ConcurrentDictionary<ProjectId, Tuple<VersionStamp, SymbolTreeInfo>> _projectToInfo = 
            new ConcurrentDictionary<ProjectId, Tuple<VersionStamp, SymbolTreeInfo>>();
        private readonly ConcurrentDictionary<string, Tuple<DateTime, SymbolTreeInfo, HashSet<ProjectId>>> _metadataPathToInfo =
            new ConcurrentDictionary<string, Tuple<DateTime, SymbolTreeInfo, HashSet<ProjectId>>>();

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

        private static ValueTuple<bool, DateTime> GetLastWriteTime(string path)
        {
            return IOUtilities.PerformIO(
                () => ValueTuple.Create(true, File.GetLastWriteTimeUtc(path)),
                ValueTuple.Create(false, default(DateTime)));
        }

        private class SymbolTreeInfoCacheService : ISymbolTreeInfoCacheService
        {
            private readonly ConcurrentDictionary<ProjectId, Tuple<VersionStamp, SymbolTreeInfo>> _projectToInfo;
            private readonly ConcurrentDictionary<string, Tuple<DateTime, SymbolTreeInfo, HashSet<ProjectId>>> _metadataPathToInfo;

            public SymbolTreeInfoCacheService(
                ConcurrentDictionary<ProjectId, Tuple<VersionStamp, SymbolTreeInfo>> projectToInfo,
                ConcurrentDictionary<string, Tuple<DateTime, SymbolTreeInfo, HashSet<ProjectId>>> metadataPathToInfo)
            {
                _projectToInfo = projectToInfo;
                _metadataPathToInfo = metadataPathToInfo;
            }

            public Task<ValueTuple<bool, SymbolTreeInfo>> TryGetSymbolTreeInfoAsync(PortableExecutableReference reference, CancellationToken cancellationToken)
            {
                var key = GetReferenceKey(reference);
                if (key != null)
                {
                    Tuple<DateTime, SymbolTreeInfo, HashSet<ProjectId>> tuple;
                    if (_metadataPathToInfo.TryGetValue(key, out tuple))
                    {
                        var version = GetLastWriteTime(key);
                        if (version.Item1 && version.Item2 == tuple.Item1)
                        {
                            return Task.FromResult(ValueTuple.Create(true, tuple.Item2));
                        }
                    }
                }

                return Task.FromResult(default(ValueTuple<bool, SymbolTreeInfo>));
            }

            public async Task<ValueTuple<bool, SymbolTreeInfo>> TryGetSymbolTreeInfoAsync(Project project, CancellationToken cancellationToken)
            {
                Tuple<VersionStamp, SymbolTreeInfo> tuple;
                if (_projectToInfo.TryGetValue(project.Id, out tuple))
                {
                    var version = await project.GetSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                    if (version == tuple.Item1)
                    {
                        return ValueTuple.Create(true, tuple.Item2);
                    }
                }

                return default(ValueTuple<bool, SymbolTreeInfo>);
            }
        }

        private class IncrementalAnalyzer : IncrementalAnalyzerBase
        {
            private readonly ConcurrentDictionary<ProjectId, Tuple<VersionStamp, SymbolTreeInfo>> _projectToInfo;

            // Note: the Incremental-Analyzer infrastructure guarantees that it will call all the methods
            // on this type in a serial fashion.  As such, we don't need explicit locking, or threadsafe
            // collections (if they're only used by this type).  So, for example, the map we populate
            // needs to be a ConcurrentDictionary as it will be read and written from multiple types.
            // However, the HashSet<ProjectId> is ok as it will only be used by this type and there is
            // no concurrency in this type on its own.
            private readonly ConcurrentDictionary<string, Tuple<DateTime, SymbolTreeInfo, HashSet<ProjectId>>> _metadataPathToInfo;

            public IncrementalAnalyzer(
                ConcurrentDictionary<ProjectId, Tuple<VersionStamp, SymbolTreeInfo>> projectToInfo,
                ConcurrentDictionary<string, Tuple<DateTime, SymbolTreeInfo, HashSet<ProjectId>>> metadataPathToInfo)
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
                Tuple<VersionStamp, SymbolTreeInfo> tuple;
                if (_projectToInfo.TryGetValue(project.Id, out tuple) && tuple.Item1 == version)
                {
                    return;
                }

                var info = await SymbolTreeInfo.GetInfoForSourceAssemblyAsync(project, cancellationToken).ConfigureAwait(false);
                tuple = Tuple.Create(version, info);
                _projectToInfo.AddOrUpdate(project.Id, tuple, (_1, _2) => tuple);
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
                    var lastWriteTime = GetLastWriteTime(key);
                    if (!lastWriteTime.Item1)
                    {
                        // Couldn't get the write time.  Just ignore this reference.
                        return compilation;
                    }

                    Tuple<DateTime, SymbolTreeInfo, HashSet<ProjectId>> tuple;
                    if (_metadataPathToInfo.TryGetValue(key, out tuple) && tuple.Item1 == lastWriteTime.Item2) 
                    {
                        // We've already computed and cached the info for this reference.
                        return compilation;
                    }

                    compilation = compilation ?? await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                    var assembly = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                    if (assembly != null)
                    {
                        var info = await SymbolTreeInfo.TryGetInfoForMetadataAssemblyAsync(project.Solution, assembly, reference, cancellationToken).ConfigureAwait(false);
                        tuple = tuple ?? Tuple.Create(lastWriteTime.Item2, info, new HashSet<ProjectId>());

                        // Keep track that this dll is referenced by this project.
                        tuple.Item3.Add(project.Id);

                        _metadataPathToInfo.AddOrUpdate(key, tuple, (_1, _2) => tuple);
                    }
                }

                return compilation;
            }

            public override void RemoveProject(ProjectId projectId)
            {
                Tuple<VersionStamp, SymbolTreeInfo> tuple;
                _projectToInfo.TryRemove(projectId, out tuple);

                RemoveMetadataReferences(projectId);
            }

            private void RemoveMetadataReferences(ProjectId projectId)
            {
                foreach (var kvp in _metadataPathToInfo.ToArray())
                {
                    var tuple = kvp.Value;
                    if (kvp.Value.Item3.Remove(projectId))
                    {
                        if (kvp.Value.Item3.Count == 0)
                        {
                            // This metadata dll isn't referenced by any project.  We can just dump it.
                            Tuple<DateTime, SymbolTreeInfo, HashSet<ProjectId>> unneeded;
                            _metadataPathToInfo.TryRemove(kvp.Key, out unneeded);
                        }
                    }
                }
            }
        }
    }
}
