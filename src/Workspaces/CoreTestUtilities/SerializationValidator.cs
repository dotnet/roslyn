// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Execution;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    internal sealed class SerializationValidator
    {
        private sealed class AssetProvider : AbstractAssetProvider
        {
            private readonly SerializationValidator _validator;

            public AssetProvider(SerializationValidator validator)
                => _validator = validator;

            public override Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken)
                => _validator.GetValueAsync<T>(checksum);
        }

        public RemotableDataService RemotableDataService { get; }
        public ISerializerService Serializer { get; }

        public SerializationValidator(HostWorkspaceServices services)
        {
            RemotableDataService = (RemotableDataService)services.GetRequiredService<IRemotableDataService>();
            Serializer = services.GetRequiredService<ISerializerService>();
        }

        public async Task<T> GetValueAsync<T>(Checksum checksum)
        {
            var data = (await RemotableDataService.TestOnly_GetRemotableDataAsync(checksum, CancellationToken.None).ConfigureAwait(false))!;

            using var stream = SerializableBytes.CreateWritableStream();
            using (var writer = new ObjectWriter(stream, leaveOpen: true))
            {
                await data.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);
            }

            stream.Position = 0;
            using var reader = ObjectReader.TryGetReader(stream);

            // deserialize bits to object
            return Serializer.Deserialize<T>(data.Kind, reader, CancellationToken.None);
        }

        public async Task<Solution> GetSolutionAsync(PinnedRemotableDataScope scope)
        {
            var (solutionInfo, _) = await new AssetProvider(this).CreateSolutionInfoAndOptionsAsync(scope.SolutionChecksum, CancellationToken.None).ConfigureAwait(false);

            var workspace = new AdhocWorkspace();
            return workspace.AddSolution(solutionInfo);
        }

        public ChecksumObjectCollection<ProjectStateChecksums> ToProjectObjects(ProjectChecksumCollection collection)
            => new ChecksumObjectCollection<ProjectStateChecksums>(this, collection);

        public ChecksumObjectCollection<DocumentStateChecksums> ToDocumentObjects(DocumentChecksumCollection collection)
            => new ChecksumObjectCollection<DocumentStateChecksums>(this, collection);

        public ChecksumObjectCollection<DocumentStateChecksums> ToDocumentObjects(TextDocumentChecksumCollection collection)
            => new ChecksumObjectCollection<DocumentStateChecksums>(this, collection);

        public ChecksumObjectCollection<DocumentStateChecksums> ToDocumentObjects(AnalyzerConfigDocumentChecksumCollection collection)
            => new ChecksumObjectCollection<DocumentStateChecksums>(this, collection);

        internal async Task VerifyAssetAsync(SolutionStateChecksums solutionObject)
        {
            await VerifyAssetSerializationAsync<SolutionInfo.SolutionAttributes>(
                solutionObject.Attributes, WellKnownSynchronizationKind.SolutionAttributes,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s)).ConfigureAwait(false);

            foreach (var projectChecksum in solutionObject.Projects)
            {
                var projectObject = await GetValueAsync<ProjectStateChecksums>(projectChecksum).ConfigureAwait(false);
                await VerifyAssetAsync(projectObject).ConfigureAwait(false);
            }
        }

        internal async Task VerifyAssetAsync(ProjectStateChecksums projectObject)
        {
            var info = await VerifyAssetSerializationAsync<ProjectInfo.ProjectAttributes>(
                projectObject.Info, WellKnownSynchronizationKind.ProjectAttributes,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s)).ConfigureAwait(false);

            await VerifyAssetSerializationAsync<CompilationOptions>(
                projectObject.CompilationOptions, WellKnownSynchronizationKind.CompilationOptions,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));

            await VerifyAssetSerializationAsync<ParseOptions>(
                projectObject.ParseOptions, WellKnownSynchronizationKind.ParseOptions,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));

            foreach (var checksum in projectObject.Documents)
            {
                var documentObject = await GetValueAsync<DocumentStateChecksums>(checksum).ConfigureAwait(false);
                await VerifyAssetAsync(documentObject).ConfigureAwait(false);
            }

            foreach (var checksum in projectObject.ProjectReferences)
            {
                await VerifyAssetSerializationAsync<ProjectReference>(
                    checksum, WellKnownSynchronizationKind.ProjectReference,
                    (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));
            }

            foreach (var checksum in projectObject.MetadataReferences)
            {
                await VerifyAssetSerializationAsync<MetadataReference>(
                    checksum, WellKnownSynchronizationKind.MetadataReference,
                    (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));
            }

            foreach (var checksum in projectObject.AnalyzerReferences)
            {
                await VerifyAssetSerializationAsync<AnalyzerReference>(
                    checksum, WellKnownSynchronizationKind.AnalyzerReference,
                    (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));
            }

            foreach (var checksum in projectObject.AdditionalDocuments)
            {
                var documentObject = await GetValueAsync<DocumentStateChecksums>(checksum).ConfigureAwait(false);
                await VerifyAssetAsync(documentObject).ConfigureAwait(false);
            }

            foreach (var checksum in projectObject.AnalyzerConfigDocuments)
            {
                var documentObject = await GetValueAsync<DocumentStateChecksums>(checksum).ConfigureAwait(false);
                await VerifyAssetAsync(documentObject).ConfigureAwait(false);
            }
        }

        internal async Task VerifyAssetAsync(DocumentStateChecksums documentObject)
        {
            var info = await VerifyAssetSerializationAsync<DocumentInfo.DocumentAttributes>(
                documentObject.Info, WellKnownSynchronizationKind.DocumentAttributes,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s)).ConfigureAwait(false);

            await VerifyAssetSerializationAsync<SourceText>(
                documentObject.Text, WellKnownSynchronizationKind.SourceText,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));
        }

        internal async Task<T> VerifyAssetSerializationAsync<T>(
            Checksum checksum,
            WellKnownSynchronizationKind kind,
            Func<T, WellKnownSynchronizationKind, ISerializerService, RemotableData> assetGetter)
        {
            // re-create asset from object
            var syncObject = (await RemotableDataService.TestOnly_GetRemotableDataAsync(checksum, CancellationToken.None).ConfigureAwait(false))!;

            var recoveredValue = await GetValueAsync<T>(checksum).ConfigureAwait(false);
            var recreatedSyncObject = assetGetter(recoveredValue, kind, Serializer);

            // make sure original object and re-created object are same.
            SynchronizationObjectEqual(syncObject, recreatedSyncObject);

            return recoveredValue;
        }

        internal async Task VerifySolutionStateSerializationAsync(Solution solution, Checksum solutionChecksum)
        {
            var solutionObjectFromSyncObject = await GetValueAsync<SolutionStateChecksums>(solutionChecksum);
            Assert.True(solution.State.TryGetStateChecksums(out var solutionObjectFromSolution));

            SolutionStateEqual(solutionObjectFromSolution, solutionObjectFromSyncObject);
        }

        internal void SolutionStateEqual(SolutionStateChecksums solutionObject1, SolutionStateChecksums solutionObject2)
        {
            ChecksumWithChildrenEqual(solutionObject1, solutionObject2);

            ProjectStatesEqual(ToProjectObjects(solutionObject1.Projects), ToProjectObjects(solutionObject2.Projects));
        }

        internal void ProjectStateEqual(ProjectStateChecksums projectObjects1, ProjectStateChecksums projectObjects2)
        {
            ChecksumWithChildrenEqual(projectObjects1, projectObjects2);

            ChecksumWithChildrenEqual(ToDocumentObjects(projectObjects1.Documents), ToDocumentObjects(projectObjects2.Documents));
            ChecksumWithChildrenEqual(ToDocumentObjects(projectObjects1.AdditionalDocuments), ToDocumentObjects(projectObjects2.AdditionalDocuments));
            ChecksumWithChildrenEqual(ToDocumentObjects(projectObjects1.AnalyzerConfigDocuments), ToDocumentObjects(projectObjects2.AnalyzerConfigDocuments));
        }

        internal void ProjectStatesEqual(ChecksumObjectCollection<ProjectStateChecksums> projectObjects1, ChecksumObjectCollection<ProjectStateChecksums> projectObjects2)
        {
            SynchronizationObjectEqual(projectObjects1, projectObjects2);

            Assert.Equal(projectObjects1.Count, projectObjects2.Count);

            for (var i = 0; i < projectObjects1.Count; i++)
            {
                ProjectStateEqual(projectObjects1[i], projectObjects2[i]);
            }
        }

        internal static void ChecksumWithChildrenEqual<T>(ChecksumObjectCollection<T> checksums1, ChecksumObjectCollection<T> checksums2) where T : ChecksumWithChildren
        {
            SynchronizationObjectEqual(checksums1, checksums2);

            Assert.Equal(checksums1.Count, checksums2.Count);

            for (var i = 0; i < checksums1.Count; i++)
            {
                ChecksumWithChildrenEqual(checksums1[i], checksums2[i]);
            }
        }

        internal static void ChecksumWithChildrenEqual(ChecksumWithChildren checksums1, ChecksumWithChildren checksums2)
        {
            Assert.Equal(checksums1.Checksum, checksums2.Checksum);
            Assert.Equal(checksums1.Children.Count, checksums2.Children.Count);

            for (var i = 0; i < checksums1.Children.Count; i++)
            {
                var child1 = checksums1.Children[i];
                var child2 = checksums2.Children[i];

                Assert.Equal(child1.GetType(), child2.GetType());

                if (child1 is Checksum)
                {
                    Assert.Equal((Checksum)child1, (Checksum)child2);
                    continue;
                }

                ChecksumWithChildrenEqual((ChecksumCollection)child1, (ChecksumCollection)child2);
            }
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

            await VerifyCollectionInService(ToDocumentObjects(projectObject.Documents), expectedDocumentCount).ConfigureAwait(false);

            await VerifyCollectionInService(projectObject.ProjectReferences, expectedProjectReferenceCount, WellKnownSynchronizationKind.ProjectReference).ConfigureAwait(false);
            await VerifyCollectionInService(projectObject.MetadataReferences, expectedMetadataReferenceCount, WellKnownSynchronizationKind.MetadataReference).ConfigureAwait(false);
            await VerifyCollectionInService(projectObject.AnalyzerReferences, expectedAnalyzerReferenceCount, WellKnownSynchronizationKind.AnalyzerReference).ConfigureAwait(false);

            await VerifyCollectionInService(ToDocumentObjects(projectObject.AdditionalDocuments), expectedAdditionalDocumentCount).ConfigureAwait(false);
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
            await VerifyChecksumInServiceAsync(documentObject.Text, WellKnownSynchronizationKind.SourceText).ConfigureAwait(false);
        }

        internal async Task VerifySynchronizationObjectInServiceAsync(RemotableData syncObject)
            => await VerifyChecksumInServiceAsync(syncObject.Checksum, syncObject.Kind).ConfigureAwait(false);

        internal async Task VerifyChecksumInServiceAsync(Checksum checksum, WellKnownSynchronizationKind kind)
        {
            Assert.NotNull(checksum);
            var otherObject = (await RemotableDataService.TestOnly_GetRemotableDataAsync(checksum, CancellationToken.None).ConfigureAwait(false))!;

            ChecksumEqual(checksum, kind, otherObject.Checksum, otherObject.Kind);
        }

        internal static void SynchronizationObjectEqual<T>(T checksumObject1, T checksumObject2) where T : RemotableData
            => ChecksumEqual(checksumObject1.Checksum, checksumObject1.Kind, checksumObject2.Checksum, checksumObject2.Kind);

        internal static void ChecksumEqual(Checksum checksum1, WellKnownSynchronizationKind kind1, Checksum checksum2, WellKnownSynchronizationKind kind2)
        {
            Assert.Equal(checksum1, checksum2);
            Assert.Equal(kind1, kind2);
        }
    }
}
