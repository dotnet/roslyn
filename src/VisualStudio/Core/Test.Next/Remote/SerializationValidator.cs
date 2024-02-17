// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.VisualStudio.PlatformUI;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.UnitTests
{
    internal sealed class SerializationValidator
    {
        private sealed class AssetProvider : AbstractAssetProvider
        {
            private readonly SerializationValidator _validator;

            public AssetProvider(SerializationValidator validator)
                => _validator = validator;

            public override async ValueTask<T> GetAssetAsync<T>(AssetHint assetHint, Checksum checksum, CancellationToken cancellationToken)
                => await _validator.GetValueAsync<T>(checksum).ConfigureAwait(false);
        }

        internal sealed class ChecksumObjectCollection<T> : IEnumerable<T>
        {
            public ImmutableArray<T> Children { get; }

            /// <summary>
            /// Indicates what kind of object it is
            /// <see cref="WellKnownSynchronizationKind"/> for examples.
            /// 
            /// this will be used in tranportation framework and deserialization service
            /// to hand shake how to send over data and deserialize serialized data
            /// </summary>
            public readonly WellKnownSynchronizationKind Kind;

            /// <summary>
            /// Checksum of this object
            /// </summary>
            public readonly Checksum Checksum;

            public ChecksumObjectCollection(SerializationValidator validator, ChecksumCollection collection)
            {
                Checksum = collection.Checksum;
                Kind = collection.GetWellKnownSynchronizationKind();

                // using .Result here since we don't want to convert all calls to this to async.
                // and none of ChecksumWithChildren actually use async
                Children = ImmutableArray.CreateRange(collection.Select(c => validator.GetValueAsync<T>(c).Result));
            }

            public int Count => Children.Length;
            public T this[int index] => Children[index];
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public IEnumerator<T> GetEnumerator() => Children.Select(t => t).GetEnumerator();
        }

        public SolutionAssetStorage AssetStorage { get; }
        public ISerializerService Serializer { get; }
        public HostWorkspaceServices Services { get; }

        public SerializationValidator(HostWorkspaceServices services)
        {
            AssetStorage = services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
            Serializer = services.GetRequiredService<ISerializerService>();
            Services = services;
        }

        private async Task<SolutionAsset> GetRequiredAssetAsync(Checksum checksum)
        {
            var data = await AssetStorage.GetTestAccessor().GetRequiredAssetAsync(checksum, CancellationToken.None).ConfigureAwait(false);
            Contract.ThrowIfNull(data);
            return new(checksum, data);
        }

        public async Task<T> GetValueAsync<T>(Checksum checksum)
        {
            var data = await GetRequiredAssetAsync(checksum).ConfigureAwait(false);
            Contract.ThrowIfNull(data.Value);

            using var context = new SolutionReplicationContext();
            using var stream = SerializableBytes.CreateWritableStream();
            using (var writer = new ObjectWriter(stream, leaveOpen: true))
            {
                Serializer.Serialize(data.Value, writer, context, CancellationToken.None);
            }

            stream.Position = 0;
            using var reader = ObjectReader.TryGetReader(stream);
            Contract.ThrowIfNull(reader);

            // deserialize bits to object
            var result = Serializer.Deserialize(data.Kind, reader, CancellationToken.None);
            Contract.ThrowIfNull<object?>(result);
            return (T)result;
        }

        public async Task<Solution> GetSolutionAsync(SolutionAssetStorage.Scope scope)
        {
            var solutionInfo = await new AssetProvider(this).CreateSolutionInfoAsync(scope.SolutionChecksum, CancellationToken.None).ConfigureAwait(false);

            var workspace = new AdhocWorkspace(Services.HostServices);
            return workspace.AddSolution(solutionInfo);
        }

        public ChecksumObjectCollection<ProjectStateChecksums> ToProjectObjects(ChecksumCollection collection)
            => new(this, collection);

        public ChecksumObjectCollection<DocumentStateChecksums> ToDocumentObjects(ChecksumCollection collection)
            => new(this, collection);

        internal async Task VerifyAssetAsync(SolutionStateChecksums solutionObject)
        {
            await VerifyAssetSerializationAsync<SolutionInfo.SolutionAttributes>(
                solutionObject.Attributes, WellKnownSynchronizationKind.SolutionAttributes,
                (v, k, s) => new SolutionAsset(v.Checksum, v)).ConfigureAwait(false);

            foreach (var (projectChecksum, projectId) in solutionObject.Projects)
            {
                var projectObject = await GetValueAsync<ProjectStateChecksums>(projectChecksum).ConfigureAwait(false);
                Assert.Equal(projectObject.ProjectId, projectId);
                await VerifyAssetAsync(projectObject).ConfigureAwait(false);
            }
        }

        internal async Task VerifyAssetAsync(ProjectStateChecksums projectObject)
        {
            var info = await VerifyAssetSerializationAsync<ProjectInfo.ProjectAttributes>(
                projectObject.Info, WellKnownSynchronizationKind.ProjectAttributes,
                (v, k, s) => new SolutionAsset(v.Checksum, v)).ConfigureAwait(false);

            await VerifyAssetSerializationAsync<CompilationOptions>(
                projectObject.CompilationOptions, WellKnownSynchronizationKind.CompilationOptions,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v));

            await VerifyAssetSerializationAsync<ParseOptions>(
                projectObject.ParseOptions, WellKnownSynchronizationKind.ParseOptions,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v));

            foreach (var checksum in projectObject.ProjectReferences)
            {
                await VerifyAssetSerializationAsync<ProjectReference>(
                    checksum, WellKnownSynchronizationKind.ProjectReference,
                    (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v));
            }

            foreach (var checksum in projectObject.MetadataReferences)
            {
                await VerifyAssetSerializationAsync<MetadataReference>(
                    checksum, WellKnownSynchronizationKind.MetadataReference,
                    (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v));
            }

            foreach (var checksum in projectObject.AnalyzerReferences)
            {
                await VerifyAssetSerializationAsync<AnalyzerReference>(
                    checksum, WellKnownSynchronizationKind.AnalyzerReference,
                    (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v));
            }

            foreach (var (checksum, documentId) in projectObject.Documents)
            {
                var documentObject = await GetValueAsync<DocumentStateChecksums>(checksum).ConfigureAwait(false);
                Assert.Equal(documentObject.DocumentId, documentId);
                await VerifyAssetAsync(documentObject).ConfigureAwait(false);
            }

            foreach (var (checksum, documentId) in projectObject.AdditionalDocuments)
            {
                var documentObject = await GetValueAsync<DocumentStateChecksums>(checksum).ConfigureAwait(false);
                Assert.Equal(documentObject.DocumentId, documentId);
                await VerifyAssetAsync(documentObject).ConfigureAwait(false);
            }

            foreach (var (checksum, documentId) in projectObject.AnalyzerConfigDocuments)
            {
                var documentObject = await GetValueAsync<DocumentStateChecksums>(checksum).ConfigureAwait(false);
                Assert.Equal(documentObject.DocumentId, documentId);
                await VerifyAssetAsync(documentObject).ConfigureAwait(false);
            }
        }

        internal async Task VerifyAssetAsync(DocumentStateChecksums documentObject)
        {
            var info = await VerifyAssetSerializationAsync<DocumentInfo.DocumentAttributes>(
                documentObject.Info, WellKnownSynchronizationKind.DocumentAttributes,
                (v, k, s) => new SolutionAsset(v.Checksum, v)).ConfigureAwait(false);

            await VerifyAssetSerializationAsync<SerializableSourceText>(
                documentObject.Text, WellKnownSynchronizationKind.SerializableSourceText,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v));
        }

        internal async Task<T> VerifyAssetSerializationAsync<T>(
            Checksum checksum,
            WellKnownSynchronizationKind kind,
            Func<T, WellKnownSynchronizationKind, ISerializerService, SolutionAsset> assetGetter)
        {
            // re-create asset from object
            var syncObject = await GetRequiredAssetAsync(checksum).ConfigureAwait(false);

            var recoveredValue = await GetValueAsync<T>(checksum).ConfigureAwait(false);
            var recreatedSyncObject = assetGetter(recoveredValue, kind, Serializer);

            // make sure original object and re-created object are same.
            SynchronizationObjectEqual(syncObject, recreatedSyncObject);

            return recoveredValue;
        }

        internal async Task VerifySolutionStateSerializationAsync(Solution solution, Checksum solutionChecksum)
        {
            var solutionCompilationObjectFromSyncObject = await GetValueAsync<SolutionCompilationStateChecksums>(solutionChecksum);
            Contract.ThrowIfFalse(solution.CompilationState.TryGetStateChecksums(out var solutionCompilationObjectFromSolution));

            SolutionCompilationStateEqual(solutionCompilationObjectFromSolution, solutionCompilationObjectFromSyncObject);

            var solutionObjectFromSyncObject = await GetValueAsync<SolutionStateChecksums>(solutionCompilationObjectFromSyncObject.SolutionState);
            Contract.ThrowIfFalse(solution.CompilationState.SolutionState.TryGetStateChecksums(out var solutionObjectFromSolution));

            SolutionStateEqual(solutionObjectFromSolution, solutionObjectFromSyncObject);
        }

        private static void AssertChecksumCollectionEqual<TId>(
            ChecksumsAndIds<TId> collection1, ChecksumsAndIds<TId> collection2)
        {
            AssertChecksumCollectionEqual(collection1.Checksums, collection2.Checksums);
            AssertEx.Equal(collection1.Ids, collection2.Ids);
        }

        private static void AssertChecksumCollectionEqual(
            ChecksumCollection collection1, ChecksumCollection collection2)
        {
            Assert.Equal(collection1.Checksum, collection2.Checksum);
            AssertEx.Equal(collection1.Children, collection2.Children);
        }

        internal void SolutionCompilationStateEqual(SolutionCompilationStateChecksums solutionObject1, SolutionCompilationStateChecksums solutionObject2)
        {
            Assert.Equal(solutionObject1.Checksum, solutionObject2.Checksum);
            Assert.Equal(solutionObject1.FrozenSourceGeneratedDocumentIdentities.HasValue, solutionObject2.FrozenSourceGeneratedDocumentIdentities.HasValue);
            if (solutionObject1.FrozenSourceGeneratedDocumentIdentities.HasValue)
                AssertChecksumCollectionEqual(solutionObject1.FrozenSourceGeneratedDocumentIdentities.Value, solutionObject2.FrozenSourceGeneratedDocumentIdentities!.Value);

            Assert.Equal(solutionObject1.FrozenSourceGeneratedDocuments.HasValue, solutionObject2.FrozenSourceGeneratedDocuments.HasValue);
            if (solutionObject1.FrozenSourceGeneratedDocuments.HasValue)
                AssertChecksumCollectionEqual(solutionObject1.FrozenSourceGeneratedDocuments.Value, solutionObject2.FrozenSourceGeneratedDocuments!.Value);
        }

        internal void SolutionStateEqual(SolutionStateChecksums solutionObject1, SolutionStateChecksums solutionObject2)
        {
            Assert.Equal(solutionObject1.Checksum, solutionObject2.Checksum);
            Assert.Equal(solutionObject1.Attributes, solutionObject2.Attributes);
            AssertChecksumCollectionEqual(solutionObject1.Projects, solutionObject2.Projects);
            AssertChecksumCollectionEqual(solutionObject1.AnalyzerReferences, solutionObject2.AnalyzerReferences);

            ProjectStatesEqual(ToProjectObjects(solutionObject1.Projects.Checksums), ToProjectObjects(solutionObject2.Projects.Checksums));
        }

        private void ProjectStateEqual(ProjectStateChecksums projectObjects1, ProjectStateChecksums projectObjects2)
        {
            Assert.Equal(projectObjects1.Checksum, projectObjects2.Checksum);
            Assert.Equal(projectObjects1.Info, projectObjects2.Info);
            Assert.Equal(projectObjects1.CompilationOptions, projectObjects2.CompilationOptions);
            Assert.Equal(projectObjects1.ParseOptions, projectObjects2.ParseOptions);
            AssertChecksumCollectionEqual(projectObjects1.ProjectReferences, projectObjects2.ProjectReferences);
            AssertChecksumCollectionEqual(projectObjects1.MetadataReferences, projectObjects2.MetadataReferences);
            AssertChecksumCollectionEqual(projectObjects1.AnalyzerReferences, projectObjects2.AnalyzerReferences);
            AssertChecksumCollectionEqual(projectObjects1.Documents, projectObjects2.Documents);
            AssertChecksumCollectionEqual(projectObjects1.AdditionalDocuments, projectObjects2.AdditionalDocuments);
            AssertChecksumCollectionEqual(projectObjects1.AnalyzerConfigDocuments, projectObjects2.AnalyzerConfigDocuments);

            DocumentStatesEqual(ToDocumentObjects(projectObjects1.Documents.Checksums), ToDocumentObjects(projectObjects2.Documents.Checksums));
            DocumentStatesEqual(ToDocumentObjects(projectObjects1.AdditionalDocuments.Checksums), ToDocumentObjects(projectObjects2.AdditionalDocuments.Checksums));
            DocumentStatesEqual(ToDocumentObjects(projectObjects1.AnalyzerConfigDocuments.Checksums), ToDocumentObjects(projectObjects2.AnalyzerConfigDocuments.Checksums));
        }

        private static void DocumentStateEqual(DocumentStateChecksums documentObjects1, DocumentStateChecksums documentObjects2)
        {
            Assert.Equal(documentObjects1.Checksum, documentObjects2.Checksum);
            Assert.Equal(documentObjects1.Info, documentObjects2.Info);
            Assert.Equal(documentObjects1.Text, documentObjects2.Text);
        }

        private void ProjectStatesEqual(ChecksumObjectCollection<ProjectStateChecksums> projectObjects1, ChecksumObjectCollection<ProjectStateChecksums> projectObjects2)
        {
            SynchronizationObjectEqual(projectObjects1, projectObjects2);

            Assert.Equal(projectObjects1.Count, projectObjects2.Count);

            for (var i = 0; i < projectObjects1.Count; i++)
                ProjectStateEqual(projectObjects1[i], projectObjects2[i]);
        }

        private static void DocumentStatesEqual(ChecksumObjectCollection<DocumentStateChecksums> documentObjects1, ChecksumObjectCollection<DocumentStateChecksums> documentObjects2)
        {
            SynchronizationObjectEqual(documentObjects1, documentObjects2);

            Assert.Equal(documentObjects1.Count, documentObjects2.Count);

            for (var i = 0; i < documentObjects1.Count; i++)
                DocumentStateEqual(documentObjects1[i], documentObjects2[i]);
        }

        internal async Task VerifySnapshotInServiceAsync(
            ProjectStateChecksums projectObject,
            int expectedDocumentCount,
            int expectedProjectReferenceCount,
            int expectedMetadataReferenceCount,
            int expectedAnalyzerReferenceCount,
            int expectedAdditionalDocumentCount)
        {
            await VerifyChecksumInServiceAsync(projectObject.Checksum, projectObject.GetWellKnownSynchronizationKind()).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(projectObject.Info, WellKnownSynchronizationKind.ProjectAttributes).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(projectObject.CompilationOptions, WellKnownSynchronizationKind.CompilationOptions).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(projectObject.ParseOptions, WellKnownSynchronizationKind.ParseOptions).ConfigureAwait(false);

            await VerifyCollectionInService(ToDocumentObjects(projectObject.Documents.Checksums), expectedDocumentCount).ConfigureAwait(false);

            await VerifyCollectionInService(projectObject.ProjectReferences, expectedProjectReferenceCount, WellKnownSynchronizationKind.ProjectReference).ConfigureAwait(false);
            await VerifyCollectionInService(projectObject.MetadataReferences, expectedMetadataReferenceCount, WellKnownSynchronizationKind.MetadataReference).ConfigureAwait(false);
            await VerifyCollectionInService(projectObject.AnalyzerReferences, expectedAnalyzerReferenceCount, WellKnownSynchronizationKind.AnalyzerReference).ConfigureAwait(false);

            await VerifyCollectionInService(ToDocumentObjects(projectObject.AdditionalDocuments.Checksums), expectedAdditionalDocumentCount).ConfigureAwait(false);
        }

        internal async Task VerifyCollectionInService(ChecksumCollection checksums, int expectedCount, WellKnownSynchronizationKind expectedItemKind)
        {
            await VerifyChecksumInServiceAsync(checksums.Checksum, checksums.GetWellKnownSynchronizationKind()).ConfigureAwait(false);
            Assert.Equal(checksums.Count, expectedCount);

            foreach (var checksum in checksums)
            {
                await VerifyChecksumInServiceAsync(checksum, expectedItemKind).ConfigureAwait(false);
            }
        }

        internal async Task VerifyCollectionInService(ChecksumObjectCollection<DocumentStateChecksums> documents, int expectedCount)
        {
            await VerifySynchronizationObjectInServiceAsync(documents).ConfigureAwait(false);
            Assert.Equal(documents.Count, expectedCount);

            foreach (var documentId in documents)
            {
                await VerifySnapshotInServiceAsync(documentId).ConfigureAwait(false);
            }
        }

        internal async Task VerifySnapshotInServiceAsync(DocumentStateChecksums documentObject)
        {
            await VerifyChecksumInServiceAsync(documentObject.Checksum, documentObject.GetWellKnownSynchronizationKind()).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(documentObject.Info, WellKnownSynchronizationKind.DocumentAttributes).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(documentObject.Text, WellKnownSynchronizationKind.SerializableSourceText).ConfigureAwait(false);
        }

        internal async Task VerifySynchronizationObjectInServiceAsync(SolutionAsset syncObject)
            => await VerifyChecksumInServiceAsync(syncObject.Checksum, syncObject.Kind).ConfigureAwait(false);

        internal async Task VerifySynchronizationObjectInServiceAsync<T>(ChecksumObjectCollection<T> syncObject)
            => await VerifyChecksumInServiceAsync(syncObject.Checksum, syncObject.Kind).ConfigureAwait(false);

        internal async Task VerifyChecksumInServiceAsync(Checksum checksum, WellKnownSynchronizationKind kind)
        {
            Assert.True(checksum != Checksum.Null);
            var otherObject = await GetRequiredAssetAsync(checksum).ConfigureAwait(false);

            ChecksumEqual(checksum, kind, otherObject.Checksum, otherObject.Kind);
        }

        internal static void SynchronizationObjectEqual<T>(ChecksumObjectCollection<T> checksumObject1, ChecksumObjectCollection<T> checksumObject2)
            => ChecksumEqual(checksumObject1.Checksum, checksumObject1.Kind, checksumObject2.Checksum, checksumObject2.Kind);

        internal static void SynchronizationObjectEqual<T>(ChecksumObjectCollection<T> checksumObject1, SolutionAsset checksumObject2)
            => ChecksumEqual(checksumObject1.Checksum, checksumObject1.Kind, checksumObject2.Checksum, checksumObject2.Kind);

        internal static void SynchronizationObjectEqual(SolutionAsset checksumObject1, SolutionAsset checksumObject2)
            => ChecksumEqual(checksumObject1.Checksum, checksumObject1.Kind, checksumObject2.Checksum, checksumObject2.Kind);

        internal static void ChecksumEqual(Checksum checksum1, WellKnownSynchronizationKind kind1, Checksum checksum2, WellKnownSynchronizationKind kind2)
        {
            Assert.Equal(checksum1, checksum2);
            Assert.Equal(kind1, kind2);
        }
    }
}
