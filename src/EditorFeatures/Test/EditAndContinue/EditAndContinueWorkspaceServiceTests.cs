// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    [UseExportProvider]
    public sealed class EditAndContinueWorkspaceServiceTests : TestBase
    {
        private static readonly TestComposition s_composition = FeaturesTestCompositions.Features;

        private static readonly ActiveStatementProvider s_noActiveStatements =
            _ => Task.FromResult(ImmutableArray<ActiveStatementDebugInfo>.Empty);

        private static readonly SolutionActiveStatementSpanProvider s_noSolutionActiveSpans =
            (_, _) => Task.FromResult(ImmutableArray<TextSpan>.Empty);

        private static readonly DocumentActiveStatementSpanProvider s_noDocumentActiveSpans =
            _ => Task.FromResult(ImmutableArray<TextSpan>.Empty);

        private readonly EditAndContinueDiagnosticUpdateSource _diagnosticUpdateSource;
        private readonly Mock<IDiagnosticAnalyzerService> _mockDiagnosticService;
        private readonly MockDebuggeeModuleMetadataProvider _mockDebugeeModuleMetadataProvider;

        private Func<Project, CompilationOutputs> _mockCompilationOutputsProvider;
        private readonly List<DiagnosticsUpdatedArgs> _emitDiagnosticsUpdated;
        private int _emitDiagnosticsClearedCount;
        private readonly List<string> _telemetryLog;
        private int _telemetryId;

        public EditAndContinueWorkspaceServiceTests()
        {
            _mockDiagnosticService = new Mock<IDiagnosticAnalyzerService>(MockBehavior.Strict);
            _mockDiagnosticService.Setup(s => s.Reanalyze(It.IsAny<Workspace>(), It.IsAny<IEnumerable<ProjectId>>(), It.IsAny<IEnumerable<DocumentId>>(), It.IsAny<bool>()));

            _diagnosticUpdateSource = new EditAndContinueDiagnosticUpdateSource();
            _emitDiagnosticsUpdated = new List<DiagnosticsUpdatedArgs>();
            _diagnosticUpdateSource.DiagnosticsUpdated += (object sender, DiagnosticsUpdatedArgs args) => _emitDiagnosticsUpdated.Add(args);
            _diagnosticUpdateSource.DiagnosticsCleared += (object sender, EventArgs args) => _emitDiagnosticsClearedCount++;

            _mockDebugeeModuleMetadataProvider = new MockDebuggeeModuleMetadataProvider
            {
                IsEditAndContinueAvailable = _ => (0, null)
            };

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.NewGuid());
            _telemetryLog = new List<string>();
        }

        private EditAndContinueWorkspaceService CreateEditAndContinueService(Workspace workspace)
        {
            return new EditAndContinueWorkspaceService(
                workspace,
                _mockDiagnosticService.Object,
                _diagnosticUpdateSource,
                _mockDebugeeModuleMetadataProvider,
                _mockCompilationOutputsProvider,
                testReportTelemetry: data => EditAndContinueWorkspaceService.LogDebuggingSessionTelemetry(data, (id, message) => _telemetryLog.Add($"{id}: {message.GetMessage()}"), () => ++_telemetryId));
        }

        private static DebuggingSession StartDebuggingSession(EditAndContinueWorkspaceService service, CommittedSolution.DocumentState initialState = CommittedSolution.DocumentState.MatchesBuildOutput)
        {
            var solution = service.Test_GetWorkspace().CurrentSolution;

            service.StartDebuggingSession(solution);
            var session = service.Test_GetDebuggingSession();
            if (initialState != CommittedSolution.DocumentState.None)
            {
                SetDocumentsState(session, solution, initialState);
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

        private (DebuggeeModuleInfo, Guid) EmitAndLoadLibraryToDebuggee(string source, string assemblyName = "", string sourceFilePath = "test1.cs", Encoding encoding = null)
        {
            var (debuggeeModuleInfo, moduleId) = EmitLibrary(source, assemblyName, sourceFilePath, encoding);
            LoadLibraryToDebuggee(debuggeeModuleInfo);
            return (debuggeeModuleInfo, moduleId);
        }

        private void LoadLibraryToDebuggee(DebuggeeModuleInfo debuggeeModuleInfo)
        {
            _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo = mvid => debuggeeModuleInfo;
            _mockDebugeeModuleMetadataProvider.IsEditAndContinueAvailable = _ => (0, null);
        }

        private (DebuggeeModuleInfo, Guid) EmitLibrary(
            string source,
            string assemblyName = "",
            string sourceFilePath = "test1.cs",
            Encoding encoding = null,
            DebugInformationFormat pdbFormat = DebugInformationFormat.PortablePdb)
        {
            encoding ??= Encoding.UTF8;

            var sourceText = SourceText.From(new MemoryStream(encoding.GetBytes(source)), encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256);
            var tree = SyntaxFactory.ParseSyntaxTree(sourceText, TestOptions.RegularPreview, sourceFilePath);
            var compilation = CSharpTestBase.CreateCompilationWithMscorlib40(tree, options: TestOptions.DebugDll, assemblyName: assemblyName);

            var (peImage, pdbImage) = compilation.EmitToArrays(new EmitOptions(debugInformationFormat: pdbFormat));
            var symReader = SymReaderTestHelpers.OpenDummySymReader(pdbImage);

            var moduleMetadata = ModuleMetadata.CreateFromImage(peImage);
            var moduleId = moduleMetadata.GetModuleVersionId();
            var debuggeeModuleInfo = new DebuggeeModuleInfo(moduleMetadata, symReader);

            // associate the binaries with the project
            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId)
            {
                OpenPdbStreamImpl = () =>
                {
                    var pdbStream = new MemoryStream();
                    pdbImage.WriteToStream(pdbStream);
                    pdbStream.Position = 0;
                    return pdbStream;
                }
            };

            // library not loaded yet:
            _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo = mvid => null;
            _mockDebugeeModuleMetadataProvider.IsEditAndContinueAvailable = _ => null;

            return (debuggeeModuleInfo, moduleId);
        }

        private static SourceText CreateSourceTextFromFile(string path)
        {
            using var stream = File.OpenRead(path);
            return SourceText.From(stream, Encoding.UTF8, SourceHashAlgorithm.Sha256);
        }

        private static TextSpan GetSpan(string str, string substr)
            => new TextSpan(str.IndexOf(substr), substr.Length);

        [Fact]
        public async Task RunMode_ProjectThatDoesNotSupportEnC()
        {
            using var workspace = new TestWorkspace(composition: FeaturesTestCompositions.Features.AddParts(typeof(DummyLanguageService)));

            var solution = workspace.CurrentSolution;
            var project = solution.AddProject("dummy_proj", "dummy_proj", DummyLanguageService.LanguageName);
            var document = project.AddDocument("test", SourceText.From("dummy1"));
            workspace.ChangeSolution(document.Project.Solution);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            // no changes:
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // change the source:
            workspace.ChangeDocument(document1.Id, SourceText.From("dummy2"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task RunMode_DesignTimeOnlyDocument()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition);

            var project = workspace.CurrentSolution.Projects.Single();
            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(project.Id),
                name: "design-time-only.cs",
                folders: Array.Empty<string>(),
                sourceCodeKind: SourceCodeKind.Regular,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class C2 {}"), VersionStamp.Create(), "design-time-only.cs")),
                filePath: "design-time-only.cs",
                isGenerated: false,
                designTimeOnly: true,
                documentServiceProvider: null);

            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path).AddDocument(documentInfo));
            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            // update a design-time-only source file:
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentInfo.Id);
            workspace.ChangeDocument(document1.Id, SourceText.From("class UpdatedC2 {}"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentInfo.Id);

            // no updates:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // validate solution update status and emit - changes made in design-time-only documents are ignored:
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

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
            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition))
            {
                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.Empty);

                var service = CreateEditAndContinueService(workspace);
                var project = workspace.CurrentSolution.Projects.Single();

                StartDebuggingSession(service);

                // no changes:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                // change the source:
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);
            }
        }

        [Fact]
        public async Task RunMode_ErrorReadingModuleFile()
        {
            // empty module file will cause read error:
            var moduleFile = Temp.CreateFile();

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition))
            {
                var project = workspace.CurrentSolution.Projects.Single();

                _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                // no changes:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                // change the source:
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
                diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                // validate solution update status and emit - changes made during run mode are ignored:
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
                Assert.Empty(deltas);
            }
        }

        [Fact]
        public async Task RunMode_DocumentOutOfSync()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition);
            var service = CreateEditAndContinueService(workspace);

            var project = workspace.CurrentSolution.Projects.Single();
            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));
            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

            var document1 = project.Documents.Single();

            var debuggingSession = StartDebuggingSession(service);
            debuggingSession.LastCommittedSolution.Test_SetDocumentState(document1.Id, CommittedSolution.DocumentState.OutOfSync);

            // no changes:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // change the source:
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void RenamedMethod() { System.Console.WriteLine(1); } }"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // no Rude Edits, since the document is out-of-sync
            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // the document is now in-sync (a file watcher observed a change and updated the status):
            debuggingSession.LastCommittedSolution.Test_SetDocumentState(document2.Id, CommittedSolution.DocumentState.MatchesBuildOutput);

            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
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

            using var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition);

            var project = workspace.CurrentSolution.Projects.Single();
            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            // add a source file:
            var document2 = project.AddDocument("file2.cs", SourceText.From("class C2 {}"));
            workspace.ChangeSolution(document2.Project.Solution);

            // no changes in document1:
            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document1, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics1);

            // update in document2:
            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "ENC1003" }, diagnostics2.Select(d => d.Id));

            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

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

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));

                _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

                var service = CreateEditAndContinueService(workspace);

                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                StartDebuggingSession(service);

                // no changes:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                // change the source:
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // validate solution update status and emit - changes made during run mode are ignored:
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
                Assert.Empty(deltas);

                diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
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

            using var workspace = TestWorkspace.CreateCSharp(source, composition: s_composition);

            var project = workspace.CurrentSolution.Projects.Single();
            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));
            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            // update the document
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From(source));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            Assert.Equal(document1.Id, document2.Id);
            Assert.NotSame(document1, document2);

            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics2);

            // validate solution update status and emit - changes made during run mode are ignored:
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            service.EndDebuggingSession();

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_ProjectThatDoesNotSupportEnC()
        {
            var composition = FeaturesTestCompositions.Features.AddParts(typeof(DummyLanguageService));

            using (var workspace = new TestWorkspace(composition: composition))
            {
                var solution = workspace.CurrentSolution;
                var project = solution.AddProject("dummy_proj", "dummy_proj", DummyLanguageService.LanguageName);
                var document = project.AddDocument("test", SourceText.From("dummy1"));
                workspace.ChangeSolution(document.Project.Solution);

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);
                service.StartEditSession(s_noActiveStatements);

                // change the source:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("dummy2"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // validate solution update status and emit:
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
                Assert.Empty(deltas);
            }
        }

        [Fact]
        public async Task BreakMode_DesignTimeOnlyDocument_Dynamic()
        {
            using var workspace = TestWorkspace.CreateCSharp("class C {}", composition: s_composition);

            var project = workspace.CurrentSolution.Projects.Single();
            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(project.Id),
                name: "design-time-only.cs",
                folders: Array.Empty<string>(),
                sourceCodeKind: SourceCodeKind.Regular,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class D {}"), VersionStamp.Create(), "design-time-only.cs")),
                filePath: "design-time-only.cs",
                isGenerated: false,
                designTimeOnly: true,
                documentServiceProvider: null);

            var solution = workspace.CurrentSolution.AddDocument(documentInfo);
            workspace.ChangeSolution(solution);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);
            service.StartEditSession(s_noActiveStatements);

            // change the source:
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentInfo.Id);
            workspace.ChangeDocument(document1.Id, SourceText.From("class E {}"));

            // validate solution update status and emit:
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            Assert.Empty(deltas);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BreakMode_DesignTimeOnlyDocument_Wpf(bool delayLoad)
        {
            var sourceA = "class A { public void M() { } }";
            var sourceB = "class B { public void M() { } }";
            var sourceC = "class C { public void M() { } }";

            var dir = Temp.CreateDirectory();
            var sourceFileA = dir.CreateFile("a.cs").WriteAllText(sourceA);

            using var workspace = new TestWorkspace(composition: s_composition);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var documentA = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("a.cs", SourceText.From(sourceA, Encoding.UTF8), filePath: sourceFileA.Path);

            var documentB = documentA.Project.
                AddDocument("b.g.i.cs", SourceText.From(sourceB, Encoding.UTF8), filePath: "b.g.i.cs");

            var documentC = documentB.Project.
                AddDocument("c.g.i.vb", SourceText.From(sourceC, Encoding.UTF8), filePath: "c.g.i.vb");

            workspace.ChangeSolution(documentC.Project.Solution);

            // only compile A; B and C are design-time-only:
            var (moduleInfo, moduleId) = EmitLibrary(sourceA, sourceFilePath: sourceFileA.Path);

            if (!delayLoad)
            {
                LoadLibraryToDebuggee(moduleInfo);
            }

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession(s_noActiveStatements);

            // change the source (rude edit):
            workspace.ChangeDocument(documentB.Id, SourceText.From("class B { public void RenamedMethod() { } }"));
            workspace.ChangeDocument(documentC.Id, SourceText.From("class C { public void RenamedMethod() { } }"));
            var documentB2 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentB.Id);
            var documentC2 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentC.Id);

            // no Rude Edits reported:
            Assert.Empty(await service.GetDocumentDiagnosticsAsync(documentB2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false));
            Assert.Empty(await service.GetDocumentDiagnosticsAsync(documentC2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false));

            // validate solution update status and emit:
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            Assert.Empty(_emitDiagnosticsUpdated);

            if (delayLoad)
            {
                LoadLibraryToDebuggee(moduleInfo);

                // validate solution update status and emit:
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
                Assert.Empty(_emitDiagnosticsUpdated);
            }

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Fact]
        public async Task BreakMode_ErrorReadingModuleFile()
        {
            // module file is empty, which will cause a read error:
            var moduleFile = Temp.CreateFile();

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);
                service.StartEditSession(s_noActiveStatements);

                // change the source:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                Assert.Empty(_emitDiagnosticsUpdated);
                Assert.Equal(0, _emitDiagnosticsClearedCount);

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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
        public async Task BreakMode_ErrorReadingPdbFile()
        {
            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1);

            using var workspace = new TestWorkspace(composition: s_composition);

            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId)
            {
                OpenPdbStreamImpl = () =>
                {
                    throw new IOException("Error");
                }
            };

            var service = CreateEditAndContinueService(workspace);
            StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);
            service.StartEditSession(s_noActiveStatements);

            // change the source:
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.GetDocument(document1.Id);

            // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // an error occurred so we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            Assert.Empty(_emitDiagnosticsUpdated);
            Assert.Equal(0, _emitDiagnosticsClearedCount);

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            Assert.Empty(deltas);

            Assert.Equal(1, _emitDiagnosticsClearedCount);
            var eventArgs = _emitDiagnosticsUpdated.Single();
            Assert.Null(eventArgs.DocumentId);
            Assert.Equal(project.Id, eventArgs.ProjectId);
            AssertEx.Equal(new[] { "Warning ENC1006: " + string.Format(FeaturesResources.UnableToReadSourceFileOrPdb, sourceFile.Path) },
                eventArgs.Diagnostics.Select(d => $"{d.Severity} {d.Id}: {d.Message}"));

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
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=1"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_ErrorReadingSourceFile()
        {
            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1);

            using var workspace = new TestWorkspace(composition: s_composition);

            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);
            StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);
            service.StartEditSession(s_noActiveStatements);

            // change the source:
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.GetDocument(document1.Id);

            using var fileLock = File.Open(sourceFile.Path, FileMode.Open, FileAccess.Read, FileShare.None);

            // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // an error occurred so we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            Assert.Empty(_emitDiagnosticsUpdated);
            Assert.Equal(0, _emitDiagnosticsClearedCount);

            // try apply changes:
            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            Assert.Empty(deltas);

            Assert.Equal(1, _emitDiagnosticsClearedCount);
            var eventArgs = _emitDiagnosticsUpdated.Single();
            Assert.Null(eventArgs.DocumentId);
            Assert.Equal(project.Id, eventArgs.ProjectId);
            AssertEx.Equal(new[] { "Warning ENC1006: " + string.Format(FeaturesResources.UnableToReadSourceFileOrPdb, sourceFile.Path) },
                eventArgs.Diagnostics.Select(d => $"{d.Severity} {d.Id}: {d.Message}"));

            _emitDiagnosticsUpdated.Clear();
            _emitDiagnosticsClearedCount = 0;

            fileLock.Dispose();

            // try apply changes again:
            (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);
            Assert.NotEmpty(deltas);

            Assert.Equal(1, _emitDiagnosticsClearedCount);
            Assert.Empty(_emitDiagnosticsUpdated);
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
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_FileAdded()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition);

            var project = workspace.CurrentSolution.Projects.Single();
            workspace.ChangeSolution(project.Solution.WithProjectOutputFilePath(project.Id, moduleFile.Path));
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            _mockDebugeeModuleMetadataProvider.IsEditAndContinueAvailable = _ => (errorCode: 123, errorMessage: "*message*");

            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);
            service.StartEditSession(s_noActiveStatements);

            // add a source file:
            var document2 = project.AddDocument("file2.cs", SourceText.From("class C2 {}"));
            workspace.ChangeSolution(document2.Project.Solution);

            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(
                new[] { "ENC0071: " + string.Format(FeaturesResources.Adding_a_new_file_will_prevent_the_debug_session_from_continuing) },
                diagnostics2.Select(d => $"{d.Id}: {d.GetMessage()}"));

            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            Assert.Empty(deltas);

            AssertEx.Equal(
                new[] { "ENC2123: " + string.Format(FeaturesResources.EditAndContinueDisallowedByProject, "Test", "*message*") },
                _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => $"{d.Id}: {d.Message}"));

            service.EndEditSession();
            service.EndDebuggingSession();

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=1",
                "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC2123",
                "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=2|RudeEditKind=71|RudeEditSyntaxKind=0|RudeEditBlocking=True"
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
            using (var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId);

                _mockDebugeeModuleMetadataProvider.IsEditAndContinueAvailable = guid =>
                {
                    Assert.Equal(moduleId, guid);
                    return (errorCode: 123, errorMessage: "*message*");
                };

                var service = CreateEditAndContinueService(workspace);

                var debuggingSession = StartDebuggingSession(service);

                service.StartEditSession(s_noActiveStatements);
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                // change the source:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From(source2));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // We do not report module diagnostics until emit.
                // This is to make the analysis deterministic (not dependent on the current state of the debuggee).
                var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Empty(diagnostics1);

                // validate solution update status and emit:
                Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
                Assert.Empty(deltas);

                AssertEx.Equal(new[] { "ENC2123: " + string.Format(FeaturesResources.EditAndContinueDisallowedByProject, "Test", "*message*") },
                    _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => $"{d.Id}: {d.Message}"));

                service.EndEditSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                service.EndDebuggingSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                    "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC2123"
                }, _telemetryLog);
            }
        }

        [Fact]
        public async Task BreakMode_Encodings()
        {
            var source1 = "class C1 { void M() { System.Console.WriteLine(\"ã\"); } }";

            var encoding = Encoding.GetEncoding(1252);

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(source1, encoding);

            using var workspace = new TestWorkspace(composition: s_composition);

            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, encoding), filePath: sourceFile.Path);

            var documentId = document1.Id;

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path, encoding: encoding);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession(s_noActiveStatements);

            // Emulate opening the file, which will trigger "out-of-sync" check.
            // Since we find content matching the PDB checksum we update the committed solution with this source text.
            // If we used wrong encoding this would lead to a false change detected below.
            await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(documentId, debuggingSession.CancellationToken).ConfigureAwait(false);

            // EnC service queries for a document, which triggers read of the source file from disk.
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Fact]
        public async Task BreakMode_RudeEdits()
        {
            var moduleId = Guid.NewGuid();

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId);

                var service = CreateEditAndContinueService(workspace);

                var debuggingSession = StartDebuggingSession(service);

                service.StartEditSession(s_noActiveStatements);
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                // change the source (rude edit):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M1() { System.Console.WriteLine(1); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                    diagnostics1.Select(d => $"{d.Id}: {d.GetMessage()}"));

                // validate solution update status and emit:
                Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
                Assert.Empty(deltas);

                service.EndEditSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray.Create(document2.Id), false);

                service.EndDebuggingSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

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
            var source0 = "class C1 { void M() { System.Console.WriteLine(0); } }";
            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs");

            using var workspace = new TestWorkspace(composition: s_composition);

            var project = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40));

            workspace.ChangeSolution(project.Solution);

            // compile with source0:
            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source0, sourceFilePath: sourceFile.Path);

            // update the file with source1 before session starts:
            sourceFile.WriteAllText(source1);

            // source1 is reflected in workspace before session starts:
            var document1 = project.AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);
            workspace.ChangeSolution(document1.Project.Solution);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession(s_noActiveStatements);
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // change the source (rude edit):
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void RenamedMethod() { System.Console.WriteLine(1); } }"));
            var document2 = workspace.CurrentSolution.GetDocument(document1.Id);

            // no Rude Edits, since the document is out-of-sync
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // since the document is out-of-sync we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            Assert.Empty(deltas);

            AssertEx.Equal(
                new[] { "ENC1005: " + string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFile.Path) },
                _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => $"{d.Id}: {d.Message}"));

            _emitDiagnosticsUpdated.Clear();
            _emitDiagnosticsClearedCount = 0;

            // update the file to match the build:
            sourceFile.WriteAllText(source0);

            // we do not reload the content of out-of-sync file for analyzer query:
            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // debugger query will trigger reload of out-of-sync file content:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            // now we see the rude edit:
            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "ENC0020" }, diagnostics.Select(d => d.Id));

            (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            Assert.Empty(deltas);
            Assert.Empty(_emitDiagnosticsUpdated);

            service.EndEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray.Create(document2.Id), false);

            service.EndDebuggingSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

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

            using var workspace = new TestWorkspace(composition: s_composition);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);

            // do not initialize the document state - we will detect the state based on the PDB content.
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession(s_noActiveStatements);

            // change the source (rude edit since the base document content matches the PDB checksum, so the document is not out-of-sync):
            workspace.ChangeDocument(document1.Id, SourceText.From("abstract class C { public abstract void M(); public abstract void N(); }"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // Rude Edits reported:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(
                new[] { "ENC0023: " + string.Format(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            Assert.Empty(deltas);

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Fact]
        public async Task BreakMode_RudeEdits_DelayLoadedModule()
        {
            var source1 = "class C { public void M() { } }";
            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1);

            using var workspace = new TestWorkspace(composition: s_composition);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (debuggeeModuleInfo, _) = EmitLibrary(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);

            // do not initialize the document state - we will detect the state based on the PDB content.
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession(s_noActiveStatements);

            // change the source (rude edit) before the library is loaded:
            workspace.ChangeDocument(document1.Id, SourceText.From("class C { public void Renamed() { } }"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // Rude Edits reported:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(
                new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            Assert.Empty(deltas);

            // load library to the debuggee:
            LoadLibraryToDebuggee(debuggeeModuleInfo);

            // Rude Edits still reported:
            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(
                new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            Assert.Empty(deltas);

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Fact]
        public async Task BreakMode_SyntaxError()
        {
            var moduleId = Guid.NewGuid();

            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId);

                var service = CreateEditAndContinueService(workspace);

                var debuggingSession = StartDebuggingSession(service);

                service.StartEditSession(s_noActiveStatements);
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                // change the source (compilation error):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { "));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // compilation errors are not reported via EnC diagnostic analyzer:
                var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Empty(diagnostics1);

                // validate solution update status and emit:
                Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
                Assert.Empty(deltas);

                service.EndEditSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                service.EndDebuggingSession();
                VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

                AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

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

            using var workspace = TestWorkspace.CreateCSharp(sourceV1, composition: s_composition);

            var project = workspace.CurrentSolution.Projects.Single();
            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(sourceV1);

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service);

            service.StartEditSession(s_noActiveStatements);
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // change the source (compilation error):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { int i = 0L; System.Console.WriteLine(i); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // compilation errors are not reported via EnC diagnostic analyzer:
            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics1);

            // The EnC analyzer does not check for and block on all semantic errors as they are already reported by diagnostic analyzer.
            // Blocking update on semantic errors would be possible, but the status check is only an optimization to avoid emitting.
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

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
            using (var workspace = TestWorkspace.CreateCSharp("class Program { void Main() { System.Console.WriteLine(1); } }", composition: s_composition))
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
                service.StartEditSession(s_noActiveStatements);

                // change C.cs to have a compilation error:
                var projectC = workspace.CurrentSolution.GetProjectsByName("C").Single();
                var documentC = projectC.Documents.Single(d => d.Name == "C.cs");
                workspace.ChangeDocument(documentC.Id, SourceText.From("class C { void M() { "));

                // Common.cs is included in projects B and C. Both of these projects must have no errors, otherwise update is blocked.
                Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: "Common.cs", CancellationToken.None).ConfigureAwait(false));

                // No changes in project containing file B.cs.
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: "B.cs", CancellationToken.None).ConfigureAwait(false));

                // All projects must have no errors.
                Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                service.EndEditSession();
                service.EndDebuggingSession();
            }
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_EmitError()
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var workspace = TestWorkspace.CreateCSharp(sourceV1, composition: s_composition);

            var project = workspace.CurrentSolution.Projects.Single();
            EmitAndLoadLibraryToDebuggee(sourceV1);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            service.StartEditSession(s_noActiveStatements);
            var editSession = service.Test_GetEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // change the source (valid edit but passing no encoding to emulate emit error):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", encoding: null));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "CS8055" }, _emitDiagnosticsUpdated.Single().Diagnostics.Select(d => d.Id));
            Assert.Equal(SolutionUpdateStatus.Blocked, solutionStatusEmit);
            _emitDiagnosticsUpdated.Clear();
            _emitDiagnosticsClearedCount = 0;

            // no emitted delta:
            Assert.Empty(deltas);

            // no pending update:
            Assert.Null(editSession.Test_GetPendingSolutionUpdate());

            Assert.Throws<InvalidOperationException>(() => service.CommitSolutionUpdate());
            Assert.Throws<InvalidOperationException>(() => service.DiscardSolutionUpdate());

            // no change in non-remappable regions since we didn't have any active statements:
            Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

            // no open module readers since we didn't defer any module update:
            Assert.Empty(editSession.DebuggingSession.GetBaselineModuleReaders());

            // solution update status after discarding an update (still has update ready):
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

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

            using var workspace = new TestWorkspace(composition: s_composition);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From("class C1 { void M() { System.Console.WriteLine(0); } }", Encoding.UTF8), filePath: sourceFile.Path);

            var documentId = document1.Id;

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession(s_noActiveStatements);

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
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));
            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);
            service.CommitSolutionUpdate();

            service.EndEditSession();

            service.StartEditSession(s_noActiveStatements);

            // file watcher updates the workspace:
            workspace.ChangeDocument(documentId, CreateSourceTextFromFile(sourceFile.Path));
            var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var hasChanges = await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);

            if (saveDocument)
            {
                Assert.False(hasChanges);
                Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            }
            else
            {
                Assert.True(hasChanges);
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);
            }

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_FileUpdateBeforeDebuggingSessionStarts()
        {
            // workspace:     --V0--------------V2-------|--------V3------------------V1--------------|
            // file system:   --V0---------V1-----V2-----|------------------------------V1------------|
            //                   \--build--/      ^save  F5   ^      ^F10 (no change)   ^save         F10 (ok)
            //                                                file watcher: no-op

            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var source2 = "class C1 { void M() { System.Console.WriteLine(2); } }";
            var source3 = "class C1 { void M() { System.Console.WriteLine(3); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(source2);

            using var workspace = new TestWorkspace(composition: s_composition);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document2 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source2, Encoding.UTF8), filePath: sourceFile.Path);

            var documentId = document2.Id;

            var project = document2.Project;
            workspace.ChangeSolution(project.Solution);

            var (_, moduleId) = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession(s_noActiveStatements);

            // user edits the file:
            workspace.ChangeDocument(documentId, SourceText.From(source3, Encoding.UTF8));
            var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // EnC service queries for a document, but the source file on disk doesn't match the PDB

            // We don't report rude edits for out-of-sync documents:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document3, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics);

            // since the document is out-of-sync we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);

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

            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));
            (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);

            // the content actually hasn't changed:
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);

            service.EndEditSession();
            service.EndDebuggingSession();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BreakMode_ValidSignificantChange_DocumentOutOfSync(bool delayLoad)
        {
            var sourceOnDisk = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(sourceOnDisk);

            using var workspace = new TestWorkspace(composition: s_composition);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From("class C1 { void M() { System.Console.WriteLine(0); } }", Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var (moduleInfo, moduleId) = EmitLibrary(sourceOnDisk, sourceFilePath: sourceFile.Path);

            if (!delayLoad)
            {
                LoadLibraryToDebuggee(moduleInfo);
            }

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            service.StartEditSession(s_noActiveStatements);
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // no changes have been made to the project
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);
            Assert.Empty(deltas);

            Assert.Empty(_emitDiagnosticsUpdated);

            _emitDiagnosticsUpdated.Clear();
            _emitDiagnosticsClearedCount = 0;

            // a file watcher observed a change and updated the document, so it now reflects the content on disk (the code that we compiled):
            workspace.ChangeDocument(document1.Id, SourceText.From(sourceOnDisk, Encoding.UTF8));
            var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics = await service.GetDocumentDiagnosticsAsync(document3, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // the content of the file is now exactly the same as the compiled document, so there is no change to be applied:
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            (solutionStatusEmit, _) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.None, solutionStatusEmit);

            service.EndEditSession();

            // no diagnostics reported via a document analyzer
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            service.EndDebuggingSession();

            // no diagnostics reported via a document analyzer
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            Assert.Empty(debuggingSession.Test_GetModulesPreparedForUpdate());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BreakMode_ValidSignificantChange_EmitSuccessful(bool commitUpdate)
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var workspace = TestWorkspace.CreateCSharp(sourceV1, composition: s_composition);

            var project = workspace.CurrentSolution.Projects.Single();
            var (debuggeeModuleInfo, moduleId) = EmitAndLoadLibraryToDebuggee(sourceV1);

            var diagnosticUpdateSource = new EditAndContinueDiagnosticUpdateSource();
            var emitDiagnosticsUpdated = new List<DiagnosticsUpdatedArgs>();
            diagnosticUpdateSource.DiagnosticsUpdated += (object sender, DiagnosticsUpdatedArgs args) => emitDiagnosticsUpdated.Add(args);

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service);

            service.StartEditSession(s_noActiveStatements);
            var editSession = service.Test_GetEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            // change the source (valid edit):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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
            var pendingUpdate = editSession.Test_GetPendingSolutionUpdate();
            var (baselineProjectId, newBaseline) = pendingUpdate.EmitBaselines.Single();
            AssertEx.Equal(deltas, pendingUpdate.Deltas);
            Assert.Empty(pendingUpdate.ModuleReaders);
            Assert.Equal(project.Id, baselineProjectId);
            Assert.Equal(moduleId, newBaseline.OriginalMetadata.GetModuleVersionId());

            if (commitUpdate)
            {
                // all update providers either provided updates or had no change to apply:
                service.CommitSolutionUpdate();

                Assert.Null(editSession.Test_GetPendingSolutionUpdate());

                // no change in non-remappable regions since we didn't have any active statements:
                Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

                // no open module readers since we didn't defer any module update:
                Assert.Empty(editSession.DebuggingSession.GetBaselineModuleReaders());

                // verify that baseline is added:
                Assert.Same(newBaseline, editSession.DebuggingSession.Test_GetProjectEmitBaseline(project.Id));

                // solution update status after committing an update:
                var commitedUpdateSolutionStatus = await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.False(commitedUpdateSolutionStatus);
            }
            else
            {
                // another update provider blocked the update:
                service.DiscardSolutionUpdate();

                Assert.Null(editSession.Test_GetPendingSolutionUpdate());

                // solution update status after committing an update:
                var discardedUpdateSolutionStatus = await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
                Assert.True(discardedUpdateSolutionStatus);
            }

            service.EndEditSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            service.EndDebuggingSession();
            VerifyReanalyzeInvocation(workspace, null, ImmutableArray<DocumentId>.Empty, false);

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

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

            using var workspace = TestWorkspace.CreateCSharp(sourceV1, composition: s_composition);

            var project = workspace.CurrentSolution.Projects.Single();
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path, pdbFile.Path);

            // set up an active statement in the first method, so that we can test preservation of local signature.
            Task<ImmutableArray<ActiveStatementDebugInfo>> activeStatementProvider(CancellationToken _)
            {
                return Task.FromResult(ImmutableArray.Create(new ActiveStatementDebugInfo(
                    new ActiveInstructionId(moduleId, methodToken: 0x06000001, methodVersion: 1, ilOffset: 0),
                    documentNameOpt: document1.Name,
                    linePositionSpan: new LinePositionSpan(new LinePosition(0, 15), new LinePosition(0, 16)),
                    threadIds: ImmutableArray.Create(Guid.NewGuid()),
                    ActiveStatementFlags.IsLeafFrame)));
            }

            // module not loaded
            _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo = mvid => null;

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            service.StartEditSession(activeStatementProvider);
            var editSession = service.Test_GetEditSession();

            // change the source (valid edit):
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M1() { int a = 1; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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
            var pendingUpdate = editSession.Test_GetPendingSolutionUpdate();
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
                Assert.Null(editSession.Test_GetPendingSolutionUpdate());

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
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                service.EndEditSession();

                // make another update:
                service.StartEditSession(s_noActiveStatements);

                // Update M1 - this method has an active statement, so we will attempt to preserve the local signature.
                // Since the method hasn't been edited before we'll read the baseline PDB to get the signature token.
                // This validates that the Portable PDB reader can be used (and is not disposed) for a second generation edit.
                var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document3.Id, SourceText.From("class C1 { void M1() { int a = 3; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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
                Assert.Null(editSession.Test_GetPendingSolutionUpdate());

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

            var (peImageA, pdbImageA) = compilationA.EmitToArrays(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
            var symReaderA = SymReaderTestHelpers.OpenDummySymReader(pdbImageA);

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

            using (var workspace = TestWorkspace.CreateCSharp(source1, composition: s_composition))
            {
                var solution = workspace.CurrentSolution;
                var projectA = solution.Projects.Single();
                var projectB = solution.AddProject("B", "A", "C#").AddMetadataReferences(projectA.MetadataReferences).AddDocument("DocB", source1, filePath: "DocB.cs").Project;
                workspace.ChangeSolution(projectB.Solution);

                _mockCompilationOutputsProvider = project =>
                    (project.Id == projectA.Id) ? new CompilationOutputFiles(moduleFileA.Path) :
                    (project.Id == projectB.Id) ? new CompilationOutputFiles(moduleFileB.Path, pdbFileB.Path) :
                    throw ExceptionUtilities.UnexpectedValue(project);

                // only module A is loaded
                _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo =
                    mvid => (mvid == moduleIdA) ? debuggeeModuleInfoA : null;

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession(s_noActiveStatements);
                var editSession = service.Test_GetEditSession();

                //
                // First update.
                //

                workspace.ChangeDocument(projectA.Documents.Single().Id, SourceText.From(source2, Encoding.UTF8));
                workspace.ChangeDocument(projectB.Documents.Single().Id, SourceText.From(source2, Encoding.UTF8));

                // validate solution update status and emit:
                Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);

                var deltaA = deltas.Single(d => d.Mvid == moduleIdA);
                var deltaB = deltas.Single(d => d.Mvid == moduleIdB);
                Assert.Equal(2, deltas.Length);

                // the update should be stored on the service:
                var pendingUpdate = editSession.Test_GetPendingSolutionUpdate();
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
                Assert.Null(editSession.Test_GetPendingSolutionUpdate());

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
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                service.EndEditSession();
                service.StartEditSession(s_noActiveStatements);
                editSession = service.Test_GetEditSession();

                //
                // Second update.
                //

                workspace.ChangeDocument(projectA.Documents.Single().Id, SourceText.From(source3, Encoding.UTF8));
                workspace.ChangeDocument(projectB.Documents.Single().Id, SourceText.From(source3, Encoding.UTF8));

                // validate solution update status and emit:
                Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(SolutionUpdateStatus.Ready, solutionStatusEmit);

                deltaA = deltas.Single(d => d.Mvid == moduleIdA);
                deltaB = deltas.Single(d => d.Mvid == moduleIdB);
                Assert.Equal(2, deltas.Length);

                // the update should be stored on the service:
                pendingUpdate = editSession.Test_GetPendingSolutionUpdate();
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
                Assert.Null(editSession.Test_GetPendingSolutionUpdate());

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
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

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
            using (var workspace = TestWorkspace.CreateCSharp("class C1 { void M() { System.Console.WriteLine(1); } }", composition: s_composition))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.NewGuid())
                {
                    OpenPdbStreamImpl = () => null,
                    OpenAssemblyStreamImpl = () => null,
                };

                // module not loaded
                _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo = mvid => null;

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession(s_noActiveStatements);

                // change the source (valid edit):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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

            using (var workspace = TestWorkspace.CreateCSharp(sourceV1, composition: s_composition))
            {
                var project = workspace.CurrentSolution.Projects.Single();
                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.NewGuid())
                {
                    OpenPdbStreamImpl = () => pdbStream,
                    OpenAssemblyStreamImpl = () => throw new IOException(),
                };

                // module not loaded
                _mockDebugeeModuleMetadataProvider.TryGetBaselineModuleInfo = mvid => null;

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                service.StartEditSession(s_noActiveStatements);

                // change the source (valid edit):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                var (solutionStatusEmit, deltas) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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

        [Fact]
        public async Task ActiveStatements()
        {
            var sourceV1 = "class C { void F() { G(1); } void G(int a) => System.Console.WriteLine(1); }";
            var sourceV2 = "class C { int x; void F() { G(2); G(1); } void G(int a) => System.Console.WriteLine(2); }";

            using var workspace = TestWorkspace.CreateCSharp(sourceV1, composition: s_composition);
            var activeSpan11 = GetSpan(sourceV1, "G(1);");
            var activeSpan12 = GetSpan(sourceV1, "System.Console.WriteLine(1)");
            var activeSpan21 = GetSpan(sourceV2, "G(2); G(1);");
            var activeSpan22 = GetSpan(sourceV2, "System.Console.WriteLine(2)");
            var adjustedActiveSpan1 = GetSpan(sourceV2, "G(2);");
            var adjustedActiveSpan2 = GetSpan(sourceV2, "System.Console.WriteLine(2)");

            var project = workspace.CurrentSolution.Projects.Single();
            var document1 = project.Documents.Single();
            var documentId = document1.Id;
            var documentName = document1.Name;

            var sourceTextV1 = document1.GetTextSynchronously(CancellationToken.None);
            var sourceTextV2 = SourceText.From(sourceV2, Encoding.UTF8);

            var activeLineSpan11 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan11);
            var activeLineSpan12 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan12);
            var activeLineSpan21 = sourceTextV2.Lines.GetLinePositionSpan(activeSpan21);
            var activeLineSpan22 = sourceTextV2.Lines.GetLinePositionSpan(activeSpan22);
            var adjustedActiveLineSpan1 = sourceTextV2.Lines.GetLinePositionSpan(adjustedActiveSpan1);
            var adjustedActiveLineSpan2 = sourceTextV2.Lines.GetLinePositionSpan(adjustedActiveSpan2);

            var service = CreateEditAndContinueService(workspace);

            // default if called outside of edit session
            Assert.True((await service.GetBaseActiveStatementSpansAsync(ImmutableArray.Create(documentId), CancellationToken.None).ConfigureAwait(false)).IsDefault);

            var debuggingSession = StartDebuggingSession(service);

            // default if called outside of edit session
            Assert.True((await service.GetBaseActiveStatementSpansAsync(ImmutableArray.Create(documentId), CancellationToken.None).ConfigureAwait(false)).IsDefault);

            var moduleId = Guid.NewGuid();
            var threadId = Guid.NewGuid();
            var activeInstruction1 = new ActiveInstructionId(moduleId, methodToken: 0x06000001, methodVersion: 1, ilOffset: 1);
            var activeInstruction2 = new ActiveInstructionId(moduleId, methodToken: 0x06000002, methodVersion: 1, ilOffset: 1);

            var activeStatements = ImmutableArray.Create(
                new ActiveStatementDebugInfo(
                    activeInstruction1,
                    documentName,
                    activeLineSpan11,
                    threadIds: ImmutableArray.Create(threadId),
                    ActiveStatementFlags.IsNonLeafFrame),
                new ActiveStatementDebugInfo(
                    activeInstruction2,
                    documentName,
                    activeLineSpan12,
                    threadIds: ImmutableArray.Create(threadId),
                    ActiveStatementFlags.IsLeafFrame));

            service.StartEditSession(_ => Task.FromResult(activeStatements));

            var editSession = service.Test_GetEditSession();

            var baseSpans = await service.GetBaseActiveStatementSpansAsync(ImmutableArray.Create(documentId), CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"({activeLineSpan11}, IsNonLeafFrame)",
                $"({activeLineSpan12}, IsLeafFrame)"
            }, baseSpans.Single().Select(s => s.ToString()));

            var trackedActiveSpans1 = ImmutableArray.Create(activeSpan11, activeSpan12);

            var currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document1, (_) => Task.FromResult(trackedActiveSpans1), CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"({activeLineSpan11}, IsNonLeafFrame)",
                $"({activeLineSpan12}, IsLeafFrame)"
            }, currentSpans.Select(s => s.ToString()));

            Assert.Equal(activeLineSpan11,
                await service.GetCurrentActiveStatementPositionAsync(document1.Project.Solution, (_, _) => Task.FromResult(trackedActiveSpans1), activeInstruction1, CancellationToken.None).ConfigureAwait(false));

            Assert.Equal(activeLineSpan12,
                await service.GetCurrentActiveStatementPositionAsync(document1.Project.Solution, (_, _) => Task.FromResult(trackedActiveSpans1), activeInstruction2, CancellationToken.None).ConfigureAwait(false));

            // change the source (valid edit):
            workspace.ChangeDocument(document1.Id, sourceTextV2);
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // tracking span update triggered by the edit:
            var trackedActiveSpans2 = ImmutableArray.Create(activeSpan21, activeSpan22);

            baseSpans = await service.GetBaseActiveStatementSpansAsync(ImmutableArray.Create(documentId), CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"({activeLineSpan11}, IsNonLeafFrame)",
                $"({activeLineSpan12}, IsLeafFrame)"
            }, baseSpans.Single().Select(s => s.ToString()));

            currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document2, _ => Task.FromResult(trackedActiveSpans2), CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"({adjustedActiveLineSpan1}, IsNonLeafFrame)",
                $"({adjustedActiveLineSpan2}, IsLeafFrame)"
            }, currentSpans.Select(s => s.ToString()));

            Assert.Equal(adjustedActiveLineSpan1,
                await service.GetCurrentActiveStatementPositionAsync(document2.Project.Solution, (_, _) => Task.FromResult(trackedActiveSpans2), activeInstruction1, CancellationToken.None).ConfigureAwait(false));

            Assert.Equal(adjustedActiveLineSpan2,
                await service.GetCurrentActiveStatementPositionAsync(document2.Project.Solution, (_, _) => Task.FromResult(trackedActiveSpans2), activeInstruction2, CancellationToken.None).ConfigureAwait(false));
        }

        [Theory]
        [CombinatorialData]
        public async Task ActiveStatements_SyntaxErrorOrOutOfSyncDocument(bool isOutOfSync)
        {
            var sourceV1 = "class C { void F() => G(1); void G(int a) => System.Console.WriteLine(1); }";

            // syntax error (missing ';') unless testing out-of-sync document
            var sourceV2 = isOutOfSync ?
                "class C { int x; void F() => G(1); void G(int a) => System.Console.WriteLine(2); }" :
                "class C { int x void F() => G(1); void G(int a) => System.Console.WriteLine(2); }";

            using var workspace = TestWorkspace.CreateCSharp(sourceV1, composition: s_composition);
            var activeSpan11 = GetSpan(sourceV1, "G(1)");
            var activeSpan12 = GetSpan(sourceV1, "System.Console.WriteLine(1)");

            var project = workspace.CurrentSolution.Projects.Single();
            var document1 = project.Documents.Single();
            var documentId = document1.Id;
            var documentName = document1.Name;

            var sourceTextV1 = document1.GetTextSynchronously(CancellationToken.None);
            var sourceTextV2 = SourceText.From(sourceV2, Encoding.UTF8);

            var activeLineSpan11 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan11);
            var activeLineSpan12 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan12);
            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service,
                isOutOfSync ? CommittedSolution.DocumentState.OutOfSync : CommittedSolution.DocumentState.MatchesBuildOutput);

            var moduleId = Guid.NewGuid();
            var threadId = Guid.NewGuid();
            var activeInstruction1 = new ActiveInstructionId(moduleId, methodToken: 0x06000001, methodVersion: 1, ilOffset: 1);
            var activeInstruction2 = new ActiveInstructionId(moduleId, methodToken: 0x06000002, methodVersion: 1, ilOffset: 1);

            var activeStatements = ImmutableArray.Create(
                new ActiveStatementDebugInfo(
                    activeInstruction1,
                    documentName,
                    activeLineSpan11,
                    threadIds: ImmutableArray.Create(threadId),
                    ActiveStatementFlags.IsNonLeafFrame),
                new ActiveStatementDebugInfo(
                    activeInstruction2,
                    documentName,
                    activeLineSpan12,
                    threadIds: ImmutableArray.Create(threadId),
                    ActiveStatementFlags.IsLeafFrame));

            service.StartEditSession(_ => Task.FromResult(activeStatements));

            var editSession = service.Test_GetEditSession();

            // change the source (valid edit):
            workspace.ChangeDocument(document1.Id, sourceTextV2);
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var baseSpans = await service.GetBaseActiveStatementSpansAsync(ImmutableArray.Create(documentId), CancellationToken.None).ConfigureAwait(false);

            if (isOutOfSync)
            {
                Assert.Empty(baseSpans.Single());
            }
            else
            {
                AssertEx.Equal(new[]
                {
                    $"({activeLineSpan11}, IsNonLeafFrame)",
                    $"({activeLineSpan12}, IsLeafFrame)"
                }, baseSpans.Single().Select(s => s.ToString()));
            }

            // no active statements due to syntax error or out-of-sync document:
            var currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.True(currentSpans.IsDefault);

            Assert.Null(await service.GetCurrentActiveStatementPositionAsync(document2.Project.Solution, s_noSolutionActiveSpans, activeInstruction1, CancellationToken.None).ConfigureAwait(false));
            Assert.Null(await service.GetCurrentActiveStatementPositionAsync(document2.Project.Solution, s_noSolutionActiveSpans, activeInstruction2, CancellationToken.None).ConfigureAwait(false));
        }

        [Fact]
        public async Task ActiveStatements_ForeignDocument()
        {
            var composition = FeaturesTestCompositions.Features.AddParts(typeof(DummyLanguageService));

            using var workspace = new TestWorkspace(composition: composition);
            var solution = workspace.CurrentSolution;
            var project = solution.AddProject("dummy_proj", "dummy_proj", DummyLanguageService.LanguageName);
            var document = project.AddDocument("test", SourceText.From("dummy1"));
            workspace.ChangeSolution(document.Project.Solution);

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service);

            var activeStatements = ImmutableArray.Create(
                new ActiveStatementDebugInfo(
                    new ActiveInstructionId(default, methodToken: 0x06000001, methodVersion: 1, ilOffset: 0),
                    documentNameOpt: document.Name,
                    linePositionSpan: new LinePositionSpan(new LinePosition(0, 1), new LinePosition(0, 2)),
                    threadIds: ImmutableArray.Create(default(Guid)),
                    ActiveStatementFlags.IsNonLeafFrame));

            service.StartEditSession(_ => Task.FromResult(activeStatements));

            // active statements are tracked not in non-Roslyn projects:
            var currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.True(currentSpans.IsDefault);

            var baseSpans = await service.GetBaseActiveStatementSpansAsync(ImmutableArray.Create(document.Id), CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(baseSpans.Single());
        }
    }
}
