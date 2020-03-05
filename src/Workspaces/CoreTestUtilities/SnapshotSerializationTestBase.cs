﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Execution;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class SnapshotSerializationTestBase
    {
        internal static Solution CreateFullSolution(HostServices? hostServices = null)
        {
            var solution = new AdhocWorkspace(hostServices ?? Host.Mef.MefHostServices.DefaultHost).CurrentSolution;
            var languages = ImmutableHashSet.Create(LanguageNames.CSharp, LanguageNames.VisualBasic);
            var solutionOptions = solution.Workspace.Services.GetRequiredService<IOptionService>().GetSerializableOptionsSnapshot(languages);
            solution = solution.WithOptions(solutionOptions);

            var csCode = "class A { }";
            var project1 = solution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
            var document1 = project1.AddDocument("Document1", SourceText.From(csCode));

            var vbCode = "Class B\r\nEnd Class";
            var project2 = document1.Project.Solution.AddProject("Project2", "Project2.dll", LanguageNames.VisualBasic);
            var document2 = project2.AddDocument("Document2", SourceText.From(vbCode));

            project1 = document2.Project.Solution.GetRequiredProject(project1.Id).AddProjectReference(new ProjectReference(project2.Id, ImmutableArray.Create("test")));
            project1 = project1.AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            project1 = project1.AddAnalyzerReference(new AnalyzerFileReference(typeof(object).Assembly.Location, new TestAnalyzerAssemblyLoader()));

            project1 = project1.AddAdditionalDocument("Additional", SourceText.From("hello"), ImmutableArray.Create("test"), @".\Add").Project;

            return project1.Solution.AddAnalyzerConfigDocuments(
                ImmutableArray.Create(
                    DocumentInfo.Create(
                        DocumentId.CreateNewId(project1.Id),
                        ".editorconfig",
                        loader: TextLoader.From(TextAndVersion.Create(SourceText.From("root = true"), VersionStamp.Create())))));
        }

        internal static async Task VerifyAssetAsync(IRemotableDataService service, SolutionStateChecksums solutionObject)
        {
            await VerifyAssetSerializationAsync<SolutionInfo.SolutionAttributes>(
                service, solutionObject.Info, WellKnownSynchronizationKind.SolutionAttributes,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s)).ConfigureAwait(false);

            foreach (var projectChecksum in solutionObject.Projects)
            {
                var projectObject = await service.GetValueAsync<ProjectStateChecksums>(projectChecksum).ConfigureAwait(false);
                await VerifyAssetAsync(service, projectObject).ConfigureAwait(false);
            }
        }

        internal static async Task VerifyAssetAsync(IRemotableDataService service, ProjectStateChecksums projectObject)
        {
            var info = await VerifyAssetSerializationAsync<ProjectInfo.ProjectAttributes>(
                service, projectObject.Info, WellKnownSynchronizationKind.ProjectAttributes,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s)).ConfigureAwait(false);

            await VerifyAssetSerializationAsync<CompilationOptions>(
                service, projectObject.CompilationOptions, WellKnownSynchronizationKind.CompilationOptions,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));

            await VerifyAssetSerializationAsync<ParseOptions>(
                service, projectObject.ParseOptions, WellKnownSynchronizationKind.ParseOptions,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));

            foreach (var checksum in projectObject.Documents)
            {
                var documentObject = await service.GetValueAsync<DocumentStateChecksums>(checksum).ConfigureAwait(false);
                await VerifyAssetAsync(service, documentObject).ConfigureAwait(false);
            }

            foreach (var checksum in projectObject.ProjectReferences)
            {
                await VerifyAssetSerializationAsync<ProjectReference>(
                    service, checksum, WellKnownSynchronizationKind.ProjectReference,
                    (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));
            }

            foreach (var checksum in projectObject.MetadataReferences)
            {
                await VerifyAssetSerializationAsync<MetadataReference>(
                    service, checksum, WellKnownSynchronizationKind.MetadataReference,
                    (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));
            }

            foreach (var checksum in projectObject.AnalyzerReferences)
            {
                await VerifyAssetSerializationAsync<AnalyzerReference>(
                    service, checksum, WellKnownSynchronizationKind.AnalyzerReference,
                    (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));
            }

            foreach (var checksum in projectObject.AdditionalDocuments)
            {
                var documentObject = await service.GetValueAsync<DocumentStateChecksums>(checksum).ConfigureAwait(false);
                await VerifyAssetAsync(service, documentObject).ConfigureAwait(false);
            }

            foreach (var checksum in projectObject.AnalyzerConfigDocuments)
            {
                var documentObject = await service.GetValueAsync<DocumentStateChecksums>(checksum).ConfigureAwait(false);
                await VerifyAssetAsync(service, documentObject).ConfigureAwait(false);
            }
        }

        internal static async Task VerifyAssetAsync(IRemotableDataService service, DocumentStateChecksums documentObject)
        {
            var info = await VerifyAssetSerializationAsync<DocumentInfo.DocumentAttributes>(
                service, documentObject.Info, WellKnownSynchronizationKind.DocumentAttributes,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s)).ConfigureAwait(false);

            await VerifyAssetSerializationAsync<SourceText>(
                service, documentObject.Text, WellKnownSynchronizationKind.SourceText,
                (v, k, s) => new SolutionAsset(s.CreateChecksum(v, CancellationToken.None), v, s));
        }

        internal static async Task<T> VerifyAssetSerializationAsync<T>(
            IRemotableDataService service,
            Checksum checksum,
            WellKnownSynchronizationKind kind,
            Func<T, WellKnownSynchronizationKind, ISerializerService, RemotableData> assetGetter)
        {
            // re-create asset from object
            var syncService = (RemotableDataServiceFactory.Service)service;
            var syncObject = (await syncService.TestOnly_GetRemotableDataAsync(checksum, CancellationToken.None).ConfigureAwait(false))!;

            var recoveredValue = await service.GetValueAsync<T>(checksum).ConfigureAwait(false);
            var recreatedSyncObject = assetGetter(recoveredValue, kind, syncService.Serializer_TestOnly);

            // make sure original object and re-created object are same.
            SynchronizationObjectEqual(syncObject, recreatedSyncObject);

            return recoveredValue;
        }

        internal static async Task VerifySolutionStateSerializationAsync(IRemotableDataService service, Solution solution, Checksum solutionChecksum)
        {
            var solutionObjectFromSyncObject = await service.GetValueAsync<SolutionStateChecksums>(solutionChecksum);
            Assert.True(solution.State.TryGetStateChecksums(out var solutionObjectFromSolution));

            SolutionStateEqual(service, solutionObjectFromSolution, solutionObjectFromSyncObject);
        }

        internal static void SolutionStateEqual(IRemotableDataService service, SolutionStateChecksums solutionObject1, SolutionStateChecksums solutionObject2)
        {
            ChecksumWithChildrenEqual(solutionObject1, solutionObject2);

            ProjectStatesEqual(service, solutionObject1.Projects.ToProjectObjects(service), solutionObject2.Projects.ToProjectObjects(service));
        }

        internal static void ProjectStateEqual(IRemotableDataService service, ProjectStateChecksums projectObjects1, ProjectStateChecksums projectObjects2)
        {
            ChecksumWithChildrenEqual(projectObjects1, projectObjects2);

            ChecksumWithChildrenEqual(projectObjects1.Documents.ToDocumentObjects(service), projectObjects2.Documents.ToDocumentObjects(service));
            ChecksumWithChildrenEqual(projectObjects1.AdditionalDocuments.ToDocumentObjects(service), projectObjects2.AdditionalDocuments.ToDocumentObjects(service));
            ChecksumWithChildrenEqual(projectObjects1.AnalyzerConfigDocuments.ToDocumentObjects(service), projectObjects2.AnalyzerConfigDocuments.ToDocumentObjects(service));
        }

        internal static void ProjectStatesEqual(IRemotableDataService service, ChecksumObjectCollection<ProjectStateChecksums> projectObjects1, ChecksumObjectCollection<ProjectStateChecksums> projectObjects2)
        {
            SynchronizationObjectEqual(projectObjects1, projectObjects2);

            Assert.Equal(projectObjects1.Count, projectObjects2.Count);

            for (var i = 0; i < projectObjects1.Count; i++)
            {
                ProjectStateEqual(service, projectObjects1[i], projectObjects2[i]);
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

        internal static async Task VerifySnapshotInServiceAsync(
            IRemotableDataService snapshotService,
            ProjectStateChecksums projectObject,
            int expectedDocumentCount,
            int expectedProjectReferenceCount,
            int expectedMetadataReferenceCount,
            int expectedAnalyzerReferenceCount,
            int expectedAdditionalDocumentCount)
        {
            await VerifyChecksumInServiceAsync(snapshotService, projectObject.Checksum, projectObject.GetWellKnownSynchronizationKind()).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(snapshotService, projectObject.Info, WellKnownSynchronizationKind.ProjectAttributes).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(snapshotService, projectObject.CompilationOptions, WellKnownSynchronizationKind.CompilationOptions).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(snapshotService, projectObject.ParseOptions, WellKnownSynchronizationKind.ParseOptions).ConfigureAwait(false);

            await VerifyCollectionInService(snapshotService, projectObject.Documents.ToDocumentObjects(snapshotService), expectedDocumentCount).ConfigureAwait(false);

            await VerifyCollectionInService(snapshotService, projectObject.ProjectReferences, expectedProjectReferenceCount, WellKnownSynchronizationKind.ProjectReference).ConfigureAwait(false);
            await VerifyCollectionInService(snapshotService, projectObject.MetadataReferences, expectedMetadataReferenceCount, WellKnownSynchronizationKind.MetadataReference).ConfigureAwait(false);
            await VerifyCollectionInService(snapshotService, projectObject.AnalyzerReferences, expectedAnalyzerReferenceCount, WellKnownSynchronizationKind.AnalyzerReference).ConfigureAwait(false);

            await VerifyCollectionInService(snapshotService, projectObject.AdditionalDocuments.ToDocumentObjects(snapshotService), expectedAdditionalDocumentCount).ConfigureAwait(false);
        }

        internal static async Task VerifyCollectionInService(IRemotableDataService snapshotService, ChecksumCollection checksums, int expectedCount, WellKnownSynchronizationKind expectedItemKind)
        {
            await VerifyChecksumInServiceAsync(snapshotService, checksums.Checksum, checksums.GetWellKnownSynchronizationKind()).ConfigureAwait(false);
            Assert.Equal(checksums.Count, expectedCount);

            foreach (var checksum in checksums)
            {
                await VerifyChecksumInServiceAsync(snapshotService, checksum, expectedItemKind).ConfigureAwait(false);
            }
        }

        internal static async Task VerifyCollectionInService(IRemotableDataService snapshotService, ChecksumObjectCollection<DocumentStateChecksums> documents, int expectedCount)
        {
            await VerifySynchronizationObjectInServiceAsync(snapshotService, documents).ConfigureAwait(false);
            Assert.Equal(documents.Count, expectedCount);

            foreach (var documentId in documents)
            {
                await VerifySnapshotInServiceAsync(snapshotService, documentId).ConfigureAwait(false);
            }
        }

        internal static async Task VerifySnapshotInServiceAsync(IRemotableDataService snapshotService, DocumentStateChecksums documentObject)
        {
            await VerifyChecksumInServiceAsync(snapshotService, documentObject.Checksum, documentObject.GetWellKnownSynchronizationKind()).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(snapshotService, documentObject.Info, WellKnownSynchronizationKind.DocumentAttributes).ConfigureAwait(false);
            await VerifyChecksumInServiceAsync(snapshotService, documentObject.Text, WellKnownSynchronizationKind.SourceText).ConfigureAwait(false);
        }

        internal static async Task VerifySynchronizationObjectInServiceAsync(IRemotableDataService snapshotService, RemotableData syncObject)
        {
            await VerifyChecksumInServiceAsync(snapshotService, syncObject.Checksum, syncObject.Kind).ConfigureAwait(false);
        }

        internal static async Task VerifyChecksumInServiceAsync(IRemotableDataService snapshotService, Checksum checksum, WellKnownSynchronizationKind kind)
        {
            Assert.NotNull(checksum);
            var service = (RemotableDataServiceFactory.Service)snapshotService;
            var otherObject = (await service.TestOnly_GetRemotableDataAsync(checksum, CancellationToken.None).ConfigureAwait(false))!;

            ChecksumEqual(checksum, kind, otherObject.Checksum, otherObject.Kind);
        }

        internal static void SynchronizationObjectEqual<T>(T checksumObject1, T checksumObject2) where T : RemotableData
        {
            ChecksumEqual(checksumObject1.Checksum, checksumObject1.Kind, checksumObject2.Checksum, checksumObject2.Kind);
        }

        internal static void ChecksumEqual(Checksum checksum1, WellKnownSynchronizationKind kind1, Checksum checksum2, WellKnownSynchronizationKind kind2)
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
