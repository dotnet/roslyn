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

        private Task<ChecksumCollection> BuildAsync(IReadOnlyList<ProjectReference> key, IEnumerable<ProjectReference> projectReferences, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(key, projectReferences, WellKnownChecksumObjects.ProjectReferences, CreateChecksumCollectionsAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(IReadOnlyList<MetadataReference> key, IEnumerable<MetadataReference> metadataReferences, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(key, metadataReferences, WellKnownChecksumObjects.MetadataReferences, CreateChecksumCollectionsAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(IReadOnlyList<AnalyzerReference> key, IEnumerable<AnalyzerReference> analyzerReferences, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(key, analyzerReferences, WellKnownChecksumObjects.AnalyzerReferences, CreateChecksumCollectionsAsync, cancellationToken);
        }

        private async Task<SolutionChecksumObject> CreateSolutionChecksumObjectAsync(SolutionState key, SolutionState solutionState, string kind, CancellationToken cancellationToken)
        {
            var assetBuilder = new AssetBuilder(_checksumTree);
            var info = await assetBuilder.BuildAsync(solutionState, cancellationToken).ConfigureAwait(false);

            var subTreeNode = _checksumTree.GetOrCreateSubTreeNode(key);
            var subSnapshotBuilder = new ChecksumTreeBuilder(subTreeNode);

            var projects = await subSnapshotBuilder.BuildAsync(solutionState.ProjectStates, solutionState.ProjectIds.Select(id => solutionState.ProjectStates[id]), cancellationToken).ConfigureAwait(false);
            return new SolutionChecksumObject(_serializer, info.Checksum, projects);
        }

        private async Task<ProjectChecksumObject> CreateProjectChecksumObjectAsync(ProjectState key, ProjectState projectState, string kind, CancellationToken cancellationToken)
        {
            var assetBuilder = new AssetBuilder(_checksumTree);
            var info = await assetBuilder.BuildAsync(projectState, cancellationToken).ConfigureAwait(false);

            var subTreeNode = _checksumTree.GetOrCreateSubTreeNode(key);
            var subSnapshotBuilder = new ChecksumTreeBuilder(subTreeNode);

            var documents = await subSnapshotBuilder.BuildAsync(projectState.DocumentStates, projectState.DocumentIds.Select(id => projectState.DocumentStates[id]), WellKnownChecksumObjects.Documents, cancellationToken).ConfigureAwait(false);
            var projectReferences = await subSnapshotBuilder.BuildAsync(projectState.ProjectReferences, projectState.ProjectReferences, cancellationToken).ConfigureAwait(false);
            var metadataReferences = await subSnapshotBuilder.BuildAsync(projectState.MetadataReferences, projectState.MetadataReferences, cancellationToken).ConfigureAwait(false);
            var analyzerReferences = await subSnapshotBuilder.BuildAsync(projectState.AnalyzerReferences, projectState.AnalyzerReferences, cancellationToken).ConfigureAwait(false);
            var additionalDocuments = await subSnapshotBuilder.BuildAsync(projectState.AdditionalDocumentStates, projectState.AdditionalDocumentIds.Select(id => projectState.AdditionalDocumentStates[id]), WellKnownChecksumObjects.TextDocuments, cancellationToken).ConfigureAwait(false);

            var subAssetBuilder = new AssetBuilder(subTreeNode);
            var compilationOptions = await subAssetBuilder.BuildAsync(projectState, projectState.CompilationOptions, cancellationToken).ConfigureAwait(false);
            var parseOptions = await subAssetBuilder.BuildAsync(projectState, projectState.ParseOptions, cancellationToken).ConfigureAwait(false);

            return new ProjectChecksumObject(
                _serializer, info.Checksum, compilationOptions.Checksum, parseOptions.Checksum,
                documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments);
        }

        private async Task<DocumentChecksumObject> CreateDocumentChecksumObjectAsync(TextDocumentState key, TextDocumentState documentState, string kind, CancellationToken cancellationToken)
        {
            var assetBuilder = new AssetBuilder(_checksumTree);
            var info = await assetBuilder.BuildAsync(documentState, cancellationToken).ConfigureAwait(false);

            var sourceText = await key.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var text = await assetBuilder.BuildAsync(key, sourceText, cancellationToken).ConfigureAwait(false);

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

        private Task<ChecksumCollection> CreateChecksumCollectionsAsync(
            IReadOnlyList<ProjectReference> key, IEnumerable<ProjectReference> references, string kind, CancellationToken cancellationToken)
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollectionTask(kind);
            }

            var assetBuilder = new AssetBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollectionsAsync(references, kind, assetBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateChecksumCollectionsAsync(
            IReadOnlyList<MetadataReference> key, IEnumerable<MetadataReference> references, string kind, CancellationToken cancellationToken)
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollectionTask(kind);
            }

            var assetBuilder = new AssetBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollectionsAsync(references, kind, assetBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateChecksumCollectionsAsync(
            IReadOnlyList<AnalyzerReference> key, IEnumerable<AnalyzerReference> references, string kind, CancellationToken cancellationToken)
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollectionTask(kind);
            }

            var assetBuilder = new AssetBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollectionsAsync(references, kind, assetBuilder.BuildAsync, cancellationToken);
        }

        private async Task<ChecksumCollection> CreateChecksumCollectionsAsync<TValue, TChecksumObject>(
           IEnumerable<TValue> items, string kind, Func<TValue, CancellationToken, Task<TChecksumObject>> buildAsync, CancellationToken cancellationToken) where TChecksumObject : ChecksumObject
        {
            var list = new List<Checksum>();
            foreach (var item in items)
            {
                var checksumObject = await buildAsync(item, cancellationToken).ConfigureAwait(false);
                list.Add(checksumObject.Checksum);
            }

            return new ChecksumCollection(_serializer, kind, list.ToArray());
        }

        private Task<ChecksumCollection> GetEmptyChecksumCollectionTask(string kind)
        {
            return ChecksumTreeCollection.GetOrCreateEmptyChecksumCollection(_serializer, kind);
        }
    }
}
