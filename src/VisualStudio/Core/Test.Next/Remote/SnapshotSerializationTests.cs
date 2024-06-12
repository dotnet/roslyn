// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.UnitTests
{
    [CollectionDefinition(Name)]
    public class AssemblyLoadTestFixtureCollection : ICollectionFixture<AssemblyLoadTestFixture>
    {
        public const string Name = nameof(AssemblyLoadTestFixtureCollection);
        private AssemblyLoadTestFixtureCollection() { }
    }

    [Collection(AssemblyLoadTestFixtureCollection.Name)]
    [UseExportProvider]
    public class SnapshotSerializationTests
    {
        private readonly AssemblyLoadTestFixture _testFixture;
        public SnapshotSerializationTests(AssemblyLoadTestFixture testFixture)
        {
            _testFixture = testFixture;
        }

        private static Workspace CreateWorkspace(Type[] additionalParts = null)
            => new AdhocWorkspace(FeaturesTestCompositions.Features.AddParts(additionalParts).WithTestHostParts(TestHost.OutOfProcess).GetHostServices());

        internal static Solution CreateFullSolution(Workspace workspace)
        {
            var solution = workspace.CurrentSolution;

            var csCode = "class A { }";
            var project1 = solution.AddProject("Project", "Project.dll", LanguageNames.CSharp);
            var document1 = project1.AddDocument("Document1", SourceText.From(csCode));

            var vbCode = "Class B\r\nEnd Class";
            var project2 = document1.Project.Solution.AddProject("Project2", "Project2.dll", LanguageNames.VisualBasic);
            var document2 = project2.AddDocument("Document2", SourceText.From(vbCode));

            solution = document2.Project.Solution.GetRequiredProject(project1.Id)
                .AddProjectReference(new ProjectReference(project2.Id, ImmutableArray.Create("test")))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddAnalyzerReference(new AnalyzerFileReference(Path.Combine(TempRoot.Root, "path1"), new TestAnalyzerAssemblyLoader()))
                .AddAdditionalDocument("Additional", SourceText.From("hello"), ImmutableArray.Create("test"), @".\Add").Project.Solution;

            return solution
                .WithAnalyzerReferences(new[] { new AnalyzerFileReference(Path.Combine(TempRoot.Root, "path2"), new TestAnalyzerAssemblyLoader()) })
                .AddAnalyzerConfigDocuments(
                ImmutableArray.Create(
                    DocumentInfo.Create(
                        DocumentId.CreateNewId(project1.Id),
                        ".editorconfig",
                        loader: TextLoader.From(TextAndVersion.Create(SourceText.From("root = true"), VersionStamp.Create())))));
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Empty()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var validator = new SerializationValidator(workspace.Services);

            using var scope = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false);
            var checksum = scope.SolutionChecksum;
            var solutionSyncObject = await scope.GetAssetAsync(checksum, CancellationToken.None).ConfigureAwait(false);

            await validator.VerifySynchronizationObjectInServiceAsync(solutionSyncObject).ConfigureAwait(false);

            var solutionObject = await validator.GetValueAsync<SolutionStateChecksums>(checksum).ConfigureAwait(false);
            await validator.VerifyChecksumInServiceAsync(solutionObject.Attributes, WellKnownSynchronizationKind.SolutionAttributes).ConfigureAwait(false);

            var projectsSyncObject = await scope.GetAssetAsync(solutionObject.Projects.Checksum, CancellationToken.None).ConfigureAwait(false);
            await validator.VerifySynchronizationObjectInServiceAsync(projectsSyncObject).ConfigureAwait(false);

            Assert.Equal(0, solutionObject.Projects.Count);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Empty_Serialization()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;

            var validator = new SerializationValidator(workspace.Services);
            using var scope = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false);
            await validator.VerifySolutionStateSerializationAsync(solution, scope.SolutionChecksum).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Project()
        {
            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var project = solution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var validator = new SerializationValidator(workspace.Services);

            using var scope = await validator.AssetStorage.StoreAssetsAsync(project.Solution, CancellationToken.None).ConfigureAwait(false);
            var checksum = scope.SolutionChecksum;
            var solutionSyncObject = await scope.GetAssetAsync(checksum, CancellationToken.None).ConfigureAwait(false);

            await validator.VerifySynchronizationObjectInServiceAsync(solutionSyncObject).ConfigureAwait(false);

            var solutionObject = await validator.GetValueAsync<SolutionStateChecksums>(checksum).ConfigureAwait(false);

            await validator.VerifyChecksumInServiceAsync(solutionObject.Attributes, WellKnownSynchronizationKind.SolutionAttributes);

            var projectSyncObject = await scope.GetAssetAsync(solutionObject.Projects.Checksum, CancellationToken.None).ConfigureAwait(false);
            await validator.VerifySynchronizationObjectInServiceAsync(projectSyncObject).ConfigureAwait(false);

            Assert.Equal(1, solutionObject.Projects.Count);
            await validator.VerifySnapshotInServiceAsync(validator.ToProjectObjects(solutionObject.Projects)[0], 0, 0, 0, 0, 0).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Project_Serialization()
        {
            using var workspace = CreateWorkspace();
            var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var validator = new SerializationValidator(workspace.Services);

            using var snapshot = await validator.AssetStorage.StoreAssetsAsync(project.Solution, CancellationToken.None).ConfigureAwait(false);
            await validator.VerifySolutionStateSerializationAsync(project.Solution, snapshot.SolutionChecksum).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId()
        {
            var code = "class A { }";

            using var workspace = CreateWorkspace();
            var document = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp).AddDocument("Document", SourceText.From(code));

            var validator = new SerializationValidator(workspace.Services);

            using var scope = await validator.AssetStorage.StoreAssetsAsync(document.Project.Solution, CancellationToken.None).ConfigureAwait(false);
            var syncObject = await scope.GetAssetAsync(scope.SolutionChecksum, CancellationToken.None).ConfigureAwait(false);
            var solutionObject = await validator.GetValueAsync<SolutionStateChecksums>(syncObject.Checksum).ConfigureAwait(false);

            await validator.VerifySynchronizationObjectInServiceAsync(syncObject).ConfigureAwait(false);
            await validator.VerifyChecksumInServiceAsync(solutionObject.Attributes, WellKnownSynchronizationKind.SolutionAttributes).ConfigureAwait(false);
            await validator.VerifyChecksumInServiceAsync(solutionObject.Projects.Checksum, WellKnownSynchronizationKind.ChecksumCollection).ConfigureAwait(false);

            Assert.Equal(1, solutionObject.Projects.Count);
            await validator.VerifySnapshotInServiceAsync(validator.ToProjectObjects(solutionObject.Projects)[0], 1, 0, 0, 0, 0).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Serialization()
        {
            var code = "class A { }";

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution;
            var document = solution.AddProject("Project", "Project.dll", LanguageNames.CSharp).AddDocument("Document", SourceText.From(code));

            var validator = new SerializationValidator(workspace.Services);

            using var scope = await validator.AssetStorage.StoreAssetsAsync(document.Project.Solution, CancellationToken.None).ConfigureAwait(false);
            await validator.VerifySolutionStateSerializationAsync(document.Project.Solution, scope.SolutionChecksum).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full()
        {
            using var workspace = CreateWorkspace();
            var solution = CreateFullSolution(workspace);

            var firstProjectChecksum = await solution.GetProject(solution.ProjectIds[0]).State.GetChecksumAsync(CancellationToken.None);
            var secondProjectChecksum = await solution.GetProject(solution.ProjectIds[1]).State.GetChecksumAsync(CancellationToken.None);

            var validator = new SerializationValidator(workspace.Services);

            using var scope = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false);
            var syncObject = await scope.GetAssetAsync(scope.SolutionChecksum, CancellationToken.None).ConfigureAwait(false);
            var solutionObject = await validator.GetValueAsync<SolutionStateChecksums>(syncObject.Checksum).ConfigureAwait(false);

            await validator.VerifySynchronizationObjectInServiceAsync(syncObject).ConfigureAwait(false);
            await validator.VerifyChecksumInServiceAsync(solutionObject.Attributes, WellKnownSynchronizationKind.SolutionAttributes).ConfigureAwait(false);
            await validator.VerifyChecksumInServiceAsync(solutionObject.Projects.Checksum, WellKnownSynchronizationKind.ChecksumCollection).ConfigureAwait(false);

            Assert.Equal(2, solutionObject.Projects.Count);

            var projects = validator.ToProjectObjects(solutionObject.Projects);
            await validator.VerifySnapshotInServiceAsync(projects.Where(p => p.Checksum == firstProjectChecksum).First(), 1, 1, 1, 1, 1).ConfigureAwait(false);
            await validator.VerifySnapshotInServiceAsync(projects.Where(p => p.Checksum == secondProjectChecksum).First(), 1, 0, 0, 0, 0).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Serialization()
        {
            using var workspace = CreateWorkspace();
            var solution = CreateFullSolution(workspace);

            var validator = new SerializationValidator(workspace.Services);

            using var scope = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false);
            await validator.VerifySolutionStateSerializationAsync(solution, scope.SolutionChecksum).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Asset_Serialization()
        {
            using var workspace = CreateWorkspace();
            var solution = CreateFullSolution(workspace);

            var validator = new SerializationValidator(workspace.Services);

            using var scope = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false);
            var solutionObject = await validator.GetValueAsync<SolutionStateChecksums>(scope.SolutionChecksum);
            await validator.VerifyAssetAsync(solutionObject).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Asset_Serialization_Desktop()
        {
            using var workspace = CreateWorkspace();
            var solution = CreateFullSolution(workspace);

            var validator = new SerializationValidator(workspace.Services);

            using var scope = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false);
            var solutionObject = await validator.GetValueAsync<SolutionStateChecksums>(scope.SolutionChecksum);
            await validator.VerifyAssetAsync(solutionObject).ConfigureAwait(false);
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Duplicate()
        {
            using var workspace = CreateWorkspace();
            var solution = CreateFullSolution(workspace);

            // this is just data, one can hold the id outside of using statement. but
            // one can't get asset using checksum from the id.
            SolutionStateChecksums solutionId1;
            SolutionStateChecksums solutionId2;

            var validator = new SerializationValidator(workspace.Services);

            using (var scope1 = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                solutionId1 = await validator.GetValueAsync<SolutionStateChecksums>(scope1.SolutionChecksum).ConfigureAwait(false);
            }

            using (var scope2 = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                solutionId2 = await validator.GetValueAsync<SolutionStateChecksums>(scope2.SolutionChecksum).ConfigureAwait(false);
            }

            // once pinned snapshot scope is released, there is no way to get back to asset.
            // catch Exception because it will throw 2 different exception based on release or debug (ExceptionUtilities.UnexpectedValue)
            Assert.ThrowsAny<Exception>(() => validator.SolutionStateEqual(solutionId1, solutionId2));
        }

        [Fact]
        public void MetadataReference_RoundTrip_Test()
        {
            using var workspace = CreateWorkspace();

            var reference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

            var serializer = workspace.Services.GetService<ISerializerService>();
            var assetFromFile = new SolutionAsset(serializer.CreateChecksum(reference, CancellationToken.None), reference);

            var assetFromStorage = CloneAsset(serializer, assetFromFile);
            _ = CloneAsset(serializer, assetFromStorage);
        }

        [Fact]
        public async Task Workspace_RoundTrip_Test()
        {
            using var workspace = CreateWorkspace();
            var solution = CreateFullSolution(workspace);

            var validator = new SerializationValidator(workspace.Services);

            var scope1 = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false);

            // recover solution from given snapshot
            var recovered = await validator.GetSolutionAsync(scope1).ConfigureAwait(false);
            var solutionObject1 = await validator.GetValueAsync<SolutionStateChecksums>(scope1.SolutionChecksum).ConfigureAwait(false);

            // create new snapshot from recovered solution
            using var scope2 = await validator.AssetStorage.StoreAssetsAsync(recovered, CancellationToken.None).ConfigureAwait(false);

            // verify asset created by recovered solution is good
            var solutionObject2 = await validator.GetValueAsync<SolutionStateChecksums>(scope2.SolutionChecksum).ConfigureAwait(false);
            await validator.VerifyAssetAsync(solutionObject2).ConfigureAwait(false);

            // verify snapshots created from original solution and recovered solution are same
            validator.SolutionStateEqual(solutionObject1, solutionObject2);

            // recover new solution from recovered solution
            var roundtrip = await validator.GetSolutionAsync(scope2).ConfigureAwait(false);

            // create new snapshot from round tripped solution
            using var scope3 = await validator.AssetStorage.StoreAssetsAsync(roundtrip, CancellationToken.None).ConfigureAwait(false);
            // verify asset created by round trip solution is good
            var solutionObject3 = await validator.GetValueAsync<SolutionStateChecksums>(scope3.SolutionChecksum).ConfigureAwait(false);
            await validator.VerifyAssetAsync(solutionObject3).ConfigureAwait(false);

            // verify snapshots created from original solution and round trip solution are same.
            validator.SolutionStateEqual(solutionObject2, solutionObject3);
        }

        [Fact]
        public async Task Workspace_RoundTrip_Test_Desktop()
        {
            using var workspace = CreateWorkspace();
            var solution = CreateFullSolution(workspace);

            var validator = new SerializationValidator(workspace.Services);

            var scope1 = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None).ConfigureAwait(false);

            // recover solution from given snapshot
            var recovered = await validator.GetSolutionAsync(scope1).ConfigureAwait(false);
            var solutionObject1 = await validator.GetValueAsync<SolutionStateChecksums>(scope1.SolutionChecksum).ConfigureAwait(false);

            // create new snapshot from recovered solution
            using var scope2 = await validator.AssetStorage.StoreAssetsAsync(recovered, CancellationToken.None).ConfigureAwait(false);

            // verify asset created by recovered solution is good
            var solutionObject2 = await validator.GetValueAsync<SolutionStateChecksums>(scope2.SolutionChecksum).ConfigureAwait(false);
            await validator.VerifyAssetAsync(solutionObject2).ConfigureAwait(false);

            // verify snapshots created from original solution and recovered solution are same
            validator.SolutionStateEqual(solutionObject1, solutionObject2);
            scope1.Dispose();

            // recover new solution from recovered solution
            var roundtrip = await validator.GetSolutionAsync(scope2).ConfigureAwait(false);

            // create new snapshot from round tripped solution
            using var scope3 = await validator.AssetStorage.StoreAssetsAsync(roundtrip, CancellationToken.None).ConfigureAwait(false);
            // verify asset created by round trip solution is good
            var solutionObject3 = await validator.GetValueAsync<SolutionStateChecksums>(scope3.SolutionChecksum).ConfigureAwait(false);
            await validator.VerifyAssetAsync(solutionObject3).ConfigureAwait(false);

            // verify snapshots created from original solution and round trip solution are same.
            validator.SolutionStateEqual(solutionObject2, solutionObject3);
        }

        [Fact]
        public void Missing_Metadata_Serialization_Test()
        {
            using var workspace = CreateWorkspace();
            var serializer = workspace.Services.GetService<ISerializerService>();

            var reference = new MissingMetadataReference();

            // make sure this doesn't throw
            var assetFromFile = new SolutionAsset(serializer.CreateChecksum(reference, CancellationToken.None), reference);
            var assetFromStorage = CloneAsset(serializer, assetFromFile);
            _ = CloneAsset(serializer, assetFromStorage);
        }

        [Fact]
        public void Missing_Analyzer_Serialization_Test()
        {
            using var workspace = CreateWorkspace();
            var serializer = workspace.Services.GetService<ISerializerService>();

            var reference = new AnalyzerFileReference(Path.Combine(TempRoot.Root, "missing_reference"), new MissingAnalyzerLoader());

            // make sure this doesn't throw
            var assetFromFile = new SolutionAsset(serializer.CreateChecksum(reference, CancellationToken.None), reference);
            var assetFromStorage = CloneAsset(serializer, assetFromFile);
            _ = CloneAsset(serializer, assetFromStorage);
        }

        [Fact]
        public void Missing_Analyzer_Serialization_Desktop_Test()
        {
            using var workspace = CreateWorkspace();
            var serializer = workspace.Services.GetService<ISerializerService>();

            var reference = new AnalyzerFileReference(Path.Combine(TempRoot.Root, "missing_reference"), new MissingAnalyzerLoader());

            // make sure this doesn't throw
            var assetFromFile = new SolutionAsset(serializer.CreateChecksum(reference, CancellationToken.None), reference);
            var assetFromStorage = CloneAsset(serializer, assetFromFile);
            _ = CloneAsset(serializer, assetFromStorage);
        }

        [Fact]
        public void RoundTrip_Analyzer_Serialization_Test()
        {
            using var tempRoot = new TempRoot();
            using var workspace = CreateWorkspace();

            var serializer = workspace.Services.GetService<ISerializerService>();

            // actually shadow copy content
            var location = typeof(object).Assembly.Location;
            var file = tempRoot.CreateFile("shadow", "dll");
            file.CopyContentFrom(location);

            var reference = new AnalyzerFileReference(location, new MockShadowCopyAnalyzerAssemblyLoader(ImmutableDictionary<string, string>.Empty.Add(location, file.Path)));

            // make sure this doesn't throw
            var assetFromFile = new SolutionAsset(serializer.CreateChecksum(reference, CancellationToken.None), reference);
            var assetFromStorage = CloneAsset(serializer, assetFromFile);
            _ = CloneAsset(serializer, assetFromStorage);
        }

        [Fact]
        public void RoundTrip_Analyzer_Serialization_Desktop_Test()
        {
            using var tempRoot = new TempRoot();

            using var workspace = CreateWorkspace();
            var serializer = workspace.Services.GetService<ISerializerService>();

            // actually shadow copy content
            var location = typeof(object).Assembly.Location;
            var file = tempRoot.CreateFile("shadow", "dll");
            file.CopyContentFrom(location);

            var reference = new AnalyzerFileReference(location, new MockShadowCopyAnalyzerAssemblyLoader(ImmutableDictionary<string, string>.Empty.Add(location, file.Path)));

            // make sure this doesn't throw
            var assetFromFile = new SolutionAsset(serializer.CreateChecksum(reference, CancellationToken.None), reference);
            var assetFromStorage = CloneAsset(serializer, assetFromFile);
            _ = CloneAsset(serializer, assetFromStorage);
        }

        [Fact]
        public void ShadowCopied_Analyzer_Serialization_Desktop_Test()
        {
            using var tempRoot = new TempRoot();
            using var workspace = CreateWorkspace();
            var reference = CreateShadowCopiedAnalyzerReference(tempRoot);

            var serializer = workspace.Services.GetService<ISerializerService>();

            // make sure this doesn't throw
            var assetFromFile = new SolutionAsset(serializer.CreateChecksum(reference, CancellationToken.None), reference);

            // this will verify serialized analyzer reference return same checksum as the original one
            _ = CloneAsset(serializer, assetFromFile);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1107294")]
        public async Task SnapshotWithIdenticalAnalyzerFiles()
        {
            using var workspace = CreateWorkspace();
            var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            using var temp = new TempRoot();
            var dir = temp.CreateDirectory();

            // create two analyzer assembly files whose content is identical but path is different:
            var file1 = dir.CreateFile("analyzer1.dll").CopyContentFrom(_testFixture.FaultyAnalyzer);
            var file2 = dir.CreateFile("analyzer2.dll").CopyContentFrom(_testFixture.FaultyAnalyzer);

            var analyzer1 = new AnalyzerFileReference(file1.Path, TestAnalyzerAssemblyLoader.LoadNotImplemented);
            var analyzer2 = new AnalyzerFileReference(file2.Path, TestAnalyzerAssemblyLoader.LoadNotImplemented);

            project = project.AddAnalyzerReferences(new[] { analyzer1, analyzer2 });

            var validator = new SerializationValidator(workspace.Services);
            using var snapshot = await validator.AssetStorage.StoreAssetsAsync(project.Solution, CancellationToken.None).ConfigureAwait(false);

            var recovered = await validator.GetSolutionAsync(snapshot).ConfigureAwait(false);
            AssertEx.Equal(new[] { file1.Path, file2.Path }, recovered.GetProject(project.Id).AnalyzerReferences.Select(r => r.FullPath));
        }

        [Fact]
        public async Task SnapshotWithMissingReferencesTest()
        {
            using var workspace = CreateWorkspace();
            var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.CSharp);

            var metadata = new MissingMetadataReference();
            var analyzer = new AnalyzerFileReference(Path.Combine(TempRoot.Root, "missing_reference"), new MissingAnalyzerLoader());

            project = project.AddMetadataReference(metadata);
            project = project.AddAnalyzerReference(analyzer);

            var validator = new SerializationValidator(workspace.Services);

            using var snapshot = await validator.AssetStorage.StoreAssetsAsync(project.Solution, CancellationToken.None).ConfigureAwait(false);
            // this shouldn't throw
            var recovered = await validator.GetSolutionAsync(snapshot).ConfigureAwait(false);
        }

        [Fact]
        public async Task UnknownLanguageTest()
        {
            using var workspace = CreateWorkspace(new[] { typeof(NoCompilationLanguageService) });
            var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", NoCompilationConstants.LanguageName);

            var validator = new SerializationValidator(workspace.Services);

            using var snapshot = await validator.AssetStorage.StoreAssetsAsync(project.Solution, CancellationToken.None).ConfigureAwait(false);
            // this shouldn't throw
            var recovered = await validator.GetSolutionAsync(snapshot).ConfigureAwait(false);
        }

        [Fact]
        public async Task EmptyAssetChecksumTest()
        {
            var document = CreateWorkspace().CurrentSolution.AddProject("empty", "empty", LanguageNames.CSharp).AddDocument("empty", SourceText.From(""));
            var serializer = document.Project.Solution.Services.GetService<ISerializerService>();

            var source = serializer.CreateChecksum(await document.GetTextAsync().ConfigureAwait(false), CancellationToken.None);
            var metadata = serializer.CreateChecksum(new MissingMetadataReference(), CancellationToken.None);
            var analyzer = serializer.CreateChecksum(new AnalyzerFileReference(Path.Combine(TempRoot.Root, "missing"), new MissingAnalyzerLoader()), CancellationToken.None);

            Assert.NotEqual(source, metadata);
            Assert.NotEqual(source, analyzer);
            Assert.NotEqual(metadata, analyzer);
        }

        [Fact]
        public async Task VBParseOptionsInCompilationOptions()
        {
            var project = CreateWorkspace().CurrentSolution.AddProject("empty", "empty", LanguageNames.VisualBasic);
            project = project.WithCompilationOptions(
                ((VisualBasic.VisualBasicCompilationOptions)project.CompilationOptions).WithParseOptions((VisualBasic.VisualBasicParseOptions)project.ParseOptions));

            var checksum = await project.State.GetChecksumAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.NotNull(checksum);
        }

        [Fact]
        public async Task TestMetadataXmlDocComment()
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

            using var workspace = CreateWorkspace();
            var solution = workspace.CurrentSolution
                .AddProject("Project", "Project.dll", LanguageNames.CSharp)
                .AddMetadataReference(MetadataReference.CreateFromFile(tempCorlib.Path))
                .Solution;

            var validator = new SerializationValidator(workspace.Services);

            using var scope = await validator.AssetStorage.StoreAssetsAsync(solution, CancellationToken.None);
            // recover solution from given snapshot
            var recovered = await validator.GetSolutionAsync(scope);

            var compilation = await recovered.Projects.First().GetCompilationAsync(CancellationToken.None);
            var objectType = compilation.GetTypeByMetadataName("System.Object");
            var xmlDocComment = objectType.GetDocumentationCommentXml();

            Assert.False(string.IsNullOrEmpty(xmlDocComment));
        }

        [Fact]
        public void TestEncodingSerialization()
        {
            using var workspace = CreateWorkspace();

            var serializer = workspace.Services.GetService<ISerializerService>();

            // test with right serializable encoding
            var sourceText = SourceText.From("Hello", Encoding.UTF8);
            using (var stream = SerializableBytes.CreateWritableStream())
            {
                using var context = new SolutionReplicationContext();

                using (var objectWriter = new ObjectWriter(stream, leaveOpen: true))
                {
                    serializer.Serialize(sourceText, objectWriter, context, CancellationToken.None);
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
                using var context = new SolutionReplicationContext();

                using (var objectWriter = new ObjectWriter(stream, leaveOpen: true))
                {
                    serializer.Serialize(sourceText, objectWriter, context, CancellationToken.None);
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

            using var workspace = CreateWorkspace();
            var serializer = workspace.Services.GetService<ISerializerService>();

            VerifyOptions(csharpOptions);
            VerifyOptions(vbOptions);

            void VerifyOptions(CompilationOptions originalOptions)
            {
                using var stream = SerializableBytes.CreateWritableStream();
                using var context = new SolutionReplicationContext();

                using (var objectWriter = new ObjectWriter(stream, leaveOpen: true))
                {
                    serializer.Serialize(originalOptions, objectWriter, context, CancellationToken.None);
                }

                stream.Position = 0;
                using var objectReader = ObjectReader.TryGetReader(stream);
                var recoveredOptions = serializer.Deserialize<CompilationOptions>(originalOptions.GetWellKnownSynchronizationKind(), objectReader, CancellationToken.None);

                var original = serializer.CreateChecksum(originalOptions, CancellationToken.None);
                var recovered = serializer.CreateChecksum(recoveredOptions, CancellationToken.None);

                Assert.Equal(original, recovered);
            }
        }

        private static SolutionAsset CloneAsset(ISerializerService serializer, SolutionAsset asset)
        {
            using var stream = SerializableBytes.CreateWritableStream();
            using var context = new SolutionReplicationContext();

            using (var writer = new ObjectWriter(stream, leaveOpen: true))
            {
                serializer.Serialize(asset.Value, writer, context, CancellationToken.None);
            }

            stream.Position = 0;
            using var reader = ObjectReader.TryGetReader(stream);
            var recovered = serializer.Deserialize<object>(asset.Kind, reader, CancellationToken.None);
            var assetFromStorage = new SolutionAsset(serializer.CreateChecksum(recovered, CancellationToken.None), recovered);

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
            protected override string PreparePathToLoad(string fullPath)
                => throw new FileNotFoundException(fullPath);
        }

        private class MissingMetadataReference : PortableExecutableReference
        {
            public MissingMetadataReference()
                : base(MetadataReferenceProperties.Assembly, "missing_reference", XmlDocumentationProvider.Default)
            {
            }

            protected override DocumentationProvider CreateDocumentationProvider()
                => null;

            protected override Metadata GetMetadataImpl()
                => throw new FileNotFoundException("can't find");

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
                => this;
        }

        private class MockShadowCopyAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
        {
            private readonly ImmutableDictionary<string, string> _map;

            public MockShadowCopyAnalyzerAssemblyLoader(ImmutableDictionary<string, string> map)
                => _map = map;

            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
                => Assembly.LoadFrom(_map[fullPath]);
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
