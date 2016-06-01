// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SnapshotSerializationTests : SnapshotSerializationTestBase
    {
        [Fact]
        public async Task CreateSolutionSnapshotId_Empty()
        {
            var solution = new AdhocWorkspace().CurrentSolution;

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
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

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
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

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
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

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
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

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
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

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(document.Project.Solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySnapshotSerializationAsync(solution, snapshot.Id).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
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

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
            using (var snapshot = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                await VerifySnapshotSerializationAsync(solution, snapshot.Id).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task CreateSolutionSnapshotId_Full_Asset_Serialization()
        {
            var solution = CreateFullSolution();

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
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

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
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

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;
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

            var snapshotService = (new SolutionSnapshotService()).CreateService(solution.Workspace.Services) as ISolutionSnapshotService;

            // snapshot1 builds graph
            var snapshot1 = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false);

            // snapshot2 reuse data cached from snapshot1
            using (var snapshot2 = await snapshotService.CreateSnapshotAsync(solution, CancellationToken.None).ConfigureAwait(false))
            {
                // make sure cache worked
                Assert.True(object.ReferenceEquals(snapshot1.Id, snapshot2.Id));

                // let original data holder go
                snapshot1.Dispose();

                // now test whether we are rebuilding assets correctly.
                var solutionId = snapshot2.Id;

                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId).ConfigureAwait(false);
                await VerifyChecksumInService(snapshotService, solutionId.Info, WellKnownChecksumObjects.SolutionSnapshotInfo).ConfigureAwait(false);
                await VerifyChecksumObjectInServiceAsync(snapshotService, solutionId.Projects).ConfigureAwait(false);

                Assert.Equal(solutionId.Projects.Objects.Length, 2);

                await VerifySnapshotInServiceAsync(snapshotService, solutionId.Projects.Objects[0], 1, 1, 1, 1, 1);
                await VerifySnapshotInServiceAsync(snapshotService, solutionId.Projects.Objects[1], 1, 0, 0, 0, 0);
            }
        }
    }
}