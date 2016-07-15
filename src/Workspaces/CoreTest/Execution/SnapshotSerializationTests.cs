// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SnapshotSerializationTests : SnapshotSerializationTestBase
    {
        [Fact]
        public async Task CreateSolutionSnapshotId_Empty()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                var solutionId = snapshot.Id;
                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId).ConfigureAwait(false);
                await VerifyChecksumInService(snapshotService, solutionId.Info, WellKnownChecksumObjects.SolutionSnapshotInfo).ConfigureAwait(false);
                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId.Projects).ConfigureAwait(false);

                Assert.Equal(solutionId.Projects.Objects.Length, 0);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Empty_Serialization()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySnapshotSerializationAsync(solution, snapshot.Id).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Project()
        {
            var solution = new AdhocWorkspace().CurrentSolution;
            var project = solution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                var solutionId = snapshot.Id;
                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId).ConfigureAwait(false);
                await VerifyChecksumInService(snapshotService, solutionId.Info, WellKnownChecksumObjects.SolutionSnapshotInfo).ConfigureAwait(false);
                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId.Projects).ConfigureAwait(false);

                Assert.Equal(solutionId.Projects.Objects.Length, 1);
                await VerifySnapshotInServiceAsync(snapshotService, solutionId.Projects.Objects[0], 0, 0, 0, 0, 0);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Project_Serialization()
        {
            var solution = new AdhocWorkspace().CurrentSolution;
            var project = solution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySnapshotSerializationAsync(solution, snapshot.Id).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId()
        {
            var code = "class A { }";

            var solution = new AdhocWorkspace().CurrentSolution;
            var project = solution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
            var document = project.AddDocument("Document", SourceText.From(code));

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(document.Project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                var solutionId = snapshot.Id;
                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId).ConfigureAwait(false);
                await VerifyChecksumInService(snapshotService, solutionId.Info, WellKnownChecksumObjects.SolutionSnapshotInfo).ConfigureAwait(false);
                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId.Projects).ConfigureAwait(false);

                Assert.Equal(solutionId.Projects.Objects.Length, 1);
                await VerifySnapshotInServiceAsync(snapshotService, solutionId.Projects.Objects[0], 1, 0, 0, 0, 0);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Serialization()
        {
            var code = "class A { }";

            var solution = new AdhocWorkspace().CurrentSolution;
            var project = solution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
            var document = project.AddDocument("Document", SourceText.From(code));

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(document.Project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySnapshotSerializationAsync(solution, snapshot.Id).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                var solutionId = snapshot.Id;

                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId).ConfigureAwait(false);
                await VerifyChecksumInService(snapshotService, solutionId.Info, WellKnownChecksumObjects.SolutionSnapshotInfo).ConfigureAwait(false);
                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId.Projects).ConfigureAwait(false);

                Assert.Equal(solutionId.Projects.Objects.Length, 2);

                await VerifySnapshotInServiceAsync(snapshotService, solutionId.Projects.Objects[0], 1, 1, 1, 1, 1);
                await VerifySnapshotInServiceAsync(snapshotService, solutionId.Projects.Objects[1], 1, 0, 0, 0, 0);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Serialization()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySnapshotSerializationAsync(solution, snapshot.Id).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Asset_Serialization()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifyAssetAsync(snapshotService, solution, snapshot.Id).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Asset_Serialization_Desktop()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var solution = CreateFullSolution(hostServices);

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifyAssetAsync(snapshotService, solution, snapshot.Id).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Duplicate()
        {
            var solution = CreateFullSolution();

            // this is just data, one can hold the id outside of using statement. but
            // one can't get asset using checksum from the id.
            SolutionSnapshotId solutionId1;
            SolutionSnapshotId solutionId2;

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot1 = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                solutionId1 = snapshot1.Id;
            }

            using (var snapshot2 = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                solutionId2 = snapshot2.Id;
            }

            SnapshotEqual(solutionId1, solutionId2);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Cache()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot1 = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            using (var snapshot2 = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                var solutionId1 = snapshot1.Id;
                var solutionId2 = snapshot2.Id;

                Assert.True(object.ReferenceEquals(solutionId1, solutionId2));
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Rebuild()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as SolutionSnapshotServiceFactory.Service;

            // builds snapshot graph
            using (var snapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                snapshotService.TestOnly_ClearCache();

                // now test whether we are rebuilding assets correctly.
                var solutionId = snapshot.Id;

                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId).ConfigureAwait(false);
                await VerifyChecksumInService(snapshotService, solutionId.Info, WellKnownChecksumObjects.SolutionSnapshotInfo).ConfigureAwait(false);
                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId.Projects).ConfigureAwait(false);

                Assert.Equal(solutionId.Projects.Objects.Length, 2);

                await VerifySnapshotInServiceAsync(snapshotService, solutionId.Projects.Objects[0], 1, 1, 1, 1, 1);
                await VerifySnapshotInServiceAsync(snapshotService, solutionId.Projects.Objects[1], 1, 0, 0, 0, 0);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Incremental()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;

            // snapshot1 builds graph
            using (var snapshot1 = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                // now test whether we are rebuilding assets correctly.
                var solutionId1 = snapshot1.Id;
                {
                    await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId1).ConfigureAwait(false);
                    await VerifyChecksumInService(snapshotService, solutionId1.Info, WellKnownChecksumObjects.SolutionSnapshotInfo).ConfigureAwait(false);
                    await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId1.Projects).ConfigureAwait(false);

                    Assert.Equal(solutionId1.Projects.Objects.Length, 2);

                    await VerifySnapshotInServiceAsync(snapshotService, solutionId1.Projects.Objects[0], 1, 1, 1, 1, 1);
                    await VerifySnapshotInServiceAsync(snapshotService, solutionId1.Projects.Objects[1], 1, 0, 0, 0, 0);
                }

                // update solution
                var solution2 = solution.AddDocument(DocumentId.CreateNewId(solution.ProjectIds.First(), "incremental"), "incremental", "incremental");

                // snapshot2 reuse some data cached from snapshot1
                using (var snapshot2 = await snapshotService.CreateSnapshotAsync(solution2, CancellationToken.None).ConfigureAwait(false))
                {
                    // now test whether we are rebuilding assets correctly.
                    var solutionId2 = snapshot2.Id;
                    {
                        await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId2).ConfigureAwait(false);
                        await VerifyChecksumInService(snapshotService, solutionId2.Info, WellKnownChecksumObjects.SolutionSnapshotInfo).ConfigureAwait(false);
                        await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId2.Projects).ConfigureAwait(false);

                        Assert.Equal(solutionId2.Projects.Objects.Length, 2);

                        await VerifySnapshotInServiceAsync(snapshotService, solutionId2.Projects.Objects[0], 2, 1, 1, 1, 1);
                        await VerifySnapshotInServiceAsync(snapshotService, solutionId2.Projects.Objects[1], 1, 0, 0, 0, 0);
                    }

                    // make sure solutionSnapshots are changed
                    Assert.False(object.ReferenceEquals(solutionId1, solutionId2));
                    Assert.False(object.ReferenceEquals(solutionId1.Projects, solutionId2.Projects));

                    // this is due to we not having a key that make sure things are not changed in the info
                    Assert.False(object.ReferenceEquals(solutionId1.Info, solutionId2.Info));

                    // make sure projectSnapshots are changed
                    var projectId1 = solutionId1.Projects.Objects[0];
                    var projectId2 = solutionId2.Projects.Objects[0];

                    Assert.False(object.ReferenceEquals(projectId1, projectId2));
                    Assert.False(object.ReferenceEquals(projectId1.Documents, projectId2.Documents));

                    Assert.False(object.ReferenceEquals(projectId1.Info, projectId2.Info));
                    Assert.False(object.ReferenceEquals(projectId1.ProjectReferences, projectId2.ProjectReferences));
                    Assert.False(object.ReferenceEquals(projectId1.MetadataReferences, projectId2.MetadataReferences));
                    Assert.False(object.ReferenceEquals(projectId1.AnalyzerReferences, projectId2.AnalyzerReferences));
                    Assert.False(object.ReferenceEquals(projectId1.AdditionalDocuments, projectId2.AdditionalDocuments));

                    // actual elements are same
                    Assert.True(object.ReferenceEquals(projectId1.CompilationOptions, projectId2.CompilationOptions));
                    Assert.True(object.ReferenceEquals(projectId1.ParseOptions, projectId2.ParseOptions));
                    Assert.True(object.ReferenceEquals(projectId1.Documents.Objects[0], projectId2.Documents.Objects[0]));
                    Assert.True(object.ReferenceEquals(projectId1.ProjectReferences.Objects[0], projectId2.ProjectReferences.Objects[0]));
                    Assert.True(object.ReferenceEquals(projectId1.MetadataReferences.Objects[0], projectId2.MetadataReferences.Objects[0]));
                    Assert.True(object.ReferenceEquals(projectId1.AnalyzerReferences.Objects[0], projectId2.AnalyzerReferences.Objects[0]));
                    Assert.True(object.ReferenceEquals(projectId1.AdditionalDocuments.Objects[0], projectId2.AdditionalDocuments.Objects[0]));

                    // project unchanged are same
                    Assert.True(object.ReferenceEquals(solutionId1.Projects.Objects[1], solutionId2.Projects.Objects[1]));
                }
            }
        }

        [Fact]
        public async Task MetadataReference_RoundTrip_Test()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var workspace = new AdhocWorkspace(hostServices);
            var reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

            var serializer = new Serializer(workspace.Services);
            var snapshotStorages = new SnapshotStorages();
            var assetBuilder = new AssetBuilder(snapshotStorages.CreateSnapshotStorage(workspace.CurrentSolution));

            var assetFromFile = await assetBuilder.BuildAsync(reference, CancellationToken.None).ConfigureAwait(false);
            var assetFromStorage = await CloneAssetAsync(serializer, assetBuilder, assetFromFile).ConfigureAwait(false);
            var assetFromStorage2 = await CloneAssetAsync(serializer, assetBuilder, assetFromStorage).ConfigureAwait(false);
        }

        [Fact]
        public async Task Workspace_RoundTrip_Test()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot1 = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                // recover solution from given snapshot
                var recovered = await GetSolutionAsync(snapshotService, snapshot1).ConfigureAwait(false);

                // create new snapshot from recovered solution
                using (var snapshot2 = await snapshotService.CreateSnapshotAsync(recovered, CancellationToken.None).ConfigureAwait(false))
                {
                    // verify asset created by recovered solution is good
                    await VerifyAssetAsync(snapshotService, recovered, snapshot2.Id).ConfigureAwait(false);

                    // verify snapshots created from original solution and recovered solution are same
                    SnapshotEqual(snapshot1.Id, snapshot2.Id);

                    // recover new solution from recovered solution
                    var roundtrip = await GetSolutionAsync(snapshotService, snapshot2).ConfigureAwait(false);

                    // create new snapshot from round tripped solution
                    using (var snapshot3 = await snapshotService.CreateSnapshotAsync(roundtrip, CancellationToken.None).ConfigureAwait(false))
                    {
                        // verify asset created by rount trip solution is good
                        await VerifyAssetAsync(snapshotService, recovered, snapshot2.Id).ConfigureAwait(false);

                        // verify snapshots created from original solution and round trip solution are same.
                        SnapshotEqual(snapshot1.Id, snapshot3.Id);
                    }
                }
            }
        }

        [Fact]
        public async Task Workspace_RoundTrip_Test_Desktop()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var solution = CreateFullSolution(hostServices);

            var snapshotService = (new SolutionSnapshotServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot1 = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                // recover solution from given snapshot
                var recovered = await GetSolutionAsync(snapshotService, snapshot1).ConfigureAwait(false);

                // create new snapshot from recovered solution
                using (var snapshot2 = await snapshotService.CreateSnapshotAsync(recovered, CancellationToken.None).ConfigureAwait(false))
                {
                    // verify asset created by recovered solution is good
                    await VerifyAssetAsync(snapshotService, recovered, snapshot2.Id).ConfigureAwait(false);

                    // verify snapshots created from original solution and recovered solution are same
                    SnapshotEqual(snapshot1.Id, snapshot2.Id);

                    // recover new solution from recovered solution
                    var roundtrip = await GetSolutionAsync(snapshotService, snapshot2).ConfigureAwait(false);

                    // create new snapshot from round tripped solution
                    using (var snapshot3 = await snapshotService.CreateSnapshotAsync(roundtrip, CancellationToken.None).ConfigureAwait(false))
                    {
                        // verify asset created by rount trip solution is good
                        await VerifyAssetAsync(snapshotService, recovered, snapshot2.Id).ConfigureAwait(false);

                        // verify snapshots created from original solution and round trip solution are same.
                        SnapshotEqual(snapshot1.Id, snapshot3.Id);
                    }
                }
            }
        }

        private async Task<Solution> GetSolutionAsync(ISolutionSnapshotService service, SolutionSnapshot snapshot)
        {
            var workspace = new AdhocWorkspace();

            var solutionInfo = await GetValueAsync<SolutionSnapshotInfo>(service, snapshot.Id.Info, WellKnownChecksumObjects.SolutionSnapshotInfo).ConfigureAwait(false);

            var projects = new List<ProjectInfo>();
            foreach (var projectSnapshot in snapshot.Id.Projects.Objects)
            {
                var documents = new List<DocumentInfo>();
                foreach (var documentSnapshot in projectSnapshot.Documents.Objects)
                {
                    var documentInfo = await GetValueAsync<DocumentSnapshotInfo>(service, documentSnapshot.Info, WellKnownChecksumObjects.DocumentSnapshotInfo).ConfigureAwait(false);
                    var text = await GetValueAsync<SourceText>(service, documentSnapshot.Text, WellKnownChecksumObjects.SourceText).ConfigureAwait(false);

                    // TODO: do we need version?
                    documents.Add(
                        DocumentInfo.Create(
                            documentInfo.Id,
                            documentInfo.Name,
                            documentInfo.Folders,
                            documentInfo.SourceCodeKind,
                            TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())),
                            documentInfo.FilePath,
                            documentInfo.IsGenerated));
                }

                var p2p = new List<ProjectReference>();
                foreach (var checksum in projectSnapshot.ProjectReferences.Objects)
                {
                    var reference = await GetValueAsync<ProjectReference>(service, checksum, WellKnownChecksumObjects.ProjectReference).ConfigureAwait(false);
                    p2p.Add(reference);
                }
                var metadata = new List<MetadataReference>();
                foreach (var checksum in projectSnapshot.MetadataReferences.Objects)
                {
                    var reference = await GetValueAsync<MetadataReference>(service, checksum, WellKnownChecksumObjects.MetadataReference).ConfigureAwait(false);
                    metadata.Add(reference);
                }

                var analyzers = new List<AnalyzerReference>();
                foreach (var checksum in projectSnapshot.AnalyzerReferences.Objects)
                {
                    var reference = await GetValueAsync<AnalyzerReference>(service, checksum, WellKnownChecksumObjects.AnalyzerReference).ConfigureAwait(false);
                    analyzers.Add(reference);
                }

                var additionals = new List<DocumentInfo>();
                foreach (var documentSnapshot in projectSnapshot.AdditionalDocuments.Objects)
                {
                    var documentInfo = await GetValueAsync<DocumentSnapshotInfo>(service, documentSnapshot.Info, WellKnownChecksumObjects.DocumentSnapshotInfo).ConfigureAwait(false);
                    var text = await GetValueAsync<SourceText>(service, documentSnapshot.Text, WellKnownChecksumObjects.SourceText).ConfigureAwait(false);

                    // TODO: do we need version?
                    additionals.Add(
                        DocumentInfo.Create(
                            documentInfo.Id,
                            documentInfo.Name,
                            documentInfo.Folders,
                            documentInfo.SourceCodeKind,
                            TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())),
                            documentInfo.FilePath,
                            documentInfo.IsGenerated));
                }

                var projectInfo = await GetValueAsync<ProjectSnapshotInfo>(service, projectSnapshot.Info, WellKnownChecksumObjects.ProjectSnapshotInfo).ConfigureAwait(false);
                var compilationOptions = await GetValueAsync<CompilationOptions>(service, projectSnapshot.CompilationOptions, WellKnownChecksumObjects.CompilationOptions).ConfigureAwait(false);
                var parseOptions = await GetValueAsync<ParseOptions>(service, projectSnapshot.ParseOptions, WellKnownChecksumObjects.ParseOptions).ConfigureAwait(false);

                projects.Add(
                    ProjectInfo.Create(
                        projectInfo.Id, projectInfo.Version, projectInfo.Name, projectInfo.AssemblyName,
                        projectInfo.Language, projectInfo.FilePath, projectInfo.OutputFilePath,
                        compilationOptions, parseOptions,
                        documents, p2p, metadata, analyzers, additionals));
            }

            return workspace.AddSolution(SolutionInfo.Create(solutionInfo.Id, solutionInfo.Version, solutionInfo.FilePath, projects));
        }

        private static async Task<Asset> CloneAssetAsync(Serializer serializer, AssetBuilder assetBuilder, Asset asset)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                await asset.WriteToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = new ObjectReader(stream))
                {
                    var recovered = serializer.Deserialize<MetadataReference>(WellKnownChecksumObjects.MetadataReference, reader, CancellationToken.None);
                    var assetFromStorage = await assetBuilder.BuildAsync(recovered, CancellationToken.None).ConfigureAwait(false);

                    Assert.Equal(asset.Checksum, assetFromStorage.Checksum);

                    return assetFromStorage;
                }
            }
        }
    }
}