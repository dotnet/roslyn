// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Execution
{
    internal struct SnapshotBuilder
    {
        private readonly bool _rebuild;
        private readonly Serializer _serializer;
        private readonly SnapshotStorage _storage;

        public SnapshotBuilder(Serializer serializer, SnapshotStorage storage, bool rebuild = false)
        {
            _rebuild = rebuild;

            _serializer = serializer;
            _storage = storage;
        }

        public Task<SolutionSnapshotId> BuildAsync(Solution solution, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(solution, solution, SolutionSnapshotId.Name, CreateSolutionSnapshotIdAsync, _rebuild, cancellationToken);
        }

        public Task<ProjectSnapshotId> BuildAsync(Project project, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), project, ProjectSnapshotId.Name, CreateProjectSnapshotIdAsync, _rebuild, cancellationToken);
        }

        private Task<DocumentSnapshotId> BuildAsync(TextDocument document, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(document.GetTextOrDocumentState(), document, DocumentSnapshotId.Name, CreateDocumentSnapshotIdAsync, _rebuild, cancellationToken);
        }

        private Task<SnapshotIdCollection<ProjectSnapshotId>> BuildAsync(Solution solution, IEnumerable<Project> projects, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(solution, projects, WellKnownChecksumObjects.Projects, CreateProjectSnapshotIdsAsync, _rebuild, cancellationToken);
        }

        private Task<SnapshotIdCollection<DocumentSnapshotId>> BuildAsync(Project project, IEnumerable<TextDocument> documents, string kind, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), documents, kind, CreateDocumentSnapshotIdsAsync, _rebuild, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<ProjectReference> projectReferences, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), projectReferences, WellKnownChecksumObjects.ProjectReferences, CreateChecksumCollectionsAsync, _rebuild, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<MetadataReference> metadataReferences, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), metadataReferences, WellKnownChecksumObjects.MetadataReferences, CreateChecksumCollectionsAsync, _rebuild, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<AnalyzerReference> analyzerReferences, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), analyzerReferences, WellKnownChecksumObjects.AnalyzerReferences, CreateChecksumCollectionsAsync, _rebuild, cancellationToken);
        }

        private async Task<SolutionSnapshotId> CreateSolutionSnapshotIdAsync(
            Solution solution, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var info = await assetBuilder.BuildAsync(solution, cancellationToken).ConfigureAwait(false);
            var projects = await snapshotBuilder.BuildAsync(solution, solution.Projects, cancellationToken).ConfigureAwait(false);

            return new SolutionSnapshotId(_serializer, info.Checksum, projects);
        }

        private async Task<ProjectSnapshotId> CreateProjectSnapshotIdAsync(
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

            return new ProjectSnapshotId(
                _serializer, info.Checksum, compilationOptions.Checksum, parseOptions.Checksum,
                documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments);
        }

        private async Task<DocumentSnapshotId> CreateDocumentSnapshotIdAsync(
           TextDocument document, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var info = await assetBuilder.BuildAsync(document, cancellationToken).ConfigureAwait(false);

            var state = document.GetTextOrDocumentState();
            var sourceText = await state.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var text = await assetBuilder.BuildAsync(state, sourceText, cancellationToken).ConfigureAwait(false);

            return new DocumentSnapshotId(_serializer, info.Checksum, text.Checksum);
        }

        private async Task<SnapshotIdCollection<ProjectSnapshotId>> CreateProjectSnapshotIdsAsync(
           IEnumerable<Project> projects, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<ProjectSnapshotId>();
            foreach (var project in projects)
            {
                builder.Add(await snapshotBuilder.BuildAsync(project, cancellationToken).ConfigureAwait(false));
            }

            return new SnapshotIdCollection<ProjectSnapshotId>(_serializer, builder.ToImmutable(), kind);
        }

        private async Task<SnapshotIdCollection<DocumentSnapshotId>> CreateDocumentSnapshotIdsAsync(
           IEnumerable<TextDocument> documents, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<DocumentSnapshotId>();
            foreach (var document in documents)
            {
                builder.Add(await snapshotBuilder.BuildAsync(document, cancellationToken).ConfigureAwait(false));
            }

            return new SnapshotIdCollection<DocumentSnapshotId>(_serializer, builder.ToImmutable(), kind);
        }

        private async Task<ChecksumCollection> CreateChecksumCollectionsAsync(
           IEnumerable<ProjectReference> references, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<Checksum>();
            foreach (var reference in references)
            {
                var asset = await assetBuilder.BuildAsync(reference, cancellationToken).ConfigureAwait(false);
                builder.Add(asset.Checksum);
            }

            return new ChecksumCollection(_serializer, builder.ToImmutable(), kind);
        }

        private async Task<ChecksumCollection> CreateChecksumCollectionsAsync(
           IEnumerable<MetadataReference> references, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<Checksum>();
            foreach (var reference in references)
            {
                var asset = await assetBuilder.BuildAsync(reference, cancellationToken).ConfigureAwait(false);
                builder.Add(asset.Checksum);
            }

            return new ChecksumCollection(_serializer, builder.ToImmutable(), kind);
        }

        private async Task<ChecksumCollection> CreateChecksumCollectionsAsync(
           IEnumerable<AnalyzerReference> references, string kind, SnapshotBuilder snapshotBuilder, AssetBuilder assetBuilder, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<Checksum>();
            foreach (var reference in references)
            {
                var asset = await assetBuilder.BuildAsync(reference, cancellationToken).ConfigureAwait(false);
                builder.Add(asset.Checksum);
            }

            return new ChecksumCollection(_serializer, builder.ToImmutable(), kind);
        }
    }
}
