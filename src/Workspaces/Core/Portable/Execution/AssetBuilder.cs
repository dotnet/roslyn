// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    internal struct AssetBuilder
    {
        private readonly Serializer _serializer;
        private readonly IChecksumTreeNode _checksumTree;

        public AssetBuilder(Solution solution) : this(new AssetOnlyTreeNode(solution))
        {
        }

        public AssetBuilder(IChecksumTreeNode checksumTree)
        {
            _checksumTree = checksumTree;
            _serializer = checksumTree.Serializer;
        }

        public Asset Build(SolutionState solutionState, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAsset(solutionState, GetInfo(solutionState), WellKnownChecksumObjects.SolutionChecksumObjectInfo, CreateSolutionChecksumObjectInfo, cancellationToken);
        }

        public Asset Build(ProjectState projectState, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAsset(projectState, GetInfo(projectState), WellKnownChecksumObjects.ProjectChecksumObjectInfo, CreateProjectChecksumObjectInfo, cancellationToken);
        }

        public Asset Build(TextDocumentState document, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAsset(document, GetInfo(document), WellKnownChecksumObjects.DocumentChecksumObjectInfo, CreateDocumentChecksumObjectInfo, cancellationToken);
        }

        public Asset Build(ProjectState projectState, CompilationOptions compilationOptions, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAsset(compilationOptions, projectState, WellKnownChecksumObjects.CompilationOptions, CreateCompilationOptions, cancellationToken);
        }

        public Asset Build(ProjectState projectState, ParseOptions parseOptions, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAsset(parseOptions, projectState, WellKnownChecksumObjects.ParseOptions, CreateParseOptions, cancellationToken);
        }

        public Asset Build(ProjectReference reference, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAsset(reference, reference, WellKnownChecksumObjects.ProjectReference, CreateProjectReference, cancellationToken);
        }

        public Asset Build(MetadataReference reference, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAsset(reference, reference, WellKnownChecksumObjects.MetadataReference, CreateMetadataReference, cancellationToken);
        }

        public Asset Build(AnalyzerReference reference, CancellationToken cancellationToken)
        {
            return _checksumTree.GetOrCreateAsset(reference, reference, WellKnownChecksumObjects.AnalyzerReference, CreateAnalyzerReference, cancellationToken);
        }

        public Asset Build(TextDocumentState state, SourceText text, CancellationToken cancellationToken)
        {
            // TODO: currently this is a bit wierd not to hold onto source text.
            //       it would be nice if SourceText is changed like how recoverable syntax tree work.
            var asset = _checksumTree.GetOrCreateAsset(state, state, WellKnownChecksumObjects.SourceText, CreateSourceText, cancellationToken);

            // make sure we keep text alive. this is to make sure we don't do any async call in asset builder
            GC.KeepAlive(text);
            return asset;
        }

        public Asset Build(OptionSet options, string language, CancellationToken cancellationToken)
        {
            // get around issue where this can't be captured in struct
            var local = this;
            return _checksumTree.GetOrCreateAsset(options, options, WellKnownChecksumObjects.OptionSet, (v, k, c) => local.CreateOptionSet(v, language, k, c), cancellationToken);
        }

        private Asset CreateSolutionChecksumObjectInfo(SolutionChecksumObjectInfo info, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new Asset<SolutionChecksumObjectInfo>(info, kind, _serializer.SerializeSolutionChecksumObjectInfo);
        }

        private Asset CreateProjectChecksumObjectInfo(ProjectChecksumObjectInfo info, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new Asset<ProjectChecksumObjectInfo>(info, kind, _serializer.SerializeProjectChecksumObjectInfo);
        }

        private Asset CreateDocumentChecksumObjectInfo(DocumentChecksumObjectInfo info, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new Asset<DocumentChecksumObjectInfo>(info, kind, _serializer.SerializeDocumentChecksumObjectInfo);
        }

        private Asset CreateCompilationOptions(ProjectState projectState, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new LanguageSpecificAsset<CompilationOptions>(projectState.Language, projectState.CompilationOptions, kind, _serializer.SerializeCompilationOptions);
        }

        private Asset CreateParseOptions(ProjectState projectState, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new LanguageSpecificAsset<ParseOptions>(projectState.Language, projectState.ParseOptions, kind, _serializer.SerializeParseOptions);
        }

        private Asset CreateProjectReference(ProjectReference reference, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new Asset<ProjectReference>(reference, kind, _serializer.SerializeProjectReference);
        }

        private Asset CreateMetadataReference(MetadataReference reference, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var checksum = _serializer.HostSerializationService.CreateChecksum(reference, cancellationToken);
            return new MetadataReferenceAsset(_serializer, reference, checksum, kind);
        }

        private Asset CreateAnalyzerReference(AnalyzerReference reference, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var checksum = _serializer.HostSerializationService.CreateChecksum(reference, cancellationToken);
            return new AnalyzerReferenceAsset(_serializer, reference, checksum, kind);
        }

        private Asset CreateSourceText(TextDocumentState state, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SourceText text;
            Contract.ThrowIfFalse(state.TryGetText(out text));

            var checksum = new Checksum(text.GetChecksum());
            return new SourceTextAsset(_serializer, state, checksum, kind);
        }

        private Asset CreateOptionSet(OptionSet options, string language, string kind, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var serializer = _serializer;
            return new Asset<OptionSet>(options, kind, (o, w, c) => serializer.SerializeOptionSet(o, language, w, c));
        }

        private SolutionChecksumObjectInfo GetInfo(SolutionState solutionState)
        {
            return new SolutionChecksumObjectInfo(solutionState.Id, solutionState.Version, solutionState.FilePath);
        }

        private ProjectChecksumObjectInfo GetInfo(ProjectState projectState)
        {
            return new ProjectChecksumObjectInfo(projectState.Id, projectState.Version, projectState.Name, projectState.AssemblyName, projectState.Language, projectState.FilePath, projectState.OutputFilePath);
        }

        private DocumentChecksumObjectInfo GetInfo(TextDocumentState documentState)
        {
            // we might just split it to TextDocument and Document.
            return new DocumentChecksumObjectInfo(documentState.Id, documentState.Name, documentState.Folders, GetSourceCodeKind(documentState), documentState.FilePath, IsGenerated(documentState));
        }

        private bool IsGenerated(TextDocumentState documentState)
        {
            var source = documentState as DocumentState;
            if (source != null)
            {
                return source.IsGenerated;
            }

            // no source
            return false;
        }

        private SourceCodeKind GetSourceCodeKind(TextDocumentState documentState)
        {
            var source = documentState as DocumentState;
            if (source != null)
            {
                return source.SourceCodeKind;
            }

            // no source
            return SourceCodeKind.Regular;
        }

        private sealed class AssetOnlyTreeNode : IChecksumTreeNode
        {
            public AssetOnlyTreeNode(Solution solution)
            {
                Serializer = ChecksumTreeCollection.GetOrCreateSerializer(solution.Workspace.Services);
            }

            public Serializer Serializer { get; }

            public Asset GetOrCreateAsset<TKey, TValue>(
                TKey key, TValue value, string kind, Func<TValue, string, CancellationToken, Asset> valueGetter, CancellationToken cancellationToken)
                where TKey : class
            {
                Contract.ThrowIfNull(key);
                return valueGetter(value, kind, cancellationToken);
            }

            public TResult GetOrCreateChecksumObjectWithChildren<TKey, TValue, TResult>(
                TKey key, TValue value, string kind, Func<TKey, TValue, string, CancellationToken, TResult> valueGetter, CancellationToken cancellationToken)
                where TKey : class
                where TResult : ChecksumObjectWithChildren
            {
                return Contract.FailWithReturn<TResult>("shouldn't be called");
            }

            public Task<TResult> GetOrCreateChecksumObjectWithChildrenAsync<TKey, TValue, TResult>(
                TKey key, TValue value, string kind, Func<TKey, TValue, string, CancellationToken, Task<TResult>> valueGetterAsync, CancellationToken cancellationToken)
                where TKey : class
                where TResult : ChecksumObjectWithChildren
            {
                return Contract.FailWithReturn<Task<TResult>>("shouldn't be called");
            }

            public IChecksumTreeNode GetOrCreateSubTreeNode<TKey>(TKey key)
            {
                return Contract.FailWithReturn<IChecksumTreeNode>("shouldn't be called");
            }
        }
    }
}
