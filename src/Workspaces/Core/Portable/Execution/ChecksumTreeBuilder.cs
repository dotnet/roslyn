// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Execution
{
    internal struct ChecksumTreeBuilder
    {
        private readonly Serializer _serializer;
        private readonly IChecksumTreeNode _checksumTree;

        public ChecksumTreeBuilder(IChecksumTreeNode checksumTree)
        {
            _checksumTree = checksumTree;
            _serializer = checksumTree.Serializer;
        }

        public Task<SolutionChecksumObject> BuildAsync(SolutionState solutionState, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(solutionState, solutionState, SolutionChecksumObject.Name, CreateSolutionChecksumObjectAsync, cancellationToken);
        }

        public Task<ProjectChecksumObject> BuildAsync(ProjectState projectState, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(projectState, projectState, ProjectChecksumObject.Name, CreateProjectChecksumObjectAsync, cancellationToken);
        }

        private Task<DocumentChecksumObject> BuildAsync(TextDocumentState documentState, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(documentState, documentState, DocumentChecksumObject.Name, CreateDocumentChecksumObjectAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(ImmutableDictionary<ProjectId, ProjectState> key, IEnumerable<ProjectState> projectStates, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(key, projectStates, WellKnownChecksumObjects.Projects, CreateProjectChecksumCollectionAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync<TState>(ImmutableDictionary<DocumentId, TState> key, IEnumerable<TextDocumentState> documentStates, string kind, CancellationToken cancellationToken)
            where TState : TextDocumentState
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(key, documentStates, kind, CreateDocumentChecksumObjectAsync, cancellationToken);
        }

        private ChecksumCollection Build(IReadOnlyList<ProjectReference> key, IEnumerable<ProjectReference> projectReferences, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildren(key, projectReferences, WellKnownChecksumObjects.ProjectReferences, CreateChecksumCollections, cancellationToken);
        }

        private ChecksumCollection Build(IReadOnlyList<MetadataReference> key, IEnumerable<MetadataReference> metadataReferences, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildren(key, metadataReferences, WellKnownChecksumObjects.MetadataReferences, CreateChecksumCollections, cancellationToken);
        }

        private ChecksumCollection Build(IReadOnlyList<AnalyzerReference> key, IEnumerable<AnalyzerReference> analyzerReferences, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildren(key, analyzerReferences, WellKnownChecksumObjects.AnalyzerReferences, CreateChecksumCollections, cancellationToken);
        }

        private async Task<SolutionChecksumObject> CreateSolutionChecksumObjectAsync(SolutionState key, SolutionState solutionState, string kind, CancellationToken cancellationToken)
        {
            var assetBuilder = new AssetBuilder(_checksumTree);
            var info = assetBuilder.Build(solutionState, cancellationToken);

            var subTreeNode = _checksumTree.GetOrCreateSubTreeNode(key);
            var subSnapshotBuilder = new ChecksumTreeBuilder(subTreeNode);

            var projects = await subSnapshotBuilder.BuildAsync(solutionState.ProjectStates, solutionState.ProjectIds.Select(id => solutionState.ProjectStates[id]), cancellationToken).ConfigureAwait(false);
            return new SolutionChecksumObject(_serializer, info.Checksum, projects);
        }

        private async Task<ProjectChecksumObject> CreateProjectChecksumObjectAsync(ProjectState key, ProjectState projectState, string kind, CancellationToken cancellationToken)
        {
            var assetBuilder = new AssetBuilder(_checksumTree);
            var info = assetBuilder.Build(projectState, cancellationToken);

            var subTreeNode = _checksumTree.GetOrCreateSubTreeNode(key);
            var subSnapshotBuilder = new ChecksumTreeBuilder(subTreeNode);

            var documents = await subSnapshotBuilder.BuildAsync(projectState.DocumentStates, projectState.DocumentIds.Select(id => projectState.DocumentStates[id]), WellKnownChecksumObjects.Documents, cancellationToken).ConfigureAwait(false);
            var projectReferences = subSnapshotBuilder.Build(projectState.ProjectReferences, projectState.ProjectReferences, cancellationToken);
            var metadataReferences = subSnapshotBuilder.Build(projectState.MetadataReferences, projectState.MetadataReferences, cancellationToken);
            var analyzerReferences = subSnapshotBuilder.Build(projectState.AnalyzerReferences, projectState.AnalyzerReferences, cancellationToken);
            var additionalDocuments = await subSnapshotBuilder.BuildAsync(projectState.AdditionalDocumentStates, projectState.AdditionalDocumentIds.Select(id => projectState.AdditionalDocumentStates[id]), WellKnownChecksumObjects.TextDocuments, cancellationToken).ConfigureAwait(false);

            var subAssetBuilder = new AssetBuilder(subTreeNode);

            // set Asset.Null if this particular project doesn't support compiler options.
            // this one is really bit wierd since project state has both compilation/parse options but only has support compilation. 
            // for now, we use support compilation for both options
            var compilationOptions = projectState.SupportsCompilation ? subAssetBuilder.Build(projectState, projectState.CompilationOptions, cancellationToken) : Asset.Null;
            var parseOptions = projectState.SupportsCompilation ? subAssetBuilder.Build(projectState, projectState.ParseOptions, cancellationToken) : Asset.Null;

            return new ProjectChecksumObject(
                _serializer, info.Checksum, compilationOptions.Checksum, parseOptions.Checksum,
                documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments);
        }

        private async Task<DocumentChecksumObject> CreateDocumentChecksumObjectAsync(TextDocumentState key, TextDocumentState documentState, string kind, CancellationToken cancellationToken)
        {
            var assetBuilder = new AssetBuilder(_checksumTree);
            var info = assetBuilder.Build(documentState, cancellationToken);

            // TODO: think of a way to skip getting text
            var sourceText = await key.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var text = assetBuilder.Build(key, sourceText, cancellationToken);

            return new DocumentChecksumObject(_serializer, info.Checksum, text.Checksum);
        }

        private Task<ChecksumCollection> CreateProjectChecksumCollectionAsync(
            ImmutableDictionary<ProjectId, ProjectState> key, IEnumerable<ProjectState> projectStates, string kind, CancellationToken cancellationToken)
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollectionTask(kind);
            }

            var snapshotBuilder = new ChecksumTreeBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollectionsAsync(projectStates, kind, snapshotBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateDocumentChecksumObjectAsync<TState>(
            ImmutableDictionary<DocumentId, TState> key, IEnumerable<TextDocumentState> documentStates, string kind, CancellationToken cancellationToken)
            where TState : TextDocumentState
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollectionTask(kind);
            }

            var snapshotBuilder = new ChecksumTreeBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollectionsAsync(documentStates, kind, snapshotBuilder.BuildAsync, cancellationToken);
        }

        private async Task<ChecksumCollection> CreateChecksumCollectionsAsync<TValue, TChecksumObject>(
            IEnumerable<TValue> items, string kind, Func<TValue, CancellationToken, Task<TChecksumObject>> buildAsync, CancellationToken cancellationToken) where TChecksumObject : ChecksumObject
        {
            using (var pooledObject = Creator.CreateList<Task<TChecksumObject>>())
            {
                // create asyn checksums concurrently
                var tasks = pooledObject.Object;
                foreach (var item in items)
                {
                    tasks.Add(buildAsync(item, cancellationToken));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                var checksums = new Checksum[tasks.Count];

                for (var i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];

                    // we use await here to make sure when exception is raised, especially cancellation exception,
                    // right exception is raised from task. if we use .Result directly, it will raise aggregated exception
                    // rather than cancellation exception.
                    // since task is already completed, when there is no exception for the task. await will be no-op
                    checksums[i] = (await task.ConfigureAwait(false)).Checksum;
                }

                return new ChecksumCollection(_serializer, kind, checksums);
            }
        }

        private ChecksumCollection CreateChecksumCollections(
            IReadOnlyList<ProjectReference> key, IEnumerable<ProjectReference> references, string kind, CancellationToken cancellationToken)
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollection(kind);
            }

            var assetBuilder = new AssetBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollections(references, kind, assetBuilder.Build, cancellationToken);
        }

        private ChecksumCollection CreateChecksumCollections(
            IReadOnlyList<MetadataReference> key, IEnumerable<MetadataReference> references, string kind, CancellationToken cancellationToken)
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollection(kind);
            }

            var assetBuilder = new AssetBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollections(references, kind, assetBuilder.Build, cancellationToken);
        }

        private ChecksumCollection CreateChecksumCollections(
            IReadOnlyList<AnalyzerReference> key, IEnumerable<AnalyzerReference> references, string kind, CancellationToken cancellationToken)
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollection(kind);
            }

            var assetBuilder = new AssetBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollections(references, kind, assetBuilder.Build, cancellationToken);
        }

        private ChecksumCollection CreateChecksumCollections<TValue>(IEnumerable<TValue> items, string kind, Func<TValue, CancellationToken, ChecksumObject> build, CancellationToken cancellationToken)
        {
            var list = new List<Checksum>();
            foreach (var item in items)
            {
                var checksumObject = build(item, cancellationToken);
                list.Add(checksumObject.Checksum);
            }

            return new ChecksumCollection(_serializer, kind, list.ToArray());
        }

        private ChecksumCollection GetEmptyChecksumCollection(string kind)
        {
            return ChecksumTreeCollection.GetOrCreateEmptyChecksumCollection(_serializer, kind);
        }

        private Task<ChecksumCollection> GetEmptyChecksumCollectionTask(string kind)
        {
            return ChecksumTreeCollection.GetOrCreateEmptyChecksumCollectionTask(_serializer, kind);
        }
    }
}
