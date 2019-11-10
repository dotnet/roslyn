// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public sealed class EditAndContinueWorkspaceServiceTests : TestBase
    {
        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;
        private readonly Mock<IDiagnosticAnalyzerService> _mockDiagnosticService;
        private readonly MockDebuggeeModuleMetadataProvider _mockDebugeeModuleMetadataProvider;
        private readonly Mock<IActiveStatementTrackingService> _mockActiveStatementTrackingService;
        private readonly MockCompilationOutputsProviderService _mockCompilationOutputsService;

        private Mock<IActiveStatementProvider> _mockActiveStatementProvider;
        private readonly List<Guid> _modulesPreparedForUpdate;
        private readonly List<DiagnosticsUpdatedArgs> _emitDiagnosticsUpdated;
        private int _emitDiagnosticsClearedCount;
        private readonly List<string> _telemetryLog;
        private int _telemetryId;

        public EditAndContinueWorkspaceServiceTests()
        {
            _modulesPreparedForUpdate = new List<Guid>();
            _mockDiagnosticService = new Mock<IDiagnosticAnalyzerService>(MockBehavior.Strict);
            _mockDiagnosticService.Setup(s => s.Reanalyze(It.IsAny<Workspace>(), It.IsAny<IEnumerable<ProjectId>>(), It.IsAny<IEnumerable<DocumentId>>(), It.IsAny<bool>()));

            _diagnosticUpdateSource = new EditAndContinueDiagnosticUpdateSource();
            _emitDiagnosticsUpdated = new List<DiagnosticsUpdatedArgs>();
            _diagnosticUpdateSource.DiagnosticsUpdated += (object sender, DiagnosticsUpdatedArgs args) => _emitDiagnosticsUpdated.Add(args);
            _diagnosticUpdateSource.DiagnosticsCleared += (object sender, EventArgs args) => _emitDiagnosticsClearedCount++;

            _mockActiveStatementProvider = new Mock<IActiveStatementProvider>(MockBehavior.Strict);
            _mockActiveStatementProvider.Setup(p => p.GetActiveStatementsAsync(It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(ImmutableArray<ActiveStatementDebugInfo>.Empty));

            _mockDebugeeModuleMetadataProvider = new MockDebuggeeModuleMetadataProvider
            {
                IsEditAndContinueAvailable = (Guid guid, out int errorCode, out string localizedMessage) =>
                {
                    errorCode = 0;
                    localizedMessage = null;
                    return true;
                },
                PrepareModuleForUpdate = mvid => _modulesPreparedForUpdate.Add(mvid)
            };

            _mockActiveStatementTrackingService = new Mock<IActiveStatementTrackingService>(MockBehavior.Strict);
            _mockActiveStatementTrackingService.Setup(s => s.StartTracking(It.IsAny<EditSession>()));
            _mockActiveStatementTrackingService.Setup(s => s.EndTracking());

            _mockCompilationOutputsService = new MockCompilationOutputsProviderService();
            _telemetryLog = new List<string>();
        }

        private EditAndContinueWorkspaceService CreateEditAndContinueService(Workspace workspace)
            => new EditAndContinueWorkspaceService(
                workspace,
                _mockActiveStatementTrackingService.Object,
                _mockCompilationOutputsService,
                _mockDiagnosticService.Object,
                _diagnosticUpdateSource,
                _mockActiveStatementProvider.Object,
                _mockDebugeeModuleMetadataProvider,
                reportTelemetry: data => EditAndContinueWorkspaceService.LogDebuggingSessionTelemetry(data, (id, message) => _telemetryLog.Add($"{id}: {message.GetMessage()}"), () => ++_telemetryId));

        private DebuggingSession StartDebuggingSession(EditAndContinueWorkspaceService service, CommittedSolution.DocumentState initialState = CommittedSolution.DocumentState.MatchesDebuggee)
        {
            service.StartDebuggingSession();
            var session = service.Test_GetDebuggingSession();
            if (initialState != CommittedSolution.DocumentState.None)
            {
                SetDocumentsState(session, session.Workspace.CurrentSolution, initialState);
            }

            return session;
        }

        internal static void SetDocumentsState(DebuggingSession session, Solution solution, CommittedSolution.DocumentState state)
        {
            foreach (var project in solution.Projects)
            {
                foreach (var document in project.Documents)
                {
                    session.LastCommittedSolution.Test_SetDocumentState(document.Id, state);
                }
            }
        }

        private void VerifyReanalyzeInvocation(params object[] expectedArgs)
            => _mockDiagnosticService.Invocations.VerifyAndClear((nameof(IDiagnosticAnalyzerService.Reanalyze), expectedArgs));

        internal static Guid ReadModuleVersionId(Stream stream)
        {
            using (var peReader = new PEReader(stream))
            {
                var metadataReader = peReader.GetMetadataReader();
                var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
                return metadataReader.GetGuid(mvidHandle);
            }
        }

        private sealed class DesignTimeOnlyDocumentServiceProvider : IDocumentServiceProvider
        {
            private sealed class DesignTimeOnlyDocumentPropertiesService : DocumentPropertiesService
            {
                public static readonly DesignTimeOnlyDocumentPropertiesService Instance = new DesignTimeOnlyDocumentPropertiesService();
                public override bool DesignTimeOnly => true;
            }

            TService IDocumentServiceProvider.GetService<TService>()
                => DesignTimeOnlyDocumentPropertiesService.Instance is TService documentProperties ?
                    documentProperties : DefaultTextDocumentServiceProvider.Instance.GetService<TService>();
        }

        private (DebuggeeModuleInfo, Guid) EmitAndLoadLibraryToDebuggee(string source, ProjectId projectId, string assemblyName = "", string sourceFilePath = "test1.cs")
        {
            var sourceText = SourceText.From(new MemoryStream(Encoding.UTF8.GetBytes(source)), encoding: Encoding.UTF8, checksumAlgorithm: SourceHashAlgorithm.Sha256);
            var tree = SyntaxFactory.ParseSyntaxTree(sourceText, TestOptions.RegularPreview, sourceFilePath);
            var compilation = CSharpTestBase.CreateCompilationWithMscorlib40(tree, options: TestOptions.DebugDll, assemblyName: assemblyName);
            var (peImage, symReader) = SymReaderTestHelpers.EmitAndOpenDummySymReader(compilation, DebugInformationFormat.PortablePdb);

            var moduleMetadata = ModuleMetadata.CreateFromImage(peImage);
            var moduleId = moduleMetadata.GetModuleVersionId();
            var debuggeeModuleInfo = new DebuggeeModuleInfo(moduleMetadata, symReader);

            // "load" it to the debuggee:
            _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo = mvid => debuggeeModuleInfo;

            // associate the binaries with the project
            _mockCompilationOutputsService.Outputs.Add(projectId, new MockCompilationOutputs(moduleId));

            return (debuggeeModuleInfo, moduleId);
        }

        private SourceText CreateSourceTextFromFile(string path)
        {
            using var stream = File.OpenRead(path);
            return SourceText.From(stream, Encoding.UTF8, SourceHashAlgorithm.Sha256);
        }


        [Fact]
        public void ActiveStatementTracking()
        {
            using (var workspace = new TestWorkspace())
            {
                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession();
                _mockActiveStatementTrackingService.Verify(ts => ts.StartTracking(It.IsAny<EditSession>()), Times.Once());

                service.EndEditSession();
                _mockActiveStatementTrackingService.Verify(ts => ts.EndTracking(), Times.Once());

                service.EndDebuggingSession();

                _mockActiveStatementTrackingService.Verify(ts => ts.StartTracking(It.IsAny<EditSession>()), Times.Once());
                _mockActiveStatementTrackingService.Verify(ts => ts.EndTracking(), Times.Once());
            }
        }

        [Fact]
        public async Task RunMode_ProjectThatDoesNotSupportEnC()
        {
            var exportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithPart(typeof(DummyLanguageService)));

            using (var workspace = new TestWorkspace(exportProvider: exportProviderFactory.CreateExportProvider()))
            {
                var solution = workspace.CurrentSolution;
                var project = solution.AddProject("dummy_proj", "dummy_proj", DummyLanguageService.LanguageName);
                var document = project.AddDocument("test", SourceText.From("dummy1"));
                workspace.ChangeSolution(document.Project.Solution);

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                // no changes:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                // change the source:
                workspace.ChangeDocument(document1.Id, SourceText.From("dummy2"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);
            }
        }

        [Fact]
        public async Task RunMode_DesignTimeOnlyDocument()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }");

            var project = workspace.CurrentSolution.Projects.Single();
            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(project.Id),
                name: "design-time-only.cs",
                folders: Array.Empty<string>(),
                sourceCodeKind: SourceCodeKind.Regular,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class C2 {}"), VersionStamp.Create(), "design-time-only.cs")),
                filePath: "design-time-only.cs",
                isGenerated: false,
                documentServiceProvider: new DesignTimeOnlyDocumentServiceProvider());

            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path).AddDocument(documentInfo));
            _mockCompilationOutputsService.Outputs.Add(project.Id, new CompilationOutputFiles(moduleFile.Path));

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            // update a design-time-only source file:
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentInfo.Id);
            workspace.ChangeDocument(document1.Id, SourceText.From("class UpdatedC2 {}"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentInfo.Id);

            // no updates:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // validate solution update status and emit - changes made in design-time-only documents are ignored:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

            service.EndDebuggingSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task RunMode_ProjectNotBuilt()
        {
            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }"))
            {
                var service = CreateEditAndContinueService(workspace);

                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsService.Outputs.Add(project.Id, new MockCompilationOutputs(Guid.Empty));

                StartDebuggingSession(service);

                // no changes:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                // change the source:
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);
            }
        }

        [Fact]
        public async Task RunMode_ErrorReadingFile()
        {
            var moduleFile = Temp.CreateFile();

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }"))
            {
                var project = workspace.CurrentSolution.Projects.Single();

                _mockCompilationOutputsService.Outputs.Add(project.Id, new CompilationOutputFiles(moduleFile.Path));

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                // no changes:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                // change the source:
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
                diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                // validate solution update status and emit - changes made during run mode are ignored:
                var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
                Assert.Empty(deltas);
            }
        }

        [Fact]
        public async Task RunMode_DocumentOutOfSync()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }");
            var service = CreateEditAndContinueService(workspace);

            var project = workspace.CurrentSolution.Projects.Single();
            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));
            _mockCompilationOutputsService.Outputs.Add(project.Id, new CompilationOutputFiles(moduleFile.Path));

            var document1 = project.Documents.Single();

            var debuggingSession = StartDebuggingSession(service);
            debuggingSession.LastCommittedSolution.Test_SetDocumentState(document1.Id, CommittedSolution.DocumentState.OutOfSync);

            // no changes:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // change the source:
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void RenamedMethod() { System.Console.WriteLine(1); } }"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // no Rude Edits, since the document is out-of-sync
            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // the document is now in-sync (a file watcher observed a change and updated the status):
            debuggingSession.LastCommittedSolution.Test_SetDocumentState(document2.Id, CommittedSolution.DocumentState.MatchesDebuggee);

            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "ENC1003" }, diagnostics.Select(d => d.Id));

            service.EndDebuggingSession();

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task RunMode_FileAdded()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }");

            var project = workspace.CurrentSolution.Projects.Single();
            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            _mockCompilationOutputsService.Outputs.Add(project.Id, new CompilationOutputFiles(moduleFile.Path));

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            // add a source file:
            var document2 = project.AddDocument("file2.cs", SourceText.From("class C2 {}"));
            workspace.ChangeSolution(document2.Project.Solution);

            // no changes in document1:
            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document1, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics1);

            // update in document2:
            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "ENC1003" }, diagnostics2.Select(d => d.Id));

            // validate solution update status and emit - changes made during run mode are ignored:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

            service.EndDebuggingSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray.Create(document2.Id), false);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task RunMode_Diagnostics()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }"))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));

                _mockCompilationOutputsService.Outputs.Add(project.Id, new CompilationOutputFiles(moduleFile.Path));

                var service = CreateEditAndContinueService(workspace);

                var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

                StartDebuggingSession(service);

                // no changes:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

                // change the source:
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // validate solution update status and emit - changes made during run mode are ignored:
                solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
                Assert.Empty(deltas);

                diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(new[] { "ENC1003" }, diagnostics.Select(d => d.Id));

                service.EndDebuggingSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray.Create(document2.Id), false);

                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
                }, _telemetryLog);
            }
        }

        [Fact]
        public async Task RunMode_DifferentDocumentWithSameContent()
        {
            var source = "class C1 { void M1() { System.Console.WriteLine(1); } }";
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var workspace = TestWorkspace.CreateCSharp(source);

            var project = workspace.CurrentSolution.Projects.Single();
            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));
            _mockCompilationOutputsService.Outputs.Add(project.Id, new CompilationOutputFiles(moduleFile.Path));

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            // update the document
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From(source));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            Assert.Equal(document1.Id, document2.Id);
            Assert.NotSame(document1, document2);

            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics2);

            // validate solution update status and emit - changes made during run mode are ignored:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

            service.EndDebuggingSession();

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_ProjectThatDoesNotSupportEnC()
        {
            var exportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithPart(typeof(DummyLanguageService)));

            using (var workspace = new TestWorkspace(exportProvider: exportProviderFactory.CreateExportProvider()))
            {
                var solution = workspace.CurrentSolution;
                var project = solution.AddProject("dummy_proj", "dummy_proj", DummyLanguageService.LanguageName);
                var document = project.AddDocument("test", SourceText.From("dummy1"));
                workspace.ChangeSolution(document.Project.Solution);

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);
                service.StartEditSession();

                // change the source:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("dummy2"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // validate solution update status and emit:
                var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
                Assert.Empty(deltas);
            }
        }

        [Fact]
        public async Task BreakMode_DesignTimeOnlyDocument_Dynamic()
        {
            var exportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithPart(typeof(DummyLanguageService)));

            using var workspace = TestWorkspace.CreateCSharp("class C {}");

            var project = workspace.CurrentSolution.Projects.Single();
            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(project.Id),
                name: "design-time-only.cs",
                folders: Array.Empty<string>(),
                sourceCodeKind: SourceCodeKind.Regular,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class D {}"), VersionStamp.Create(), "design-time-only.cs")),
                filePath: "design-time-only.cs",
                isGenerated: false,
                documentServiceProvider: new DesignTimeOnlyDocumentServiceProvider());

            var solution = workspace.CurrentSolution.AddDocument(documentInfo);
            workspace.ChangeSolution(solution);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);
            service.StartEditSession();

            // change the source:
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentInfo.Id);
            workspace.ChangeDocument(document1.Id, SourceText.From("class E {}"));

            // validate solution update status and emit:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            Assert.Empty(deltas);
        }

        [Fact]
        public async Task BreakMode_DesignTimeOnlyDocument_Wpf()
        {
            var sourceA = "class A { public void M() { } }";
            var sourceB = "class B { public void M() { } }";
            var sourceC = "class C { public void M() { } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs").WriteAllText(sourceA);

            using var workspace = new TestWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var documentA = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("a.cs", SourceText.From(sourceA, Encoding.UTF8), filePath: sourceFile.Path);

            var documentB = documentA.Project.
                AddDocument("b.g.i.cs", SourceText.From(sourceB, Encoding.UTF8), filePath: "b.g.i.cs");

            var documentC = documentB.Project.
                AddDocument("c.g.i.vb", SourceText.From(sourceC, Encoding.UTF8), filePath: "c.g.i.vb");

            workspace.ChangeSolution(documentC.Project.Solution);

            // only compile A; B and C are design-time-only:
            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(sourceA, documentA.Project.Id, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession();

            // change the source (rude edit):
            workspace.ChangeDocument(documentB.Id, SourceText.From("class B { public void RenamedMethod() { } }"));
            workspace.ChangeDocument(documentC.Id, SourceText.From("class C { public void RenamedMethod() { } }"));
            var documentB2 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentB.Id);
            var documentC2 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentC.Id);

            // no Rude Edits reported:
            Assert.Empty(await service.GetDocumentDiagnosticsAsync(documentB2, CancellationToken.None).ConfigureAwait(false));
            Assert.Empty(await service.GetDocumentDiagnosticsAsync(documentC2, CancellationToken.None).ConfigureAwait(false));

            // validate solution update status and emit:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            Assert.Empty(_emitDiagnosticsUpdated);

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Fact]
        public async Task BreakMode_ErrorReadingFile()
        {
            var moduleFile = Temp.CreateFile();

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }"))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsService.Outputs.Add(project.Id, new CompilationOutputFiles(moduleFile.Path));

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);
                service.StartEditSession();

                // change the source:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                // validate solution update status and emit:
                var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

                Assert.Empty(_emitDiagnosticsUpdated);
                Assert.Equal(0, _emitDiagnosticsClearedCount);

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
                Assert.Empty(deltas);

                Assert.Equal(1, _emitDiagnosticsClearedCount);
                var eventArgs = _emitDiagnosticsUpdated.Single();
                Assert.Null(eventArgs.DocumentId);
                Assert.Equal(project.Id, eventArgs.ProjectId);
                AssertEx.Equal(new[] { "ENC1001" }, eventArgs.Diagnostics.Select(d => d.Id));
                _emitDiagnosticsUpdated.Clear();
                _emitDiagnosticsClearedCount = 0;

                service.EndEditSession();
                Assert.Empty(_emitDiagnosticsUpdated);
                Assert.Equal(0, _emitDiagnosticsClearedCount);

                service.EndDebuggingSession();
                Assert.Empty(_emitDiagnosticsUpdated);
                Assert.Equal(1, _emitDiagnosticsClearedCount);

                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                    "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC1001"
                }, _telemetryLog);
            }
        }

        [Fact]
        public async Task BreakMode_FileAdded()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }");

            var project = workspace.CurrentSolution.Projects.Single();
            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            _mockDebugeeModuleMetadataProvider.IsEditAndContinueAvailable = (Guid guid, out int errorCode, out string localizedMessage) =>
            {
                errorCode = 123;
                localizedMessage = "*message*";
                return false;
            };

            _mockCompilationOutputsService.Outputs.Add(project.Id, new CompilationOutputFiles(moduleFile.Path));

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);
            service.StartEditSession();

            // add a source file:
            var document2 = project.AddDocument("file2.cs", SourceText.From("class C2 {}"));
            workspace.ChangeSolution(document2.Project.Solution);

            // update in document2:
            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "ENC2123" }, diagnostics2.Select(d => d.Id));

            // validate solution update status and emit - changes made during run mode are ignored:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);

            service.EndEditSession();
            service.EndDebuggingSession();

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=1"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_ModuleDisallowsEditAndContinue()
        {
            var moduleId = Guid.NewGuid();

            var source1 = @"
class C1 
{ 
  void M() 
  {
    System.Console.WriteLine(1); 
    System.Console.WriteLine(2); 
    System.Console.WriteLine(3); 
  } 
}";
            var source2 = @"
class C1 
{ 

  void M() 
  {
    System.Console.WriteLine(9); 
    System.Console.WriteLine(); 
    System.Console.WriteLine(30); 
  } 
}";
            var expectedMessage = "ENC2123: " + string.Format(FeaturesResources.EditAndContinueDisallowedByProject, "Test", "*message*");

            var expectedDiagnostics = new[]
            {
                "[17..19): " + expectedMessage,
                "[66..67): " + expectedMessage,
                "[101..101): " + expectedMessage,
                "[136..137): " + expectedMessage,
            };

            string inspectDiagnostic(Diagnostic d)
                => $"{d.Location.SourceSpan}: {d.Id}: {d.GetMessage()}";

            using (var workspace = TestWorkspace.CreateCSharp(source1))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsService.Outputs.Add(project.Id, new MockCompilationOutputs(moduleId));

                bool isEditAndContinueAvailableInvocationAllowed = true;
                _mockDebugeeModuleMetadataProvider.IsEditAndContinueAvailable = (Guid guid, out int errorCode, out string localizedMessage) =>
                {
                    Assert.True(isEditAndContinueAvailableInvocationAllowed);

                    Assert.Equal(moduleId, guid);
                    errorCode = 123;
                    localizedMessage = "*message*";
                    return false;
                };

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                // change the source:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From(source2));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                isEditAndContinueAvailableInvocationAllowed = true;
                var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(expectedDiagnostics, diagnostics1.Select(inspectDiagnostic));

                // the diagnostic should be cached and we should not invoke isEditAndContinueAvailable again:
                isEditAndContinueAvailableInvocationAllowed = false;
                var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(expectedDiagnostics, diagnostics2.Select(inspectDiagnostic));

                // invalidate cache:
                service.Test_GetEditSession().ModuleInstanceLoadedOrUnloaded(moduleId);
                isEditAndContinueAvailableInvocationAllowed = true;
                var diagnostics3 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(expectedDiagnostics, diagnostics3.Select(inspectDiagnostic));

                // validate solution update status and emit:
                var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
                Assert.Empty(deltas);

                service.EndEditSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray.Create(document2.Id), false);

                service.EndDebuggingSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                AssertEx.Equal(new[] { moduleId }, _modulesPreparedForUpdate);

                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                    "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC2123"
                }, _telemetryLog);
            }
        }

        [Fact]
        public async Task BreakMode_RudeEdits()
        {
            var moduleId = Guid.NewGuid();

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }"))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsService.Outputs.Add(project.Id, new MockCompilationOutputs(moduleId));

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                // change the source (rude edit):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M1() { System.Console.WriteLine(1); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                    diagnostics1.Select(d => $"{d.Id}: {d.GetMessage()}"));

                // validate solution update status and emit:
                var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
                Assert.Empty(deltas);

                service.EndEditSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray.Create(document2.Id), false);

                service.EndDebuggingSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                AssertEx.Equal(new[] { moduleId }, _modulesPreparedForUpdate);

                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=0",
                    "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=2|RudeEditKind=20|RudeEditSyntaxKind=8875|RudeEditBlocking=True"
                }, _telemetryLog);
            }
        }

        [Fact]
        public async Task BreakMode_RudeEdits_DocumentOutOfSync()
        {
            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var workspace = TestWorkspace.CreateCSharp(source1);

            var project = workspace.CurrentSolution.Projects.Single();
            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, project.Id);
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service);
            debuggingSession.LastCommittedSolution.Test_SetDocumentState(document1.Id, CommittedSolution.DocumentState.OutOfSync);

            service.StartEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // change the source (rude edit):
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void RenamedMethod() { System.Console.WriteLine(1); } }"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // no Rude Edits, since the document is out-of-sync
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // validate solution update status and emit:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            Assert.Empty(deltas);

            AssertEx.Equal(
                new[] { "ENC1005: " + string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, "test1.cs") },
                _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => $"{d.Id}: {d.Message}"));

            _emitDiagnosticsUpdated.Clear();
            _emitDiagnosticsClearedCount = 0;

            // the document is now in-sync (a file watcher observed a change and updated the status):
            debuggingSession.LastCommittedSolution.Test_SetDocumentState(document1.Id, CommittedSolution.DocumentState.MatchesDebuggee);

            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(
                new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            // validate solution update status and emit:
            solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);

            (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            Assert.Empty(deltas);

            service.EndEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray.Create(document2.Id), false);

            service.EndDebuggingSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            AssertEx.Equal(new[] { moduleId }, _modulesPreparedForUpdate);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=0",
                "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=2|RudeEditKind=20|RudeEditSyntaxKind=8875|RudeEditBlocking=True"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_RudeEdits_DocumentWithoutSequencePoints()
        {
            var source1 = "abstract class C { public abstract void M(); }";
            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1);

            using var workspace = new TestWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, project.Id, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);

            // do not initialize the document state - we will detect the state based on the PDB content.
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession();

            // change the source (rude edit since the base document content matches the PDB checksum, so the document is not out-of-sync):
            workspace.ChangeDocument(document1.Id, SourceText.From("abstract class C { public abstract void M(); public abstract void N(); }"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // Rude Edits reported:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(
                new[] { "ENC0023: " + string.Format(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            // validate solution update status and emit:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            Assert.Empty(deltas);

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Fact]
        public async Task BreakMode_SyntaxError()
        {
            var moduleId = Guid.NewGuid();

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }"))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsService.Outputs.Add(project.Id, new MockCompilationOutputs(moduleId));

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                // change the source (compilation error):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { "));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // compilation errors are not reported via EnC diagnostic analyzer:
                var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Empty(diagnostics1);

                // validate solution update status and emit:
                var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
                Assert.Empty(deltas);

                service.EndEditSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                service.EndDebuggingSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                AssertEx.Equal(new[] { moduleId }, _modulesPreparedForUpdate);

                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=True|HadRudeEdits=False|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0"
                }, _telemetryLog);
            }
        }

        [Fact]
        public async Task BreakMode_SemanticError()
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var workspace = TestWorkspace.CreateCSharp(sourceV1);

            var project = workspace.CurrentSolution.Projects.Single();
            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(sourceV1, project.Id);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            service.StartEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // change the source (compilation error):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { int i = 0L; System.Console.WriteLine(i); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // compilation errors are not reported via EnC diagnostic analyzer:
            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics1);

            // The EnC analyzer does not check for and block on all semantic errors as they are already reported by diagnostic analyzer.
            // Blocking update on semantic errors would be possible, but the status check is only an optimization to avoid emitting.
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatus);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            Assert.Empty(deltas);

            // TODO: https://github.com/dotnet/roslyn/issues/36061 
            // Semantic errors should not be reported in emit diagnostics.
            AssertEx.Equal(new[] { "CS0266" }, _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => d.Id));
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            _emitDiagnosticsUpdated.Clear();
            _emitDiagnosticsClearedCount = 0;

            service.EndEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            service.EndDebuggingSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            AssertEx.Equal(new[] { moduleId }, _modulesPreparedForUpdate);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=CS0266"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_FileStatus_CompilationError()
        {
            using (var workspace = TestWorkspace.CreateCSharp("class Program { void Main() { System.Console.WriteLine(1); } }"))
            {
                var solution = workspace.CurrentSolution;
                var projectA = solution.Projects.Single();

                workspace.ChangeSolution(solution.
                    AddProject("B", "B", "C#").
                    AddDocument("Common.cs", "class Common {}", filePath: "Common.cs").Project.
                    AddDocument("B.cs", "class B {}", filePath: "B.cs").Project.Solution.
                    AddProject("C", "C", "C#").
                    AddDocument("Common.cs", "class Common {}", filePath: "Common.cs").Project.
                    AddDocument("C.cs", "class C {}", filePath: "C.cs").Project.Solution);

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);
                service.StartEditSession();

                // change C.cs to have a compilation error:
                var projectC = workspace.CurrentSolution.GetProjectsByName("C").Single();
                var documentC = projectC.Documents.Single(d => d.Name == "C.cs");
                workspace.ChangeDocument(documentC.Id, SourceText.From("class C { void M() { "));

                // Common.cs is included in projects B and C. Both of these projects must have no errors, otherwise update is blocked.
                var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: "Common.cs", CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);

                // No changes in project containing file B.cs.
                solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: "B.cs", CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

                // All projects must have no errors.
                solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);

                service.EndEditSession();
                service.EndDebuggingSession();
            }
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_EmitError()
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var workspace = TestWorkspace.CreateCSharp(sourceV1);

            var project = workspace.CurrentSolution.Projects.Single();
            EmitAndLoadLibraryToDebuggee(sourceV1, project.Id);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            service.StartEditSession();
            var editSession = service.Test_GetEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // change the source (valid edit but passing no encoding to emulate emit error):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", encoding: null));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatus);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "CS8055" }, _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => d.Id));
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            _emitDiagnosticsUpdated.Clear();
            _emitDiagnosticsClearedCount = 0;

            // no emitted delta:
            Assert.Empty(deltas);

            // no pending update:
            Assert.Null(service.Test_GetPendingSolutionUpdate());

            Assert.Throws<InvalidOperationException>(() => service.CommitSolutionUpdate());
            Assert.Throws<InvalidOperationException>(() => service.DiscardSolutionUpdate());

            // no change in non-remappable regions since we didn't have any active statements:
            Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

            // no open module readers since we didn't defer any module update:
            Assert.Empty(editSession.DebuggingSession.GetBaselineModuleReaders());

            // solution update status after discarding an update (still has update ready):
            var commitedUpdateSolutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Ready, commitedUpdateSolutionStatus);

            service.EndEditSession();
            Assert.Empty(_emitDiagnosticsUpdated);
            Assert.Equal(0, _emitDiagnosticsClearedCount);
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            service.EndDebuggingSession();
            Assert.Empty(_emitDiagnosticsUpdated);
            Assert.Equal(1, _emitDiagnosticsClearedCount);
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=CS8055"
            }, _telemetryLog);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BreakMode_ValidSignificantChange_ApplyBeforeFileWatcherEvent(bool saveDocument)
        {
            // Scenarios tested:
            //
            // SaveDocument=true
            // workspace:     --V0-------------|--V2--------|------------|
            // file system:   --V0---------V1--|-----V2-----|------------|
            //                   \--build--/   F5    ^      F10  ^       F10 
            //                                       save        file watcher: no-op
            // SaveDocument=false
            // workspace:     --V0-------------|--V2--------|----V1------|
            // file system:   --V0---------V1--|------------|------------|
            //                   \--build--/   F5           F10  ^       F10
            //                                                   file watcher: workspace update

            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(source1);

            using var workspace = new TestWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From("class C1 { void M() { System.Console.WriteLine(0); } }", Encoding.UTF8), filePath: sourceFile.Path);

            var documentId = document1.Id;

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, project.Id, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession();

            // The user opens the source file and changes the source before Roslyn receives file watcher event.
            var source2 = "class C1 { void M() { System.Console.WriteLine(2); } }";
            workspace.ChangeDocument(documentId, SourceText.From(source2, Encoding.UTF8));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // Save the document:
            if (saveDocument)
            {
                await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(documentId, debuggingSession.CancellationToken).ConfigureAwait(false);
                sourceFile.WriteAllText(source2);
            }

            // EnC service queries for a document, which triggers read of the source file from disk.
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatus);
            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);
            service.CommitSolutionUpdate();

            service.EndEditSession();

            service.StartEditSession();

            // file watcher updates the workspace:
            workspace.ChangeDocument(documentId, CreateSourceTextFromFile(sourceFile.Path));
            var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);

            if (saveDocument)
            {
                Assert.Equal(SolutionUpdateStatus.None, solutionStatus);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            }
            else
            {
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatus);
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);
            }

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_FileUpdateBeforeDebuggingSessionStarts()
        {
            // workspace:     --V0--------------V2-------|--------V3---------------V1--------------|
            // file system:   --V0---------V1-----V2-----|---------------------------V1------------|
            //                   \--build--/      ^save  F5   ^      ^F10 (blocked)  ^save         F10 (ok)
            //                                                file watcher: no-op

            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var source2 = "class C1 { void M() { System.Console.WriteLine(2); } }";
            var source3 = "class C1 { void M() { System.Console.WriteLine(3); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(source2);

            using var workspace = new TestWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document2 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source2, Encoding.UTF8), filePath: sourceFile.Path);

            var documentId = document2.Id;

            var project = document2.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, project.Id, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession();

            // user edits the file:
            workspace.ChangeDocument(documentId, SourceText.From(source3, Encoding.UTF8));
            var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // EnC service queries for a document, but the source file on disk doesn't match the PDB

            // We don't report rude edits for out-of-sync documents:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document3, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics);

            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatus);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);

            AssertEx.Equal(
                new[] { "ENC1005: " + string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFile.Path) },
                _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => $"{d.Id}: {d.Message}"));

            _emitDiagnosticsUpdated.Clear();
            _emitDiagnosticsClearedCount = 0;

            // undo:
            workspace.ChangeDocument(documentId, SourceText.From(source1, Encoding.UTF8));

            // save (note that this call will fail to match the content with the PDB since it uses the content prior to the actual file write)
            await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(documentId, debuggingSession.CancellationToken).ConfigureAwait(false);
            var (doc, state) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(documentId, CancellationToken.None).ConfigureAwait(false);
            Assert.Null(doc);
            Assert.Equal(CommittedSolution.DocumentState.OutOfSync, state);
            sourceFile.WriteAllText(source1);

            solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);

            // the content actually hasn't changed:
            Assert.Equal(SolutionUpdateStatus.None, solutionStatus);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_DocumentOutOfSync2()
        {
            var sourceOnDisk = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(sourceOnDisk);

            using var workspace = new TestWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From("class C1 { void M() { System.Console.WriteLine(0); } }", Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(sourceOnDisk, project.Id, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // no changes have been made to the project
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatus);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            Assert.Empty(deltas);

            Assert.Empty(_emitDiagnosticsUpdated);

            _emitDiagnosticsUpdated.Clear();
            _emitDiagnosticsClearedCount = 0;

            // a file watcher observed a change and updated the document, so it now reflects the content on disk (the code that we compiled):
            workspace.ChangeDocument(document1.Id, SourceText.From(sourceOnDisk, Encoding.UTF8));
            var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document3, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics2);

            // the content of the file is now exactly the same as the compiled document, so there is no change to be applied:
            var solutionStatus2 = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatus2);

            var (solutionStatusEmit2, deltas2) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit2);

            service.EndEditSession();

            // no diagnostics reported via a document analyzer
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            service.EndDebuggingSession();

            // no diagnostics reported via a document analyzer
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            Assert.Empty(_modulesPreparedForUpdate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BreakMode_ValidSignificantChange_EmitSuccessful(bool commitUpdate)
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var workspace = TestWorkspace.CreateCSharp(sourceV1);

            var project = workspace.CurrentSolution.Projects.Single();
            var (debuggeeModuleInfo, moduleId) = EmitAndLoadLibraryToDebuggee(sourceV1, project.Id);

            var diagnosticUpdateSource = new EditAndContinueDiagnosticUpdateSource();
            var emitDiagnosticsUpdated = new List<DiagnosticsUpdatedArgs>();
            diagnosticUpdateSource.DiagnosticsUpdated += (object sender, DiagnosticsUpdatedArgs args) => emitDiagnosticsUpdated.Add(args);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            service.StartEditSession();
            var editSession = service.Test_GetEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // change the source (valid edit):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatus);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(emitDiagnosticsUpdated);
            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);

            // check emitted delta:
            var delta = deltas.Single();
            Assert.Empty(delta.ActiveStatementsInUpdatedMethods);
            Assert.NotEmpty(delta.IL.Value);
            Assert.NotEmpty(delta.Metadata.Bytes);
            Assert.NotEmpty(delta.Pdb.Stream);
            Assert.Equal(0x06000001, delta.Pdb.UpdatedMethods.Single());
            Assert.Equal(moduleId, delta.Mvid);
            Assert.Empty(delta.NonRemappableRegions);
            Assert.Empty(delta.LineEdits);

            // the update should be stored on the service:
            var pendingUpdate = service.Test_GetPendingSolutionUpdate();
            var (baselineProjectId, newBaseline) = pendingUpdate.EmitBaselines.Single();
            AssertEx.Equal(deltas, pendingUpdate.Deltas);
            Assert.Empty(pendingUpdate.ModuleReaders);
            Assert.Equal(project.Id, baselineProjectId);
            Assert.Equal(moduleId, newBaseline.OriginalMetadata.GetModuleVersionId());

            if (commitUpdate)
            {
                // all update providers either provided updates or had no change to apply:
                service.CommitSolutionUpdate();

                Assert.Null(service.Test_GetPendingSolutionUpdate());

                // no change in non-remappable regions since we didn't have any active statements:
                Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

                // no open module readers since we didn't defer any module update:
                Assert.Empty(editSession.DebuggingSession.GetBaselineModuleReaders());

                // verify that baseline is added:
                Assert.Same(newBaseline, editSession.DebuggingSession.Test_GetProjectEmitBaseline(project.Id));

                // solution update status after committing an update:
                var commitedUpdateSolutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, commitedUpdateSolutionStatus);
            }
            else
            {
                // another update provider blocked the update:
                service.DiscardSolutionUpdate();

                Assert.Null(service.Test_GetPendingSolutionUpdate());

                // solution update status after committing an update:
                var discardedUpdateSolutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Ready, discardedUpdateSolutionStatus);
            }

            service.EndEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            service.EndDebuggingSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            AssertEx.Equal(new[] { moduleId }, _modulesPreparedForUpdate);

            // the debugger disposes the module metadata and SymReader:
            debuggeeModuleInfo.Dispose();
            Assert.True(debuggeeModuleInfo.Metadata.IsDisposed);
            Assert.Null(debuggeeModuleInfo.SymReader);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0",
            }, _telemetryLog);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BreakMode_ValidSignificantChange_EmitSuccessful_UpdateDeferred(bool commitUpdate)
        {
            var dir = Temp.CreateDirectory();

            var sourceV1 = "class C1 { void M1() { int a = 1; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(1); } }";
            var compilationV1 = CSharpTestBase.CreateCompilationWithMscorlib40(sourceV1, options: TestOptions.DebugDll, assemblyName: "lib");

            var pdbStream = new MemoryStream();
            var peImage = compilationV1.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;

            var moduleMetadata = ModuleMetadata.CreateFromImage(peImage);
            var moduleFile = dir.CreateFile("lib.dll").WriteAllBytes(peImage);
            var pdbFile = dir.CreateFile("lib.pdb").WriteAllBytes(pdbStream.ToArray());
            var moduleId = moduleMetadata.GetModuleVersionId();

            using var workspace = TestWorkspace.CreateCSharp(sourceV1);

            var project = workspace.CurrentSolution.Projects.Single();
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            _mockCompilationOutputsService.Outputs.Add(project.Id, new CompilationOutputFiles(moduleFile.Path, pdbFile.Path));

            // set up an active statement in the first method, so that we can test preservation of local signature.
            _mockActiveStatementProvider = new Mock<IActiveStatementProvider>(MockBehavior.Strict);
            _mockActiveStatementProvider.Setup(p => p.GetActiveStatementsAsync(It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(ImmutableArray.Create(new ActiveStatementDebugInfo(
                    new ActiveInstructionId(moduleId, methodToken: 0x06000001, methodVersion: 1, ilOffset: 0),
                    documentNameOpt: document1.Name,
                    linePositionSpan: new LinePositionSpan(new LinePosition(0, 15), new LinePosition(0, 16)),
                    threadIds: ImmutableArray.Create(Guid.NewGuid()),
                    ActiveStatementFlags.IsLeafFrame))));

            // module not loaded
            _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo = mvid => null;

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            service.StartEditSession();
            var editSession = service.Test_GetEditSession();

            // change the source (valid edit):
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M1() { int a = 1; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // validate solution update status and emit:
            var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatus);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);

            // delta to apply:
            var delta = deltas.Single();
            Assert.Empty(delta.ActiveStatementsInUpdatedMethods);
            Assert.NotEmpty(delta.IL.Value);
            Assert.NotEmpty(delta.Metadata.Bytes);
            Assert.NotEmpty(delta.Pdb.Stream);
            Assert.Equal(0x06000002, delta.Pdb.UpdatedMethods.Single());
            Assert.Equal(moduleId, delta.Mvid);
            Assert.Empty(delta.NonRemappableRegions);
            Assert.Empty(delta.LineEdits);

            // the update should be stored on the service:
            var pendingUpdate = service.Test_GetPendingSolutionUpdate();
            var (baselineProjectId, newBaseline) = pendingUpdate.EmitBaselines.Single();

            var readers = pendingUpdate.ModuleReaders;
            Assert.Equal(2, readers.Length);
            Assert.NotNull(readers[0]);
            Assert.NotNull(readers[1]);

            Assert.Equal(project.Id, baselineProjectId);
            Assert.Equal(moduleId, newBaseline.OriginalMetadata.GetModuleVersionId());

            if (commitUpdate)
            {
                service.CommitSolutionUpdate();
                Assert.Null(service.Test_GetPendingSolutionUpdate());

                // no change in non-remappable regions since we didn't have any active statements:
                Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

                // deferred module readers tracked:
                var baselineReaders = editSession.DebuggingSession.GetBaselineModuleReaders();
                Assert.Equal(2, baselineReaders.Length);
                Assert.Same(readers[0], baselineReaders[0]);
                Assert.Same(readers[1], baselineReaders[1]);

                // verify that baseline is added:
                Assert.Same(newBaseline, editSession.DebuggingSession.Test_GetProjectEmitBaseline(project.Id));

                // solution update status after committing an update:
                var commitedUpdateSolutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, commitedUpdateSolutionStatus);

                service.EndEditSession();

                // make another update:
                service.StartEditSession();

                // Update M1 - this method has an active statement, so we will attempt to preserve the local signature.
                // Since the method hasn't been edited before we'll read the baseline PDB to get the signature token.
                // This validates that the Portable PDB reader can be used (and is not disposed) for a second generation edit.
                var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document3.Id, SourceText.From("class C1 { void M1() { int a = 3; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);

                service.EndEditSession();
                service.EndDebuggingSession();

                // open module readers should be disposed when the debugging session ends:
                Assert.Throws<ObjectDisposedException>(() => ((MetadataReaderProvider)readers.First(r => r is MetadataReaderProvider)).GetMetadataReader());
                Assert.Throws<ObjectDisposedException>(() => ((DebugInformationReaderProvider)readers.First(r => r is DebugInformationReaderProvider)).CreateEditAndContinueMethodDebugInfoReader());
            }
            else
            {
                service.DiscardSolutionUpdate();
                Assert.Null(service.Test_GetPendingSolutionUpdate());

                // no open module readers since we didn't defer any module update:
                Assert.Empty(editSession.DebuggingSession.GetBaselineModuleReaders());

                Assert.Throws<ObjectDisposedException>(() => ((MetadataReaderProvider)readers.First(r => r is MetadataReaderProvider)).GetMetadataReader());
                Assert.Throws<ObjectDisposedException>(() => ((DebugInformationReaderProvider)readers.First(r => r is DebugInformationReaderProvider)).CreateEditAndContinueMethodDebugInfoReader());

                service.EndEditSession();
                service.EndDebuggingSession();
            }
        }

        /// <summary>
        /// Emulates two updates to Multi-TFM project.
        /// </summary>
        [Fact]
        public async Task TwoUpdatesWithLoadedAndUnloadedModule()
        {
            var dir = Temp.CreateDirectory();

            var source1 = "class A { void M() { System.Console.WriteLine(1); } }";
            var source2 = "class A { void M() { System.Console.WriteLine(2); } }";
            var source3 = "class A { void M() { System.Console.WriteLine(3); } }";

            var compilationA = CSharpTestBase.CreateCompilationWithMscorlib40(source1, options: TestOptions.DebugDll, assemblyName: "A");
            var compilationB = CSharpTestBase.CreateCompilationWithMscorlib45(source1, options: TestOptions.DebugDll, assemblyName: "B");

            var (peImageA, symReaderA) = SymReaderTestHelpers.EmitAndOpenDummySymReader(compilationA, DebugInformationFormat.PortablePdb);

            var moduleMetadataA = ModuleMetadata.CreateFromImage(peImageA);
            var moduleFileA = Temp.CreateFile("A.dll").WriteAllBytes(peImageA);
            var moduleIdA = moduleMetadataA.GetModuleVersionId();
            var debuggeeModuleInfoA = new DebuggeeModuleInfo(moduleMetadataA, symReaderA);

            var pdbStreamB = new MemoryStream();
            var peImageB = compilationB.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb), pdbStream: pdbStreamB);
            pdbStreamB.Position = 0;

            var moduleMetadataB = ModuleMetadata.CreateFromImage(peImageB);
            var moduleFileB = dir.CreateFile("B.dll").WriteAllBytes(peImageB);
            var pdbFileB = dir.CreateFile("B.pdb").WriteAllBytes(pdbStreamB.ToArray());
            var moduleIdB = moduleMetadataB.GetModuleVersionId();

            using (var workspace = TestWorkspace.CreateCSharp(source1))
            {
                var solution = workspace.CurrentSolution;
                var projectA = solution.Projects.Single();
                var projectB = solution.AddProject("B", "A", "C#").AddMetadataReferences(projectA.MetadataReferences).AddDocument("DocB", source1, filePath: "DocB.cs").Project;
                workspace.ChangeSolution(projectB.Solution);

                _mockCompilationOutputsService.Outputs.Add(projectA.Id, new CompilationOutputFiles(moduleFileA.Path));
                _mockCompilationOutputsService.Outputs.Add(projectB.Id, new CompilationOutputFiles(moduleFileB.Path, pdbFileB.Path));

                // only module A is loaded
                _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo =
                    mvid => (mvid == moduleIdA) ? debuggeeModuleInfoA : null;

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession();
                var editSession = service.Test_GetEditSession();

                //
                // First update.
                //

                workspace.ChangeDocument(projectA.Documents.Single().Id, SourceText.From(source2, Encoding.UTF8));
                workspace.ChangeDocument(projectB.Documents.Single().Id, SourceText.From(source2, Encoding.UTF8));

                // validate solution update status and emit:
                var solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatus);

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);

                var deltaA = deltas.Single(d => d.Mvid == moduleIdA);
                var deltaB = deltas.Single(d => d.Mvid == moduleIdB);
                Assert.Equal(2, deltas.Length);

                // the update should be stored on the service:
                var pendingUpdate = service.Test_GetPendingSolutionUpdate();
                var (_, newBaselineA1) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectA.Id);
                var (_, newBaselineB1) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectB.Id);

                var baselineA0 = newBaselineA1.GetInitialEmitBaseline();
                var baselineB0 = newBaselineB1.GetInitialEmitBaseline();

                var readers = pendingUpdate.ModuleReaders;
                Assert.Equal(2, readers.Length);
                Assert.NotNull(readers[0]);
                Assert.NotNull(readers[1]);

                Assert.Equal(moduleIdA, newBaselineA1.OriginalMetadata.GetModuleVersionId());
                Assert.Equal(moduleIdB, newBaselineB1.OriginalMetadata.GetModuleVersionId());

                service.CommitSolutionUpdate();
                Assert.Null(service.Test_GetPendingSolutionUpdate());

                // no change in non-remappable regions since we didn't have any active statements:
                Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

                // deferred module readers tracked:
                var baselineReaders = editSession.DebuggingSession.GetBaselineModuleReaders();
                Assert.Equal(2, baselineReaders.Length);
                Assert.Same(readers[0], baselineReaders[0]);
                Assert.Same(readers[1], baselineReaders[1]);

                // verify that baseline is added for both modules:
                Assert.Same(newBaselineA1, editSession.DebuggingSession.Test_GetProjectEmitBaseline(projectA.Id));
                Assert.Same(newBaselineB1, editSession.DebuggingSession.Test_GetProjectEmitBaseline(projectB.Id));

                // solution update status after committing an update:
                var commitedUpdateSolutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, commitedUpdateSolutionStatus);

                service.EndEditSession();
                service.StartEditSession();
                editSession = service.Test_GetEditSession();

                //
                // Second update.
                //

                workspace.ChangeDocument(projectA.Documents.Single().Id, SourceText.From(source3, Encoding.UTF8));
                workspace.ChangeDocument(projectB.Documents.Single().Id, SourceText.From(source3, Encoding.UTF8));

                // validate solution update status and emit:
                solutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatus);

                (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);

                deltaA = deltas.Single(d => d.Mvid == moduleIdA);
                deltaB = deltas.Single(d => d.Mvid == moduleIdB);
                Assert.Equal(2, deltas.Length);

                // the update should be stored on the service:
                pendingUpdate = service.Test_GetPendingSolutionUpdate();
                var (_, newBaselineA2) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectA.Id);
                var (_, newBaselineB2) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectB.Id);

                Assert.NotSame(newBaselineA1, newBaselineA2);
                Assert.NotSame(newBaselineB1, newBaselineB2);
                Assert.Same(baselineA0, newBaselineA2.GetInitialEmitBaseline());
                Assert.Same(baselineB0, newBaselineB2.GetInitialEmitBaseline());
                Assert.Same(baselineA0.OriginalMetadata, newBaselineA2.OriginalMetadata);
                Assert.Same(baselineB0.OriginalMetadata, newBaselineB2.OriginalMetadata);

                // no new module readers:
                Assert.Empty(pendingUpdate.ModuleReaders);

                service.CommitSolutionUpdate();
                Assert.Null(service.Test_GetPendingSolutionUpdate());

                // no change in non-remappable regions since we didn't have any active statements:
                Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

                // module readers tracked:
                baselineReaders = editSession.DebuggingSession.GetBaselineModuleReaders();
                Assert.Equal(2, baselineReaders.Length);
                Assert.Same(readers[0], baselineReaders[0]);
                Assert.Same(readers[1], baselineReaders[1]);

                // verify that baseline is updated for both modules:
                Assert.Same(newBaselineA2, editSession.DebuggingSession.Test_GetProjectEmitBaseline(projectA.Id));
                Assert.Same(newBaselineB2, editSession.DebuggingSession.Test_GetProjectEmitBaseline(projectB.Id));

                // solution update status after committing an update:
                commitedUpdateSolutionStatus = await service.GetSolutionUpdateStatusAsync(sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, commitedUpdateSolutionStatus);

                service.EndEditSession();

                service.EndDebuggingSession();

                // open deferred module readers should be dispose when the debugging session ends:
                Assert.Throws<ObjectDisposedException>(() => ((MetadataReaderProvider)readers.First(r => r is MetadataReaderProvider)).GetMetadataReader());
                Assert.Throws<ObjectDisposedException>(() => ((DebugInformationReaderProvider)readers.First(r => r is DebugInformationReaderProvider)).CreateEditAndContinueMethodDebugInfoReader());
            }
        }

        [Fact]
        public void GetSpansInNewDocument()
        {
            // 012345678901234567890
            // 012___890_3489____0
            var changes = new[]
            {
                new TextChange(new TextSpan(3, 5), "___"),
                new TextChange(new TextSpan(11, 2), "_"),
                new TextChange(new TextSpan(15, 3), ""),
                new TextChange(new TextSpan(20, 0), "____"),
            };

            Assert.Equal("012___890_3489____0", SourceText.From("012345678901234567890").WithChanges(changes).ToString());

            AssertEx.Equal(new[]
            {
                "[3..6)",
                "[9..10)",
                "[12..12)",
                "[14..18)"
            }, EditAndContinueWorkspaceService.GetSpansInNewDocument(changes).Select(s => s.ToString()));
        }

        [Fact]
        public async Task GetDocumentTextChangesAsync()
        {
            var source1 = @"
class C1 
{ 
  void M() 
  {
    System.Console.WriteLine(1);
    System.Console.WriteLine(2);
    System.Console.WriteLine(3);
  } 
}";
            var source2 = @"
class C1 
{ 

  void M() 
  {
    System.Console.WriteLine(9);
    System.Console.WriteLine();
    System.Console.WriteLine(30);
  } 
}";

            var oldTree = SyntaxFactory.ParseSyntaxTree(source1);
            var newTree = SyntaxFactory.ParseSyntaxTree(source2);
            var changes = await EditAndContinueWorkspaceService.GetDocumentTextChangesAsync(oldTree, newTree, CancellationToken.None).ConfigureAwait(false);

            AssertEx.Equal(new[]
            {
                "[17..17) '\r\n'",
                "[64..65) '9'",
                "[98..99) ''",
                "[133..133) '0'"
            }, changes.Select(s => $"{s.Span} '{s.NewText}'"));
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_BaselineCreationFailed_NoStream()
        {
            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }"))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsService.Outputs.Add(project.Id, new MockCompilationOutputs(Guid.NewGuid())
                {
                    OpenPdbStreamImpl = () => null,
                    OpenAssemblyStreamImpl = () => null,
                });

                // module not loaded
                _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo = mvid => null;

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession();

                // change the source (valid edit):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(new[] { "ENC1001" }, _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => d.Id));
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            }
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_BaselineCreationFailed_AssemblyReadError()
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var compilationV1 = CSharpTestBase.CreateCompilationWithMscorlib40(sourceV1, options: TestOptions.DebugDll, assemblyName: "lib");

            var pdbStream = new MemoryStream();
            var peImage = compilationV1.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;

            using (var workspace = TestWorkspace.CreateCSharp(sourceV1))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsService.Outputs.Add(project.Id, new MockCompilationOutputs(Guid.NewGuid())
                {
                    OpenPdbStreamImpl = () => pdbStream,
                    OpenAssemblyStreamImpl = () => throw new IOException(),
                });

                // module not loaded
                _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo = mvid => null;

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession();

                // change the source (valid edit):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(new[] { "ENC1001" }, _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => d.Id));
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);

                service.EndEditSession();
                service.EndDebuggingSession();

                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                    "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC1001"
                }, _telemetryLog);
            }
        }
    }
}
