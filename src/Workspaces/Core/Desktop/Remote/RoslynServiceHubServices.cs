// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    // TODO: currently, service hub provide no other way to share services between user service hub services.
    //       only way to do so is using static type
    internal static class RoslynServiceHubServices
    {
        public static readonly HostServices HostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

        // PREVIEW: unfortunately, I need dummy workspace since workspace services can be workspace specific
        public static readonly Serializer Serializer = new Serializer(new AdhocWorkspace(HostServices, workspaceKind: "dummy").Services);

        public static readonly AssetManager Asset = new AssetManager();

        public static readonly CompilationService Compilation = new CompilationService();

        // TODO: this whole thing should be refactored/improved
        public class CompilationService
        {
            public async Task<Compilation> GetCompilationAsync(SolutionSnapshotId id, ProjectId projectId, CancellationToken cancellationToken)
            {
                var solution = await GetSolutionAsync(id, cancellationToken).ConfigureAwait(false);

                // TODO: need to figure out how to deal with cancellation and exceptions in service hub
                return await solution.GetProject(projectId).GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            }

            public async Task<Solution> GetSolutionAsync(SolutionSnapshotId snapshotId, CancellationToken cancellationToken)
            {
                var workspace = new AdhocWorkspace();
                var solutionInfo = await Asset.GetAssetAsync<SolutionSnapshotInfo>(snapshotId.Info).ConfigureAwait(false);

                var projects = new List<ProjectInfo>();
                foreach (var projectSnapshot in snapshotId.Projects.Objects)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var documents = new List<DocumentInfo>();
                    foreach (var documentSnapshot in projectSnapshot.Documents.Objects)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var documentInfo = await Asset.GetAssetAsync<DocumentSnapshotInfo>(documentSnapshot.Info).ConfigureAwait(false);
                        var text = await Asset.GetAssetAsync<SourceText>(documentSnapshot.Text).ConfigureAwait(false);

                        // TODO: do we need version?
                        documents.Add(
                            DocumentInfo.Create(
                                documentInfo.Id,
                                documentInfo.Name,
                                documentInfo.Folders,
                                documentInfo.SourceCodeKind,
                                TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())),
                                documentInfo.FilePath,
                                documentInfo.IsGenerated));
                    }

                    var p2p = new List<ProjectReference>();
                    foreach (var checksum in projectSnapshot.ProjectReferences.Objects)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var reference = await Asset.GetAssetAsync<ProjectReference>(checksum).ConfigureAwait(false);
                        p2p.Add(reference);
                    }

                    var metadata = new List<MetadataReference>();
                    foreach (var checksum in projectSnapshot.MetadataReferences.Objects)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var reference = await Asset.GetAssetAsync<MetadataReference>(checksum).ConfigureAwait(false);
                        metadata.Add(reference);
                    }

                    var analyzers = new List<AnalyzerReference>();
                    foreach (var checksum in projectSnapshot.AnalyzerReferences.Objects)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var reference = await Asset.GetAssetAsync<AnalyzerReference>(checksum).ConfigureAwait(false);
                        analyzers.Add(reference);
                    }

                    var additionals = new List<DocumentInfo>();
                    foreach (var documentSnapshot in projectSnapshot.AdditionalDocuments.Objects)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var documentInfo = await Asset.GetAssetAsync<DocumentSnapshotInfo>(documentSnapshot.Info).ConfigureAwait(false);
                        var text = await Asset.GetAssetAsync<SourceText>(documentSnapshot.Text).ConfigureAwait(false);

                        // TODO: do we need version?
                        additionals.Add(
                            DocumentInfo.Create(
                                documentInfo.Id,
                                documentInfo.Name,
                                documentInfo.Folders,
                                documentInfo.SourceCodeKind,
                                TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())),
                                documentInfo.FilePath,
                                documentInfo.IsGenerated));
                    }

                    var projectInfo = await Asset.GetAssetAsync<ProjectSnapshotInfo>(projectSnapshot.Info).ConfigureAwait(false);
                    var compilationOptions = await Asset.GetAssetAsync<CompilationOptions>(projectSnapshot.CompilationOptions).ConfigureAwait(false);
                    var parseOptions = await Asset.GetAssetAsync<ParseOptions>(projectSnapshot.ParseOptions).ConfigureAwait(false);

                    projects.Add(
                        ProjectInfo.Create(
                            projectInfo.Id, projectInfo.Version, projectInfo.Name, projectInfo.AssemblyName,
                            projectInfo.Language, projectInfo.FilePath, projectInfo.OutputFilePath,
                            compilationOptions, parseOptions,
                            documents, p2p, metadata, analyzers, additionals));
                }

                return workspace.AddSolution(SolutionInfo.Create(solutionInfo.Id, solutionInfo.Version, solutionInfo.FilePath, projects));
            }
        }

        public class AssetManager
        {
            private static int s_requestId = 0;

            private readonly ConcurrentDictionary<int, AssetSource> _assetSources =
                new ConcurrentDictionary<int, AssetSource>(concurrencyLevel: 4, capacity: 10);

            private readonly ConcurrentDictionary<int, TaskCompletionSource<bool>> _assetRequests =
                new ConcurrentDictionary<int, TaskCompletionSource<bool>>(concurrencyLevel: 4, capacity: 10);

            private readonly ConcurrentDictionary<Checksum, object> _assets =
                new ConcurrentDictionary<Checksum, object>(concurrencyLevel: 4, capacity: 10);

            public void Set(Checksum checksum, object @object)
            {
                // TODO: if checksum already exist, add some debug check to verify object is same thing
                //       currently, asset once added never get deleted. need to do lifetime management
                _assets.TryAdd(checksum, @object);
            }

            public async Task<T> GetAssetAsync<T>(Checksum checksum)
            {
                // TODO: need to figure out cancellation story
                object @object;
                if (_assets.TryGetValue(checksum, out @object))
                {
                    return (T)@object;
                }

                // TODO: what happen if service doesn't come back. timeout?
                await RequestAssetAsync(checksum).ConfigureAwait(false);

                if (!_assets.TryGetValue(checksum, out @object))
                {
                    Contract.Fail("how this can happen?");
                }

                return (T)@object;
            }

            public Task RequestAssetAsync(Checksum checksum)
            {
                var requestId = Interlocked.Add(ref s_requestId, 1);
                var taskSource = new TaskCompletionSource<bool>();

                Contract.ThrowIfFalse(_assetRequests.TryAdd(requestId, taskSource));

                // there must be one that knows about object with the checksum
                foreach (var kv in _assetSources)
                {
                    var serviceId = kv.Key;
                    var source = kv.Value;

                    try
                    {
                        // request asset to source
                        // we do this wierd stuff since service hub doesn't allow service to open new stream to client
                        source.RequestAsset(serviceId, requestId, checksum);
                    }
                    catch (Exception)
                    {
                        // TODO: we need better way than this
                        // connection is closed from other side.
                        continue;
                    }

                    break;
                }

                return taskSource.Task;
            }

            public void CloseAssetRequest(int requestId, bool result)
            {
                // this will be called when we actually get asset from client
                TaskCompletionSource<bool> source;
                if (_assetRequests.TryRemove(requestId, out source))
                {
                    if (result)
                    {
                        source.TrySetResult(true);
                    }
                    else
                    {
                        // TODO: cancel? or exception
                        source.TrySetCanceled();
                    }
                }
            }

            public void RegisterAssetSource(int serviceId, AssetSource assetSource)
            {
                // TODO: do some lifetime management for assets we got
                Contract.ThrowIfFalse(_assetSources.TryAdd(serviceId, assetSource));
            }

            public void UnregisterAssetSource(int serviceId)
            {
                AssetSource dummy;
                _assetSources.TryRemove(serviceId, out dummy);
            }
        }
    }
}
