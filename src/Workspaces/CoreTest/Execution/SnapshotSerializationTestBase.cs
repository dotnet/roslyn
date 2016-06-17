// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SnapshotSerializationTestBase
    {
        internal static Solution CreateFullSolution(HostServices hostServices = null)
        {
            var solution = new AdhocWorkspace(hostServices ?? Host.Mef.MefHostServices.DefaultHost).CurrentSolution;
            var csCode = "class A { }";
            var project1 = solution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
            var document1 = project1.AddDocument("Document1", SourceText.From(csCode));

            var vbCode = "Class B\r\nEnd Class";
            var project2 = document1.Project.Solution.AddProject("Project2", "Project2.dll", LanguageNames.VisualBasic);
            var document2 = project2.AddDocument("Document2", SourceText.From(vbCode));

            project1 = document2.Project.Solution.GetProject(project1.Id).AddProjectReference(new ProjectReference(project2.Id, ImmutableArray.Create("test")));
            project1 = project1.AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            project1 = project1.AddAnalyzerReference(new AnalyzerFileReference(typeof(object).Assembly.Location, new TestAnalyzerAssemblyLoader()));

            var textDocument1 = project1.AddAdditionalDocument("Additional", SourceText.From("hello"), ImmutableArray.Create("test"), @".\Add");
            return textDocument1.Project.Solution;
        }

        internal static async Task<T> GetValueAsync<T>(ISolutionSnapshotService service, Checksum checksum, string kind)
        {
            var snapshotService = (SolutionSnapshotServiceFactory.Service)service;
            var checksumObject = await service.GetChecksumObjectAsync(checksum, CancellationToken.None).ConfigureAwait(false);

            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                // serialize asset to bits
                await checksumObject.WriteToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = new ObjectReader(stream))
                {
                    // deserialize bits to object
                    var serializer = snapshotService.Serializer;
                    return serializer.Deserialize<T>(kind, reader, CancellationToken.None);
                }
            }
        }

        internal static async Task VerifyAssetAsync(ISolutionSnapshotService service, Solution solution, SolutionSnapshotId solutionId)
        {
            await VerifyAssetSerializationAsync<SolutionSnapshotInfo>(
                service, solutionId.Info, WellKnownChecksumObjects.SolutionSnapshotInfo,
                (v, k, s) => new Asset<SolutionSnapshotInfo>(v, k, s.Serialize)).ConfigureAwait(false);

            foreach (var projectId in solutionId.Projects.Objects)
            {
                await VerifyAssetAsync(service, solution, projectId).ConfigureAwait(false);
            }
        }

        internal static async Task VerifyAssetAsync(
            ISolutionSnapshotService service,
            Solution solution,
            ProjectSnapshotId projectId)
        {
            var info = await VerifyAssetSerializationAsync<ProjectSnapshotInfo>(
                service, projectId.Info, WellKnownChecksumObjects.ProjectSnapshotInfo,
                (v, k, s) => new Asset<ProjectSnapshotInfo>(v, k, s.Serialize)).ConfigureAwait(false);

            var project = solution.GetProject(info.Id);

            await VerifyAssetSerializationAsync<CompilationOptions>(
                service, projectId.CompilationOptions, WellKnownChecksumObjects.CompilationOptions,
                (v, k, s) => new Asset<string, CompilationOptions>(project.Language, v, k, s.Serialize));

            await VerifyAssetSerializationAsync<ParseOptions>(
                service, projectId.ParseOptions, WellKnownChecksumObjects.ParseOptions,
                (v, k, s) => new Asset<string, ParseOptions>(project.Language, v, k, s.Serialize));

            foreach (var documentId in projectId.Documents.Objects)
            {
                await VerifyAssetAsync(service, solution, documentId).ConfigureAwait(false);
            }

            foreach (var projectReference in projectId.ProjectReferences.Objects)
            {
                await VerifyAssetSerializationAsync<ProjectReference>(
                    service, projectReference, WellKnownChecksumObjects.ProjectReference,
                    (v, k, s) => new Asset<ProjectReference>(v, k, s.Serialize));
            }

            foreach (var metadataReference in projectId.MetadataReferences.Objects)
            {
                await VerifyAssetSerializationAsync<MetadataReference>(
                    service, metadataReference, WellKnownChecksumObjects.MetadataReference,
                    (v, k, s) => new MetadataReferenceAsset(s, v, s.HostSerializationService.CreateChecksum(v, CancellationToken.None), k));
            }

            foreach (var analyzerReference in projectId.AnalyzerReferences.Objects)
            {
                await VerifyAssetSerializationAsync<AnalyzerReference>(
                    service, analyzerReference, WellKnownChecksumObjects.AnalyzerReference,
                    (v, k, s) => new AnalyzerReferenceAsset(s, v, s.HostSerializationService.CreateChecksum(v, CancellationToken.None), k));
            }

            foreach (var documentId in projectId.AdditionalDocuments.Objects)
            {
                await VerifyAssetAsync(service, solution, documentId).ConfigureAwait(false);
            }
        }

        internal static async Task VerifyAssetAsync(
            ISolutionSnapshotService service,
            Solution solution,
            DocumentSnapshotId documentId)
        {
            var info = await VerifyAssetSerializationAsync<DocumentSnapshotInfo>(
                service, documentId.Info, WellKnownChecksumObjects.DocumentSnapshotInfo,
                (v, k, s) => new Asset<DocumentSnapshotInfo>(v, k, s.Serialize)).ConfigureAwait(false);

            await VerifyAssetSerializationAsync<SourceText>(
                service, documentId.Text, WellKnownChecksumObjects.SourceText,
                (v, k, s) => new SourceTextAsset(s, CreateTextState(solution, v), new Checksum(v.GetChecksum(useDefaultEncodingIfNull: true)), k));
        }

        private static TextDocumentState CreateTextState(Solution solution, SourceText text)
        {
            // we just need a fake state to call GetTextAsync that return given sourcetext
            return TextDocumentState.Create(
                DocumentInfo.Create(
                    DocumentId.CreateNewId(ProjectId.CreateNewId()), "unused", loader: TextLoader.From(TextAndVersion.Create(text, VersionStamp.Default))),
                solution.Services);
        }

        internal static async Task<T> VerifyAssetSerializationAsync<T>(
            ISolutionSnapshotService service,
            Checksum checksum,
            string kind,
            Func<T, string, Serializer, Asset> assetGetter)
        {
            var snapshotService = (SolutionSnapshotServiceFactory.Service)service;
            var originalChecksumObject = await service.GetChecksumObjectAsync(checksum, CancellationToken.None).ConfigureAwait(false);

            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                // serialize asset to bits
                await originalChecksumObject.WriteToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = new ObjectReader(stream))
                {
                    // deserialize bits to object
                    var serializer = snapshotService.Serializer;
                    var recoveredValue = serializer.Deserialize<T>(kind, reader, CancellationToken.None);

                    // re-create asset from object
                    var recreatedChecksumObject = assetGetter(recoveredValue, kind, serializer);

                    // make sure original checksum object and re-created checksum object are same.
                    ChecksumObjectEqual(originalChecksumObject, recreatedChecksumObject);

                    return recoveredValue;
                }
            }
        }

        internal static async Task VerifySnapshotSerializationAsync(Solution solution, SolutionSnapshotId solutionId)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                await solutionId.WriteToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = new ObjectReader(stream))
                {
                    var serializer = new Serializer(solution.Workspace.Services);
                    var recovered = serializer.Deserialize<SolutionSnapshotId>(SolutionSnapshotId.Name, reader, CancellationToken.None);

                    SnapshotEqual(solutionId, recovered);
                }
            }
        }

        internal static void SnapshotEqual(SolutionSnapshotId solutionId1, SolutionSnapshotId solutionId2)
        {
            ChecksumObjectEqual(solutionId1, solutionId2);
            Assert.Equal(solutionId1.Info, solutionId2.Info);

            CollectionEqual(solutionId1.Projects, solutionId2.Projects);
        }

        internal static void CollectionEqual(SnapshotIdCollection<ProjectSnapshotId> projectIds1, SnapshotIdCollection<ProjectSnapshotId> projectIds2)
        {
            ChecksumObjectEqual(projectIds1, projectIds2);
            Assert.Equal(projectIds1.Objects.Length, projectIds2.Objects.Length);

            for (var i = 0; i < projectIds1.Objects.Length; i++)
            {
                SnapshotEqual(projectIds1.Objects[i], projectIds2.Objects[i]);
            }
        }

        internal static void SnapshotEqual(ProjectSnapshotId projectId1, ProjectSnapshotId projectId2)
        {
            ChecksumObjectEqual(projectId1, projectId2);

            Assert.Equal(projectId1.Info, projectId2.Info);
            Assert.Equal(projectId1.CompilationOptions, projectId2.CompilationOptions);
            Assert.Equal(projectId1.ParseOptions, projectId2.ParseOptions);

            CollectionEqual(projectId1.Documents, projectId2.Documents);

            CollectionEqual(projectId1.ProjectReferences, projectId2.ProjectReferences);
            CollectionEqual(projectId1.MetadataReferences, projectId2.MetadataReferences);
            CollectionEqual(projectId1.AnalyzerReferences, projectId2.AnalyzerReferences);

            CollectionEqual(projectId1.AdditionalDocuments, projectId2.AdditionalDocuments);
        }

        internal static void CollectionEqual(ChecksumCollection checksums1, ChecksumCollection checksums2)
        {
            ChecksumObjectEqual(checksums1, checksums2);
            Assert.Equal(checksums1.Objects.Length, checksums2.Objects.Length);

            for (var i = 0; i < checksums1.Objects.Length; i++)
            {
                Assert.Equal(checksums1.Objects[i], checksums2.Objects[i]);
            }
        }

        internal static void CollectionEqual(SnapshotIdCollection<DocumentSnapshotId> documentIds1, SnapshotIdCollection<DocumentSnapshotId> documentIds2)
        {
            ChecksumObjectEqual(documentIds1, documentIds2);
            Assert.Equal(documentIds1.Objects.Length, documentIds2.Objects.Length);

            for (var i = 0; i < documentIds1.Objects.Length; i++)
            {
                SnapshotEqual(documentIds1.Objects[i], documentIds2.Objects[i]);
            }
        }

        internal static void SnapshotEqual(DocumentSnapshotId documentId1, DocumentSnapshotId documentId2)
        {
            ChecksumObjectEqual(documentId1, documentId2);
            Assert.Equal(documentId1.Info, documentId2.Info);
            Assert.Equal(documentId1.Text, documentId2.Text);
        }

        internal static async Task VerifySnapshotInServiceAsync(
            ISolutionSnapshotService snapshotService,
            ProjectSnapshotId projectId,
            int expectedDocumentCount,
            int expectedProjectReferenceCount,
            int expectedMetadataReferenceCount,
            int expectedAnalyzerReferenceCount,
            int expectedAdditionalDocumentCount)
        {
            await VerifyChecksumObjectInServiceAsync(snapshotService, projectId).ConfigureAwait(false);
            await VerifyChecksumInService(snapshotService, projectId.Info, WellKnownChecksumObjects.ProjectSnapshotInfo).ConfigureAwait(false);
            await VerifyChecksumInService(snapshotService, projectId.CompilationOptions, WellKnownChecksumObjects.CompilationOptions).ConfigureAwait(false);
            await VerifyChecksumInService(snapshotService, projectId.ParseOptions, WellKnownChecksumObjects.ParseOptions).ConfigureAwait(false);

            await VerifyCollectionInServiceAsync(snapshotService, projectId.Documents, expectedDocumentCount).ConfigureAwait(false);

            await VerifyCollectionInServiceAsync(snapshotService, projectId.ProjectReferences, expectedProjectReferenceCount, WellKnownChecksumObjects.ProjectReference).ConfigureAwait(false);
            await VerifyCollectionInServiceAsync(snapshotService, projectId.MetadataReferences, expectedMetadataReferenceCount, WellKnownChecksumObjects.MetadataReference).ConfigureAwait(false);
            await VerifyCollectionInServiceAsync(snapshotService, projectId.AnalyzerReferences, expectedAnalyzerReferenceCount, WellKnownChecksumObjects.AnalyzerReference).ConfigureAwait(false);

            await VerifyCollectionInServiceAsync(snapshotService, projectId.AdditionalDocuments, expectedAdditionalDocumentCount).ConfigureAwait(false);
        }

        internal static async Task VerifyCollectionInServiceAsync(ISolutionSnapshotService snapshotService, ChecksumCollection checksums, int expectedCount, string expectedItemKind)
        {
            await VerifyChecksumObjectInServiceAsync(snapshotService, checksums).ConfigureAwait(false);
            Assert.Equal(checksums.Objects.Length, expectedCount);

            foreach (var checksum in checksums.Objects)
            {
                await VerifyChecksumInService(snapshotService, checksum, expectedItemKind).ConfigureAwait(false);
            }
        }

        internal static async Task VerifyCollectionInServiceAsync(ISolutionSnapshotService snapshotService, SnapshotIdCollection<DocumentSnapshotId> documents, int expectedCount)
        {
            await VerifyChecksumObjectInServiceAsync(snapshotService, documents).ConfigureAwait(false);
            Assert.Equal(documents.Objects.Length, expectedCount);

            foreach (var documentId in documents.Objects)
            {
                await VerifySnapshotInServiceAsync(snapshotService, documentId).ConfigureAwait(false);
            }
        }

        internal static async Task VerifySnapshotInServiceAsync(ISolutionSnapshotService snapshotService, DocumentSnapshotId documentId)
        {
            await VerifyChecksumObjectInServiceAsync(snapshotService, documentId).ConfigureAwait(false);
            await VerifyChecksumInService(snapshotService, documentId.Info, WellKnownChecksumObjects.DocumentSnapshotInfo).ConfigureAwait(false);
            await VerifyChecksumInService(snapshotService, documentId.Text, WellKnownChecksumObjects.SourceText).ConfigureAwait(false);
        }

        internal static Task VerifyChecksumObjectInServiceAsync<T>(ISolutionSnapshotService snapshotService, T checksumObject) where T : ChecksumObject
        {
            return VerifyChecksumInService(snapshotService, checksumObject.Checksum, checksumObject.Kind);
        }

        internal static void ChecksumObjectEqual<T>(T checksumObject1, T checksumObject2) where T : ChecksumObject
        {
            ChecksumEqual(checksumObject1.Checksum, checksumObject1.Kind, checksumObject2.Checksum, checksumObject2.Kind);
        }

        internal static async Task VerifyChecksumInService(ISolutionSnapshotService snapshotService, Checksum checksum, string kind)
        {
            Assert.NotNull(checksum);
            var otherObject = await snapshotService.GetChecksumObjectAsync(checksum, CancellationToken.None).ConfigureAwait(false);

            ChecksumEqual(checksum, kind, otherObject.Checksum, otherObject.Kind);
        }

        internal static void ChecksumEqual(Checksum checksum1, string kind1, Checksum checksum2, string kind2)
        {
            Assert.Equal(checksum1, checksum2);
            Assert.Equal(kind1, kind2);
        }

        private class TestAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.LoadFrom(fullPath);
            }
        }
    }
}