// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Test;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SolutionCrawler
{
    [UseExportProvider]
    public class WorkCoordinatorTests : TestBase
    {
        private const string SolutionCrawlerWorkspaceKind = "SolutionCrawler";

        [Fact]
        public async Task RegisterService()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind);

            Assert.Empty(workspace.ExportProvider.GetExports<IIncrementalAnalyzerProvider>());
            var registrationService = Assert.IsType<SolutionCrawlerRegistrationService>(workspace.Services.GetService<ISolutionCrawlerRegistrationService>());

            // register and unregister workspace to the service
            registrationService.Register(workspace);
            registrationService.Unregister(workspace);

            // make sure we wait for all waiter. the test wrongly assumed there won't be
            // any pending async event which is implementation detail when creating workspace
            // and changing options.
            await WaitWaiterAsync(workspace.ExportProvider);
        }

        [Fact]
        public async Task DynamicallyAddAnalyzer()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind);
            // create solution and wait for it to settle
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            // create solution crawler and add new analyzer provider dynamically
            Assert.Empty(workspace.ExportProvider.GetExports<IIncrementalAnalyzerProvider>());
            var service = Assert.IsType<SolutionCrawlerRegistrationService>(workspace.Services.GetService<ISolutionCrawlerRegistrationService>());

            service.Register(workspace);

            var worker = new Analyzer();
            var provider = new AnalyzerProvider(worker);
            service.AddAnalyzerProvider(provider, Metadata.Crawler);

            // wait for everything to settle
            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            // check whether everything ran as expected
            Assert.Equal(10, worker.SyntaxDocumentIds.Count);
            Assert.Equal(10, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, 0)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, 1)]
        [InlineData(BackgroundAnalysisScope.FullSolution, 1)]
        [Theory, WorkItem(747226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747226")]
        internal async Task SolutionAdded_Simple(BackgroundAnalysisScope analysisScope, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionId = SolutionId.CreateNewId();
            var projectId = ProjectId.CreateNewId();

            var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(),
                    projects: new[]
                    {
                            ProjectInfo.Create(projectId, VersionStamp.Create(), "P1", "P1", LanguageNames.CSharp,
                                documents: new[]
                                {
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId), "D1")
                                })
                    });

            var worker = await ExecuteOperation(workspace, w => w.OnSolutionAdded(solutionInfo));

            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, 0)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, 10)]
        [InlineData(BackgroundAnalysisScope.FullSolution, 10)]
        [Theory]
        internal async Task SolutionAdded_Complex(BackgroundAnalysisScope analysisScope, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();

            var worker = await ExecuteOperation(workspace, w => w.OnSolutionAdded(solution));
            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects)]
        [InlineData(BackgroundAnalysisScope.FullSolution)]
        [Theory]
        internal async Task Solution_Remove(BackgroundAnalysisScope analysisScope)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.OnSolutionRemoved());
            Assert.Equal(10, worker.InvalidateDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects)]
        [InlineData(BackgroundAnalysisScope.FullSolution)]
        [Theory]
        internal async Task Solution_Clear(BackgroundAnalysisScope analysisScope)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.ClearSolution());
            Assert.Equal(10, worker.InvalidateDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, 0, 0)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, 10, 2)]
        [InlineData(BackgroundAnalysisScope.FullSolution, 10, 2)]
        [Theory]
        internal async Task Solution_Reload(BackgroundAnalysisScope analysisScope, int expectedDocumentEvents, int expectedProjectEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.OnSolutionReloaded(solution));
            Assert.Equal(expectedDocumentEvents, worker.DocumentIds.Count);
            Assert.Equal(expectedProjectEvents, worker.ProjectIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, 0)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, 1)]
        [InlineData(BackgroundAnalysisScope.FullSolution, 1)]
        [Theory]
        internal async Task Solution_Change(BackgroundAnalysisScope analysisScope, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.First().DocumentIds[0];
            solution = solution.RemoveDocument(documentId);

            var changedSolution = solution.AddProject("P3", "P3", LanguageNames.CSharp).AddDocument("D1", "").Project.Solution;

            var worker = await ExecuteOperation(workspace, w => w.ChangeSolution(changedSolution));
            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, 0)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, 2)]
        [InlineData(BackgroundAnalysisScope.FullSolution, 2)]
        [Theory]
        internal async Task Project_Add(BackgroundAnalysisScope analysisScope, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var projectId = ProjectId.CreateNewId();
            var projectInfo = ProjectInfo.Create(
                projectId, VersionStamp.Create(), "P3", "P3", LanguageNames.CSharp,
                documents: new List<DocumentInfo>
                    {
                            DocumentInfo.Create(DocumentId.CreateNewId(projectId), "D1"),
                            DocumentInfo.Create(DocumentId.CreateNewId(projectId), "D2")
                    });

            var worker = await ExecuteOperation(workspace, w => w.OnProjectAdded(projectInfo));
            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects)]
        [InlineData(BackgroundAnalysisScope.FullSolution)]
        [Theory]
        internal async Task Project_Remove(BackgroundAnalysisScope analysisScope)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var projectid = workspace.CurrentSolution.ProjectIds[0];

            var worker = await ExecuteOperation(workspace, w => w.OnProjectRemoved(projectid));
            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.InvalidateDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects)]
        [InlineData(BackgroundAnalysisScope.FullSolution)]
        [Theory]
        internal async Task Project_Change(BackgroundAnalysisScope analysisScope)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = workspace.CurrentSolution.Projects.First();
            var documentId = project.DocumentIds[0];
            var solution = workspace.CurrentSolution.RemoveDocument(documentId);

            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, solution));
            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            Assert.Equal(1, worker.InvalidateDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 5)]
        [Theory]
        internal async Task Project_AssemblyName_Change(BackgroundAnalysisScope analysisScope, bool firstDocumentActive, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1");
            if (firstDocumentActive)
            {
                MakeFirstDocumentActive(project);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            project = project.WithAssemblyName("newName");
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, project.Solution));

            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentEvents, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 5)]
        [Theory]
        internal async Task Project_DefaultNamespace_Change(BackgroundAnalysisScope analysisScope, bool firstDocumentActive, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1");
            if (firstDocumentActive)
            {
                MakeFirstDocumentActive(project);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            project = project.WithDefaultNamespace("newNamespace");
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, project.Solution));

            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentEvents, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 5)]
        [Theory]
        internal async Task Project_AnalyzerOptions_Change(BackgroundAnalysisScope analysisScope, bool firstDocumentActive, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1");
            if (firstDocumentActive)
            {
                MakeFirstDocumentActive(project);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            project = project.AddAdditionalDocument("a1", SourceText.From("")).Project;
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, project.Solution));

            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentEvents, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 5)]
        [Theory]
        internal async Task Project_OutputFilePath_Change(BackgroundAnalysisScope analysisScope, bool firstDocumentActive, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1");
            if (firstDocumentActive)
            {
                MakeFirstDocumentActive(project);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var newSolution = workspace.CurrentSolution.WithProjectOutputFilePath(project.Id, "/newPath");
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, newSolution));

            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentEvents, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 5)]
        [Theory]
        internal async Task Project_OutputRefFilePath_Change(BackgroundAnalysisScope analysisScope, bool firstDocumentActive, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1");
            if (firstDocumentActive)
            {
                MakeFirstDocumentActive(project);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var newSolution = workspace.CurrentSolution.WithProjectOutputRefFilePath(project.Id, "/newPath");
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, newSolution));

            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentEvents, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 5)]
        [Theory]
        internal async Task Project_CompilationOutputInfo_Change(BackgroundAnalysisScope analysisScope, bool firstDocumentActive, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1");
            if (firstDocumentActive)
            {
                MakeFirstDocumentActive(project);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var newSolution = workspace.CurrentSolution.WithProjectCompilationOutputInfo(project.Id, new CompilationOutputInfo(assemblyPath: "/newPath"));
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, newSolution));

            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentEvents, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 5)]
        [Theory]
        internal async Task Project_RunAnalyzers_Change(BackgroundAnalysisScope analysisScope, bool firstDocumentActive, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1");
            if (firstDocumentActive)
            {
                MakeFirstDocumentActive(project);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            Assert.True(project.State.RunAnalyzers);

            var newSolution = workspace.CurrentSolution.WithRunAnalyzers(project.Id, false);
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, newSolution));

            project = workspace.CurrentSolution.GetProject(project.Id);
            Assert.False(project.State.RunAnalyzers);

            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentEvents, worker.DocumentIds.Count);
        }

        [Fact]
        public async Task Test_NeedsReanalysisOnOptionChanged()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.TryApplyChanges(w.CurrentSolution.WithOptions(w.CurrentSolution.Options.WithChangedOption(Analyzer.TestOption, false))));

            Assert.Equal(10, worker.SyntaxDocumentIds.Count);
            Assert.Equal(10, worker.DocumentIds.Count);
            Assert.Equal(2, worker.ProjectIds.Count);
        }

        [Fact]
        public async Task Test_BackgroundAnalysisScopeOptionChanged_ActiveFile()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            MakeFirstDocumentActive(workspace.CurrentSolution.Projects.First());
            await WaitWaiterAsync(workspace.ExportProvider);

            Assert.Equal(BackgroundAnalysisScope.Default, SolutionCrawlerOptions.GetBackgroundAnalysisScope(workspace.Options, LanguageNames.CSharp));

            var newAnalysisScope = BackgroundAnalysisScope.ActiveFile;
            var worker = await ExecuteOperation(workspace, w => w.TryApplyChanges(w.CurrentSolution.WithOptions(w.CurrentSolution.Options.WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, newAnalysisScope))));

            Assert.Equal(newAnalysisScope, SolutionCrawlerOptions.GetBackgroundAnalysisScope(workspace.Options, LanguageNames.CSharp));
            Assert.Equal(1, worker.SyntaxDocumentIds.Count);
            Assert.Equal(1, worker.DocumentIds.Count);
            Assert.Equal(1, worker.ProjectIds.Count);
        }

        [Fact]
        public async Task Test_BackgroundAnalysisScopeOptionChanged_FullSolution()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            Assert.Equal(BackgroundAnalysisScope.Default, SolutionCrawlerOptions.GetBackgroundAnalysisScope(workspace.Options, LanguageNames.CSharp));

            var newAnalysisScope = BackgroundAnalysisScope.FullSolution;
            var worker = await ExecuteOperation(workspace, w => w.TryApplyChanges(w.CurrentSolution.WithOptions(w.CurrentSolution.Options.WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, newAnalysisScope))));

            Assert.Equal(newAnalysisScope, SolutionCrawlerOptions.GetBackgroundAnalysisScope(workspace.Options, LanguageNames.CSharp));
            Assert.Equal(10, worker.SyntaxDocumentIds.Count);
            Assert.Equal(10, worker.DocumentIds.Count);
            Assert.Equal(2, worker.ProjectIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, 0, 0)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, 5, 1)]
        [InlineData(BackgroundAnalysisScope.FullSolution, 5, 1)]
        [Theory]
        internal async Task Project_Reload(BackgroundAnalysisScope analysisScope, int expectedDocumentEvents, int expectedProjectEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = solution.Projects[0];
            var worker = await ExecuteOperation(workspace, w => w.OnProjectReloaded(project));
            Assert.Equal(expectedDocumentEvents, worker.DocumentIds.Count);
            Assert.Equal(expectedProjectEvents, worker.ProjectIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 1, 6)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 1, 6)]
        [Theory]
        internal async Task Document_Add(BackgroundAnalysisScope analysisScope, bool activeDocument, int expectedDocumentSyntaxEvents, int expectedDocumentSemanticEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1");
            var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "D6");

            var worker = await ExecuteOperation(workspace, w =>
                {
                    w.OnDocumentAdded(info);

                    if (activeDocument)
                    {
                        var document = w.CurrentSolution.GetDocument(info.Id);
                        MakeDocumentActive(document);
                    }
                });

            Assert.Equal(expectedDocumentSyntaxEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 1, 0, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1, 0, 0)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 1, 0, 4)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 1, 0, 4)]
        [Theory]
        internal async Task Document_Remove(BackgroundAnalysisScope analysisScope, bool removeActiveDocument, int expectedDocumentInvalidatedEvents, int expectedDocumentSyntaxEvents, int expectedDocumentSemanticEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();
            if (removeActiveDocument)
            {
                MakeDocumentActive(document);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.OnDocumentRemoved(document.Id));

            Assert.Equal(expectedDocumentSyntaxEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, worker.DocumentIds.Count);
            Assert.Equal(expectedDocumentInvalidatedEvents, worker.InvalidateDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false)]
        [Theory]
        internal async Task Document_Reload(BackgroundAnalysisScope analysisScope, bool reloadActiveDocument)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            var info = solution.Projects[0].Documents[0];
            if (reloadActiveDocument)
            {
                var document = workspace.CurrentSolution.GetDocument(info.Id);
                MakeDocumentActive(document);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.OnDocumentReloaded(info));
            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            Assert.Equal(0, worker.DocumentIds.Count);
            Assert.Equal(0, worker.InvalidateDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 1)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 1)]
        [Theory]
        internal async Task Document_Reanalyze(BackgroundAnalysisScope analysisScope, bool reanalyzeActiveDocument, int expectedReanalyzeCount)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            var info = solution.Projects[0].Documents[0];
            if (reanalyzeActiveDocument)
            {
                var document = workspace.CurrentSolution.GetDocument(info.Id);
                MakeDocumentActive(document);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var lazyWorker = Assert.Single(workspace.ExportProvider.GetExports<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>());
            Assert.Equal(Metadata.Crawler, lazyWorker.Metadata);
            var worker = Assert.IsType<Analyzer>(Assert.IsAssignableFrom<AnalyzerProvider>(lazyWorker.Value).Analyzer);
            Assert.False(worker.WaitForCancellation);
            Assert.False(worker.BlockedRun);
            var service = Assert.IsType<SolutionCrawlerRegistrationService>(workspace.Services.GetService<ISolutionCrawlerRegistrationService>());

            service.Register(workspace);

            // don't rely on background parser to have tree. explicitly do it here.
            await TouchEverything(workspace.CurrentSolution);

            service.Reanalyze(workspace, worker, projectIds: null, documentIds: SpecializedCollections.SingletonEnumerable(info.Id), highPriority: false);

            await TouchEverything(workspace.CurrentSolution);

            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            Assert.Equal(expectedReanalyzeCount, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedReanalyzeCount, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 1)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 1)]
        [Theory, WorkItem(670335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670335")]
        internal async Task Document_Change(BackgroundAnalysisScope analysisScope, bool changeActiveDocument, int expectedDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();
            if (changeActiveDocument)
            {
                MakeDocumentActive(document);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.ChangeDocument(document.Id, SourceText.From("//")));

            Assert.Equal(expectedDocumentEvents, worker.SyntaxDocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 5, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 5, 5)]
        [Theory]
        internal async Task Document_AdditionalFileChange(BackgroundAnalysisScope analysisScope, bool firstDocumentActive, int expectedDocumentSyntaxEvents, int expectedDocumentSemanticEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            var project = workspace.CurrentSolution.Projects.First();
            if (firstDocumentActive)
            {
                MakeFirstDocumentActive(project);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var ncfile = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "D6");

            var worker = await ExecuteOperation(workspace, w => w.OnAdditionalDocumentAdded(ncfile));
            Assert.Equal(expectedDocumentSyntaxEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, worker.DocumentIds.Count);

            worker = await ExecuteOperation(workspace, w => w.ChangeAdditionalDocument(ncfile.Id, SourceText.From("//")));

            Assert.Equal(expectedDocumentSyntaxEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, worker.DocumentIds.Count);

            worker = await ExecuteOperation(workspace, w => w.OnAdditionalDocumentRemoved(ncfile.Id));

            Assert.Equal(expectedDocumentSyntaxEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 5, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 5, 5)]
        [Theory]
        internal async Task Document_AnalyzerConfigFileChange(BackgroundAnalysisScope analysisScope, bool firstDocumentActive, int expectedDocumentSyntaxEvents, int expectedDocumentSemanticEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            var project = workspace.CurrentSolution.Projects.First();
            if (firstDocumentActive)
            {
                MakeFirstDocumentActive(project);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var analyzerConfigDocFilePath = PathUtilities.CombineAbsoluteAndRelativePaths(Temp.CreateDirectory().Path, ".editorconfig");
            var analyzerConfigFile = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), ".editorconfig", filePath: analyzerConfigDocFilePath);

            var worker = await ExecuteOperation(workspace, w => w.OnAnalyzerConfigDocumentAdded(analyzerConfigFile));
            Assert.Equal(expectedDocumentSyntaxEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, worker.DocumentIds.Count);

            worker = await ExecuteOperation(workspace, w => w.ChangeAnalyzerConfigDocument(analyzerConfigFile.Id, SourceText.From("//")));

            Assert.Equal(expectedDocumentSyntaxEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, worker.DocumentIds.Count);

            worker = await ExecuteOperation(workspace, w => w.OnAnalyzerConfigDocumentRemoved(analyzerConfigFile.Id));

            Assert.Equal(expectedDocumentSyntaxEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, worker.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 1, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 1, 5)]
        [Theory, WorkItem(670335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670335")]
        internal async Task Document_Cancellation(BackgroundAnalysisScope analysisScope, bool activeDocument, int expectedDocumentSyntaxEvents, int expectedDocumentSemanticEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();
            if (activeDocument)
            {
                MakeDocumentActive(document);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var lazyWorker = Assert.Single(workspace.ExportProvider.GetExports<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>());
            Assert.Equal(Metadata.Crawler, lazyWorker.Metadata);
            var analyzer = Assert.IsType<Analyzer>(Assert.IsAssignableFrom<AnalyzerProvider>(lazyWorker.Value).Analyzer);
            Assert.True(analyzer.WaitForCancellation);
            Assert.False(analyzer.BlockedRun);
            var service = Assert.IsType<SolutionCrawlerRegistrationService>(workspace.Services.GetService<ISolutionCrawlerRegistrationService>());

            service.Register(workspace);

            workspace.ChangeDocument(document.Id, SourceText.From("//"));
            if (expectedDocumentSyntaxEvents > 0 || expectedDocumentSemanticEvents > 0)
            {
                analyzer.RunningEvent.Wait();
            }

            workspace.ChangeDocument(document.Id, SourceText.From("// "));
            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            Assert.Equal(expectedDocumentSyntaxEvents, analyzer.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, analyzer.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 0, 0)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 1, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 1, 5)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 1, 5)]
        [Theory, WorkItem(670335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670335")]
        internal async Task Document_Cancellation_MultipleTimes(BackgroundAnalysisScope analysisScope, bool activeDocument, int expectedDocumentSyntaxEvents, int expectedDocumentSemanticEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();
            if (activeDocument)
            {
                MakeDocumentActive(document);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            var lazyWorker = Assert.Single(workspace.ExportProvider.GetExports<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>());
            Assert.Equal(Metadata.Crawler, lazyWorker.Metadata);
            var analyzer = Assert.IsType<Analyzer>(Assert.IsAssignableFrom<AnalyzerProvider>(lazyWorker.Value).Analyzer);
            Assert.True(analyzer.WaitForCancellation);
            Assert.False(analyzer.BlockedRun);
            var service = Assert.IsType<SolutionCrawlerRegistrationService>(workspace.Services.GetService<ISolutionCrawlerRegistrationService>());

            service.Register(workspace);

            workspace.ChangeDocument(document.Id, SourceText.From("//"));
            if (expectedDocumentSyntaxEvents > 0 || expectedDocumentSemanticEvents > 0)
            {
                analyzer.RunningEvent.Wait();
                analyzer.RunningEvent.Reset();
            }

            workspace.ChangeDocument(document.Id, SourceText.From("// "));
            if (expectedDocumentSyntaxEvents > 0 || expectedDocumentSemanticEvents > 0)
            {
                analyzer.RunningEvent.Wait();
            }

            workspace.ChangeDocument(document.Id, SourceText.From("//  "));
            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            Assert.Equal(expectedDocumentSyntaxEvents, analyzer.SyntaxDocumentIds.Count);
            Assert.Equal(expectedDocumentSemanticEvents, analyzer.DocumentIds.Count);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21082"), WorkItem(670335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670335")]
        public async Task Document_InvocationReasons()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

            var lazyWorker = Assert.Single(workspace.ExportProvider.GetExports<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>());
            Assert.Equal(Metadata.Crawler, lazyWorker.Metadata);
            var analyzer = Assert.IsType<Analyzer>(Assert.IsAssignableFrom<AnalyzerProvider>(lazyWorker.Value).Analyzer);
            Assert.False(analyzer.WaitForCancellation);
            Assert.True(analyzer.BlockedRun);
            var service = Assert.IsType<SolutionCrawlerRegistrationService>(workspace.Services.GetService<ISolutionCrawlerRegistrationService>());

            service.Register(workspace);

            // first invocation will block worker
            workspace.ChangeDocument(id, SourceText.From("//"));
            analyzer.RunningEvent.Wait();

            var openReady = new ManualResetEventSlim(initialState: false);
            var closeReady = new ManualResetEventSlim(initialState: false);

            workspace.DocumentOpened += (o, e) => openReady.Set();
            workspace.DocumentClosed += (o, e) => closeReady.Set();

            // cause several different request to queue up
            workspace.ChangeDocument(id, SourceText.From("// "));
            workspace.OpenDocument(id);
            workspace.CloseDocument(id);

            openReady.Set();
            closeReady.Set();
            analyzer.BlockEvent.Set();

            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            Assert.Equal(1, analyzer.SyntaxDocumentIds.Count);
            Assert.Equal(5, analyzer.DocumentIds.Count);
        }

        [InlineData(BackgroundAnalysisScope.ActiveFile, false, 1, 1)]
        [InlineData(BackgroundAnalysisScope.ActiveFile, true, 2, 1)]
        [InlineData(BackgroundAnalysisScope.OpenFilesAndProjects, false, 0, 0)]
        [InlineData(BackgroundAnalysisScope.FullSolution, false, 0, 0)]
        [Theory, WorkItem(670335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670335")]
        internal async Task Document_ActiveDocumentChanged(BackgroundAnalysisScope analysisScope, bool hasActiveDocumentBefore, int expectedSourceSwitchDocumentEvents, int expectedNonSourceSwitchDocumentEvents)
        {
            using var workspace = WorkCoordinatorWorkspace.CreateWithAnalysisScope(analysisScope, SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            var solution = GetInitialSolutionInfo_2Projects_10Documents();
            workspace.OnSolutionAdded(solution);

            var documents = workspace.CurrentSolution.Projects.First().Documents.ToArray();
            var firstDocument = documents[0];
            var secondDocument = documents[1];
            if (hasActiveDocumentBefore)
            {
                MakeDocumentActive(firstDocument);
            }

            await WaitWaiterAsync(workspace.ExportProvider);

            // Switch to another active source document and verify expected document analysis callbacks
            var worker = await ExecuteOperation(workspace, w => MakeDocumentActive(secondDocument));
            Assert.Equal(expectedSourceSwitchDocumentEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedSourceSwitchDocumentEvents, worker.DocumentIds.Count);
            Assert.Equal(0, worker.InvalidateDocumentIds.Count);

            // Switch from an active source document to an active non-source document and verify no document analysis callbacks
            worker = await ExecuteOperation(workspace, w => ClearActiveDocument(w));
            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            Assert.Equal(0, worker.DocumentIds.Count);
            Assert.Equal(0, worker.InvalidateDocumentIds.Count);

            // Switch from an active non-source document to an active source document and verify document analysis callbacks
            worker = await ExecuteOperation(workspace, w => MakeDocumentActive(firstDocument));
            Assert.Equal(expectedNonSourceSwitchDocumentEvents, worker.SyntaxDocumentIds.Count);
            Assert.Equal(expectedNonSourceSwitchDocumentEvents, worker.DocumentIds.Count);
            Assert.Equal(0, worker.InvalidateDocumentIds.Count);
        }

        [Fact]
        public async Task Document_TopLevelType_Whitespace()
        {
            var code = @"class C { $$ }";
            var textToInsert = " ";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_TopLevelType_Character()
        {
            var code = @"class C { $$ }";
            var textToInsert = "int";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_TopLevelType_NewLine()
        {
            var code = @"class C { $$ }";
            var textToInsert = "\r\n";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_TopLevelType_NewLine2()
        {
            var code = @"class C { $$";
            var textToInsert = "\r\n";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_EmptyFile()
        {
            var code = @"$$";
            var textToInsert = "class";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_TopLevel1()
        {
            var code = @"class C
{
    public void Test($$";
            var textToInsert = "int";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_TopLevel2()
        {
            var code = @"class C
{
    public void Test(int $$";
            var textToInsert = " ";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_TopLevel3()
        {
            var code = @"class C
{
    public void Test(int i,$$";
            var textToInsert = "\r\n";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_InteriorNode1()
        {
            var code = @"class C
{
    public void Test()
    {$$";
            var textToInsert = "\r\n";

            await InsertText(code, textToInsert, expectDocumentAnalysis: false);
        }

        [Fact]
        public async Task Document_InteriorNode2()
        {
            var code = @"class C
{
    public void Test()
    {
        $$
    }";
            var textToInsert = "int";

            await InsertText(code, textToInsert, expectDocumentAnalysis: false);
        }

        [Fact]
        public async Task Document_InteriorNode_Field()
        {
            var code = @"class C
{
    int i = $$
}";
            var textToInsert = "1";

            await InsertText(code, textToInsert, expectDocumentAnalysis: false);
        }

        [Fact]
        public async Task Document_InteriorNode_Field1()
        {
            var code = @"class C
{
    int i = 1 + $$
}";
            var textToInsert = "1";

            await InsertText(code, textToInsert, expectDocumentAnalysis: false);
        }

        [Fact]
        public async Task Document_InteriorNode_Accessor()
        {
            var code = @"class C
{
    public int A
    {
        get 
        {
            $$
        }
    }
}";
            var textToInsert = "return";

            await InsertText(code, textToInsert, expectDocumentAnalysis: false);
        }

        [Fact]
        public async Task Document_TopLevelWhitespace()
        {
            var code = @"class C
{
    /// $$
    public int A()
    {
    }
}";
            var textToInsert = "return";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_TopLevelWhitespace2()
        {
            var code = @"/// $$
class C
{
    public int A()
    {
    }
}";
            var textToInsert = "return";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact]
        public async Task Document_InteriorNode_Malformed()
        {
            var code = @"class C
{
    public void Test()
    {
        $$";
            var textToInsert = "int";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [Fact, WorkItem(739943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739943")]
        public async Task SemanticChange_Propagation_Transitive()
        {
            var solution = GetInitialSolutionInfoWithP2P();

            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options
                .WithChangedOption(InternalSolutionCrawlerOptions.DirectDependencyPropagationOnly, false)));

            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var id = solution.Projects[0].Id;
            var info = DocumentInfo.Create(DocumentId.CreateNewId(id), "D6");

            var worker = await ExecuteOperation(workspace, w => w.OnDocumentAdded(info));

            Assert.Equal(1, worker.SyntaxDocumentIds.Count);
            Assert.Equal(4, worker.DocumentIds.Count);
        }

        [Fact, WorkItem(739943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739943")]
        public async Task SemanticChange_Propagation_Direct()
        {
            var solution = GetInitialSolutionInfoWithP2P();

            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProviderNoWaitNoBlock));
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options
                .WithChangedOption(InternalSolutionCrawlerOptions.DirectDependencyPropagationOnly, true)));

            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var id = solution.Projects[0].Id;
            var info = DocumentInfo.Create(DocumentId.CreateNewId(id), "D6");

            var worker = await ExecuteOperation(workspace, w => w.OnDocumentAdded(info));

            Assert.Equal(1, worker.SyntaxDocumentIds.Count);
            Assert.Equal(3, worker.DocumentIds.Count);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/23657")]
        public async Task ProgressReporterTest()
        {
            var solution = GetInitialSolutionInfoWithP2P();

            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind);
            await WaitWaiterAsync(workspace.ExportProvider);

            var service = workspace.Services.GetService<ISolutionCrawlerService>();
            var reporter = service.GetProgressReporter(workspace);
            Assert.False(reporter.InProgress);

            // set up events
            var started = false;
            var stopped = false;

            reporter.ProgressChanged += (o, s) =>
            {
                if (s.Status == ProgressStatus.Started)
                {
                    started = true;
                }
                else if (s.Status == ProgressStatus.Stopped)
                {
                    stopped = true;
                }
            };

            var registrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            registrationService.Register(workspace);

            // first mutation
            workspace.OnSolutionAdded(solution);

            await WaitAsync((SolutionCrawlerRegistrationService)registrationService, workspace);

            Assert.True(started);
            Assert.True(stopped);

            // reset
            started = false;
            stopped = false;

            // second mutation
            workspace.OnDocumentAdded(DocumentInfo.Create(DocumentId.CreateNewId(solution.Projects[0].Id), "D6"));

            await WaitAsync((SolutionCrawlerRegistrationService)registrationService, workspace);

            Assert.True(started);
            Assert.True(stopped);

            registrationService.Unregister(workspace);
        }

        [Fact]
        [WorkItem(26244, "https://github.com/dotnet/roslyn/issues/26244")]
        public async Task FileFromSameProjectTogetherTest()
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();
            var projectId3 = ProjectId.CreateNewId();

            var solution = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(),
                projects: new[]
                {
                    ProjectInfo.Create(projectId1, VersionStamp.Create(), "P1", "P1", LanguageNames.CSharp,
                        documents: GetDocuments(projectId1, count: 5)),
                    ProjectInfo.Create(projectId2, VersionStamp.Create(), "P2", "P2", LanguageNames.CSharp,
                        documents: GetDocuments(projectId2, count: 5)),
                    ProjectInfo.Create(projectId3, VersionStamp.Create(), "P3", "P3", LanguageNames.CSharp,
                        documents: GetDocuments(projectId3, count: 5))
                });

            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawlerWorkspaceKind, incrementalAnalyzer: typeof(AnalyzerProvider2));
            await WaitWaiterAsync(workspace.ExportProvider);

            // add analyzer
            var lazyWorker = Assert.Single(workspace.ExportProvider.GetExports<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>());
            Assert.Equal(Metadata.Crawler, lazyWorker.Metadata);
            var worker = Assert.IsType<Analyzer2>(Assert.IsAssignableFrom<AnalyzerProvider>(lazyWorker.Value).Analyzer);

            // enable solution crawler
            var service = Assert.IsType<SolutionCrawlerRegistrationService>(workspace.Services.GetService<ISolutionCrawlerRegistrationService>());
            service.Register(workspace);

            await WaitWaiterAsync(workspace.ExportProvider);

            // we want to test order items processed by solution crawler.
            // but since everything async, lazy and cancellable, order is not 100% deterministic. an item might 
            // start to be processed, and get cancelled due to newly enqueued item requiring current work to be re-processed 
            // (ex, new file being added).
            // this behavior is expected in real world, but it makes testing hard. so to make ordering deterministic
            // here we first block solution crawler from processing any item using global operation.
            // and then make sure all delayed work item enqueue to be done through waiters. work item enqueue is async
            // and delayed since one of responsibility of solution cralwer is aggregating workspace events to fewer
            // work items.
            // once we are sure everything is stablized, we let solution crawler to process by releasing global operation.
            // what this test is interested in is the order solution crawler process the pending works. so this should
            // let the test not care about cancellation or work not enqueued yet.

            // block solution cralwer from processing.
            var globalOperation = workspace.Services.GetService<IGlobalOperationNotificationService>();
            using (var operation = globalOperation.Start("Block SolutionCrawler"))
            {
                // make sure global operaiton is actually started
                // otherwise, solution crawler might processed event we are later waiting for
                var operationWaiter = GetListenerProvider(workspace.ExportProvider).GetWaiter(FeatureAttribute.GlobalOperation);
                await operationWaiter.ExpeditedWaitAsync();

                // mutate solution
                workspace.OnSolutionAdded(solution);

                // wait for workspace events to be all processed
                var workspaceWaiter = GetListenerProvider(workspace.ExportProvider).GetWaiter(FeatureAttribute.Workspace);
                await workspaceWaiter.ExpeditedWaitAsync();

                // now wait for semantic processor to finish
                var crawlerListener = (AsynchronousOperationListener)GetListenerProvider(workspace.ExportProvider).GetListener(FeatureAttribute.SolutionCrawler);

                // first, wait for first work to be queued.
                //
                // since asyncToken doesn't distinguish whether (1) certain event is happened but all processed or (2) it never happened yet,
                // to check (1), we must wait for first item, and then wait for all items to be processed.
                await crawlerListener.WaitUntilConditionIsMetAsync(
                    pendingTokens => pendingTokens.Any(token => token.Tag == (object)SolutionCrawlerRegistrationService.EnqueueItem));

                // and then wait them to be processed
                await crawlerListener.WaitUntilConditionIsMetAsync(pendingTokens => pendingTokens.Where(token => token.Tag == workspace).IsEmpty());

                // let analyzer to process
                operation.Done();
            }

            // wait analyzers to finish process
            await WaitAsync(service, workspace);

            Assert.Equal(1, worker.DocumentIds.Take(5).Select(d => d.ProjectId).Distinct().Count());
            Assert.Equal(1, worker.DocumentIds.Skip(5).Take(5).Select(d => d.ProjectId).Distinct().Count());
            Assert.Equal(1, worker.DocumentIds.Skip(10).Take(5).Select(d => d.ProjectId).Distinct().Count());

            service.Unregister(workspace);
        }

        private static async Task InsertText(string code, string text, bool expectDocumentAnalysis, string language = LanguageNames.CSharp)
        {
            using var workspace = TestWorkspace.Create(
                language,
                compilationOptions: null,
                parseOptions: null,
                new[] { code },
                composition: EditorTestCompositions.EditorFeatures.AddExcludedPartTypes(typeof(IIncrementalAnalyzerProvider)).AddParts(typeof(AnalyzerProviderNoWaitNoBlock)),
                workspaceKind: SolutionCrawlerWorkspaceKind);

            SetOptions(workspace);
            var testDocument = workspace.Documents.First();
            var textBuffer = testDocument.GetTextBuffer();

            var lazyWorker = Assert.Single(workspace.ExportProvider.GetExports<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>());
            Assert.Equal(Metadata.Crawler, lazyWorker.Metadata);
            var analyzer = Assert.IsType<Analyzer>(Assert.IsAssignableFrom<AnalyzerProvider>(lazyWorker.Value).Analyzer);
            Assert.False(analyzer.WaitForCancellation);
            Assert.False(analyzer.BlockedRun);
            var service = Assert.IsType<SolutionCrawlerRegistrationService>(workspace.Services.GetService<ISolutionCrawlerRegistrationService>());

            service.Register(workspace);

            var insertPosition = testDocument.CursorPosition;

            using (var edit = textBuffer.CreateEdit())
            {
                edit.Insert(insertPosition.Value, text);
                edit.Apply();
            }

            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            Assert.Equal(1, analyzer.SyntaxDocumentIds.Count);
            Assert.Equal(expectDocumentAnalysis ? 1 : 0, analyzer.DocumentIds.Count);
        }

        private static async Task<Analyzer> ExecuteOperation(TestWorkspace workspace, Action<TestWorkspace> operation)
        {
            var lazyWorker = Assert.Single(workspace.ExportProvider.GetExports<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>());
            Assert.Equal(Metadata.Crawler, lazyWorker.Metadata);
            var worker = Assert.IsType<Analyzer>(Assert.IsAssignableFrom<AnalyzerProvider>(lazyWorker.Value).Analyzer);
            Assert.False(worker.WaitForCancellation);
            Assert.False(worker.BlockedRun);
            var service = Assert.IsType<SolutionCrawlerRegistrationService>(workspace.Services.GetService<ISolutionCrawlerRegistrationService>());
            worker.Reset();

            service.Register(workspace);

            // don't rely on background parser to have tree. explicitly do it here.
            await TouchEverything(workspace.CurrentSolution);
            operation(workspace);
            await TouchEverything(workspace.CurrentSolution);

            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            return worker;
        }

        private static async Task TouchEverything(Solution solution)
        {
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    await document.GetTextAsync();
                    await document.GetSyntaxRootAsync();
                    await document.GetSemanticModelAsync();
                }
            }
        }

        private static async Task WaitAsync(SolutionCrawlerRegistrationService service, TestWorkspace workspace)
        {
            await WaitWaiterAsync(workspace.ExportProvider);

            service.GetTestAccessor().WaitUntilCompletion(workspace);
        }

        private static async Task WaitWaiterAsync(ExportProvider provider)
        {
            var workspaceWaiter = GetListenerProvider(provider).GetWaiter(FeatureAttribute.Workspace);
            await workspaceWaiter.ExpeditedWaitAsync();

            var solutionCrawlerWaiter = GetListenerProvider(provider).GetWaiter(FeatureAttribute.SolutionCrawler);
            await solutionCrawlerWaiter.ExpeditedWaitAsync();
        }

        private static SolutionInfo GetInitialSolutionInfoWithP2P()
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();
            var projectId3 = ProjectId.CreateNewId();
            var projectId4 = ProjectId.CreateNewId();
            var projectId5 = ProjectId.CreateNewId();

            var solution = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(),
                projects: new[]
                {
                    ProjectInfo.Create(projectId1, VersionStamp.Create(), "P1", "P1", LanguageNames.CSharp,
                        documents: new[] { DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "D1") }),
                    ProjectInfo.Create(projectId2, VersionStamp.Create(), "P2", "P2", LanguageNames.CSharp,
                        documents: new[] { DocumentInfo.Create(DocumentId.CreateNewId(projectId2), "D2") },
                        projectReferences: new[] { new ProjectReference(projectId1) }),
                    ProjectInfo.Create(projectId3, VersionStamp.Create(), "P3", "P3", LanguageNames.CSharp,
                        documents: new[] { DocumentInfo.Create(DocumentId.CreateNewId(projectId3), "D3") },
                        projectReferences: new[] { new ProjectReference(projectId2) }),
                    ProjectInfo.Create(projectId4, VersionStamp.Create(), "P4", "P4", LanguageNames.CSharp,
                        documents: new[] { DocumentInfo.Create(DocumentId.CreateNewId(projectId4), "D4") }),
                    ProjectInfo.Create(projectId5, VersionStamp.Create(), "P5", "P5", LanguageNames.CSharp,
                        documents: new[] { DocumentInfo.Create(DocumentId.CreateNewId(projectId5), "D5") },
                        projectReferences: new[] { new ProjectReference(projectId4) }),
                });

            return solution;
        }

        private static SolutionInfo GetInitialSolutionInfo_2Projects_10Documents()
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            return SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(),
                        projects: new[]
                        {
                            ProjectInfo.Create(projectId1, VersionStamp.Create(), "P1", "P1", LanguageNames.CSharp,
                                documents: GetDocuments(projectId1, count: 5)),
                            ProjectInfo.Create(projectId2, VersionStamp.Create(), "P2", "P2", LanguageNames.CSharp,
                                documents: GetDocuments(projectId2, count: 5))
                        });
        }

        private static IEnumerable<DocumentInfo> GetDocuments(ProjectId projectId, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return DocumentInfo.Create(DocumentId.CreateNewId(projectId), $"D{i + 1}");
            }
        }

        private static AsynchronousOperationListenerProvider GetListenerProvider(ExportProvider provider)
            => provider.GetExportedValue<AsynchronousOperationListenerProvider>();

        private static void SetOptions(Workspace workspace)
        {
            // override default timespan to make test run faster
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.ActiveFileWorkerBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.AllFilesWorkerBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.PreviewBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.ProjectPropagationBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.SemanticChangeBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.EntireProjectWorkerBackOffTimeSpanInMS, 100)));
        }

        private static void MakeFirstDocumentActive(Project project)
            => MakeDocumentActive(project.Documents.First());

        private static void MakeDocumentActive(Document document)
        {
            var documentTrackingService = (TestDocumentTrackingService)document.Project.Solution.Workspace.Services.GetRequiredService<IDocumentTrackingService>();
            documentTrackingService.SetActiveDocument(document.Id);
        }

        private static void ClearActiveDocument(Workspace workspace)
        {
            var documentTrackingService = (TestDocumentTrackingService)workspace.Services.GetService<IDocumentTrackingService>();
            documentTrackingService.SetActiveDocument(null);
        }

        private class WorkCoordinatorWorkspace : TestWorkspace
        {
            private static readonly TestComposition s_composition = EditorTestCompositions.EditorFeatures.AddParts(typeof(TestDocumentTrackingService)).AddExcludedPartTypes(typeof(IIncrementalAnalyzerProvider));

            private readonly IAsynchronousOperationWaiter _workspaceWaiter;
            private readonly IAsynchronousOperationWaiter _solutionCrawlerWaiter;

            public WorkCoordinatorWorkspace(string workspaceKind = null, bool disablePartialSolutions = true, Type incrementalAnalyzer = null)
                : base(composition: incrementalAnalyzer is null ? s_composition : s_composition.AddParts(incrementalAnalyzer), workspaceKind: workspaceKind, disablePartialSolutions: disablePartialSolutions)
            {
                _workspaceWaiter = GetListenerProvider(ExportProvider).GetWaiter(FeatureAttribute.Workspace);
                _solutionCrawlerWaiter = GetListenerProvider(ExportProvider).GetWaiter(FeatureAttribute.SolutionCrawler);

                Assert.False(_workspaceWaiter.HasPendingWork);
                Assert.False(_solutionCrawlerWaiter.HasPendingWork);

                WorkCoordinatorTests.SetOptions(this);
            }

            public static WorkCoordinatorWorkspace CreateWithAnalysisScope(BackgroundAnalysisScope analysisScope, string workspaceKind = null, bool disablePartialSolutions = true, Type incrementalAnalyzer = null)
            {
                var workspace = new WorkCoordinatorWorkspace(workspaceKind, disablePartialSolutions, incrementalAnalyzer);
                workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options
                    .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, analysisScope)));
                return workspace;
            }

            protected override void Dispose(bool finalize)
            {
                base.Dispose(finalize);

                Assert.False(_workspaceWaiter.HasPendingWork);
                Assert.False(_solutionCrawlerWaiter.HasPendingWork);
            }
        }

        private class AnalyzerProvider : IIncrementalAnalyzerProvider
        {
            public readonly IIncrementalAnalyzer Analyzer;

            public AnalyzerProvider(IIncrementalAnalyzer analyzer)
                => Analyzer = analyzer;

            public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
                => Analyzer;
        }

        [ExportIncrementalAnalyzerProvider(name: "TestAnalyzer", workspaceKinds: new[] { SolutionCrawlerWorkspaceKind })]
        [Shared]
        [PartNotDiscoverable]
        private class AnalyzerProviderNoWaitNoBlock : AnalyzerProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public AnalyzerProviderNoWaitNoBlock()
                : base(new Analyzer())
            {
            }
        }

        [ExportIncrementalAnalyzerProvider(name: "TestAnalyzer", workspaceKinds: new[] { SolutionCrawlerWorkspaceKind })]
        [Shared]
        [PartNotDiscoverable]
        private class AnalyzerProviderWaitNoBlock : AnalyzerProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public AnalyzerProviderWaitNoBlock()
                : base(new Analyzer(waitForCancellation: true))
            {
            }
        }

        [ExportIncrementalAnalyzerProvider(name: "TestAnalyzer", workspaceKinds: new[] { SolutionCrawlerWorkspaceKind })]
        [Shared]
        [PartNotDiscoverable]
        private class AnalyzerProviderNoWaitBlock : AnalyzerProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public AnalyzerProviderNoWaitBlock()
                : base(new Analyzer(blockedRun: true))
            {
            }
        }

        [ExportIncrementalAnalyzerProvider(name: "TestAnalyzer", workspaceKinds: new[] { SolutionCrawlerWorkspaceKind })]
        [Shared]
        [PartNotDiscoverable]
        private class AnalyzerProvider2 : AnalyzerProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public AnalyzerProvider2()
                : base(new Analyzer2())
            {
            }
        }

        internal static class Metadata
        {
            public static readonly IncrementalAnalyzerProviderMetadata Crawler = new IncrementalAnalyzerProviderMetadata(new Dictionary<string, object> { { "WorkspaceKinds", new[] { SolutionCrawlerWorkspaceKind } }, { "HighPriorityForActiveFile", false }, { "Name", "TestAnalyzer" } });
        }

        private class Analyzer : IIncrementalAnalyzer
        {
            public static readonly Option<bool> TestOption = new Option<bool>("TestOptions", "TestOption", defaultValue: true);

            public readonly ManualResetEventSlim BlockEvent;
            public readonly ManualResetEventSlim RunningEvent;

            public readonly HashSet<DocumentId> SyntaxDocumentIds = new HashSet<DocumentId>();
            public readonly HashSet<DocumentId> DocumentIds = new HashSet<DocumentId>();
            public readonly HashSet<ProjectId> ProjectIds = new HashSet<ProjectId>();

            public readonly HashSet<DocumentId> InvalidateDocumentIds = new HashSet<DocumentId>();
            public readonly HashSet<ProjectId> InvalidateProjectIds = new HashSet<ProjectId>();

            public Analyzer(bool waitForCancellation = false, bool blockedRun = false)
            {
                WaitForCancellation = waitForCancellation;
                BlockedRun = blockedRun;

                this.BlockEvent = new ManualResetEventSlim(initialState: false);
                this.RunningEvent = new ManualResetEventSlim(initialState: false);
            }

            public bool WaitForCancellation { get; }

            public bool BlockedRun { get; }

            public void Reset()
            {
                BlockEvent.Reset();
                RunningEvent.Reset();

                SyntaxDocumentIds.Clear();
                DocumentIds.Clear();
                ProjectIds.Clear();

                InvalidateDocumentIds.Clear();
                InvalidateProjectIds.Clear();
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                this.ProjectIds.Add(project.Id);
                return Task.CompletedTask;
            }

            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (bodyOpt == null)
                {
                    this.DocumentIds.Add(document.Id);
                }

                return Task.CompletedTask;
            }

            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                this.SyntaxDocumentIds.Add(document.Id);
                Process(document.Id, cancellationToken);
                return Task.CompletedTask;
            }

            public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
            {
                InvalidateDocumentIds.Add(documentId);
                return Task.CompletedTask;
            }

            public Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
            {
                InvalidateProjectIds.Add(projectId);
                return Task.CompletedTask;
            }

            private void Process(DocumentId _, CancellationToken cancellationToken)
            {
                if (BlockedRun && !RunningEvent.IsSet)
                {
                    this.RunningEvent.Set();

                    // Wait until unblocked
                    this.BlockEvent.Wait();
                }

                if (WaitForCancellation && !RunningEvent.IsSet)
                {
                    this.RunningEvent.Set();

                    cancellationToken.WaitHandle.WaitOne();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
                => e.Option == TestOption;

            #region unused 
            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
                => Task.CompletedTask;
            #endregion
        }

        private class Analyzer2 : IIncrementalAnalyzer
        {
            public readonly List<DocumentId> DocumentIds = new List<DocumentId>();

            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                this.DocumentIds.Add(document.Id);
                return Task.CompletedTask;
            }

            #region unused 
            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e) => false;
            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken) => Task.CompletedTask;
            public Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken) => Task.CompletedTask;
            #endregion
        }

#if false
        private string GetListenerTrace(ExportProvider provider)
        {
            var sb = new StringBuilder();

            var workspaceWaiter = GetListeners(provider).First(l => l.Metadata.FeatureName == FeatureAttribute.Workspace).Value as TestAsynchronousOperationListener;
            sb.AppendLine("workspace");
            sb.AppendLine(workspaceWaiter.Trace());

            var solutionCrawlerWaiter = GetListeners(provider).First(l => l.Metadata.FeatureName == FeatureAttribute.SolutionCrawler).Value as TestAsynchronousOperationListener;
            sb.AppendLine("solutionCrawler");
            sb.AppendLine(solutionCrawlerWaiter.Trace());

            return sb.ToString();
        }

        internal abstract partial class TestAsynchronousOperationListener : IAsynchronousOperationListener, IAsynchronousOperationWaiter
        {
            private readonly object gate = new object();
            private readonly HashSet<TaskCompletionSource<bool>> pendingTasks = new HashSet<TaskCompletionSource<bool>>();
            private readonly StringBuilder sb = new StringBuilder();

            private int counter;

            public TestAsynchronousOperationListener()
            {
            }

            public IAsyncToken BeginAsyncOperation(string name, object tag = null)
            {
                lock (gate)
                {
                    return new AsyncToken(this, name);
                }
            }

            private void Increment(string name)
            {
                lock (gate)
                {
                    sb.AppendLine("i -> " + name + ":" + counter++);
                }
            }

            private void Decrement(string name)
            {
                lock (gate)
                {
                    counter--;
                    if (counter == 0)
                    {
                        foreach (var task in pendingTasks)
                        {
                            task.SetResult(true);
                        }

                        pendingTasks.Clear();
                    }

                    sb.AppendLine("d -> " + name + ":" + counter);
                }
            }

            public virtual Task CreateWaitTask()
            {
                lock (gate)
                {
                    var source = new TaskCompletionSource<bool>();
                    if (counter == 0)
                    {
                        // There is nothing to wait for, so we are immediately done
                        source.SetResult(true);
                    }
                    else
                    {
                        pendingTasks.Add(source);
                    }

                    return source.Task;
                }
            }

            public bool TrackActiveTokens { get; set; }

            public bool HasPendingWork
            {
                get
                {
                    return counter != 0;
                }
            }

            private class AsyncToken : IAsyncToken
            {
                private readonly TestAsynchronousOperationListener listener;
                private readonly string name;
                private bool disposed;

                public AsyncToken(TestAsynchronousOperationListener listener, string name)
                {
                    this.listener = listener;
                    this.name = name;

                    listener.Increment(name);
                }

                public void Dispose()
                {
                    lock (listener.gate)
                    {
                        if (disposed)
                        {
                            throw new InvalidOperationException("Double disposing of an async token");
                        }

                        disposed = true;
                        listener.Decrement(this.name);
                    }
                }
            }

            public string Trace()
            {
                return sb.ToString();
            }
        }
#endif
    }
}
