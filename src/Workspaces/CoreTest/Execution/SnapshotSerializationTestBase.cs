// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Execution;
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

        internal static async Task<T> GetValueAsync<T>(ISolutionChecksumService service, Checksum checksum, string kind)
        {
            var snapshotService = (SolutionChecksumServiceFactory.Service)service;
            var checksumObject = service.GetChecksumObject(checksum, CancellationToken.None);

            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                // serialize asset to bits
                await checksumObject.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = new ObjectReader(stream))
                {
                    // deserialize bits to object
                    var serializer = snapshotService.Serializer_TestOnly;
                    return serializer.Deserialize<T>(kind, reader, CancellationToken.None);
                }
            }
        }

        internal static async Task VerifyAssetAsync(ISolutionChecksumService service, Solution solution, SolutionChecksumObject solutionId)
        {
            await VerifyAssetSerializationAsync<SolutionChecksumObjectInfo>(
                service, solutionId.Info, WellKnownChecksumObjects.SolutionChecksumObjectInfo,
                (v, k, s) => new Asset<SolutionChecksumObjectInfo>(v, k, s.SerializeSolutionChecksumObjectInfo)).ConfigureAwait(false);

            foreach (var checksum in solutionId.Projects)
            {
                var projectId = (ProjectChecksumObject)service.GetChecksumObject(checksum, CancellationToken.None);
                await VerifyAssetAsync(service, solution, projectId).ConfigureAwait(false);
            }
        }

        internal static async Task VerifyAssetAsync(
            ISolutionChecksumService service,
            Solution solution,
            ProjectChecksumObject projectId)
        {
            var info = await VerifyAssetSerializationAsync<ProjectChecksumObjectInfo>(
                service, projectId.Info, WellKnownChecksumObjects.ProjectChecksumObjectInfo,
                (v, k, s) => new Asset<ProjectChecksumObjectInfo>(v, k, s.SerializeProjectChecksumObjectInfo)).ConfigureAwait(false);

            var project = solution.GetProject(info.Id);

            await VerifyAssetSerializationAsync<CompilationOptions>(
                service, projectId.CompilationOptions, WellKnownChecksumObjects.CompilationOptions,
                (v, k, s) => new LanguageSpecificAsset<CompilationOptions>(project.Language, v, k, s.SerializeCompilationOptions));

            await VerifyAssetSerializationAsync<ParseOptions>(
                service, projectId.ParseOptions, WellKnownChecksumObjects.ParseOptions,
                (v, k, s) => new LanguageSpecificAsset<ParseOptions>(project.Language, v, k, s.SerializeParseOptions));

            foreach (var checksum in projectId.Documents)
            {
                var documentId = (DocumentChecksumObject)service.GetChecksumObject(checksum, CancellationToken.None);
                await VerifyAssetAsync(service, solution, documentId).ConfigureAwait(false);
            }

            foreach (var projectReference in projectId.ProjectReferences)
            {
                await VerifyAssetSerializationAsync<ProjectReference>(
                    service, projectReference, WellKnownChecksumObjects.ProjectReference,
                    (v, k, s) => new Asset<ProjectReference>(v, k, s.SerializeProjectReference));
            }

            foreach (var metadataReference in projectId.MetadataReferences)
            {
                await VerifyAssetSerializationAsync<MetadataReference>(
                    service, metadataReference, WellKnownChecksumObjects.MetadataReference,
                    (v, k, s) => new MetadataReferenceAsset(s, v, s.HostSerializationService.CreateChecksum(v, CancellationToken.None), k));
            }

            foreach (var analyzerReference in projectId.AnalyzerReferences)
            {
                await VerifyAssetSerializationAsync<AnalyzerReference>(
                    service, analyzerReference, WellKnownChecksumObjects.AnalyzerReference,
                    (v, k, s) => new AnalyzerReferenceAsset(s, v, s.HostSerializationService.CreateChecksum(v, CancellationToken.None), k));
            }

            foreach (var checksum in projectId.AdditionalDocuments)
            {
                var documentId = (DocumentChecksumObject)service.GetChecksumObject(checksum, CancellationToken.None);
                await VerifyAssetAsync(service, solution, documentId).ConfigureAwait(false);
            }
        }

        internal static async Task VerifyAssetAsync(
            ISolutionChecksumService service,
            Solution solution,
            DocumentChecksumObject documentId)
        {
            var info = await VerifyAssetSerializationAsync<DocumentChecksumObjectInfo>(
                service, documentId.Info, WellKnownChecksumObjects.DocumentChecksumObjectInfo,
                (v, k, s) => new Asset<DocumentChecksumObjectInfo>(v, k, s.SerializeDocumentChecksumObjectInfo)).ConfigureAwait(false);

            await VerifyAssetSerializationAsync<SourceText>(
                service, documentId.Text, WellKnownChecksumObjects.SourceText,
                (v, k, s) => new SourceTextAsset(s, CreateTextState(solution, v), new Checksum(v.GetChecksum()), k));
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
            ISolutionChecksumService service,
            Checksum checksum,
            string kind,
            Func<T, string, Serializer, Asset> assetGetter)
        {
            var snapshotService = (SolutionChecksumServiceFactory.Service)service;
            var originalChecksumObject = service.GetChecksumObject(checksum, CancellationToken.None);

            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                // serialize asset to bits
                await originalChecksumObject.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = new ObjectReader(stream))
                {
                    // deserialize bits to object
                    var serializer = snapshotService.Serializer_TestOnly;
                    var recoveredValue = serializer.Deserialize<T>(kind, reader, CancellationToken.None);

                    // re-create asset from object
                    var recreatedChecksumObject = assetGetter(recoveredValue, kind, serializer);

                    // make sure original checksum object and re-created checksum object are same.
                    ChecksumObjectEqual(originalChecksumObject, recreatedChecksumObject);

                    return recoveredValue;
                }
            }
        }

        internal static async Task VerifySnapshotSerializationAsync(ISolutionChecksumService service, Solution solution, SolutionChecksumObject solutionId)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                await solutionId.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = new ObjectReader(stream))
                {
                    var serializer = new Serializer(solution.Workspace.Services);
                    var recovered = serializer.Deserialize<SolutionChecksumObject>(SolutionChecksumObject.Name, reader, CancellationToken.None);

                    SnapshotEqual(service, solutionId, recovered);
                }
            }
        }

        internal static void SnapshotEqual(ISolutionChecksumService service, SolutionChecksumObject solutionId1, SolutionChecksumObject solutionId2)
        {
            ChecksumObjectEqual(solutionId1, solutionId2);
            Assert.Equal(solutionId1.Info, solutionId2.Info);

            CollectionEqual(service, solutionId1.Projects.ToProjectObjects(service), solutionId2.Projects.ToProjectObjects(service));
        }

        internal static void CollectionEqual(ISolutionChecksumService service, ChecksumObjectCollection<ProjectChecksumObject> projectIds1, ChecksumObjectCollection<ProjectChecksumObject> projectIds2)
        {
            ChecksumObjectEqual(projectIds1, projectIds2);
            Assert.Equal(projectIds1.Count, projectIds2.Count);

            for (var i = 0; i < projectIds1.Count; i++)
            {
                SnapshotEqual(service, projectIds1[i], projectIds2[i]);
            }
        }

        internal static void SnapshotEqual(ISolutionChecksumService service, ProjectChecksumObject projectId1, ProjectChecksumObject projectId2)
        {
            ChecksumObjectEqual(projectId1, projectId2);

            Assert.Equal(projectId1.Info, projectId2.Info);
            Assert.Equal(projectId1.CompilationOptions, projectId2.CompilationOptions);
            Assert.Equal(projectId1.ParseOptions, projectId2.ParseOptions);

            CollectionEqual(projectId1.Documents.ToDocumentObjects(service), projectId2.Documents.ToDocumentObjects(service));

            CollectionEqual(projectId1.ProjectReferences, projectId2.ProjectReferences);
            CollectionEqual(projectId1.MetadataReferences, projectId2.MetadataReferences);
            CollectionEqual(projectId1.AnalyzerReferences, projectId2.AnalyzerReferences);

            CollectionEqual(projectId1.AdditionalDocuments.ToDocumentObjects(service), projectId2.AdditionalDocuments.ToDocumentObjects(service));
        }

        internal static void CollectionEqual(ChecksumCollection checksums1, ChecksumCollection checksums2)
        {
            ChecksumObjectEqual(checksums1, checksums2);
            Assert.Equal(checksums1.Count, checksums2.Count);

            for (var i = 0; i < checksums1.Count; i++)
            {
                Assert.Equal(checksums1[i], checksums2[i]);
            }
        }

        internal static void CollectionEqual(ChecksumObjectCollection<DocumentChecksumObject> documentIds1, ChecksumObjectCollection<DocumentChecksumObject> documentIds2)
        {
            ChecksumObjectEqual(documentIds1, documentIds2);
            Assert.Equal(documentIds1.Count, documentIds2.Count);

            for (var i = 0; i < documentIds1.Count; i++)
            {
                SnapshotEqual(documentIds1[i], documentIds2[i]);
            }
        }

        internal static void SnapshotEqual(DocumentChecksumObject documentId1, DocumentChecksumObject documentId2)
        {
            ChecksumObjectEqual(documentId1, documentId2);
            Assert.Equal(documentId1.Info, documentId2.Info);
            Assert.Equal(documentId1.Text, documentId2.Text);
        }

        internal static void VerifySnapshotInService(
            ISolutionChecksumService snapshotService,
            ProjectChecksumObject projectId,
            int expectedDocumentCount,
            int expectedProjectReferenceCount,
            int expectedMetadataReferenceCount,
            int expectedAnalyzerReferenceCount,
            int expectedAdditionalDocumentCount)
        {
            VerifyChecksumObjectInService(snapshotService, projectId);
            VerifyChecksumInService(snapshotService, projectId.Info, WellKnownChecksumObjects.ProjectChecksumObjectInfo);
            VerifyChecksumInService(snapshotService, projectId.CompilationOptions, WellKnownChecksumObjects.CompilationOptions);
            VerifyChecksumInService(snapshotService, projectId.ParseOptions, WellKnownChecksumObjects.ParseOptions);

            VerifyCollectionInService(snapshotService, projectId.Documents.ToDocumentObjects(snapshotService), expectedDocumentCount);

            VerifyCollectionInService(snapshotService, projectId.ProjectReferences, expectedProjectReferenceCount, WellKnownChecksumObjects.ProjectReference);
            VerifyCollectionInService(snapshotService, projectId.MetadataReferences, expectedMetadataReferenceCount, WellKnownChecksumObjects.MetadataReference);
            VerifyCollectionInService(snapshotService, projectId.AnalyzerReferences, expectedAnalyzerReferenceCount, WellKnownChecksumObjects.AnalyzerReference);

            VerifyCollectionInService(snapshotService, projectId.AdditionalDocuments.ToDocumentObjects(snapshotService), expectedAdditionalDocumentCount);
        }

        internal static void VerifyCollectionInService(ISolutionChecksumService snapshotService, ChecksumCollection checksums, int expectedCount, string expectedItemKind)
        {
            VerifyChecksumObjectInService(snapshotService, checksums);
            Assert.Equal(checksums.Count, expectedCount);

            foreach (var checksum in checksums)
            {
                VerifyChecksumInService(snapshotService, checksum, expectedItemKind);
            }
        }

        internal static void VerifyCollectionInService(ISolutionChecksumService snapshotService, ChecksumObjectCollection<DocumentChecksumObject> documents, int expectedCount)
        {
            VerifyChecksumObjectInService(snapshotService, documents);
            Assert.Equal(documents.Count, expectedCount);

            foreach (var documentId in documents)
            {
                VerifySnapshotInService(snapshotService, documentId);
            }
        }

        internal static void VerifySnapshotInService(ISolutionChecksumService snapshotService, DocumentChecksumObject documentId)
        {
            VerifyChecksumObjectInService(snapshotService, documentId);
            VerifyChecksumInService(snapshotService, documentId.Info, WellKnownChecksumObjects.DocumentChecksumObjectInfo);
            VerifyChecksumInService(snapshotService, documentId.Text, WellKnownChecksumObjects.SourceText);
        }

        internal static void VerifyChecksumObjectInService<T>(ISolutionChecksumService snapshotService, T checksumObject) where T : ChecksumObject
        {
            VerifyChecksumInService(snapshotService, checksumObject.Checksum, checksumObject.Kind);
        }

        internal static void ChecksumObjectEqual<T>(T checksumObject1, T checksumObject2) where T : ChecksumObject
        {
            ChecksumEqual(checksumObject1.Checksum, checksumObject1.Kind, checksumObject2.Checksum, checksumObject2.Kind);
        }

        internal static void VerifyChecksumInService(ISolutionChecksumService snapshotService, Checksum checksum, string kind)
        {
            Assert.NotNull(checksum);
            var otherObject = snapshotService.GetChecksumObject(checksum, CancellationToken.None);

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