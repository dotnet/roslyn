// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    internal struct AssetBuilder
    {
        private readonly Serializer _serializer;
        private readonly ChecksumTreeNodeCache _checksumTree;

        public AssetBuilder(Solution solution) : this(new AssetOnlyTreeNodeCache(solution))
        {
        }

        public AssetBuilder(ChecksumTreeNodeCache checksumTree)
        {
            _checksumTree = checksumTree;
            _serializer = checksumTree.Serializer;
        }

        public Task<Asset> BuildAsync(Solution solution, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAssetAsync(solution, GetInfo(solution), WellKnownChecksumObjects.SolutionChecksumObjectInfo, CreateSolutionChecksumObjectInfoAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(Project project, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAssetAsync(project.Solution.GetProjectState(project.Id), GetInfo(project), WellKnownChecksumObjects.ProjectChecksumObjectInfo, CreateProjectChecksumObjectInfoAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(TextDocument document, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAssetAsync(document.State, GetInfo(document), WellKnownChecksumObjects.DocumentChecksumObjectInfo, CreateDocumentChecksumObjectInfoAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(Project project, CompilationOptions compilationOptions, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAssetAsync(compilationOptions, project, WellKnownChecksumObjects.CompilationOptions, CreateCompilationOptionsAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(Project project, ParseOptions parseOptions, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAssetAsync(parseOptions, project, WellKnownChecksumObjects.ParseOptions, CreateParseOptionsAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(ProjectReference reference, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAssetAsync(reference, reference, WellKnownChecksumObjects.ProjectReference, CreateProjectReferenceAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(MetadataReference reference, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAssetAsync(reference, reference, WellKnownChecksumObjects.MetadataReference, CreateMetadataReferenceAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(AnalyzerReference reference, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAssetAsync(reference, reference, WellKnownChecksumObjects.AnalyzerReference, CreateAnalyzerReferenceAsync, cancellationToken);
        }

        public Task<Asset> BuildAsync(TextDocumentState state, SourceText unused, CancellationToken cancellationToken)
        {
            // TODO: currently this is a bit wierd not to hold onto source text.
            //       it would be nice if SourceText is changed like how recoverable syntax tree work.
            return _checksumTree.GetOrCreateAssetAsync(state, state, WellKnownChecksumObjects.SourceText, CreateSourceTextAsync, cancellationToken);
        }

        private Task<Asset> CreateSolutionChecksumObjectInfoAsync(SolutionChecksumObjectInfo info, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<SolutionChecksumObjectInfo>(info, kind, _serializer.SerializeSolutionSnapshotInfo));
        }

        private Task<Asset> CreateProjectChecksumObjectInfoAsync(ProjectChecksumObjectInfo info, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<ProjectChecksumObjectInfo>(info, kind, _serializer.SerializeProjectSnapshotInfo));
        }

        private Task<Asset> CreateDocumentChecksumObjectInfoAsync(DocumentChecksumObjectInfo info, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<DocumentChecksumObjectInfo>(info, kind, _serializer.SerializeDocumentSnapshotInfo));
        }

        private Task<Asset> CreateCompilationOptionsAsync(Project project, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new LanguageSpecificAsset<CompilationOptions>(project.Language, project.CompilationOptions, kind, _serializer.SerializeCompilationOptions));
        }

        private Task<Asset> CreateParseOptionsAsync(Project project, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new LanguageSpecificAsset<ParseOptions>(project.Language, project.ParseOptions, kind, _serializer.SerializeParseOptions));
        }

        private Task<Asset> CreateProjectReferenceAsync(ProjectReference reference, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Asset>(new Asset<ProjectReference>(reference, kind, _serializer.SerializeProjectReference));
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
            var checksum = new Checksum(text.GetChecksum());

            return new SourceTextAsset(_serializer, state, checksum, kind);
        }

        private SolutionChecksumObjectInfo GetInfo(Solution solution)
        {
            return new SolutionChecksumObjectInfo(solution.Id, solution.Version, solution.FilePath);
        }

        private ProjectChecksumObjectInfo GetInfo(Project project)
        {
            return new ProjectChecksumObjectInfo(project.Id, project.Version, project.Name, project.AssemblyName, project.Language, project.FilePath, project.OutputFilePath);
        }

        private DocumentChecksumObjectInfo GetInfo(TextDocument document)
        {
            // we might just split it to TextDocument and Document.
            return new DocumentChecksumObjectInfo(document.Id, document.Name, document.Folders, GetSourceCodeKind(document), document.FilePath, IsGenerated(document));
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

        private sealed class AssetOnlyTreeNodeCache : ChecksumTreeNodeCache
        {
            public AssetOnlyTreeNodeCache(Solution solution) :
                base(solution)
            {
            }

            public override void AddAdditionalAsset(Asset asset, CancellationToken cancellationToken)
            {
                Contract.Fail("shouldn't be called");
            }

            public override Task<TChecksumObject> GetOrCreateChecksumObjectWithChildrenAsync<TKey, TValue, TChecksumObject>(
                TKey key, TValue value, string kind,
                Func<TValue, string, SnapshotBuilder, AssetBuilder, CancellationToken, Task<TChecksumObject>> valueGetterAsync, CancellationToken cancellationToken)
            {
                return Contract.FailWithReturn<Task<TChecksumObject>>("shouldn't be called");
            }

            public override Task<TAsset> GetOrCreateAssetAsync<TKey, TValue, TAsset>(
                TKey key, TValue value, string kind,
                Func<TValue, string, CancellationToken, Task<TAsset>> valueGetterAsync, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(key);
                return valueGetterAsync(value, kind, cancellationToken);
            }
        }
    }
}
