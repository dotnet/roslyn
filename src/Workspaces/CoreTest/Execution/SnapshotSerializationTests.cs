// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests.Execution;
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

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                var checksum = snapshot.SolutionChecksum;
                var solutionSyncObject = snapshotService.GetRemotableData(checksum, CancellationToken.None);

                VerifySynchronizationObjectInService(snapshotService, solutionSyncObject);

                var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(checksum).ConfigureAwait(false);
                VerifyChecksumInService(snapshotService, solutionObject.Info, WellKnownSynchronizationKinds.SolutionAttributes);

                var projectsSyncObject = snapshotService.GetRemotableData(solutionObject.Projects.Checksum, CancellationToken.None);
                VerifySynchronizationObjectInService(snapshotService, projectsSyncObject);

                Assert.Equal(solutionObject.Projects.Count, 0);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Empty_Serialization()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySolutionStateSerializationAsync(snapshotService, solution, snapshot.SolutionChecksum).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Project()
        {
            var project = new AdhocWorkspace().CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(project.Solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                var checksum = snapshot.SolutionChecksum;
                var solutionSyncObject = snapshotService.GetRemotableData(checksum, CancellationToken.None);

                VerifySynchronizationObjectInService(snapshotService, solutionSyncObject);

                var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(checksum).ConfigureAwait(false);

                VerifyChecksumInService(snapshotService, solutionObject.Info, WellKnownSynchronizationKinds.SolutionAttributes);

                var projectSyncObject = snapshotService.GetRemotableData(solutionObject.Projects.Checksum, CancellationToken.None);
                VerifySynchronizationObjectInService(snapshotService, projectSyncObject);

                Assert.Equal(solutionObject.Projects.Count, 1);
                VerifySnapshotInService(snapshotService, solutionObject.Projects.ToProjectObjects(snapshotService)[0], 0, 0, 0, 0, 0);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Project_Serialization()
        {
            var project = new AdhocWorkspace().CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(project.Solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySolutionStateSerializationAsync(snapshotService, project.Solution, snapshot.SolutionChecksum).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId()
        {
            var code = "class A { }";

            var document = new AdhocWorkspace().CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp).AddDocument("Document", SourceText.From(code));

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(document.Project.Solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(document.Project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                var syncObject = snapshotService.GetRemotableData(snapshot.SolutionChecksum, CancellationToken.None);
                var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(syncObject.Checksum).ConfigureAwait(false);

                VerifySynchronizationObjectInService(snapshotService, syncObject);
                VerifyChecksumInService(snapshotService, solutionObject.Info, WellKnownSynchronizationKinds.SolutionAttributes);
                VerifyChecksumInService(snapshotService, solutionObject.Projects.Checksum, WellKnownSynchronizationKinds.Projects);

                Assert.Equal(solutionObject.Projects.Count, 1);
                VerifySnapshotInService(snapshotService, solutionObject.Projects.ToProjectObjects(snapshotService)[0], 1, 0, 0, 0, 0);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Serialization()
        {
            var code = "class A { }";

            var document = new AdhocWorkspace().CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp).AddDocument("Document", SourceText.From(code));

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(document.Project.Solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(document.Project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySolutionStateSerializationAsync(snapshotService, document.Project.Solution, snapshot.SolutionChecksum).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                var syncObject = snapshotService.GetRemotableData(snapshot.SolutionChecksum, CancellationToken.None);
                var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(syncObject.Checksum).ConfigureAwait(false);

                VerifySynchronizationObjectInService(snapshotService, syncObject);
                VerifyChecksumInService(snapshotService, solutionObject.Info, WellKnownSynchronizationKinds.SolutionAttributes);
                VerifyChecksumInService(snapshotService, solutionObject.Projects.Checksum, WellKnownSynchronizationKinds.Projects);

                Assert.Equal(solutionObject.Projects.Count, 2);

                var projects = solutionObject.Projects.ToProjectObjects(snapshotService);
                VerifySnapshotInService(snapshotService, projects[0], 1, 1, 1, 1, 1);
                VerifySnapshotInService(snapshotService, projects[1], 1, 0, 0, 0, 0);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Serialization()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySolutionStateSerializationAsync(snapshotService, solution, snapshot.SolutionChecksum).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Asset_Serialization()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot.SolutionChecksum);
                await VerifyAssetAsync(snapshotService, solutionObject).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Asset_Serialization_Desktop()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var solution = CreateFullSolution(hostServices);

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot.SolutionChecksum);
                await VerifyAssetAsync(snapshotService, solutionObject).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Duplicate()
        {
            var solution = CreateFullSolution();

            // this is just data, one can hold the id outside of using statement. but
            // one can't get asset using checksum from the id.
            SolutionStateChecksums solutionId1;
            SolutionStateChecksums solutionId2;

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot1 = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                solutionId1 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot1.SolutionChecksum).ConfigureAwait(false);
            }

            using (var snapshot2 = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                solutionId2 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot2.SolutionChecksum).ConfigureAwait(false);
            }

            // once pinned snapshot scope is released, there is no way to get back to asset.
            // catch Exception because it will throw 2 different exception based on release or debug (ExceptionUtilities.UnexpectedValue)
            Assert.ThrowsAny<Exception>(() => SolutionStateEqual(snapshotService, solutionId1, solutionId2));
        }

        [Fact]
        public async Task MetadataReference_RoundTrip_Test()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var workspace = new AdhocWorkspace(hostServices);
            var reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

            var serializer = new Serializer(workspace.Services);
            var assetFromFile = SolutionAsset.Create(serializer.CreateChecksum(reference, CancellationToken.None), reference, serializer);

            var assetFromStorage = await CloneAssetAsync(serializer, assetFromFile).ConfigureAwait(false);
            var assetFromStorage2 = await CloneAssetAsync(serializer, assetFromStorage).ConfigureAwait(false);
        }

        [Fact]
        public async Task Workspace_RoundTrip_Test()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSynchronizationService;
            var snapshot1 = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false);

            // recover solution from given snapshot
            var recovered = await GetSolutionAsync(snapshotService, snapshot1).ConfigureAwait(false);
            var solutionObject1 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot1.SolutionChecksum).ConfigureAwait(false);

            // create new snapshot from recovered solution
            var snapshot2 = await snapshotService.CreatePinnedRemotableDataScopeAsync(recovered, CancellationToken.None).ConfigureAwait(false);

            // verify asset created by recovered solution is good
            var solutionObject2 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot2.SolutionChecksum).ConfigureAwait(false);
            await VerifyAssetAsync(snapshotService, solutionObject2).ConfigureAwait(false);

            // verify snapshots created from original solution and recovered solution are same
            SolutionStateEqual(snapshotService, solutionObject1, solutionObject2);
            snapshot1.Dispose();

            // recover new solution from recovered solution
            var roundtrip = await GetSolutionAsync(snapshotService, snapshot2).ConfigureAwait(false);

            // create new snapshot from round tripped solution
            using (var snapshot3 = await snapshotService.CreatePinnedRemotableDataScopeAsync(roundtrip, CancellationToken.None).ConfigureAwait(false))
            {
                // verify asset created by rount trip solution is good
                var solutionObject3 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot3.SolutionChecksum).ConfigureAwait(false);
                await VerifyAssetAsync(snapshotService, solutionObject3).ConfigureAwait(false);

                // verify snapshots created from original solution and round trip solution are same.
                SolutionStateEqual(snapshotService, solutionObject2, solutionObject3);
                snapshot2.Dispose();
            }
        }

        [Fact]
        public async Task Workspace_RoundTrip_Test_Desktop()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var solution = CreateFullSolution(hostServices);

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(solution.Workspace.Services) as ISolutionSynchronizationService;
            var snapshot1 = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false);

            // recover solution from given snapshot
            var recovered = await GetSolutionAsync(snapshotService, snapshot1).ConfigureAwait(false);
            var solutionObject1 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot1.SolutionChecksum).ConfigureAwait(false);

            // create new snapshot from recovered solution
            var snapshot2 = await snapshotService.CreatePinnedRemotableDataScopeAsync(recovered, CancellationToken.None).ConfigureAwait(false);

            // verify asset created by recovered solution is good
            var solutionObject2 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot2.SolutionChecksum).ConfigureAwait(false);
            await VerifyAssetAsync(snapshotService, solutionObject2).ConfigureAwait(false);

            // verify snapshots created from original solution and recovered solution are same
            SolutionStateEqual(snapshotService, solutionObject1, solutionObject2);
            snapshot1.Dispose();

            // recover new solution from recovered solution
            var roundtrip = await GetSolutionAsync(snapshotService, snapshot2).ConfigureAwait(false);

            // create new snapshot from round tripped solution
            using (var snapshot3 = await snapshotService.CreatePinnedRemotableDataScopeAsync(roundtrip, CancellationToken.None).ConfigureAwait(false))
            {
                // verify asset created by rount trip solution is good
                var solutionObject3 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot3.SolutionChecksum).ConfigureAwait(false);
                await VerifyAssetAsync(snapshotService, solutionObject3).ConfigureAwait(false);

                // verify snapshots created from original solution and round trip solution are same.
                SolutionStateEqual(snapshotService, solutionObject2, solutionObject3);
                snapshot2.Dispose();
            }
        }

        [Fact]
        public async Task OptionSet_Serialization()
        {
            var workspace = new AdhocWorkspace();

            await VerifyOptionSetsAsync(workspace, LanguageNames.CSharp).ConfigureAwait(false);
            await VerifyOptionSetsAsync(workspace, LanguageNames.VisualBasic).ConfigureAwait(false);
        }

        [Fact]
        public async Task OptionSet_Serialization_CustomValue()
        {
            var workspace = new AdhocWorkspace();

            workspace.Options = workspace.Options.WithChangedOption(CodeStyleOptions.QualifyFieldAccess, LanguageNames.CSharp, new CodeStyleOption<bool>(false, NotificationOption.Error))
                                                 .WithChangedOption(CodeStyleOptions.QualifyMethodAccess, LanguageNames.VisualBasic, new CodeStyleOption<bool>(true, NotificationOption.Warning))
                                                 .WithChangedOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent, new CodeStyleOption<bool>(false, NotificationOption.Suggestion))
                                                 .WithChangedOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic, new CodeStyleOption<bool>(true, NotificationOption.None));

            await VerifyOptionSetsAsync(workspace, LanguageNames.CSharp).ConfigureAwait(false);
            await VerifyOptionSetsAsync(workspace, LanguageNames.VisualBasic).ConfigureAwait(false);
        }

        [Fact]
        public async Task Missing_Metadata_Serailization_Test()
        {
            var workspace = new AdhocWorkspace();
            var serializer = new Serializer(workspace.Services);

            var reference = new MissingMetadataReference();

            // make sure this doesn't throw
            var assetFromFile = SolutionAsset.Create(serializer.CreateChecksum(reference, CancellationToken.None), reference, serializer);
            var assetFromStorage = await CloneAssetAsync(serializer, assetFromFile).ConfigureAwait(false);
            var assetFromStorage2 = await CloneAssetAsync(serializer, assetFromStorage).ConfigureAwait(false);
        }

        [Fact]
        public async Task Missing_Analyzer_Serailization_Test()
        {
            var workspace = new AdhocWorkspace();
            var serializer = new Serializer(workspace.Services);

            var reference = new AnalyzerFileReference("missing_reference", new MissingAnalyzerLoader());

            // make sure this doesn't throw
            var assetFromFile = SolutionAsset.Create(serializer.CreateChecksum(reference, CancellationToken.None), reference, serializer);
            var assetFromStorage = await CloneAssetAsync(serializer, assetFromFile).ConfigureAwait(false);
            var assetFromStorage2 = await CloneAssetAsync(serializer, assetFromStorage).ConfigureAwait(false);
        }

        [Fact]
        public async Task Missing_Analyzer_Serailization_Desktop_Test()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var workspace = new AdhocWorkspace(hostServices);
            var serializer = new Serializer(workspace.Services);

            var reference = new AnalyzerFileReference("missing_reference", new MissingAnalyzerLoader());

            // make sure this doesn't throw
            var assetFromFile = SolutionAsset.Create(serializer.CreateChecksum(reference, CancellationToken.None), reference, serializer);
            var assetFromStorage = await CloneAssetAsync(serializer, assetFromFile).ConfigureAwait(false);
            var assetFromStorage2 = await CloneAssetAsync(serializer, assetFromStorage).ConfigureAwait(false);
        }

        [Fact]
        public async Task SnapshotWithMissingReferencesTest()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var project = new AdhocWorkspace(hostServices).CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var metadata = new MissingMetadataReference();
            var analyzer = new AnalyzerFileReference("missing_reference", new MissingAnalyzerLoader());

            project = project.AddMetadataReference(metadata);
            project = project.AddAnalyzerReference(analyzer);

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(project.Solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                // this shouldn't throw
                var recovered = await GetSolutionAsync(snapshotService, snapshot).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task UnknownLanguageTest()
        {
            var hostServices = MefHostServices.Create(MefHostServices.DefaultAssemblies.Add(typeof(NullLanguageService).Assembly));

            var project = new AdhocWorkspace(hostServices).CurrentSolution.AddProject("Project", "Project.dll", NullLanguageService.TestLanguage);

            var snapshotService = (new SolutionSynchronizationServiceFactory()).CreateService(project.Solution.Workspace.Services) as ISolutionSynchronizationService;
            using (var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                // this shouldn't throw
                var recovered = await GetSolutionAsync(snapshotService, snapshot).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task EmptyAssetChecksumTest()
        {
            var document = new AdhocWorkspace().CurrentSolution.AddProject("empty", "empty", LanguageNames.CSharp).AddDocument("empty", SourceText.From(""));
            var serializer = new Serializer(document.Project.Solution.Workspace.Services);

            var source = serializer.CreateChecksum(await document.GetTextAsync().ConfigureAwait(false), CancellationToken.None);
            var metadata = serializer.CreateChecksum(new MissingMetadataReference(), CancellationToken.None);
            var analyzer = serializer.CreateChecksum(new AnalyzerFileReference("missing", new MissingAnalyzerLoader()), CancellationToken.None);

            Assert.NotEqual(source, metadata);
            Assert.NotEqual(source, analyzer);
            Assert.NotEqual(metadata, analyzer);
        }

        private static async Task VerifyOptionSetsAsync(Workspace workspace, string language)
        {
            var assetBuilder = new CustomAssetBuilder(workspace.CurrentSolution);
            var serializer = new Serializer(workspace.Services);

            var asset = assetBuilder.Build(workspace.Options, language, CancellationToken.None);

            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                await asset.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = new ObjectReader(stream))
                {
                    var recovered = serializer.Deserialize<OptionSet>(asset.Kind, reader, CancellationToken.None);
                    var assetFromStorage = assetBuilder.Build(recovered, language, CancellationToken.None);

                    Assert.Equal(asset.Checksum, assetFromStorage.Checksum);

                    // option should be exactly same
                    Assert.Equal(0, recovered.GetChangedOptions(workspace.Options).Count());
                }
            }
        }

        private async Task<Solution> GetSolutionAsync(ISolutionSynchronizationService service, PinnedRemotableDataScope syncScope)
        {
            var workspace = new AdhocWorkspace();

            var solutionObject = await service.GetValueAsync<SolutionStateChecksums>(syncScope.SolutionChecksum);
            var solutionInfo = await service.GetValueAsync<SolutionInfo.SolutionAttributes>(solutionObject.Info).ConfigureAwait(false);

            var projects = new List<ProjectInfo>();
            foreach (var projectObject in solutionObject.Projects.ToProjectObjects(service))
            {
                var projectInfo = await service.GetValueAsync<ProjectInfo.ProjectAttributes>(projectObject.Info).ConfigureAwait(false);
                if (!workspace.Services.IsSupported(projectInfo.Language))
                {
                    continue;
                }

                var documents = new List<DocumentInfo>();
                foreach (var documentObject in projectObject.Documents.ToDocumentObjects(service))
                {
                    var documentInfo = await service.GetValueAsync<DocumentInfo.DocumentAttributes>(documentObject.Info).ConfigureAwait(false);
                    var text = await service.GetValueAsync<SourceText>(documentObject.Text).ConfigureAwait(false);

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
                foreach (var checksum in projectObject.ProjectReferences)
                {
                    var reference = await service.GetValueAsync<ProjectReference>(checksum).ConfigureAwait(false);
                    p2p.Add(reference);
                }

                var metadata = new List<MetadataReference>();
                foreach (var checksum in projectObject.MetadataReferences)
                {
                    var reference = await service.GetValueAsync<MetadataReference>(checksum).ConfigureAwait(false);
                    metadata.Add(reference);
                }

                var analyzers = new List<AnalyzerReference>();
                foreach (var checksum in projectObject.AnalyzerReferences)
                {
                    var reference = await service.GetValueAsync<AnalyzerReference>(checksum).ConfigureAwait(false);
                    analyzers.Add(reference);
                }

                var additionals = new List<DocumentInfo>();
                foreach (var documentObject in projectObject.AdditionalDocuments.ToDocumentObjects(service))
                {
                    var documentInfo = await service.GetValueAsync<DocumentInfo.DocumentAttributes>(documentObject.Info).ConfigureAwait(false);
                    var text = await service.GetValueAsync<SourceText>(documentObject.Text).ConfigureAwait(false);

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

                var compilationOptions = await service.GetValueAsync<CompilationOptions>(projectObject.CompilationOptions).ConfigureAwait(false);
                var parseOptions = await service.GetValueAsync<ParseOptions>(projectObject.ParseOptions).ConfigureAwait(false);

                projects.Add(
                    ProjectInfo.Create(
                        projectInfo.Id, projectInfo.Version, projectInfo.Name, projectInfo.AssemblyName,
                        projectInfo.Language, projectInfo.FilePath, projectInfo.OutputFilePath,
                        compilationOptions, parseOptions,
                        documents, p2p, metadata, analyzers, additionals, projectInfo.IsSubmission));
            }

            return workspace.AddSolution(SolutionInfo.Create(solutionInfo.Id, solutionInfo.Version, solutionInfo.FilePath, projects));
        }

        private static async Task<RemotableData> CloneAssetAsync(Serializer serializer, RemotableData asset)
        {
            using (var stream = SerializableBytes.CreateWritableStream())
            using (var writer = new ObjectWriter(stream))
            {
                await asset.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);

                stream.Position = 0;
                using (var reader = new ObjectReader(stream))
                {
                    var recovered = serializer.Deserialize<object>(asset.Kind, reader, CancellationToken.None);
                    var assetFromStorage = SolutionAsset.Create(serializer.CreateChecksum(recovered, CancellationToken.None), recovered, serializer);

                    Assert.Equal(asset.Checksum, assetFromStorage.Checksum);
                    return assetFromStorage;
                }
            }
        }

        private interface INullLanguageService : ILanguageService { }

        [ExportLanguageService(typeof(INullLanguageService), TestLanguage), Shared]
        private class NullLanguageService : INullLanguageService
        {
            public const string TestLanguage = nameof(TestLanguage);

            // do nothing
        }

        private class MissingAnalyzerLoader : AnalyzerAssemblyLoader
        {
            protected override Assembly LoadFromPathImpl(string fullPath)
            {
                throw new FileNotFoundException(fullPath);
            }
        }

        private class MissingMetadataReference : PortableExecutableReference
        {
            public MissingMetadataReference() :
                base(MetadataReferenceProperties.Assembly, "missing_reference", XmlDocumentationProvider.Default)
            {
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                return null;
            }

            protected override Metadata GetMetadataImpl()
            {
                throw new FileNotFoundException("can't find");
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            {
                return this;
            }
        }
    }
}