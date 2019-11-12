// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
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
        private const string SolutionCrawler = nameof(SolutionCrawler);

        [Fact]
        public async Task RegisterService()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var registrationService = new SolutionCrawlerRegistrationService(
                SpecializedCollections.EmptyEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>>(),
                AsynchronousOperationListenerProvider.NullProvider);

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
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            // create solution and wait for it to settle
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            // create solution crawler and add new analyzer provider dynamically
            var service = new SolutionCrawlerRegistrationService(
                SpecializedCollections.EmptyEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>>(),
                GetListenerProvider(workspace.ExportProvider));

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

        [Fact, WorkItem(747226, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/747226")]
        public async Task SolutionAdded_Simple()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
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
            Assert.Equal(1, worker.SyntaxDocumentIds.Count);
        }

        [Fact]
        public async Task SolutionAdded_Complex()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);

            var worker = await ExecuteOperation(workspace, w => w.OnSolutionAdded(solution));
            Assert.Equal(10, worker.SyntaxDocumentIds.Count);
        }

        [Fact]
        public async Task Solution_Remove()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.OnSolutionRemoved());
            Assert.Equal(10, worker.InvalidateDocumentIds.Count);
        }

        [Fact]
        public async Task Solution_Clear()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.ClearSolution());
            Assert.Equal(10, worker.InvalidateDocumentIds.Count);
        }

        [Fact]
        public async Task Solution_Reload()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.OnSolutionReloaded(solution));
            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
        }

        [Fact]
        public async Task Solution_Change()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var solution = workspace.CurrentSolution;
            var documentId = solution.Projects.First().DocumentIds[0];
            solution = solution.RemoveDocument(documentId);

            var changedSolution = solution.AddProject("P3", "P3", LanguageNames.CSharp).AddDocument("D1", "").Project.Solution;

            var worker = await ExecuteOperation(workspace, w => w.ChangeSolution(changedSolution));
            Assert.Equal(1, worker.SyntaxDocumentIds.Count);
        }

        [Fact]
        public async Task Project_Add()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
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
            Assert.Equal(2, worker.SyntaxDocumentIds.Count);
        }

        [Fact]
        public async Task Project_Remove()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var projectid = workspace.CurrentSolution.ProjectIds[0];

            var worker = await ExecuteOperation(workspace, w => w.OnProjectRemoved(projectid));
            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.InvalidateDocumentIds.Count);
        }

        [Fact]
        public async Task Project_Change()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = workspace.CurrentSolution.Projects.First();
            var documentId = project.DocumentIds[0];
            var solution = workspace.CurrentSolution.RemoveDocument(documentId);

            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, solution));
            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            Assert.Equal(1, worker.InvalidateDocumentIds.Count);
        }

        [Fact]
        public async Task Project_AssemblyName_Change()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1").WithAssemblyName("newName");
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, project.Solution));

            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);
        }

        [Fact]
        public async Task Project_DefaultNamespace_Change()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1").WithDefaultNamespace("newNamespace");
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, project.Solution));

            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);
        }

        [Fact]
        public async Task Project_AnalyzerOptions_Change()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1").AddAdditionalDocument("a1", SourceText.From("")).Project;
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, project.Solution));

            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);
        }

        [Fact]
        public async Task Project_OutputFilePath_Change()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var projectId = workspace.CurrentSolution.Projects.First(p => p.Name == "P1").Id;
            var newSolution = workspace.CurrentSolution.WithProjectOutputFilePath(projectId, "/newPath");
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(projectId, newSolution));

            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);
        }

        [Fact]
        public async Task Project_OutputRefFilePath_Change()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var projectId = workspace.CurrentSolution.Projects.First(p => p.Name == "P1").Id;
            var newSolution = workspace.CurrentSolution.WithProjectOutputRefFilePath(projectId, "/newPath");
            var worker = await ExecuteOperation(workspace, w => w.ChangeProject(projectId, newSolution));

            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);
        }

        [Fact]
        public async Task Test_NeedsReanalysisOnOptionChanged()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solutionInfo = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solutionInfo);
            await WaitWaiterAsync(workspace.ExportProvider);

            var worker = await ExecuteOperation(workspace, w => w.Options = w.Options.WithChangedOption(Analyzer.TestOption, false));

            Assert.Equal(10, worker.SyntaxDocumentIds.Count);
            Assert.Equal(10, worker.DocumentIds.Count);
            Assert.Equal(2, worker.ProjectIds.Count);
        }

        [Fact]
        public async Task Project_Reload()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = solution.Projects[0];
            var worker = await ExecuteOperation(workspace, w => w.OnProjectReloaded(project));
            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
        }

        [Fact]
        public async Task Document_Add()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = solution.Projects[0];
            var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "D6");

            var worker = await ExecuteOperation(workspace, w => w.OnDocumentAdded(info));
            Assert.Equal(1, worker.SyntaxDocumentIds.Count);
            Assert.Equal(6, worker.DocumentIds.Count);
        }

        [Fact]
        public async Task Document_Remove()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

            var worker = await ExecuteOperation(workspace, w => w.OnDocumentRemoved(id));

            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            Assert.Equal(4, worker.DocumentIds.Count);
            Assert.Equal(1, worker.InvalidateDocumentIds.Count);
        }

        [Fact]
        public async Task Document_Reload()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var id = solution.Projects[0].Documents[0];

            var worker = await ExecuteOperation(workspace, w => w.OnDocumentReloaded(id));
            Assert.Equal(0, worker.SyntaxDocumentIds.Count);
        }

        [Fact]
        public async Task Document_Reanalyze()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var info = solution.Projects[0].Documents[0];

            var worker = new Analyzer();
            var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(worker), Metadata.Crawler);
            var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListenerProvider(workspace.ExportProvider));

            service.Register(workspace);

            // don't rely on background parser to have tree. explicitly do it here.
            await TouchEverything(workspace.CurrentSolution);

            service.Reanalyze(workspace, worker, projectIds: null, documentIds: SpecializedCollections.SingletonEnumerable<DocumentId>(info.Id), highPriority: false);

            await TouchEverything(workspace.CurrentSolution);

            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            Assert.Equal(1, worker.SyntaxDocumentIds.Count);
            Assert.Equal(1, worker.DocumentIds.Count);
        }

        [Fact, WorkItem(670335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670335")]
        public async Task Document_Change()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

            var worker = await ExecuteOperation(workspace, w => w.ChangeDocument(id, SourceText.From("//")));

            Assert.Equal(1, worker.SyntaxDocumentIds.Count);
        }

        [Fact]
        public async Task Document_AdditionalFileChange()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = solution.Projects[0];
            var ncfile = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "D6");

            var worker = await ExecuteOperation(workspace, w => w.OnAdditionalDocumentAdded(ncfile));
            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);

            worker = await ExecuteOperation(workspace, w => w.ChangeAdditionalDocument(ncfile.Id, SourceText.From("//")));

            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);

            worker = await ExecuteOperation(workspace, w => w.OnAdditionalDocumentRemoved(ncfile.Id));

            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);
        }

        [Fact]
        public async Task Document_AnalyzerConfigFileChange()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var project = solution.Projects[0];
            var analyzerConfigDocFilePath = PathUtilities.CombineAbsoluteAndRelativePaths(Temp.CreateDirectory().Path, ".editorconfig");
            var analyzerConfigFile = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), ".editorconfig", filePath: analyzerConfigDocFilePath);

            var worker = await ExecuteOperation(workspace, w => w.OnAnalyzerConfigDocumentAdded(analyzerConfigFile));
            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);

            worker = await ExecuteOperation(workspace, w => w.ChangeAnalyzerConfigDocument(analyzerConfigFile.Id, SourceText.From("//")));

            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);

            worker = await ExecuteOperation(workspace, w => w.OnAnalyzerConfigDocumentRemoved(analyzerConfigFile.Id));

            Assert.Equal(5, worker.SyntaxDocumentIds.Count);
            Assert.Equal(5, worker.DocumentIds.Count);
        }

        [Fact, WorkItem(670335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670335")]
        public async Task Document_Cancellation()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

            var analyzer = new Analyzer(waitForCancellation: true);
            var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(analyzer), Metadata.Crawler);
            var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListenerProvider(workspace.ExportProvider));

            service.Register(workspace);

            workspace.ChangeDocument(id, SourceText.From("//"));
            analyzer.RunningEvent.Wait();

            workspace.ChangeDocument(id, SourceText.From("// "));
            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            Assert.Equal(1, analyzer.SyntaxDocumentIds.Count);
            Assert.Equal(5, analyzer.DocumentIds.Count);
        }

        [Fact, WorkItem(670335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670335")]
        public async Task Document_Cancellation_MultipleTimes()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

            var analyzer = new Analyzer(waitForCancellation: true);
            var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(analyzer), Metadata.Crawler);
            var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListenerProvider(workspace.ExportProvider));

            service.Register(workspace);

            workspace.ChangeDocument(id, SourceText.From("//"));
            analyzer.RunningEvent.Wait();
            analyzer.RunningEvent.Reset();

            workspace.ChangeDocument(id, SourceText.From("// "));
            analyzer.RunningEvent.Wait();

            workspace.ChangeDocument(id, SourceText.From("//  "));
            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            Assert.Equal(1, analyzer.SyntaxDocumentIds.Count);
            Assert.Equal(5, analyzer.DocumentIds.Count);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21082"), WorkItem(670335, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/670335")]
        public async Task Document_InvocationReasons()
        {
            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            var solution = GetInitialSolutionInfo_2Projects_10Documents(workspace);
            workspace.OnSolutionAdded(solution);
            await WaitWaiterAsync(workspace.ExportProvider);

            var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

            var analyzer = new Analyzer(blockedRun: true);
            var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(analyzer), Metadata.Crawler);
            var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListenerProvider(workspace.ExportProvider));

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

        [Fact]
        public void VBPropertyTest()
        {
            var markup = @"Class C
    Default Public Property G(x As Integer) As Integer
        Get
            $$
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class";
            MarkupTestFile.GetPosition(markup, out var code, out int position);

            var root = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseCompilationUnit(code);
            var property = root.FindToken(position).Parent.FirstAncestorOrSelf<Microsoft.CodeAnalysis.VisualBasic.Syntax.PropertyBlockSyntax>();
            var memberId = Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxFactsService.Instance.GetMethodLevelMemberId(root, property);

            Assert.Equal(0, memberId);
        }

        [Fact, WorkItem(739943, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/739943")]
        public async Task SemanticChange_Propagation_Transitive()
        {
            var solution = GetInitialSolutionInfoWithP2P();

            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            workspace.Options = workspace.Options.WithChangedOption(InternalSolutionCrawlerOptions.DirectDependencyPropagationOnly, false);

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

            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            workspace.Options = workspace.Options.WithChangedOption(InternalSolutionCrawlerOptions.DirectDependencyPropagationOnly, true);

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

            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
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

            using var workspace = new WorkCoordinatorWorkspace(SolutionCrawler);
            await WaitWaiterAsync(workspace.ExportProvider);

            // add analyzer
            var worker = new Analyzer2();
            var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(worker), Metadata.Crawler);

            // enable solution crawler
            var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListenerProvider(workspace.ExportProvider));
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
                await operationWaiter.CreateExpeditedWaitTask();

                // mutate solution
                workspace.OnSolutionAdded(solution);

                // wait for workspace events to be all processed
                var workspaceWaiter = GetListenerProvider(workspace.ExportProvider).GetWaiter(FeatureAttribute.Workspace);
                await workspaceWaiter.CreateExpeditedWaitTask();

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

        private async Task InsertText(string code, string text, bool expectDocumentAnalysis, string language = LanguageNames.CSharp)
        {
            using var workspace = TestWorkspace.Create(
                SolutionCrawler, language, compilationOptions: null, parseOptions: null, content: code, exportProvider: EditorServicesUtil.ExportProvider);
            SetOptions(workspace);
            var testDocument = workspace.Documents.First();
            var textBuffer = testDocument.GetTextBuffer();

            var analyzer = new Analyzer();
            var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(analyzer), Metadata.Crawler);
            var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListenerProvider(workspace.ExportProvider));

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

        private async Task<Analyzer> ExecuteOperation(TestWorkspace workspace, Action<TestWorkspace> operation)
        {
            var worker = new Analyzer();
            var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(worker), Metadata.Crawler);
            var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListenerProvider(workspace.ExportProvider));

            service.Register(workspace);

            // don't rely on background parser to have tree. explicitly do it here.
            await TouchEverything(workspace.CurrentSolution);
            operation(workspace);
            await TouchEverything(workspace.CurrentSolution);

            await WaitAsync(service, workspace);

            service.Unregister(workspace);

            return worker;
        }

        private async Task TouchEverything(Solution solution)
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

        private async Task WaitAsync(SolutionCrawlerRegistrationService service, TestWorkspace workspace)
        {
            await WaitWaiterAsync(workspace.ExportProvider);

            service.WaitUntilCompletion_ForTestingPurposesOnly(workspace);
        }

        private async Task WaitWaiterAsync(ExportProvider provider)
        {
            var workspaceWaiter = GetListenerProvider(provider).GetWaiter(FeatureAttribute.Workspace);
            await workspaceWaiter.CreateExpeditedWaitTask();

            var solutionCrawlerWaiter = GetListenerProvider(provider).GetWaiter(FeatureAttribute.SolutionCrawler);
            await solutionCrawlerWaiter.CreateExpeditedWaitTask();
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

        private static SolutionInfo GetInitialSolutionInfo_2Projects_10Documents(TestWorkspace workspace)
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
        {
            return provider.GetExportedValue<AsynchronousOperationListenerProvider>();
        }

        private static void SetOptions(Workspace workspace)
        {
            // override default timespan to make test run faster
            workspace.Options = workspace.Options.WithChangedOption(InternalSolutionCrawlerOptions.ActiveFileWorkerBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.AllFilesWorkerBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.PreviewBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.ProjectPropagationBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.SemanticChangeBackOffTimeSpanInMS, 0)
                                                 .WithChangedOption(InternalSolutionCrawlerOptions.EntireProjectWorkerBackOffTimeSpanInMS, 100);
        }

        private class WorkCoordinatorWorkspace : TestWorkspace
        {
            private readonly IAsynchronousOperationWaiter _workspaceWaiter;
            private readonly IAsynchronousOperationWaiter _solutionCrawlerWaiter;

            public WorkCoordinatorWorkspace(string workspaceKind = null, bool disablePartialSolutions = true)
                : base(EditorServicesUtil.ExportProvider, workspaceKind, disablePartialSolutions)
            {
                _workspaceWaiter = GetListenerProvider(ExportProvider).GetWaiter(FeatureAttribute.Workspace);
                _solutionCrawlerWaiter = GetListenerProvider(ExportProvider).GetWaiter(FeatureAttribute.SolutionCrawler);

                Assert.False(_workspaceWaiter.HasPendingWork);
                Assert.False(_solutionCrawlerWaiter.HasPendingWork);

                SetOptions(this);
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
            {
                Analyzer = analyzer;
            }

            public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            {
                return Analyzer;
            }
        }

        internal class Metadata : IncrementalAnalyzerProviderMetadata
        {
            public Metadata(params string[] workspaceKinds)
                : base(new Dictionary<string, object> { { "WorkspaceKinds", workspaceKinds }, { "HighPriorityForActiveFile", false }, { "Name", "TestAnalyzer" } })
            {
            }

            public static readonly Metadata Crawler = new Metadata(SolutionCrawler);
        }

        private class Analyzer : IIncrementalAnalyzer
        {
            public static readonly Option<bool> TestOption = new Option<bool>("TestOptions", "TestOption", defaultValue: true);

            private readonly bool _waitForCancellation;
            private readonly bool _blockedRun;

            public readonly ManualResetEventSlim BlockEvent;
            public readonly ManualResetEventSlim RunningEvent;

            public readonly HashSet<DocumentId> SyntaxDocumentIds = new HashSet<DocumentId>();
            public readonly HashSet<DocumentId> DocumentIds = new HashSet<DocumentId>();
            public readonly HashSet<ProjectId> ProjectIds = new HashSet<ProjectId>();

            public readonly HashSet<DocumentId> InvalidateDocumentIds = new HashSet<DocumentId>();
            public readonly HashSet<ProjectId> InvalidateProjectIds = new HashSet<ProjectId>();

            public Analyzer(bool waitForCancellation = false, bool blockedRun = false)
            {
                _waitForCancellation = waitForCancellation;
                _blockedRun = blockedRun;

                this.BlockEvent = new ManualResetEventSlim(initialState: false);
                this.RunningEvent = new ManualResetEventSlim(initialState: false);
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

            public void RemoveDocument(DocumentId documentId)
            {
                InvalidateDocumentIds.Add(documentId);
            }

            public void RemoveProject(ProjectId projectId)
            {
                InvalidateProjectIds.Add(projectId);
            }

            private void Process(DocumentId documentId, CancellationToken cancellationToken)
            {
                if (_blockedRun && !RunningEvent.IsSet)
                {
                    this.RunningEvent.Set();

                    // Wait until unblocked
                    this.BlockEvent.Wait();
                }

                if (_waitForCancellation && !RunningEvent.IsSet)
                {
                    this.RunningEvent.Set();

                    cancellationToken.WaitHandle.WaitOne();
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return e.Option == TestOption;
            }

            #region unused 
            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
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
            public void RemoveDocument(DocumentId documentId) { }
            public void RemoveProject(ProjectId projectId) { }
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
