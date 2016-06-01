// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Execution
{
    internal class SnapshotBuilder
    {
        private readonly bool _rebuild;
        private readonly Serializer _serializer;
        private readonly SnapshotStorage _storage;
        private readonly AssetBuilder _assetBuilder;

        public SnapshotBuilder(Serializer serializer, SnapshotStorage storage, bool rebuild = false)
        {
            _rebuild = rebuild;

            _serializer = serializer;
            _storage = storage;

            _assetBuilder = new AssetBuilder(serializer, storage);
        }

        public Task<SolutionSnapshotId> BuildAsync(Solution solution, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(solution, solution, SolutionSnapshotId.Name,
                async (s, k, c) =>
                {
                    var info = await _assetBuilder.BuildAsync(s, c).ConfigureAwait(false);
                    var projects = await BuildAsync(s, s.Projects, c).ConfigureAwait(false);

                    return new SolutionSnapshotId(_serializer, info.Checksum, projects);
                }, _rebuild, cancellationToken);
        }

        private Task<ProjectSnapshotId> BuildAsync(Project project, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), project, ProjectSnapshotId.Name,
                async (p, k, c) =>
                {
                    var info = await _assetBuilder.BuildAsync(p, c).ConfigureAwait(false);
                    var compilationOptions = await _assetBuilder.BuildAsync(p, p.CompilationOptions, c).ConfigureAwait(false);
                    var parseOptions = await _assetBuilder.BuildAsync(p, p.ParseOptions, c).ConfigureAwait(false);

                    var documents = await BuildAsync(p, p.Documents, WellKnownChecksumObjects.Documents, c).ConfigureAwait(false);

                    var projectReferences = await BuildAsync(p, p.ProjectReferences, c).ConfigureAwait(false);
                    var metadataReferences = await BuildAsync(p, p.MetadataReferences, c).ConfigureAwait(false);
                    var analyzerReferences = await BuildAsync(p, p.AnalyzerReferences, c).ConfigureAwait(false);

                    var additionalDocuments = await BuildAsync(p, p.AdditionalDocuments, WellKnownChecksumObjects.TextDocuments, c).ConfigureAwait(false);

                    return new ProjectSnapshotId(
                        _serializer, info.Checksum, compilationOptions.Checksum, parseOptions.Checksum,
                        documents, projectReferences, metadataReferences, analyzerReferences, additionalDocuments);
                }, _rebuild, cancellationToken);
        }

        private Task<DocumentSnapshotId> BuildAsync(TextDocument document, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(document.GetTextOrDocumentState(), document, DocumentSnapshotId.Name,
                async (d, k, c) =>
                {
                    var info = await _assetBuilder.BuildAsync(d, c).ConfigureAwait(false);

                    var state = d.GetTextOrDocumentState();
                    var sourceText = await state.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var text = await _assetBuilder.BuildAsync(state, sourceText, cancellationToken).ConfigureAwait(false);

                    return new DocumentSnapshotId(_serializer, info.Checksum, text.Checksum);
                }, _rebuild, cancellationToken);
        }

        private Task<SnapshotIdCollection<ProjectSnapshotId>> BuildAsync(Solution solution, IEnumerable<Project> projects, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(solution, projects, WellKnownChecksumObjects.Projects,
                async (ps, k, c) =>
                {
                    var builder = ImmutableArray.CreateBuilder<ProjectSnapshotId>();
                    foreach (var project in ps)
                    {
                        builder.Add(await BuildAsync(project, cancellationToken).ConfigureAwait(false));
                    }

                    return new SnapshotIdCollection<ProjectSnapshotId>(_serializer, builder.ToImmutable(), k);
                }, _rebuild, cancellationToken);
        }

        private Task<SnapshotIdCollection<DocumentSnapshotId>> BuildAsync(Project project, IEnumerable<TextDocument> documents, string kind, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), documents, kind,
                async (ds, k, c) =>
                {
                    var builder = ImmutableArray.CreateBuilder<DocumentSnapshotId>();
                    foreach (var document in ds)
                    {
                        builder.Add(await BuildAsync(document, cancellationToken).ConfigureAwait(false));
                    }

                    return new SnapshotIdCollection<DocumentSnapshotId>(_serializer, builder.ToImmutable(), k);
                }, _rebuild, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<ProjectReference> projectReferences, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), projectReferences, WellKnownChecksumObjects.ProjectReferences,
                async (ps, k, c) =>
                {
                    var builder = ImmutableArray.CreateBuilder<Checksum>();
                    foreach (var reference in ps)
                    {
                        var asset = await _assetBuilder.BuildAsync(reference, cancellationToken).ConfigureAwait(false);
                        builder.Add(asset.Checksum);
                    }

                    return new ChecksumCollection(_serializer, builder.ToImmutable(), k);
                }, _rebuild, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<MetadataReference> metadataReferences, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), metadataReferences, WellKnownChecksumObjects.MetadataReferences,
                async (ms, k, c) =>
                {
                    var builder = ImmutableArray.CreateBuilder<Checksum>();
                    foreach (var reference in ms)
                    {
                        var asset = await _assetBuilder.BuildAsync(reference, cancellationToken).ConfigureAwait(false);
                        builder.Add(asset.Checksum);
                    }

                    return new ChecksumCollection(_serializer, builder.ToImmutable(), k);
                }, _rebuild, cancellationToken);
        }

        private Task<ChecksumCollection> BuildAsync(Project project, IEnumerable<AnalyzerReference> analyzerReferences, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateHierarchicalChecksumObjectAsync(project.Solution.GetProjectState(project.Id), analyzerReferences, WellKnownChecksumObjects.AnalyzerReferences,
                async (cs, k, c) =>
                {
                    var builder = ImmutableArray.CreateBuilder<Checksum>();
                    foreach (var reference in cs)
                    {
                        var asset = await _assetBuilder.BuildAsync(reference, cancellationToken).ConfigureAwait(false);
                        builder.Add(asset.Checksum);
                    }

                    return new ChecksumCollection(_serializer, builder.ToImmutable(), k);
                }, _rebuild, cancellationToken);
        }
    }
}
