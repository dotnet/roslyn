// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Execution
{
    internal struct SnapshotBuilder
    {
        private readonly Serializer _serializer;
        private readonly ChecksumTreeNodeCache _checksumTree;

        public SnapshotBuilder(ChecksumTreeNodeCache checksumTree)
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

        private Task<ChecksumCollection> BuildAsync(Solution solution, IEnumerable<Project> projects, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(solution, projects, WellKnownChecksumObjects.Projects, CreateProjectChecksumCollectionAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<TextDocument> documents, string kind, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(project.Solution.GetProjectState(project.Id), documents, kind, CreateDocumentChecksumObjectAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<ProjectReference> projectReferences, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(project.Solution.GetProjectState(project.Id), projectReferences, WellKnownChecksumObjects.ProjectReferences, CreateChecksumCollectionsAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<MetadataReference> metadataReferences, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(project.Solution.GetProjectState(project.Id), metadataReferences, WellKnownChecksumObjects.MetadataReferences, CreateChecksumCollectionsAsync, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<AnalyzerReference> analyzerReferences, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateChecksumObjectWithChildrenAsync(project.Solution.GetProjectState(project.Id), analyzerReferences, WellKnownChecksumObjects.AnalyzerReferences, CreateChecksumCollectionsAsync, cancellationToken);
        }

        private async Task<SolutionChecksumObject> CreateSolutionChecksumObjectAsync(
            Solution solution, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var info = await assetBuilder.BuildAsync(solution, cancellationToken).ConfigureAwait(false);
            var projects = await snapshotBuilder.BuildAsync(solution, solution.Projects, cancellationToken).ConfigureAwait(false);

            return new SolutionChecksumObject(_serializer, info.Checksum, projects);
        }

        private async Task<ProjectChecksumObject> CreateProjectChecksumObjectAsync(
            Project project, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var info = await assetBuilder.BuildAsync(project, cancellationToken).ConfigureAwait(false);
            var compilationOptions = await assetBuilder.BuildAsync(project, project.CompilationOptions, cancellationToken).ConfigureAwait(false);
            var parseOptions = await assetBuilder.BuildAsync(project, project.ParseOptions, cancellationToken).ConfigureAwait(false);

            var documents = await snapshotBuilder.BuildAsync(project, project.Documents, WellKnownChecksumObjects.Documents, cancellationToken).ConfigureAwait(false);

            var projectReferences = await snapshotBuilder.BuildAsync(project, project.ProjectReferences, cancellationToken).ConfigureAwait(false);
            var metadataReferences = await snapshotBuilder.BuildAsync(project, project.MetadataReferences, cancellationToken).ConfigureAwait(false);
            var analyzerReferences = await snapshotBuilder.BuildAsync(project, project.AnalyzerReferences, cancellationToken).ConfigureAwait(false);

            var additionalDocuments = await snapshotBuilder.BuildAsync(project, project.AdditionalDocuments, WellKnownChecksumObjects.TextDocuments, cancellationToken).ConfigureAwait(false);

            return new ProjectChecksumObject(
                _serializer, info.Checksum, compilationOptions.Checksum, parseOptions.Checksum,
                documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments);
        }

        private async Task<DocumentChecksumObject> CreateDocumentChecksumObjectAsync(
           TextDocument document, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var info = await assetBuilder.BuildAsync(document, cancellationToken).ConfigureAwait(false);

            var state = document.State;
            var sourceText = await state.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var text = await assetBuilder.BuildAsync(state, sourceText, cancellationToken).ConfigureAwait(false);

            return new DocumentChecksumObject(_serializer, info.Checksum, text.Checksum);
        }

        private Task<ChecksumCollection> CreateProjectChecksumCollectionAsync(
           IEnumerable<Project> projects, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            return CreateChecksumCollectionsAsync(projects, kind, snapshotBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateDocumentChecksumObjectAsync(
           IEnumerable<TextDocument> documents, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            return CreateChecksumCollectionsAsync(documents, kind, snapshotBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateChecksumCollectionsAsync(
           IEnumerable<ProjectReference> references, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            return CreateChecksumCollectionsAsync(references, kind, assetBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateChecksumCollectionsAsync(
           IEnumerable<MetadataReference> references, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            return CreateChecksumCollectionsAsync(references, kind, assetBuilder.BuildAsync, cancellationToken);
        }

        private Task<ChecksumCollection> CreateChecksumCollectionsAsync(
           IEnumerable<AnalyzerReference> references, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            return CreateChecksumCollectionsAsync(references, kind, assetBuilder.BuildAsync, cancellationToken);
        }

        private async Task<ChecksumCollection> CreateChecksumCollectionsAsync<TValue, TChecksumObject>(
           IEnumerable<TValue> checksumObjects, string kind, Func<TValue, CancellationToken, Task<TChecksumObject>> buildAsync, CancellationToken cancellationToken) where TChecksumObject : ChecksumObject
        {
            var list = new List<Checksum>();
            foreach (var checksumObject in checksumObjects)
            {
                var asset = await buildAsync(checksumObject, cancellationToken).ConfigureAwait(false);
                list.Add(asset.Checksum);
            }

            return new ChecksumCollection(_serializer, kind, list.ToArray());
        }
    }
}
