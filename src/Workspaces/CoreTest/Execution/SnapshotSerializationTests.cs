// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
using Microsoft.CodeAnalysis.Test.Utilities;
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

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false);
            var checksum = snapshot.SolutionChecksum;
            var solutionSyncObject = snapshot.GetRemotableData(checksum, CancellationToken.None);

            VerifySynchronizationObjectInService(snapshotService, solutionSyncObject);

            var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(checksum).ConfigureAwait(false);
            VerifyChecksumInService(snapshotService, solutionObject.Info, WellKnownSynchronizationKind.SolutionAttributes);

            var projectsSyncObject = snapshot.GetRemotableData(solutionObject.Projects.Checksum, CancellationToken.None);
            VerifySynchronizationObjectInService(snapshotService, projectsSyncObject);

            Assert.Equal(solutionObject.Projects.Count, 0);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Empty_Serialization()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false);
            await VerifySolutionStateSerializationAsync(snapshotService, solution, snapshot.SolutionChecksum).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Project()
        {
            var project = new AdhocWorkspace().CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(project.Solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(project.Solution, CancellationToken.None).ConfigureAwait(false);
            var checksum = snapshot.SolutionChecksum;
            var solutionSyncObject = snapshot.GetRemotableData(checksum, CancellationToken.None);

            VerifySynchronizationObjectInService(snapshotService, solutionSyncObject);

            var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(checksum).ConfigureAwait(false);

            VerifyChecksumInService(snapshotService, solutionObject.Info, WellKnownSynchronizationKind.SolutionAttributes);

            var projectSyncObject = snapshot.GetRemotableData(solutionObject.Projects.Checksum, CancellationToken.None);
            VerifySynchronizationObjectInService(snapshotService, projectSyncObject);

            Assert.Equal(solutionObject.Projects.Count, 1);
            VerifySnapshotInService(snapshotService, solutionObject.Projects.ToProjectObjects(snapshotService)[0], 0, 0, 0, 0, 0);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Project_Serialization()
        {
            var project = new AdhocWorkspace().CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(project.Solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(project.Solution, CancellationToken.None).ConfigureAwait(false);
            await VerifySolutionStateSerializationAsync(snapshotService, project.Solution, snapshot.SolutionChecksum).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId()
        {
            var code = "class A { }";

            var document = new AdhocWorkspace().CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp).AddDocument("Document", SourceText.From(code));

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(document.Project.Solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(document.Project.Solution, CancellationToken.None).ConfigureAwait(false);
            var syncObject = snapshot.GetRemotableData(snapshot.SolutionChecksum, CancellationToken.None);
            var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(syncObject.Checksum).ConfigureAwait(false);

            VerifySynchronizationObjectInService(snapshotService, syncObject);
            VerifyChecksumInService(snapshotService, solutionObject.Info, WellKnownSynchronizationKind.SolutionAttributes);
            VerifyChecksumInService(snapshotService, solutionObject.Projects.Checksum, WellKnownSynchronizationKind.Projects);

            Assert.Equal(solutionObject.Projects.Count, 1);
            VerifySnapshotInService(snapshotService, solutionObject.Projects.ToProjectObjects(snapshotService)[0], 1, 0, 0, 0, 0);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Serialization()
        {
            var code = "class A { }";

            var document = new AdhocWorkspace().CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp).AddDocument("Document", SourceText.From(code));

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(document.Project.Solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(document.Project.Solution, CancellationToken.None).ConfigureAwait(false);
            await VerifySolutionStateSerializationAsync(snapshotService, document.Project.Solution, snapshot.SolutionChecksum).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full()
        {
            var solution = CreateFullSolution();

            var firstProjectChecksum = await solution.GetProject(solution.ProjectIds[0]).State.GetChecksumAsync(CancellationToken.None);
            var secondProjectChecksum = await solution.GetProject(solution.ProjectIds[1]).State.GetChecksumAsync(CancellationToken.None);

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false);
            var syncObject = snapshot.GetRemotableData(snapshot.SolutionChecksum, CancellationToken.None);
            var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(syncObject.Checksum).ConfigureAwait(false);

            VerifySynchronizationObjectInService(snapshotService, syncObject);
            VerifyChecksumInService(snapshotService, solutionObject.Info, WellKnownSynchronizationKind.SolutionAttributes);
            VerifyChecksumInService(snapshotService, solutionObject.Projects.Checksum, WellKnownSynchronizationKind.Projects);

            Assert.Equal(solutionObject.Projects.Count, 2);

            var projects = solutionObject.Projects.ToProjectObjects(snapshotService);
            VerifySnapshotInService(snapshotService, projects.Where(p => p.Checksum == firstProjectChecksum).First(), 1, 1, 1, 1, 1);
            VerifySnapshotInService(snapshotService, projects.Where(p => p.Checksum == secondProjectChecksum).First(), 1, 0, 0, 0, 0);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Serialization()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false);
            await VerifySolutionStateSerializationAsync(snapshotService, solution, snapshot.SolutionChecksum).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Asset_Serialization()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false);
            var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot.SolutionChecksum);
            await VerifyAssetAsync(snapshotService, solutionObject).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Asset_Serialization_Desktop()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var solution = CreateFullSolution(hostServices);

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None).ConfigureAwait(false);
            var solutionObject = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot.SolutionChecksum);
            await VerifyAssetAsync(snapshotService, solutionObject).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Duplicate()
        {
            var solution = CreateFullSolution();

            // this is just data, one can hold the id outside of using statement. but
            // one can't get asset using checksum from the id.
            SolutionStateChecksums solutionId1;
            SolutionStateChecksums solutionId2;

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
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

            var serializer = workspace.Services.GetService<ISerializerService>();
            var assetFromFile = SolutionAsset.Create(serializer.CreateChecksum(reference, CancellationToken.None), reference, serializer);

            var assetFromStorage = await CloneAssetAsync(serializer, assetFromFile).ConfigureAwait(false);
            var assetFromStorage2 = await CloneAssetAsync(serializer, assetFromStorage).ConfigureAwait(false);
        }

        [Fact]
        public async Task Workspace_RoundTrip_Test()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
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
            using var snapshot3 = await snapshotService.CreatePinnedRemotableDataScopeAsync(roundtrip, CancellationToken.None).ConfigureAwait(false);
            // verify asset created by rount trip solution is good
            var solutionObject3 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot3.SolutionChecksum).ConfigureAwait(false);
            await VerifyAssetAsync(snapshotService, solutionObject3).ConfigureAwait(false);

            // verify snapshots created from original solution and round trip solution are same.
            SolutionStateEqual(snapshotService, solutionObject2, solutionObject3);
            snapshot2.Dispose();
        }

        [Fact]
        public async Task Workspace_RoundTrip_Test_Desktop()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var solution = CreateFullSolution(hostServices);

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
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
            using var snapshot3 = await snapshotService.CreatePinnedRemotableDataScopeAsync(roundtrip, CancellationToken.None).ConfigureAwait(false);
            // verify asset created by rount trip solution is good
            var solutionObject3 = await snapshotService.GetValueAsync<SolutionStateChecksums>(snapshot3.SolutionChecksum).ConfigureAwait(false);
            await VerifyAssetAsync(snapshotService, solutionObject3).ConfigureAwait(false);

            // verify snapshots created from original solution and round trip solution are same.
            SolutionStateEqual(snapshotService, solutionObject2, solutionObject3);
            snapshot2.Dispose();
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
                                                 .WithChangedOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent, new CodeStyleOption<bool>(false, NotificationOption.Suggestion))
                                                 .WithChangedOption(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess, LanguageNames.VisualBasic, new CodeStyleOption<bool>(true, NotificationOption.Silent));

            await VerifyOptionSetsAsync(workspace, LanguageNames.CSharp).ConfigureAwait(false);
            await VerifyOptionSetsAsync(workspace, LanguageNames.VisualBasic).ConfigureAwait(false);
        }

        [Fact]
        public async Task Missing_Metadata_Serailization_Test()
        {
            var workspace = new AdhocWorkspace();
            var serializer = workspace.Services.GetService<ISerializerService>();

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
            var serializer = workspace.Services.GetService<ISerializerService>();

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
            var serializer = workspace.Services.GetService<ISerializerService>();

            var reference = new AnalyzerFileReference("missing_reference", new MissingAnalyzerLoader());

            // make sure this doesn't throw
            var assetFromFile = SolutionAsset.Create(serializer.CreateChecksum(reference, CancellationToken.None), reference, serializer);
            var assetFromStorage = await CloneAssetAsync(serializer, assetFromFile).ConfigureAwait(false);
            var assetFromStorage2 = await CloneAssetAsync(serializer, assetFromStorage).ConfigureAwait(false);
        }

        [Fact]
        public async Task RoundTrip_Analyzer_Serailization_Test()
        {
            using var tempRoot = new TempRoot();
            var workspace = new AdhocWorkspace();
            var serializer = workspace.Services.GetService<ISerializerService>();

            // actually shadow copy content
            var location = typeof(object).Assembly.Location;
            var file = tempRoot.CreateFile("shadow", "dll");
            file.CopyContentFrom(location);

            var reference = new AnalyzerFileReference(location, new MockShadowCopyAnalyzerAssemblyLoader(ImmutableDictionary<string, string>.Empty.Add(location, file.Path)));

            // make sure this doesn't throw
            var assetFromFile = SolutionAsset.Create(serializer.CreateChecksum(reference, CancellationToken.None), reference, serializer);
            var assetFromStorage = await CloneAssetAsync(serializer, assetFromFile).ConfigureAwait(false);
            var assetFromStorage2 = await CloneAssetAsync(serializer, assetFromStorage).ConfigureAwait(false);
        }

        [Fact]
        public async Task RoundTrip_Analyzer_Serailization_Desktop_Test()
        {
            using var tempRoot = new TempRoot();
            var hostServices = MefHostServices.Create(
MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var workspace = new AdhocWorkspace(hostServices);
            var serializer = workspace.Services.GetService<ISerializerService>();

            // actually shadow copy content
            var location = typeof(object).Assembly.Location;
            var file = tempRoot.CreateFile("shadow", "dll");
            file.CopyContentFrom(location);

            var reference = new AnalyzerFileReference(location, new MockShadowCopyAnalyzerAssemblyLoader(ImmutableDictionary<string, string>.Empty.Add(location, file.Path)));

            // make sure this doesn't throw
            var assetFromFile = SolutionAsset.Create(serializer.CreateChecksum(reference, CancellationToken.None), reference, serializer);
            var assetFromStorage = await CloneAssetAsync(serializer, assetFromFile).ConfigureAwait(false);
            var assetFromStorage2 = await CloneAssetAsync(serializer, assetFromStorage).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShadowCopied_Analyzer_Serailization_Desktop_Test()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            using var tempRoot = new TempRoot();
            using var workspace = new AdhocWorkspace(hostServices);
            var reference = CreateShadowCopiedAnalyzerReference(tempRoot);

            var serializer = workspace.Services.GetService<ISerializerService>();

            // make sure this doesn't throw
            var assetFromFile = SolutionAsset.Create(serializer.CreateChecksum(reference, CancellationToken.None), reference, serializer);

            // this will verify serialized analyzer reference return same checksum as the original one
            var assetFromStorage = await CloneAssetAsync(serializer, assetFromFile).ConfigureAwait(false);
        }

        [Fact]
        public void WorkspaceAnalyzer_Serailization_Desktop_Test()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            using var tempRoot = new TempRoot();
            using var workspace = new AdhocWorkspace(hostServices);
            var reference = CreateShadowCopiedAnalyzerReference(tempRoot);

            var assetBuilder = new CustomAssetBuilder(workspace);
            var asset = assetBuilder.Build(reference, CancellationToken.None);

            // verify checksum from custom asset builder uses different checksum than regular one
            var service = workspace.Services.GetService<IReferenceSerializationService>();
            var expectedChecksum = Checksum.Create(
                WellKnownSynchronizationKind.AnalyzerReference,
                service.CreateChecksum(reference, usePathFromAssembly: false, CancellationToken.None));
            Assert.Equal(expectedChecksum, asset.Checksum);

            // verify usePathFromAssembly return different checksum for same reference
            var fromFilePath = service.CreateChecksum(reference, usePathFromAssembly: false, CancellationToken.None);
            var fromAssembly = service.CreateChecksum(reference, usePathFromAssembly: true, CancellationToken.None);
            Assert.NotEqual(fromFilePath, fromAssembly);
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

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(project.Solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(project.Solution, CancellationToken.None).ConfigureAwait(false);
            // this shouldn't throw
            var recovered = await GetSolutionAsync(snapshotService, snapshot).ConfigureAwait(false);
        }

        [Fact]
        public async Task UnknownLanguageTest()
        {
            var hostServices = MefHostServices.Create(MefHostServices.DefaultAssemblies.Add(typeof(NoCompilationConstants).Assembly));

            var project = new AdhocWorkspace(hostServices).CurrentSolution.AddProject("Project", "Project.dll", NoCompilationConstants.LanguageName);

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(project.Solution.Workspace.Services) as IRemotableDataService;
            using var snapshot = await snapshotService.CreatePinnedRemotableDataScopeAsync(project.Solution, CancellationToken.None).ConfigureAwait(false);
            // this shouldn't throw
            var recovered = await GetSolutionAsync(snapshotService, snapshot).ConfigureAwait(false);
        }

        [Fact]
        public async Task EmptyAssetChecksumTest()
        {
            var document = new AdhocWorkspace().CurrentSolution.AddProject("empty", "empty", LanguageNames.CSharp).AddDocument("empty", SourceText.From(""));
            var serializer = document.Project.Solution.Workspace.Services.GetService<ISerializerService>();

            var source = serializer.CreateChecksum(await document.GetTextAsync().ConfigureAwait(false), CancellationToken.None);
            var metadata = serializer.CreateChecksum(new MissingMetadataReference(), CancellationToken.None);
            var analyzer = serializer.CreateChecksum(new AnalyzerFileReference("missing", new MissingAnalyzerLoader()), CancellationToken.None);

            Assert.NotEqual(source, metadata);
            Assert.NotEqual(source, analyzer);
            Assert.NotEqual(metadata, analyzer);
        }

        [Fact]
        public async Task VBParseOptionsInCompilationOptions()
        {
            var project = new AdhocWorkspace().CurrentSolution.AddProject("empty", "empty", LanguageNames.VisualBasic);
            project = project.WithCompilationOptions(
                ((VisualBasic.VisualBasicCompilationOptions)project.CompilationOptions).WithParseOptions((VisualBasic.VisualBasicParseOptions)project.ParseOptions));

            var checksum = await project.State.GetChecksumAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(checksum);
        }

        [Fact]
        public async Task TestMetadataXmlDocComment()
        {
            // portable layer doesn't support xml doc comments
            // this depends on which layer supports IDocumentationProviderService
            var xmlDocComment = await GetXmlDocumentAsync(MefHostServices.Create(MefHostServices.DefaultAssemblies));
            Assert.False(string.IsNullOrEmpty(xmlDocComment));
        }

        [Fact]
        public void TestEncodingSerialization()
        {
            var hostServices = MefHostServices.Create(
                MefHostServices.DefaultAssemblies.Add(typeof(Host.TemporaryStorageServiceFactory.TemporaryStorageService).Assembly));

            var workspace = new AdhocWorkspace(hostServices);
            var serializer = workspace.Services.GetService<ISerializerService>();

            // test with right serializable encoding
            var sourceText = SourceText.From("Hello", Encoding.UTF8);
            using (var stream = SerializableBytes.CreateWritableStream())
            {
                using (var objectWriter = new ObjectWriter(stream))
                {
                    serializer.Serialize(sourceText, objectWriter, CancellationToken.None);
                }

                stream.Position = 0;

                using var objectReader = ObjectReader.TryGetReader(stream);

                var newText = serializer.Deserialize<SourceText>(sourceText.GetWellKnownSynchronizationKind(), objectReader, CancellationToken.None);
                Assert.Equal(sourceText.ToString(), newText.ToString());
            }

            // test with wrong encoding that doesn't support serialization
            sourceText = SourceText.From("Hello", new NotSerializableEncoding());
            using (var stream = SerializableBytes.CreateWritableStream())
            {
                using (var objectWriter = new ObjectWriter(stream))
                {
                    serializer.Serialize(sourceText, objectWriter, CancellationToken.None);
                }

                stream.Position = 0;

                using var objectReader = ObjectReader.TryGetReader(stream);

                var newText = serializer.Deserialize<SourceText>(sourceText.GetWellKnownSynchronizationKind(), objectReader, CancellationToken.None);
                Assert.Equal(sourceText.ToString(), newText.ToString());
            }
        }

        [Fact]
        public void TestCompilationOptions_NullableAndImport()
        {
            var csharpOptions = CSharp.CSharpCompilation.Create("dummy").Options.WithNullableContextOptions(NullableContextOptions.Warnings).WithMetadataImportOptions(MetadataImportOptions.All);
            var vbOptions = VisualBasic.VisualBasicCompilation.Create("dummy").Options.WithMetadataImportOptions(MetadataImportOptions.Internal);

            var hostServices = MefHostServices.Create(MefHostServices.DefaultAssemblies);

            var workspace = new AdhocWorkspace(hostServices);
            var serializer = workspace.Services.GetService<ISerializerService>();

            VerifyOptions(csharpOptions);
            VerifyOptions(vbOptions);

            void VerifyOptions(CompilationOptions originalOptions)
            {
                using var stream = SerializableBytes.CreateWritableStream();
                using (var objectWriter = new ObjectWriter(stream))
                {
                    serializer.Serialize(originalOptions, objectWriter, CancellationToken.None);
                }

                stream.Position = 0;
                using var objectReader = ObjectReader.TryGetReader(stream);
                var recoveredOptions = serializer.Deserialize<CompilationOptions>(originalOptions.GetWellKnownSynchronizationKind(), objectReader, CancellationToken.None);

                var original = serializer.CreateChecksum(originalOptions, CancellationToken.None);
                var recovered = serializer.CreateChecksum(recoveredOptions, CancellationToken.None);

                Assert.Equal(original, recovered);
            }
        }

        private async Task<string> GetXmlDocumentAsync(HostServices services)
        {
            using var tempRoot = new TempRoot();
            // get original assembly location
            var mscorlibLocation = typeof(object).Assembly.Location;

            // set up dll and xml doc content
            var tempDir = tempRoot.CreateDirectory();
            var tempCorlib = tempDir.CopyFile(mscorlibLocation);
            var tempCorlibXml = tempDir.CreateFile(Path.ChangeExtension(tempCorlib.Path, "xml"));
            tempCorlibXml.WriteAllText(@"<?xml version=""1.0"" encoding=""utf-8""?>
<doc>
  <assembly>
    <name>mscorlib</name>
  </assembly>
  <members>
    <member name=""T:System.Object"">
      <summary>Supports all classes in the .NET Framework class hierarchy and provides low-level services to derived classes. This is the ultimate base class of all classes in the .NET Framework; it is the root of the type hierarchy.To browse the .NET Framework source code for this type, see the Reference Source.</summary>
    </member>
  </members>
</doc>");

            // currently portable layer doesn't support xml documment
            var solution = new AdhocWorkspace(services).CurrentSolution
                                               .AddProject("Project", "Project.dll", LanguageNames.CSharp)
                                               .AddMetadataReference(MetadataReference.CreateFromFile(tempCorlib.Path))
                                               .Solution;

            var snapshotService = (new RemotableDataServiceFactory()).CreateService(solution.Workspace.Services) as IRemotableDataService;
            using var scope = await snapshotService.CreatePinnedRemotableDataScopeAsync(solution, CancellationToken.None);
            // recover solution from given snapshot
            var recovered = await GetSolutionAsync(snapshotService, scope);

            var compilation = await recovered.Projects.First().GetCompilationAsync(CancellationToken.None);
            var objectType = compilation.GetTypeByMetadataName("System.Object");
            var xmlDocComment = objectType.GetDocumentationCommentXml();

            return xmlDocComment;
        }

        private static async Task VerifyOptionSetsAsync(Workspace workspace, string language)
        {
            var assetBuilder = new CustomAssetBuilder(workspace);
            var serializer = workspace.Services.GetService<ISerializerService>();

            var asset = assetBuilder.Build(workspace.Options, language, CancellationToken.None);

            using var stream = SerializableBytes.CreateWritableStream();
            using var writer = new ObjectWriter(stream);
            await asset.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);

            stream.Position = 0;
            using var reader = ObjectReader.TryGetReader(stream);
            var recovered = serializer.Deserialize<OptionSet>(asset.Kind, reader, CancellationToken.None);
            var assetFromStorage = assetBuilder.Build(recovered, language, CancellationToken.None);

            Assert.Equal(asset.Checksum, assetFromStorage.Checksum);

            // option should be exactly same
            Assert.Equal(0, recovered.GetChangedOptions(workspace.Options).Count());
        }

        private async Task<Solution> GetSolutionAsync(IRemotableDataService service, PinnedRemotableDataScope syncScope)
        {
            var solutionInfo = await SolutionInfoCreator.CreateSolutionInfoAsync(new AssetProvider(service), syncScope.SolutionChecksum, CancellationToken.None).ConfigureAwait(false);

            var workspace = new AdhocWorkspace();
            return workspace.AddSolution(solutionInfo);
        }

        private static async Task<RemotableData> CloneAssetAsync(ISerializerService serializer, RemotableData asset)
        {
            using var stream = SerializableBytes.CreateWritableStream();
            using var writer = new ObjectWriter(stream);
            await asset.WriteObjectToAsync(writer, CancellationToken.None).ConfigureAwait(false);

            stream.Position = 0;
            using var reader = ObjectReader.TryGetReader(stream);
            var recovered = serializer.Deserialize<object>(asset.Kind, reader, CancellationToken.None);
            var assetFromStorage = SolutionAsset.Create(serializer.CreateChecksum(recovered, CancellationToken.None), recovered, serializer);

            Assert.Equal(asset.Checksum, assetFromStorage.Checksum);
            return assetFromStorage;
        }

        private static AnalyzerFileReference CreateShadowCopiedAnalyzerReference(TempRoot tempRoot)
        {
            // use 2 different files as shadow copied content
            var original = typeof(AdhocWorkspace).Assembly.Location;

            var shadow = tempRoot.CreateFile("shadow", "dll");
            shadow.CopyContentFrom(typeof(object).Assembly.Location);

            return new AnalyzerFileReference(original, new MockShadowCopyAnalyzerAssemblyLoader(ImmutableDictionary<string, string>.Empty.Add(original, shadow.Path)));
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
            public MissingMetadataReference()
                : base(MetadataReferenceProperties.Assembly, "missing_reference", XmlDocumentationProvider.Default)
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

        private class MockShadowCopyAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
        {
            private readonly ImmutableDictionary<string, string> _map;

            public MockShadowCopyAnalyzerAssemblyLoader(ImmutableDictionary<string, string> map)
            {
                _map = map;
            }

            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.LoadFrom(_map[fullPath]);
            }
        }

        private class NotSerializableEncoding : Encoding
        {
            private readonly Encoding _real = Encoding.UTF8;

            public override string WebName => _real.WebName;
            public override int GetByteCount(char[] chars, int index, int count) => _real.GetByteCount(chars, index, count);
            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) => GetBytes(chars, charIndex, charCount, bytes, byteIndex);
            public override int GetCharCount(byte[] bytes, int index, int count) => GetCharCount(bytes, index, count);
            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) => GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            public override int GetMaxByteCount(int charCount) => GetMaxByteCount(charCount);
            public override int GetMaxCharCount(int byteCount) => GetMaxCharCount(byteCount);
        }
    }
}
