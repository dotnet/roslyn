// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SolutionCrawler
{
    public class WorkCoordinatorTests
    {
        private const string SolutionCrawler = "SolutionCrawler";

        [WpfFact]
        public void RegisterService()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var registrationService = new SolutionCrawlerRegistrationService(
                    SpecializedCollections.EmptyEnumerable<Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>>(),
                    AggregateAsynchronousOperationListener.EmptyListeners);

                // register and unregister workspace to the service
                registrationService.Register(workspace);
                registrationService.Unregister(workspace);
            }
        }

        [WpfFact, WorkItem(747226)]
        public async Task SolutionAdded_Simple()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
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
        }

        [WpfFact]
        public async Task SolutionAdded_Complex()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);

                var worker = await ExecuteOperation(workspace, w => w.OnSolutionAdded(solution));
                Assert.Equal(10, worker.SyntaxDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Solution_Remove()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var worker = await ExecuteOperation(workspace, w => w.OnSolutionRemoved());
                Assert.Equal(10, worker.InvalidateDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Solution_Clear()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var worker = await ExecuteOperation(workspace, w => w.ClearSolution());
                Assert.Equal(10, worker.InvalidateDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Solution_Reload()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var worker = await ExecuteOperation(workspace, w => w.OnSolutionReloaded(solution));
                Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Solution_Change()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solutionInfo = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solutionInfo);
                await WaitWaiterAsync(workspace.ExportProvider);

                var solution = workspace.CurrentSolution;
                var documentId = solution.Projects.First().DocumentIds[0];
                solution = solution.RemoveDocument(documentId);

                var changedSolution = solution.AddProject("P3", "P3", LanguageNames.CSharp).AddDocument("D1", "").Project.Solution;

                var worker = await ExecuteOperation(workspace, w => w.ChangeSolution(changedSolution));
                Assert.Equal(1, worker.SyntaxDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Project_Add()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
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
        }

        [WpfFact]
        public async Task Project_Remove()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var projectid = workspace.CurrentSolution.ProjectIds[0];

                var worker = await ExecuteOperation(workspace, w => w.OnProjectRemoved(projectid));
                Assert.Equal(0, worker.SyntaxDocumentIds.Count);
                Assert.Equal(5, worker.InvalidateDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Project_Change()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solutionInfo = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solutionInfo);
                await WaitWaiterAsync(workspace.ExportProvider);

                var project = workspace.CurrentSolution.Projects.First();
                var documentId = project.DocumentIds[0];
                var solution = workspace.CurrentSolution.RemoveDocument(documentId);

                var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, solution));
                Assert.Equal(0, worker.SyntaxDocumentIds.Count);
                Assert.Equal(1, worker.InvalidateDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Project_AssemblyName_Change()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solutionInfo = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solutionInfo);
                await WaitWaiterAsync(workspace.ExportProvider);

                var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1").WithAssemblyName("newName");
                var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, project.Solution));

                Assert.Equal(5, worker.SyntaxDocumentIds.Count);
                Assert.Equal(5, worker.DocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Project_AnalyzerOptions_Change()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solutionInfo = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solutionInfo);
                await WaitWaiterAsync(workspace.ExportProvider);

                var project = workspace.CurrentSolution.Projects.First(p => p.Name == "P1").AddAdditionalDocument("a1", SourceText.From("")).Project;
                var worker = await ExecuteOperation(workspace, w => w.ChangeProject(project.Id, project.Solution));

                Assert.Equal(5, worker.SyntaxDocumentIds.Count);
                Assert.Equal(5, worker.DocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Project_Reload()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var project = solution.Projects[0];
                var worker = await ExecuteOperation(workspace, w => w.OnProjectReloaded(project));
                Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Document_Add()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var project = solution.Projects[0];
                var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "D6");

                var worker = await ExecuteOperation(workspace, w => w.OnDocumentAdded(info));
                Assert.Equal(1, worker.SyntaxDocumentIds.Count);
                Assert.Equal(6, worker.DocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Document_Remove()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

                var worker = await ExecuteOperation(workspace, w => w.OnDocumentRemoved(id));

                Assert.Equal(0, worker.SyntaxDocumentIds.Count);
                Assert.Equal(4, worker.DocumentIds.Count);
                Assert.Equal(1, worker.InvalidateDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Document_Reload()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var id = solution.Projects[0].Documents[0];

                var worker = await ExecuteOperation(workspace, w => w.OnDocumentReloaded(id));
                Assert.Equal(0, worker.SyntaxDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Document_Reanalyze()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var info = solution.Projects[0].Documents[0];

                var worker = new Analyzer();
                var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(worker), Metadata.Crawler);
                var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListeners(workspace.ExportProvider));

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
        }

        [WorkItem(670335)]
        public async Task Document_Change()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

                var worker = await ExecuteOperation(workspace, w => w.ChangeDocument(id, SourceText.From("//")));

                Assert.Equal(1, worker.SyntaxDocumentIds.Count);
            }
        }

        [WpfFact]
        public async Task Document_AdditionalFileChange()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
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
        }

        [WorkItem(670335)]
        public async Task Document_Cancellation()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

                var analyzer = new Analyzer(waitForCancellation: true);
                var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(analyzer), Metadata.Crawler);
                var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListeners(workspace.ExportProvider));

                service.Register(workspace);

                workspace.ChangeDocument(id, SourceText.From("//"));
                analyzer.RunningEvent.Wait();

                workspace.ChangeDocument(id, SourceText.From("// "));
                await WaitAsync(service, workspace);

                service.Unregister(workspace);

                Assert.Equal(1, analyzer.SyntaxDocumentIds.Count);
                Assert.Equal(5, analyzer.DocumentIds.Count);
            }
        }

        [WorkItem(670335)]
        public async Task Document_Cancellation_MultipleTimes()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

                var analyzer = new Analyzer(waitForCancellation: true);
                var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(analyzer), Metadata.Crawler);
                var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListeners(workspace.ExportProvider));

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
        }

        [WorkItem(670335)]
        public async Task Document_InvocationReasons()
        {
            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                var solution = GetInitialSolutionInfo(workspace);
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var id = workspace.CurrentSolution.Projects.First().DocumentIds[0];

                var analyzer = new Analyzer(blockedRun: true);
                var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(analyzer), Metadata.Crawler);
                var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListeners(workspace.ExportProvider));

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
        }

        [WpfFact]
        public async Task Document_TopLevelType_Whitespace()
        {
            var code = @"class C { $$ }";
            var textToInsert = " ";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [WpfFact]
        public async Task Document_TopLevelType_Character()
        {
            var code = @"class C { $$ }";
            var textToInsert = "int";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [WpfFact]
        public async Task Document_TopLevelType_NewLine()
        {
            var code = @"class C { $$ }";
            var textToInsert = "\r\n";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [WpfFact]
        public async Task Document_TopLevelType_NewLine2()
        {
            var code = @"class C { $$";
            var textToInsert = "\r\n";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [WpfFact]
        public async Task Document_EmptyFile()
        {
            var code = @"$$";
            var textToInsert = "class";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [WpfFact]
        public async Task Document_TopLevel1()
        {
            var code = @"class C
{
    public void Test($$";
            var textToInsert = "int";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [WpfFact]
        public async Task Document_TopLevel2()
        {
            var code = @"class C
{
    public void Test(int $$";
            var textToInsert = " ";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [WpfFact]
        public async Task Document_TopLevel3()
        {
            var code = @"class C
{
    public void Test(int i,$$";
            var textToInsert = "\r\n";

            await InsertText(code, textToInsert, expectDocumentAnalysis: true);
        }

        [WpfFact]
        public async Task Document_InteriorNode1()
        {
            var code = @"class C
{
    public void Test()
    {$$";
            var textToInsert = "\r\n";

            await InsertText(code, textToInsert, expectDocumentAnalysis: false);
        }

        [WpfFact]
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

        [WpfFact]
        public async Task Document_InteriorNode_Field()
        {
            var code = @"class C
{
    int i = $$
}";
            var textToInsert = "1";

            await InsertText(code, textToInsert, expectDocumentAnalysis: false);
        }

        [WpfFact]
        public async Task Document_InteriorNode_Field1()
        {
            var code = @"class C
{
    int i = 1 + $$
}";
            var textToInsert = "1";

            await InsertText(code, textToInsert, expectDocumentAnalysis: false);
        }

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

        [WpfFact]
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

            int position;
            string code;
            MarkupTestFile.GetPosition(markup, out code, out position);

            var root = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.ParseCompilationUnit(code);
            var property = root.FindToken(position).Parent.FirstAncestorOrSelf<Microsoft.CodeAnalysis.VisualBasic.Syntax.PropertyBlockSyntax>();
            var memberId = (new Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxFactsService()).GetMethodLevelMemberId(root, property);

            Assert.Equal(0, memberId);
        }

        [WpfFact, WorkItem(739943)]
        public async Task SemanticChange_Propagation()
        {
            var solution = GetInitialSolutionInfoWithP2P();

            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                workspace.OnSolutionAdded(solution);
                await WaitWaiterAsync(workspace.ExportProvider);

                var id = solution.Projects[0].Id;
                var info = DocumentInfo.Create(DocumentId.CreateNewId(id), "D6");

                var worker = await ExecuteOperation(workspace, w => w.OnDocumentAdded(info));

                Assert.Equal(1, worker.SyntaxDocumentIds.Count);
                Assert.Equal(4, worker.DocumentIds.Count);

#if false
                Assert.True(1 == worker.SyntaxDocumentIds.Count,
                    string.Format("Expected 1 SyntaxDocumentIds, Got {0}\n\n{1}", worker.SyntaxDocumentIds.Count, GetListenerTrace(workspace.ExportProvider)));
                Assert.True(4 == worker.DocumentIds.Count, 
                    string.Format("Expected 4 DocumentIds, Got {0}\n\n{1}", worker.DocumentIds.Count, GetListenerTrace(workspace.ExportProvider)));
#endif
            }
        }

        [WpfFact]
        public async Task ProgressReporterTest()
        {
            var solution = GetInitialSolutionInfoWithP2P();

            using (var workspace = new WorkCoordinatorWorkspace(SolutionCrawler))
            {
                await WaitWaiterAsync(workspace.ExportProvider);

                var service = workspace.Services.GetService<ISolutionCrawlerService>();
                var reporter = service.GetProgressReporter(workspace);
                Assert.False(reporter.InProgress);

                // set up events
                bool started = false;
                reporter.Started += (o, a) => { started = true; };

                bool stopped = false;
                reporter.Stopped += (o, a) => { stopped = true; };

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
        }

        private async Task InsertText(string code, string text, bool expectDocumentAnalysis, string language = LanguageNames.CSharp)
        {
            using (var workspace = TestWorkspaceFactory.CreateWorkspaceFromLines(
                SolutionCrawler, language, compilationOptions: null, parseOptions: null, content: new string[] { code }))
            {
                var analyzer = new Analyzer();
                var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(analyzer), Metadata.Crawler);
                var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListeners(workspace.ExportProvider));

                service.Register(workspace);

                var testDocument = workspace.Documents.First();

                var insertPosition = testDocument.CursorPosition;
                var textBuffer = testDocument.GetTextBuffer();

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
        }

        private async Task<Analyzer> ExecuteOperation(TestWorkspace workspace, Action<TestWorkspace> operation)
        {
            var worker = new Analyzer();
            var lazyWorker = new Lazy<IIncrementalAnalyzerProvider, IncrementalAnalyzerProviderMetadata>(() => new AnalyzerProvider(worker), Metadata.Crawler);
            var service = new SolutionCrawlerRegistrationService(new[] { lazyWorker }, GetListeners(workspace.ExportProvider));

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
            var workspaceWaiter = GetListeners(provider).First(l => l.Metadata.FeatureName == FeatureAttribute.Workspace).Value as IAsynchronousOperationWaiter;
            await workspaceWaiter.CreateWaitTask();

            var solutionCrawlerWaiter = GetListeners(provider).First(l => l.Metadata.FeatureName == FeatureAttribute.SolutionCrawler).Value as IAsynchronousOperationWaiter;
            await solutionCrawlerWaiter.CreateWaitTask();
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

        private static SolutionInfo GetInitialSolutionInfo(TestWorkspace workspace)
        {
            var projectId1 = ProjectId.CreateNewId();
            var projectId2 = ProjectId.CreateNewId();

            return SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(),
                        projects: new[]
                        {
                            ProjectInfo.Create(projectId1, VersionStamp.Create(), "P1", "P1", LanguageNames.CSharp,
                                documents: new[]
                                {
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "D1"),
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "D2"),
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "D3"),
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "D4"),
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId1), "D5")
                                }),
                            ProjectInfo.Create(projectId2, VersionStamp.Create(), "P2", "P2", LanguageNames.CSharp,
                                documents: new[]
                                {
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId2), "D1"),
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId2), "D2"),
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId2), "D3"),
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId2), "D4"),
                                    DocumentInfo.Create(DocumentId.CreateNewId(projectId2), "D5")
                                })
                        });
        }

        private static IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> GetListeners(ExportProvider provider)
        {
            return provider.GetExports<IAsynchronousOperationListener, FeatureMetadata>();
        }

        [Shared]
        [Export(typeof(IAsynchronousOperationListener))]
        [Export(typeof(IAsynchronousOperationWaiter))]
        [Feature(FeatureAttribute.SolutionCrawler)]
        private class SolutionCrawlerWaiter : AsynchronousOperationListener
        {
            internal SolutionCrawlerWaiter()
            {

            }
        }

        [Shared]
        [Export(typeof(IAsynchronousOperationListener))]
        [Export(typeof(IAsynchronousOperationWaiter))]
        [Feature(FeatureAttribute.Workspace)]
        private class WorkspaceWaiter : AsynchronousOperationListener
        {
            internal WorkspaceWaiter()
            {

            }
        }

        private class WorkCoordinatorWorkspace : TestWorkspace
        {
            private readonly IAsynchronousOperationWaiter _workspaceWaiter;
            private readonly IAsynchronousOperationWaiter _solutionCrawlerWaiter;

            public WorkCoordinatorWorkspace(string workspaceKind = null, bool disablePartialSolutions = true)
                : base(TestExportProvider.CreateExportProviderWithCSharpAndVisualBasic(), workspaceKind, disablePartialSolutions)
            {
                _workspaceWaiter = GetListeners(ExportProvider).First(l => l.Metadata.FeatureName == FeatureAttribute.Workspace).Value as IAsynchronousOperationWaiter;
                _solutionCrawlerWaiter = GetListeners(ExportProvider).First(l => l.Metadata.FeatureName == FeatureAttribute.SolutionCrawler).Value as IAsynchronousOperationWaiter;

                Assert.False(_workspaceWaiter.HasPendingWork);
                Assert.False(_solutionCrawlerWaiter.HasPendingWork);
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
            private readonly Analyzer _analyzer;

            public AnalyzerProvider(Analyzer analyzer)
            {
                _analyzer = analyzer;
            }

            public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            {
                return _analyzer;
            }
        }

        internal class Metadata : IncrementalAnalyzerProviderMetadata
        {
            public Metadata(params string[] workspaceKinds)
                : base(new Dictionary<string, object> { { "WorkspaceKinds", workspaceKinds }, { "HighPriorityForActiveFile", false } })
            {
            }

            public static readonly Metadata Crawler = new Metadata(SolutionCrawler);
        }

        private class Analyzer : IIncrementalAnalyzer
        {
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

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
            {
                this.ProjectIds.Add(project.Id);
                return SpecializedTasks.EmptyTask;
            }

            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
            {
                if (bodyOpt == null)
                {
                    this.DocumentIds.Add(document.Id);
                }

                return SpecializedTasks.EmptyTask;
            }

            public Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                this.SyntaxDocumentIds.Add(document.Id);
                Process(document.Id, cancellationToken);
                return SpecializedTasks.EmptyTask;
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

            #region unused 
            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return false;
            }
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
