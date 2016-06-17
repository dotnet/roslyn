// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Execution
{
    internal struct AssetBuilder
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
            return _storage.GetOrCreateAssetAsync(solution, GetInfo(solution), WellKnownChecksumObjects.SolutionSnapshotInfo, CreateSolutionSnapshotInfoAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(Project project, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(project.Solution.GetProjectState(project.Id), GetInfo(project), WellKnownChecksumObjects.ProjectSnapshotInfo, CreateProjectSnapshotInfoAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(TextDocument document, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(document.State, GetInfo(document), WellKnownChecksumObjects.DocumentSnapshotInfo, CreateDocumentSnapshotInfoAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(Project project, CompilationOptions compilationOptions, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(compilationOptions, project, WellKnownChecksumObjects.CompilationOptions, CreateCompilationOptionsAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(Project project, ParseOptions parseOptions, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(parseOptions, project, WellKnownChecksumObjects.ParseOptions, CreateParseOptionsAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(ProjectReference reference, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(reference, reference, WellKnownChecksumObjects.ProjectReference, CreateProjectReferenceAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(MetadataReference reference, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(reference, reference, WellKnownChecksumObjects.MetadataReference, CreateMetadataReferenceAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(AnalyzerReference reference, CancellationToken cancellationToken)
        {
            return _storage.GetOrCreateAssetAsync(reference, reference, WellKnownChecksumObjects.AnalyzerReference, CreateAnalyzerReferenceAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(TextDocumentState state, SourceText unused, CancellationToken cancellationToken)
        {
            // TODO: currently this is a bit wierd not to hold onto source text.
            //       it would be nice if SourceText is changed like how recoverable syntax tree work.
            return _storage.GetOrCreateAssetAsync(state, state, WellKnownChecksumObjects.SourceText, CreateSourceTextAsync, cancellationToken);
        }

        private Task<Asset> CreateSolutionSnapshotInfoAsync(SolutionSnapshotInfo info, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<SolutionSnapshotInfo>(info, kind, _serializer.Serialize));
        }

        private Task<Asset> CreateProjectSnapshotInfoAsync(ProjectSnapshotInfo info, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<ProjectSnapshotInfo>(info, kind, _serializer.Serialize));
        }

        private Task<Asset> CreateDocumentSnapshotInfoAsync(DocumentSnapshotInfo info, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<DocumentSnapshotInfo>(info, kind, _serializer.Serialize));
        }

        private Task<Asset> CreateCompilationOptionsAsync(Project project, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<string, CompilationOptions>(project.Language, project.CompilationOptions, kind, _serializer.Serialize));
        }

        private Task<Asset> CreateParseOptionsAsync(Project project, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<string, ParseOptions>(project.Language, project.ParseOptions, kind, _serializer.Serialize));
        }

        private Task<Asset> CreateProjectReferenceAsync(ProjectReference reference, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<ProjectReference>(reference, kind, _serializer.Serialize));
        }

        private Task<Asset> CreateMetadataReferenceAsync(MetadataReference reference, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var checksum = _serializer.HostSerializationService.CreateChecksum(reference, cancellationToken);
            return Task.FromResult<Asset>(new MetadataReferenceAsset(_serializer, reference, checksum, kind));
        }

        private Task<Asset> CreateAnalyzerReferenceAsync(AnalyzerReference reference, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var checksum = _serializer.HostSerializationService.CreateChecksum(reference, cancellationToken);
            return Task.FromResult<Asset>(new AnalyzerReferenceAsset(_serializer, reference, checksum, kind));
        }

        private async Task<Asset> CreateSourceTextAsync(TextDocumentState state, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = await state.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var checksum = new Checksum(text.GetChecksum(useDefaultEncodingIfNull: true));

            return new SourceTextAsset(_serializer, state, checksum, kind);
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
            var source = document.State as DocumentState;
            if (source != null)
            {
                return source.IsGenerated;
            }

            // no source
            return false;
        }

        private SourceCodeKind GetSourceCodeKind(TextDocument document)
        {
            var source = document as Document;
            if (source != null)
            {
                return source.SourceCodeKind;
            }

            // no source
            return SourceCodeKind.Regular;
        }
    }
}
