// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Execution
{
    internal sealed class AssetBuilder
    {
        private readonly Serializer _serializer;
        private readonly SnapshotStorage _storage;

        public AssetBuilder(Serializer serializer, SnapshotStorage storage)
        {
            _serializer = serializer;
            _storage = storage;
        }

        public Task<Asset> BuildAsync(Solution solution, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(solution, GetInfo(solution), WellKnownChecksumObjects.SolutionSnapshotInfo,
                (s, k, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return Task.FromResult<Asset>(new Asset<SolutionSnapshotInfo>(s, k, _serializer.Serialize));
                }, cancellationToken);
        }

        public Task<Asset> BuildAsync(Project project, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(project.Solution.GetProjectState(project.Id), GetInfo(project), WellKnownChecksumObjects.ProjectSnapshotInfo,
                (p, k, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return Task.FromResult<Asset>(new Asset<ProjectSnapshotInfo>(p, k, _serializer.Serialize));
                }, cancellationToken);
        }

        public Task<Asset> BuildAsync(TextDocument document, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(document.GetTextOrDocumentState(), GetInfo(document), WellKnownChecksumObjects.DocumentSnapshotInfo,
                (d, k, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return Task.FromResult<Asset>(new Asset<DocumentSnapshotInfo>(d, k, _serializer.Serialize));
                }, cancellationToken);
        }

        public Task<Asset> BuildAsync(Project project, CompilationOptions compilationOptions, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(compilationOptions, project, WellKnownChecksumObjects.CompilationOptions,
                (p, k, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return Task.FromResult<Asset>(new Asset<string, CompilationOptions>(p.Language, p.CompilationOptions, k, _serializer.Serialize));
                }, cancellationToken);
        }

        public Task<Asset> BuildAsync(Project project, ParseOptions parseOptions, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(parseOptions, project, WellKnownChecksumObjects.ParseOptions,
                (p, k, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return Task.FromResult<Asset>(new Asset<string, ParseOptions>(p.Language, p.ParseOptions, k, _serializer.Serialize));
                }, cancellationToken);
        }

        public Task<Asset> BuildAsync(ProjectReference reference, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(reference, reference, WellKnownChecksumObjects.ProjectReference,
                (r, k, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return Task.FromResult<Asset>(new Asset<ProjectReference>(r, k, _serializer.Serialize));
                }, cancellationToken);
        }

        public Task<Asset> BuildAsync(MetadataReference reference, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(reference, reference, WellKnownChecksumObjects.MetadataReference,
                (r, k, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return Task.FromResult<Asset>(new Asset<MetadataReference>(r, k, _serializer.Serialize));
                }, cancellationToken);
        }

        public Task<Asset> BuildAsync(AnalyzerReference reference, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(reference, reference, WellKnownChecksumObjects.AnalyzerReference,
                (r, k, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return Task.FromResult<Asset>(new Asset<AnalyzerReference>(r, k, _serializer.Serialize));
                }, cancellationToken);
        }

        public Task<Asset> BuildAsync(TextDocumentState state, SourceText unused, CancellationToken cancellationToken)
        {
            // TODO: currently this is a bit wierd not to hold onto source text.
            //       it would be nice if SourceText is changed like how recoverable syntax tree work.
            return _storage.GetOrCreateAssetAsync(state, state, WellKnownChecksumObjects.SourceText,
                async (s, k, c) =>
                {
                    c.ThrowIfCancellationRequested();

                    var text = await state.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var checksum = new Checksum(text.GetChecksum(useDefaultEncodingIfNull: true));

                    return (Asset)new SourceTextAsset(_serializer, s, checksum, k);
                }, cancellationToken);
        }

        private SolutionSnapshotInfo GetInfo(Solution solution)
        {
            return new SolutionSnapshotInfo(solution.Id, solution.Version, solution.FilePath);
        }

        private ProjectSnapshotInfo GetInfo(Project project)
        {
            return new ProjectSnapshotInfo(project.Id, project.Version, project.Name, project.AssemblyName, project.Language, project.FilePath, project.OutputFilePath);
        }

        private DocumentSnapshotInfo GetInfo(TextDocument document)
        {
            // we might just split it to TextDocument and Document.
            return new DocumentSnapshotInfo(document.Id, document.Name, document.Folders, GetSourceCodeKind(document), document.FilePath, IsGenerated(document));
        }

        private bool IsGenerated(TextDocument document)
        {
            var source = document as Document;
            if (source != null)
            {
                return source.State.IsGenerated;
            }

            // no source
            return false;
        }

        private int GetSourceCodeKind(TextDocument document)
        {
            var source = document as Document;
            if (source != null)
            {
                return (int)source.SourceCodeKind;
            }

            // no source
            return int.MaxValue;
        }
    }
}
