// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        public Task<SolutionChecksumObject> BuildAsync(Solution solution, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(solution, solution, SolutionChecksumObject.Name, CreateSolutionChecksumObjectAsync, cancellationToken);
        }

        public Task<ProjectChecksumObject> BuildAsync(Project project, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(project.Solution.GetProjectState(project.Id), project, ProjectChecksumObject.Name, CreateProjectChecksumObjectAsync, cancellationToken);
        }

        private Task<DocumentChecksumObject> BuildAsync(TextDocument document, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(document.State, document, DocumentChecksumObject.Name, CreateDocumentChecksumObjectAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Solution key, IEnumerable<Project> projects, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(key, projects, WellKnownChecksumObjects.Projects, CreateProjectChecksumCollectionAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync<TState>(ImmutableDictionary<DocumentId, TState> key, IEnumerable<TextDocument> documents, string kind, CancellationToken cancellationToken)
            where TState : TextDocumentState
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(key, documents, kind, CreateDocumentChecksumObjectAsync, cancellationToken);
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

        private async Task<SolutionChecksumObject> CreateSolutionChecksumObjectAsync(Solution key, Solution solution, string kind, CancellationToken cancellationToken)
        {
            var assetBuilder = new AssetBuilder(_checksumTree);
            var info = await assetBuilder.BuildAsync(solution, cancellationToken).ConfigureAwait(false);

            var projects = await BuildAsync(solution, solution.Projects, cancellationToken).ConfigureAwait(false);
            return new SolutionChecksumObject(_serializer, info.Checksum, projects);
        }

        private async Task<ProjectChecksumObject> CreateProjectChecksumObjectAsync(ProjectState key, Project project, string kind, CancellationToken cancellationToken)
        {
            var assetBuilder = new AssetBuilder(_checksumTree);
            var info = await assetBuilder.BuildAsync(project, cancellationToken).ConfigureAwait(false);

            var subTreeNode = _checksumTree.GetOrCreateSubTreeNode(key);

            var subSnapshotBuilder = new ChecksumTreeBuilder(subTreeNode);

            var documents = await subSnapshotBuilder.BuildAsync(key.DocumentStates, project.Documents, WellKnownChecksumObjects.Documents, cancellationToken).ConfigureAwait(false);
            var projectReferences = await subSnapshotBuilder.BuildAsync(project.AllProjectReferences, project.AllProjectReferences, cancellationToken).ConfigureAwait(false);
            var metadataReferences = await subSnapshotBuilder.BuildAsync(project.MetadataReferences, project.MetadataReferences, cancellationToken).ConfigureAwait(false);
            var analyzerReferences = await subSnapshotBuilder.BuildAsync(project.AnalyzerReferences, project.AnalyzerReferences, cancellationToken).ConfigureAwait(false);
            var additionalDocuments = await subSnapshotBuilder.BuildAsync(key.AdditionalDocumentStates, project.AdditionalDocuments, WellKnownChecksumObjects.TextDocuments, cancellationToken).ConfigureAwait(false);

            var subAssetBuilder = new AssetBuilder(subTreeNode);
            var compilationOptions = await subAssetBuilder.BuildAsync(project, project.CompilationOptions, cancellationToken).ConfigureAwait(false);
            var parseOptions = await subAssetBuilder.BuildAsync(project, project.ParseOptions, cancellationToken).ConfigureAwait(false);

            return new ProjectChecksumObject(
                _serializer, info.Checksum, compilationOptions.Checksum, parseOptions.Checksum,
                documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments);
        }

        private async Task<DocumentChecksumObject> CreateDocumentChecksumObjectAsync(TextDocumentState key, TextDocument document, string kind, CancellationToken cancellationToken)
        {
            var assetBuilder = new AssetBuilder(_checksumTree);
            var info = await assetBuilder.BuildAsync(document, cancellationToken).ConfigureAwait(false);

            var sourceText = await key.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var text = await assetBuilder.BuildAsync(key, sourceText, cancellationToken).ConfigureAwait(false);

            return new DocumentChecksumObject(_serializer, info.Checksum, text.Checksum);
        }

        private Task<ChecksumCollection> CreateProjectChecksumCollectionAsync(Solution key, IEnumerable<Project> projects, string kind, CancellationToken cancellationToken)
        {
            if (key.ProjectIds.Count == 0)
            {
                return GetEmptyChecksumCollectionTask(kind);
            }

            var snapshotBuilder = new ChecksumTreeBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollectionsAsync(projects, kind, snapshotBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateDocumentChecksumObjectAsync<TState>(ImmutableDictionary<DocumentId, TState> key, IEnumerable<TextDocument> documents, string kind, CancellationToken cancellationToken)
            where TState : TextDocumentState
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollectionTask(kind);
            }

            var snapshotBuilder = new ChecksumTreeBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollectionsAsync(documents, kind, snapshotBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateChecksumCollectionsAsync(IReadOnlyList<ProjectReference> key, IEnumerable<ProjectReference> references, string kind, CancellationToken cancellationToken)
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollectionTask(kind);
            }

            var assetBuilder = new AssetBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollectionsAsync(references, kind, assetBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateChecksumCollectionsAsync(IReadOnlyList<MetadataReference> key, IEnumerable<MetadataReference> references, string kind, CancellationToken cancellationToken)
        {
            if (key.Count == 0)
            {
                return GetEmptyChecksumCollectionTask(kind);
            }

            var assetBuilder = new AssetBuilder(_checksumTree.GetOrCreateSubTreeNode(key));
            return CreateChecksumCollectionsAsync(references, kind, assetBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateChecksumCollectionsAsync(IReadOnlyList<AnalyzerReference> key, IEnumerable<AnalyzerReference> references, string kind, CancellationToken cancellationToken)
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
