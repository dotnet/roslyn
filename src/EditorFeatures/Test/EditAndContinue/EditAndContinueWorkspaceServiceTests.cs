// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
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
using Microsoft.CodeAnalysis.ExternalAccess.Watch.Api;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    using static ActiveStatementTestHelpers;

    [UseExportProvider]
    public sealed partial class EditAndContinueWorkspaceServiceTests : TestBase
    {
        private static readonly TestComposition s_composition = FeaturesTestCompositions.Features;

        private static readonly ActiveStatementSpanProvider s_noActiveSpans =
            (_, _, _) => new(ImmutableArray<ActiveStatementSpan>.Empty);

        private const TargetFramework DefaultTargetFramework = TargetFramework.NetStandard20;

        private Func<Project, CompilationOutputs> _mockCompilationOutputsProvider;
        private readonly List<string> _telemetryLog;
        private int _telemetryId;

        private readonly MockManagedEditAndContinueDebuggerService _debuggerService;

        public EditAndContinueWorkspaceServiceTests()
        {
            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.NewGuid());
            _telemetryLog = new List<string>();

            _debuggerService = new MockManagedEditAndContinueDebuggerService()
            {
                LoadedModules = new Dictionary<Guid, ManagedEditAndContinueAvailability>()
            };
        }

        private TestWorkspace CreateWorkspace(out Solution solution, out EditAndContinueWorkspaceService service, Type[] additionalParts = null)
        {
            var workspace = new TestWorkspace(composition: s_composition.AddParts(additionalParts));
            solution = workspace.CurrentSolution;
            service = GetEditAndContinueService(workspace);
            return workspace;
        }

        private static SourceText GetAnalyzerConfigText((string key, string value)[] analyzerConfig)
            => SourceText.From("[*.*]" + Environment.NewLine + string.Join(Environment.NewLine, analyzerConfig.Select(c => $"{c.key} = {c.value}")));

        private static (Solution, Document) AddDefaultTestProject(
            Solution solution,
            string source,
            ISourceGenerator generator = null,
            string additionalFileText = null,
            (string key, string value)[] analyzerConfig = null)
        {
            solution = AddDefaultTestProject(solution, new[] { source }, generator, additionalFileText, analyzerConfig);
            return (solution, solution.Projects.Single().Documents.Single());
        }

        private static Solution AddDefaultTestProject(
            Solution solution,
            string[] sources,
            ISourceGenerator generator = null,
            string additionalFileText = null,
            (string key, string value)[] analyzerConfig = null)
        {
            var project = solution.
                AddProject("proj", "proj", LanguageNames.CSharp).
                WithMetadataReferences(TargetFrameworkUtil.GetReferences(DefaultTargetFramework));

            solution = project.Solution;

            if (generator != null)
            {
                solution = solution.AddAnalyzerReference(project.Id, new TestGeneratorReference(generator));
            }

            if (additionalFileText != null)
            {
                solution = solution.AddAdditionalDocument(DocumentId.CreateNewId(project.Id), "additional", SourceText.From(additionalFileText));
            }

            if (analyzerConfig != null)
            {
                solution = solution.AddAnalyzerConfigDocument(
                    DocumentId.CreateNewId(project.Id),
                    name: "config",
                    GetAnalyzerConfigText(analyzerConfig),
                    filePath: Path.Combine(TempRoot.Root, "config"));
            }

            Document document = null;
            var i = 1;
            foreach (var source in sources)
            {
                var fileName = $"test{i++}.cs";

                document = solution.GetProject(project.Id).
                    AddDocument(fileName, SourceText.From(source, Encoding.UTF8), filePath: Path.Combine(TempRoot.Root, fileName));

                solution = document.Project.Solution;
            }

            return document.Project.Solution;
        }

        private EditAndContinueWorkspaceService GetEditAndContinueService(Workspace workspace)
        {
            var service = (EditAndContinueWorkspaceService)workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
            var accessor = service.GetTestAccessor();
            accessor.SetOutputProvider(project => _mockCompilationOutputsProvider(project));
            accessor.SetReportTelemetry(data => EditAndContinueWorkspaceService.LogDebuggingSessionTelemetry(data, (id, message) => _telemetryLog.Add($"{id}: {message.GetMessage()}"), () => ++_telemetryId));
            return service;
        }

        private async Task<DebuggingSession> StartDebuggingSessionAsync(
            EditAndContinueWorkspaceService service,
            Solution solution,
            CommittedSolution.DocumentState initialState = CommittedSolution.DocumentState.MatchesBuildOutput)
        {
            await service.StartDebuggingSessionAsync(
                solution,
                _debuggerService,
                captureMatchingDocuments: false,
                CancellationToken.None);

            var session = service.GetTestAccessor().GetDebuggingSession();
            if (initialState != CommittedSolution.DocumentState.None)
            {
                SetDocumentsState(session, solution, initialState);
            }

            return session;
        }

        private void EnterBreakState(
            EditAndContinueWorkspaceService service,
            ImmutableArray<ManagedActiveStatementDebugInfo> activeStatements = default,
            ImmutableArray<DocumentId> documentsWithRudeEdits = default)
        {
            _debuggerService.GetActiveStatementsImpl = () => activeStatements.NullToEmpty();
            service.BreakStateEntered(out var documentsToReanalyze);
            AssertEx.Equal(documentsWithRudeEdits.NullToEmpty(), documentsToReanalyze);
        }

        private void ExitBreakState()
        {
            _debuggerService.GetActiveStatementsImpl = () => ImmutableArray<ManagedActiveStatementDebugInfo>.Empty;
        }

        private static void CommitSolutionUpdate(
            EditAndContinueWorkspaceService service,
            ImmutableArray<DocumentId> documentsWithRudeEdits = default)
        {
            service.CommitSolutionUpdate(out var documentsToReanalyze);
            AssertEx.Equal(documentsWithRudeEdits.NullToEmpty(), documentsToReanalyze);
        }

        private static void EndDebuggingSession(EditAndContinueWorkspaceService service, ImmutableArray<DocumentId> documentsWithRudeEdits = default)
        {
            service.EndDebuggingSession(out var documentsToReanalyze);
            AssertEx.Equal(documentsWithRudeEdits.NullToEmpty(), documentsToReanalyze);
        }

        private static async Task<(ManagedModuleUpdates updates, ImmutableArray<DiagnosticData> diagnostics)> EmitSolutionUpdateAsync(
            IEditAndContinueWorkspaceService service,
            Solution solution,
            ActiveStatementSpanProvider activeStatementSpanProvider = null)
        {
            var result = await service.EmitSolutionUpdateAsync(solution, activeStatementSpanProvider ?? s_noActiveSpans, CancellationToken.None);
            return (result.ModuleUpdates, result.GetDiagnosticData(solution));
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

        private static IEnumerable<string> InspectDiagnostics(ImmutableArray<DiagnosticData> actual)
            => actual.Select(d => $"{d.ProjectId} {InspectDiagnostic(d)}");

        private static string InspectDiagnostic(DiagnosticData diagnostic)
            => $"{diagnostic.Severity} {diagnostic.Id}: {diagnostic.Message}";

        internal static Guid ReadModuleVersionId(Stream stream)
        {
            using var peReader = new PEReader(stream);
            var metadataReader = peReader.GetMetadataReader();
            var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
            return metadataReader.GetGuid(mvidHandle);
        }

        private Guid EmitAndLoadLibraryToDebuggee(string source, string sourceFilePath = null, Encoding encoding = null, string assemblyName = "")
        {
            var moduleId = EmitLibrary(source, sourceFilePath, encoding, assemblyName);
            LoadLibraryToDebuggee(moduleId);
            return moduleId;
        }

        private void LoadLibraryToDebuggee(Guid moduleId, ManagedEditAndContinueAvailability availability = default)
        {
            _debuggerService.LoadedModules.Add(moduleId, availability);
        }

        private Guid EmitLibrary(
            string source,
            string sourceFilePath = null,
            Encoding encoding = null,
            string assemblyName = "",
            DebugInformationFormat pdbFormat = DebugInformationFormat.PortablePdb,
            ISourceGenerator generator = null,
            string additionalFileText = null,
            IEnumerable<(string, string)> analyzerOptions = null)
        {
            return EmitLibrary(new[] { (source, sourceFilePath ?? Path.Combine(TempRoot.Root, "test1.cs")) }, encoding, assemblyName, pdbFormat, generator, additionalFileText, analyzerOptions);
        }

        private Guid EmitLibrary(
            (string content, string filePath)[] sources,
            Encoding encoding = null,
            string assemblyName = "",
            DebugInformationFormat pdbFormat = DebugInformationFormat.PortablePdb,
            ISourceGenerator generator = null,
            string additionalFileText = null,
            IEnumerable<(string, string)> analyzerOptions = null)
        {
            encoding ??= Encoding.UTF8;

            var parseOptions = TestOptions.RegularPreview;

            var trees = sources.Select(source =>
            {
                var sourceText = SourceText.From(new MemoryStream(encoding.GetBytes(source.content)), encoding, checksumAlgorithm: SourceHashAlgorithm.Sha256);
                return SyntaxFactory.ParseSyntaxTree(sourceText, parseOptions, source.filePath);
            });

            Compilation compilation = CSharpTestBase.CreateCompilation(trees.ToArray(), options: TestOptions.DebugDll, targetFramework: DefaultTargetFramework, assemblyName: assemblyName);

            if (generator != null)
            {
                var optionsProvider = (analyzerOptions != null) ? new EditAndContinueTestAnalyzerConfigOptionsProvider(analyzerOptions) : null;
                var additionalTexts = (additionalFileText != null) ? new[] { new InMemoryAdditionalText("additional_file", additionalFileText) } : null;
                var generatorDriver = CSharpGeneratorDriver.Create(new[] { generator }, additionalTexts, parseOptions, optionsProvider);
                generatorDriver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
                generatorDiagnostics.Verify();
                compilation = outputCompilation;
            }

            return EmitLibrary(compilation, pdbFormat);
        }

        private Guid EmitLibrary(Compilation compilation, DebugInformationFormat pdbFormat = DebugInformationFormat.PortablePdb)
        {
            var (peImage, pdbImage) = compilation.EmitToArrays(new EmitOptions(debugInformationFormat: pdbFormat));
            var symReader = SymReaderTestHelpers.OpenDummySymReader(pdbImage);

            var moduleMetadata = ModuleMetadata.CreateFromImage(peImage);
            var moduleId = moduleMetadata.GetModuleVersionId();

            // associate the binaries with the project (assumes a single project)
            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId)
            {
                OpenAssemblyStreamImpl = () =>
                {
                    var stream = new MemoryStream();
                    peImage.WriteToStream(stream);
                    stream.Position = 0;
                    return stream;
                },
                OpenPdbStreamImpl = () =>
                {
                    var stream = new MemoryStream();
                    pdbImage.WriteToStream(stream);
                    stream.Position = 0;
                    return stream;
                }
            };

            return moduleId;
        }

        private static SourceText CreateSourceTextFromFile(string path)
        {
            using var stream = File.OpenRead(path);
            return SourceText.From(stream, Encoding.UTF8, SourceHashAlgorithm.Sha256);
        }

        private static TextSpan GetSpan(string str, string substr)
            => new TextSpan(str.IndexOf(substr), substr.Length);

        private static void VerifyReadersDisposed(IEnumerable<IDisposable> readers)
        {
            foreach (var reader in readers)
            {
                Assert.Throws<ObjectDisposedException>(() =>
                {
                    if (reader is MetadataReaderProvider md)
                    {
                        md.GetMetadataReader();
                    }
                    else
                    {
                        ((DebugInformationReaderProvider)reader).CreateEditAndContinueMethodDebugInfoReader();
                    }
                });
            }
        }

        private static DocumentInfo CreateDesignTimeOnlyDocument(ProjectId projectId, string name = "design-time-only.cs", string path = "design-time-only.cs")
            => DocumentInfo.Create(
                DocumentId.CreateNewId(projectId, name),
                name: name,
                folders: Array.Empty<string>(),
                sourceCodeKind: SourceCodeKind.Regular,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class DTO {}"), VersionStamp.Create(), path)),
                filePath: path,
                isGenerated: false,
                designTimeOnly: true,
                documentServiceProvider: null);

        internal sealed class FailingTextLoader : TextLoader
        {
            public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                Assert.True(false, $"Content of document {documentId} should never be loaded");
                throw ExceptionUtilities.Unreachable;
            }
        }

        [Fact]
        public async Task StartDebuggingSession_CapturingDocuments()
        {
            var encodingA = Encoding.BigEndianUnicode;
            var encodingB = Encoding.Unicode;
            var encodingC = Encoding.GetEncoding("SJIS");
            var encodingE = Encoding.UTF8;

            var sourceA1 = "class A {}";
            var sourceB1 = "class B { int F() => 1; }";
            var sourceB2 = "class B { int F() => 2; }";
            var sourceB3 = "class B { int F() => 3; }";
            var sourceC1 = "class C { const char L = 'ワ'; }";
            var sourceD1 = "dummy code";
            var sourceE1 = "class E { }";
            var sourceBytesA1 = encodingA.GetBytes(sourceA1);
            var sourceBytesB1 = encodingB.GetBytes(sourceB1);
            var sourceBytesC1 = encodingC.GetBytes(sourceC1);
            var sourceBytesE1 = encodingE.GetBytes(sourceE1);

            var dir = Temp.CreateDirectory();
            var sourceFileA = dir.CreateFile("A.cs").WriteAllBytes(sourceBytesA1);
            var sourceFileB = dir.CreateFile("B.cs").WriteAllBytes(sourceBytesB1);
            var sourceFileC = dir.CreateFile("C.cs").WriteAllBytes(sourceBytesC1);
            var sourceFileD = dir.CreateFile("dummy").WriteAllText(sourceD1);
            var sourceFileE = dir.CreateFile("E.cs").WriteAllBytes(sourceBytesE1);
            var sourceTreeA1 = SyntaxFactory.ParseSyntaxTree(SourceText.From(sourceBytesA1, sourceBytesA1.Length, encodingA, SourceHashAlgorithm.Sha256), TestOptions.Regular, sourceFileA.Path);
            var sourceTreeB1 = SyntaxFactory.ParseSyntaxTree(SourceText.From(sourceBytesB1, sourceBytesB1.Length, encodingB, SourceHashAlgorithm.Sha256), TestOptions.Regular, sourceFileB.Path);
            var sourceTreeC1 = SyntaxFactory.ParseSyntaxTree(SourceText.From(sourceBytesC1, sourceBytesC1.Length, encodingC, SourceHashAlgorithm.Sha1), TestOptions.Regular, sourceFileC.Path);

            // E is not included in the compilation:
            var compilation = CSharpTestBase.CreateCompilation(new[] { sourceTreeA1, sourceTreeB1, sourceTreeC1 }, options: TestOptions.DebugDll, targetFramework: DefaultTargetFramework, assemblyName: "P");
            EmitLibrary(compilation);

            // change content of B on disk:
            sourceFileB.WriteAllText(sourceB2, encodingB);

            // prepare workspace as if it was loaded from project files:
            using var _ = CreateWorkspace(out var solution, out var service, new[] { typeof(DummyLanguageService) });

            var projectP = solution.AddProject("P", "P", LanguageNames.CSharp);
            solution = projectP.Solution;

            var documentIdA = DocumentId.CreateNewId(projectP.Id, debugName: "A");
            solution = solution.AddDocument(DocumentInfo.Create(
                id: documentIdA,
                name: "A",
                loader: new FileTextLoader(sourceFileA.Path, encodingA),
                filePath: sourceFileA.Path));

            var documentIdB = DocumentId.CreateNewId(projectP.Id, debugName: "B");
            solution = solution.AddDocument(DocumentInfo.Create(
                id: documentIdB,
                name: "B",
                loader: new FileTextLoader(sourceFileB.Path, encodingB),
                filePath: sourceFileB.Path));

            var documentIdC = DocumentId.CreateNewId(projectP.Id, debugName: "C");
            solution = solution.AddDocument(DocumentInfo.Create(
                id: documentIdC,
                name: "C",
                loader: new FileTextLoader(sourceFileC.Path, encodingC),
                filePath: sourceFileC.Path));

            var documentIdE = DocumentId.CreateNewId(projectP.Id, debugName: "E");
            solution = solution.AddDocument(DocumentInfo.Create(
                id: documentIdE,
                name: "E",
                loader: new FileTextLoader(sourceFileE.Path, encodingE),
                filePath: sourceFileE.Path));

            // check that are testing documents whose hash algorithm does not match the PDB (but the hash itself does):
            Assert.Equal(SourceHashAlgorithm.Sha1, solution.GetDocument(documentIdA).GetTextSynchronously(default).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha1, solution.GetDocument(documentIdB).GetTextSynchronously(default).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha1, solution.GetDocument(documentIdC).GetTextSynchronously(default).ChecksumAlgorithm);
            Assert.Equal(SourceHashAlgorithm.Sha1, solution.GetDocument(documentIdE).GetTextSynchronously(default).ChecksumAlgorithm);

            // design-time-only document with and without absolute path:
            solution = solution.
                AddDocument(CreateDesignTimeOnlyDocument(projectP.Id, name: "dt1.cs", path: Path.Combine(dir.Path, "dt1.cs"))).
                AddDocument(CreateDesignTimeOnlyDocument(projectP.Id, name: "dt2.cs", path: "dt2.cs"));

            // project that does not support EnC - the contents of documents in this project shouldn't be loaded:
            var projectQ = solution.AddProject("Q", "Q", DummyLanguageService.LanguageName);
            solution = projectQ.Solution;

            solution = solution.AddDocument(DocumentInfo.Create(
                id: DocumentId.CreateNewId(projectQ.Id, debugName: "D"),
                name: "D",
                loader: new FailingTextLoader(),
                filePath: sourceFileD.Path));

            await service.StartDebuggingSessionAsync(solution, _debuggerService, captureMatchingDocuments: true, CancellationToken.None);

            var debuggingSession = service.GetTestAccessor().GetDebuggingSession();

            var matchingDocuments = debuggingSession.LastCommittedSolution.Test_GetDocumentStates();
            AssertEx.Equal(new[]
            {
                "(A, MatchesBuildOutput)",
                "(C, MatchesBuildOutput)"
            }, matchingDocuments.Select(e => (solution.GetDocument(e.id).Name, e.state)).OrderBy(e => e.Name).Select(e => e.ToString()));

            // change content of B on disk again:
            sourceFileB.WriteAllText(sourceB3, encodingB);
            solution = solution.WithDocumentTextLoader(documentIdB, new FileTextLoader(sourceFileB.Path, encodingB), PreservationMode.PreserveValue);

            EnterBreakState(service);

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            AssertEx.Equal(new[] { $"{projectP.Id} Warning ENC1005: {string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFileB.Path)}" }, InspectDiagnostics(emitDiagnostics));

            EndDebuggingSession(service);
        }

        [Fact]
        public async Task RunMode_ProjectThatDoesNotSupportEnC()
        {
            using var _ = CreateWorkspace(out var solution, out var service, new[] { typeof(DummyLanguageService) });
            var project = solution.AddProject("dummy_proj", "dummy_proj", DummyLanguageService.LanguageName);
            var document = project.AddDocument("test", SourceText.From("dummy1"));
            solution = document.Project.Solution;

            await StartDebuggingSessionAsync(service, solution);

            // no changes:
            var document1 = solution.Projects.Single().Documents.Single();
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            // change the source:
            solution = solution.WithDocumentText(document1.Id, SourceText.From("dummy2"));
            var document2 = solution.GetDocument(document1.Id);

            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task RunMode_DesignTimeOnlyDocument()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

            var documentInfo = CreateDesignTimeOnlyDocument(document1.Project.Id);
            solution = solution.WithProjectOutputFilePath(document1.Project.Id, moduleFile.Path).AddDocument(documentInfo);

            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

            await StartDebuggingSessionAsync(service, solution);

            // update a design-time-only source file:
            solution = solution.WithDocumentText(documentInfo.Id, SourceText.From("class UpdatedC2 {}"));
            var document2 = solution.GetDocument(documentInfo.Id);

            // no updates:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            // validate solution update status and emit - changes made in design-time-only documents are ignored:
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            EndDebuggingSession(service);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task RunMode_ProjectNotBuilt()
        {
            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.Empty);

            await StartDebuggingSessionAsync(service, solution);

            // no changes:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            // change the source:
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
            var document2 = solution.GetDocument(document1.Id);

            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public async Task RunMode_DifferentDocumentWithSameContent()
        {
            var source = "class C1 { void M1() { System.Console.WriteLine(1); } }";
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, source);

            solution = solution.WithProjectOutputFilePath(document.Project.Id, moduleFile.Path);
            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

            await StartDebuggingSessionAsync(service, solution);

            // update the document
            var document1 = solution.GetDocument(document.Id);
            solution = solution.WithDocumentText(document.Id, SourceText.From(source));
            var document2 = solution.GetDocument(document.Id);

            Assert.Equal(document1.Id, document2.Id);
            Assert.NotSame(document1, document2);

            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics2);

            // validate solution update status and emit - changes made during run mode are ignored:
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            EndDebuggingSession(service);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_ProjectThatDoesNotSupportEnC()
        {
            using var _ = CreateWorkspace(out var solution, out var service, new[] { typeof(DummyLanguageService) });
            var project = solution.AddProject("dummy_proj", "dummy_proj", DummyLanguageService.LanguageName);
            var document = project.AddDocument("test", SourceText.From("dummy1"));
            solution = document.Project.Solution;

            await StartDebuggingSessionAsync(service, solution);
            EnterBreakState(service);

            // change the source:
            var document1 = solution.Projects.Single().Documents.Single();
            solution = solution.WithDocumentText(document1.Id, SourceText.From("dummy2"));
            var document2 = solution.GetDocument(document1.Id);

            // validate solution update status and emit:
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);
        }

        [Fact]
        public async Task BreakMode_DesignTimeOnlyDocument_Dynamic()
        {
            using var _ = CreateWorkspace(out var solution, out var service);

            (solution, var document) = AddDefaultTestProject(solution, "class C {}");

            var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(document.Project.Id),
                name: "design-time-only.cs",
                folders: Array.Empty<string>(),
                sourceCodeKind: SourceCodeKind.Regular,
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From("class D {}"), VersionStamp.Create(), "design-time-only.cs")),
                filePath: "design-time-only.cs",
                isGenerated: false,
                designTimeOnly: true,
                documentServiceProvider: null);

            solution = solution.AddDocument(documentInfo);

            await StartDebuggingSessionAsync(service, solution);
            EnterBreakState(service);

            // change the source:
            var document1 = solution.GetDocument(documentInfo.Id);
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class E {}"));

            // validate solution update status and emit:
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);
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

            using var _ = CreateWorkspace(out var solution, out var service);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var documentA = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("a.cs", SourceText.From(sourceA, Encoding.UTF8), filePath: sourceFileA.Path);

            var documentB = documentA.Project.
                AddDocument("b.g.i.cs", SourceText.From(sourceB, Encoding.UTF8), filePath: "b.g.i.cs");

            var documentC = documentB.Project.
                AddDocument("c.g.i.vb", SourceText.From(sourceC, Encoding.UTF8), filePath: "c.g.i.vb");

            solution = documentC.Project.Solution;

            // only compile A; B and C are design-time-only:
            var moduleId = EmitLibrary(sourceA, sourceFilePath: sourceFileA.Path);

            if (!delayLoad)
            {
                LoadLibraryToDebuggee(moduleId);
            }

            await service.StartDebuggingSessionAsync(solution, _debuggerService, captureMatchingDocuments: false, CancellationToken.None);

            EnterBreakState(service);

            // change the source (rude edit):
            solution = solution.WithDocumentText(documentB.Id, SourceText.From("class B { public void RenamedMethod() { } }"));
            solution = solution.WithDocumentText(documentC.Id, SourceText.From("class C { public void RenamedMethod() { } }"));
            var documentB2 = solution.GetDocument(documentB.Id);
            var documentC2 = solution.GetDocument(documentC.Id);

            // no Rude Edits reported:
            Assert.Empty(await service.GetDocumentDiagnosticsAsync(documentB2, s_noActiveSpans, CancellationToken.None));
            Assert.Empty(await service.GetDocumentDiagnosticsAsync(documentC2, s_noActiveSpans, CancellationToken.None));

            // validate solution update status and emit:
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(emitDiagnostics);

            if (delayLoad)
            {
                LoadLibraryToDebuggee(moduleId);

                // validate solution update status and emit:
                Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

                (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
                Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
                Assert.Empty(emitDiagnostics);
            }

            EndDebuggingSession(service);
        }

        [Theory]
        [CombinatorialData]
        public async Task ErrorReadingModuleFile(bool breakMode)
        {
            // module file is empty, which will cause a read error:
            var moduleFile = Temp.CreateFile();

            string expectedErrorMessage = null;
            try
            {
                using var stream = File.OpenRead(moduleFile.Path);
                using var peReader = new PEReader(stream);
                _ = peReader.GetMetadataReader();
            }
            catch (Exception e)
            {
                expectedErrorMessage = e.Message;
            }

            using var _w = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

            await StartDebuggingSessionAsync(service, solution);

            if (breakMode)
            {
                EnterBreakState(service);
            }

            // change the source:
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
            var document2 = solution.GetDocument(document1.Id);

            // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            AssertEx.Equal(new[] { $"{document2.Project.Id} Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, moduleFile.Path, expectedErrorMessage)}" }, InspectDiagnostics(emitDiagnostics));

            if (breakMode)
            {
                ExitBreakState();
            }

            EndDebuggingSession(service);

            if (breakMode)
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                    "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC1001"
                }, _telemetryLog);
            }
            else
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0",
                }, _telemetryLog);
            }
        }

        [Fact]
        public async Task BreakMode_ErrorReadingPdbFile()
        {
            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1);

            using var _ = CreateWorkspace(out var solution, out var service);

            var document1 = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            solution = project.Solution;

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId)
            {
                OpenPdbStreamImpl = () =>
                {
                    throw new IOException("Error");
                }
            };

            await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);
            EnterBreakState(service);

            // change the source:
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = solution.GetDocument(document1.Id);

            // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            // an error occurred so we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            AssertEx.Equal(new[] { $"{project.Id} Warning ENC1006: {string.Format(FeaturesResources.UnableToReadSourceFileOrPdb, sourceFile.Path)}" }, InspectDiagnostics(emitDiagnostics));

            EndDebuggingSession(service);

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

            using var _ = CreateWorkspace(out var solution, out var service);

            var document1 = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(DefaultTargetFramework)).
                AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            solution = project.Solution;

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);
            EnterBreakState(service);

            // change the source:
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = solution.GetDocument(document1.Id);

            using var fileLock = File.Open(sourceFile.Path, FileMode.Open, FileAccess.Read, FileShare.None);

            // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            // an error occurred so we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            // try apply changes:
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            AssertEx.Equal(new[] { $"{project.Id} Warning ENC1006: {string.Format(FeaturesResources.UnableToReadSourceFileOrPdb, sourceFile.Path)}" }, InspectDiagnostics(emitDiagnostics));

            fileLock.Dispose();

            // try apply changes again:
            (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            Assert.NotEmpty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndDebuggingSession(service);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0"
            }, _telemetryLog);
        }

        [Theory]
        [CombinatorialData]
        public async Task FileAdded(bool breakMode)
        {
            var sourceA = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var sourceB = "class C2 {}";

            var sourceFileA = Temp.CreateFile().WriteAllText(sourceA);
            var sourceFileB = Temp.CreateFile().WriteAllText(sourceB);

            using var _ = CreateWorkspace(out var solution, out var service);

            var documentA = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(sourceA, Encoding.UTF8), filePath: sourceFileA.Path);

            solution = documentA.Project.Solution;

            // Source B will be added while debugging.
            EmitAndLoadLibraryToDebuggee(sourceA, sourceFilePath: sourceFileA.Path);

            var project = documentA.Project;

            await StartDebuggingSessionAsync(service, solution);

            if (breakMode)
            {
                EnterBreakState(service);
            }

            // add a source file:
            var documentB = project.AddDocument("file2.cs", SourceText.From(sourceB, Encoding.UTF8), filePath: sourceFileB.Path);
            solution = documentB.Project.Solution;
            documentB = solution.GetDocument(documentB.Id);

            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(documentB, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics2);

            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            if (breakMode)
            {
                ExitBreakState();
            }

            EndDebuggingSession(service);

            if (breakMode)
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0"
                }, _telemetryLog);
            }
            else
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
                }, _telemetryLog);
            }
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
            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, source1);

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId);

            LoadLibraryToDebuggee(moduleId, new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.NotAllowedForRuntime, "*message*"));

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);

            // change the source:
            var document1 = solution.Projects.Single().Documents.Single();
            solution = solution.WithDocumentText(document1.Id, SourceText.From(source2));
            var document2 = solution.GetDocument(document1.Id);

            // We do not report module diagnostics until emit.
            // This is to make the analysis deterministic (not dependent on the current state of the debuggee).
            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            AssertEx.Equal(new[] { $"{document2.Project.Id} Error ENC2016: {string.Format(FeaturesResources.EditAndContinueDisallowedByProject, document2.Project.Name, "*message*")}" }, InspectDiagnostics(emitDiagnostics));

            EndDebuggingSession(service);

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC2016"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_Encodings()
        {
            var source1 = "class C1 { void M() { System.Console.WriteLine(\"ã\"); } }";

            var encoding = Encoding.GetEncoding(1252);

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(source1, encoding);

            using var _ = CreateWorkspace(out var solution, out var service);

            var document1 = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, encoding), filePath: sourceFile.Path);

            var documentId = document1.Id;

            var project = document1.Project;
            solution = project.Solution;

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path, encoding: encoding);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);

            EnterBreakState(service);

            // Emulate opening the file, which will trigger "out-of-sync" check.
            // Since we find content matching the PDB checksum we update the committed solution with this source text.
            // If we used wrong encoding this would lead to a false change detected below.
            var currentDocument = solution.GetDocument(documentId);
            await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(currentDocument, debuggingSession.CancellationToken);

            // EnC service queries for a document, which triggers read of the source file from disk.
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            EndDebuggingSession(service);
        }

        [Theory]
        [CombinatorialData]
        public async Task RudeEdits(bool breakMode)
        {
            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var source2 = "class C1 { void M1() { System.Console.WriteLine(1); } }";

            var moduleId = Guid.NewGuid();

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, source1);

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            if (breakMode)
            {
                EnterBreakState(service);
            }

            // change the source (rude edit):
            var document1 = solution.Projects.Single().Documents.Single();
            solution = solution.WithDocumentText(document1.Id, SourceText.From(source2, Encoding.UTF8));
            var document2 = solution.GetDocument(document1.Id);

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Equal(new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics1.Select(d => $"{d.Id}: {d.GetMessage()}"));

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            if (breakMode)
            {
                ExitBreakState();
            }

            EndDebuggingSession(service, documentsWithRudeEdits: ImmutableArray.Create(document2.Id));

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

            if (breakMode)
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=0",
                    "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=2|RudeEditKind=20|RudeEditSyntaxKind=8875|RudeEditBlocking=True"
                }, _telemetryLog);
            }
            else
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0",
                }, _telemetryLog);
            }
        }

        [Fact]
        public async Task BreakMode_RudeEdits_SourceGenerators()
        {
            var sourceV1 = @"
/* GENERATE: class G { int X1 => 1; } */

class C { int Y => 1; } 
";
            var sourceV2 = @"
/* GENERATE: class G { int X2 => 1; } */

class C { int Y => 2; }
";

            var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, sourceV1, generator: generator);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);
            EnterBreakState(service);

            // change the source:
            var document1 = solution.Projects.Single().Documents.Single();
            solution = solution.WithDocumentText(document1.Id, SourceText.From(sourceV2, Encoding.UTF8));

            var generatedDocument = (await solution.Projects.Single().GetSourceGeneratedDocumentsAsync()).Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(generatedDocument, s_noActiveSpans, CancellationToken.None);
            AssertEx.Equal(new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.property_) },
                diagnostics1.Select(d => $"{d.Id}: {d.GetMessage()}"));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndDebuggingSession(service, documentsWithRudeEdits: ImmutableArray.Create(generatedDocument.Id));
        }

        [Theory]
        [CombinatorialData]
        public async Task RudeEdits_DocumentOutOfSync(bool breakMode)
        {
            var source0 = "class C1 { void M() { System.Console.WriteLine(0); } }";
            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var source2 = "class C1 { void RenamedMethod() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs");

            using var _ = CreateWorkspace(out var solution, out var service);

            var project = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40));

            solution = project.Solution;

            // compile with source0:
            var moduleId = EmitAndLoadLibraryToDebuggee(source0, sourceFilePath: sourceFile.Path);

            // update the file with source1 before session starts:
            sourceFile.WriteAllText(source1);

            // source1 is reflected in workspace before session starts:
            var document1 = project.AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);
            solution = document1.Project.Solution;

            var debuggingSession = await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);

            if (breakMode)
            {
                EnterBreakState(service);
            }

            // change the source (rude edit):
            solution = solution.WithDocumentText(document1.Id, SourceText.From(source2));
            var document2 = solution.GetDocument(document1.Id);

            // no Rude Edits, since the document is out-of-sync
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            // since the document is out-of-sync we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            AssertEx.Equal(new[] { $"{project.Id} Warning ENC1005: {string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFile.Path)}" }, InspectDiagnostics(emitDiagnostics));

            // update the file to match the build:
            sourceFile.WriteAllText(source0);

            // we do not reload the content of out-of-sync file for analyzer query:
            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            // debugger query will trigger reload of out-of-sync file content:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            // now we see the rude edit:
            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Equal(new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
               diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            if (breakMode)
            {
                ExitBreakState();
            }

            EndDebuggingSession(service, documentsWithRudeEdits: ImmutableArray.Create(document2.Id));

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

            if (breakMode)
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=0",
                    "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=2|RudeEditKind=20|RudeEditSyntaxKind=8875|RudeEditBlocking=True"
                }, _telemetryLog);
            }
            else
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0",
                }, _telemetryLog);
            }
        }

        [Fact]
        public async Task BreakMode_RudeEdits_DocumentWithoutSequencePoints()
        {
            var source1 = "abstract class C { public abstract void M(); }";
            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1);

            using var _ = CreateWorkspace(out var solution, out var service);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            solution = project.Solution;

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            // do not initialize the document state - we will detect the state based on the PDB content.
            var debuggingSession = await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);

            EnterBreakState(service);

            // change the source (rude edit since the base document content matches the PDB checksum, so the document is not out-of-sync):
            solution = solution.WithDocumentText(document1.Id, SourceText.From("abstract class C { public abstract void M(); public abstract void N(); }"));
            var document2 = solution.Projects.Single().Documents.Single();

            // Rude Edits reported:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Equal(
                new[] { "ENC0023: " + string.Format(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndDebuggingSession(service, documentsWithRudeEdits: ImmutableArray.Create(document2.Id));
        }

        [Fact]
        public async Task BreakMode_RudeEdits_DelayLoadedModule()
        {
            var source1 = "class C { public void M() { } }";
            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1);

            using var _ = CreateWorkspace(out var solution, out var service);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            solution = project.Solution;

            var moduleId = EmitLibrary(source1, sourceFilePath: sourceFile.Path);

            // do not initialize the document state - we will detect the state based on the PDB content.
            var debuggingSession = await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);

            EnterBreakState(service);

            // change the source (rude edit) before the library is loaded:
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C { public void Renamed() { } }"));
            var document2 = solution.Projects.Single().Documents.Single();

            // Rude Edits reported:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Equal(
                new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            // load library to the debuggee:
            LoadLibraryToDebuggee(moduleId);

            // Rude Edits still reported:
            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Equal(
                new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndDebuggingSession(service, documentsWithRudeEdits: ImmutableArray.Create(document2.Id));
        }

        [Fact]
        public async Task BreakMode_SyntaxError()
        {
            var moduleId = Guid.NewGuid();

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);

            // change the source (compilation error):
            var document1 = solution.Projects.Single().Documents.Single();
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { "));
            var document2 = solution.Projects.Single().Documents.Single();

            // compilation errors are not reported via EnC diagnostic analyzer:
            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndDebuggingSession(service);

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=True|HadRudeEdits=False|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_SemanticError()
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, sourceV1);

            var moduleId = EmitAndLoadLibraryToDebuggee(sourceV1);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);

            // change the source (compilation error):
            var document1 = solution.Projects.Single().Documents.Single();
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { int i = 0L; System.Console.WriteLine(i); } }", Encoding.UTF8));
            var document2 = solution.Projects.Single().Documents.Single();

            // compilation errors are not reported via EnC diagnostic analyzer:
            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Empty(diagnostics1);

            // The EnC analyzer does not check for and block on all semantic errors as they are already reported by diagnostic analyzer.
            // Blocking update on semantic errors would be possible, but the status check is only an optimization to avoid emitting.
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);

            // TODO: https://github.com/dotnet/roslyn/issues/36061
            // Semantic errors should not be reported in emit diagnostics.

            AssertEx.Equal(new[] { $"{document2.Project.Id} Error CS0266: {string.Format(CSharpResources.ERR_NoImplicitConvCast, "long", "int")}" }, InspectDiagnostics(emitDiagnostics));

            EndDebuggingSession(service);

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

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
            using var _ = CreateWorkspace(out var solution, out var service);

            solution = solution.
                AddProject("A", "A", "C#").
                AddDocument("A.cs", "class Program { void Main() { System.Console.WriteLine(1); } }", filePath: "A.cs").Project.Solution.
                AddProject("B", "B", "C#").
                AddDocument("Common.cs", "class Common {}", filePath: "Common.cs").Project.
                AddDocument("B.cs", "class B {}", filePath: "B.cs").Project.Solution.
                AddProject("C", "C", "C#").
                AddDocument("Common.cs", "class Common {}", filePath: "Common.cs").Project.
                AddDocument("C.cs", "class C {}", filePath: "C.cs").Project.Solution;

            await StartDebuggingSessionAsync(service, solution);
            EnterBreakState(service);

            // change C.cs to have a compilation error:
            var projectC = solution.GetProjectsByName("C").Single();
            var documentC = projectC.Documents.Single(d => d.Name == "C.cs");
            solution = solution.WithDocumentText(documentC.Id, SourceText.From("class C { void M() { "));

            // Common.cs is included in projects B and C. Both of these projects must have no errors, otherwise update is blocked.
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: "Common.cs", CancellationToken.None));

            // No changes in project containing file B.cs.
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: "B.cs", CancellationToken.None));

            // All projects must have no errors.
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_EmitError()
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var _ = CreateWorkspace(out var solution, out var service);

            (solution, var document) = AddDefaultTestProject(solution, sourceV1);
            EmitAndLoadLibraryToDebuggee(sourceV1);

            await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);
            var editSession = service.GetTestAccessor().GetEditSession();

            // change the source (valid edit but passing no encoding to emulate emit error):
            var document1 = solution.Projects.Single().Documents.Single();
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", encoding: null));
            var document2 = solution.Projects.Single().Documents.Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            AssertEx.Equal(new[] { $"{document2.Project.Id} Error CS8055: {string.Format(CSharpResources.ERR_EncodinglessSyntaxTree)}" }, InspectDiagnostics(emitDiagnostics));

            // no emitted delta:
            Assert.Empty(updates.Updates);

            // no pending update:
            Assert.Null(editSession.GetTestAccessor().GetPendingSolutionUpdate());

            Assert.Throws<InvalidOperationException>(() => service.CommitSolutionUpdate(out var _));
            Assert.Throws<InvalidOperationException>(() => service.DiscardSolutionUpdate());

            // no change in non-remappable regions since we didn't have any active statements:
            Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

            // solution update status after discarding an update (still has update ready):
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            EndDebuggingSession(service);

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

            using var _ = CreateWorkspace(out var solution, out var service);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(DefaultTargetFramework)).
                AddDocument("test.cs", SourceText.From("class C1 { void M() { System.Console.WriteLine(0); } }", Encoding.UTF8), filePath: sourceFile.Path);

            var documentId = document1.Id;
            solution = document1.Project.Solution;

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);

            EnterBreakState(service);

            // The user opens the source file and changes the source before Roslyn receives file watcher event.
            var source2 = "class C1 { void M() { System.Console.WriteLine(2); } }";
            solution = solution.WithDocumentText(documentId, SourceText.From(source2, Encoding.UTF8));
            var document2 = solution.GetDocument(documentId);

            // Save the document:
            if (saveDocument)
            {
                await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(document2, debuggingSession.CancellationToken);
                sourceFile.WriteAllText(source2);
            }

            // EnC service queries for a document, which triggers read of the source file from disk.
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);

            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            CommitSolutionUpdate(service);

            ExitBreakState();

            EnterBreakState(service);

            // file watcher updates the workspace:
            solution = solution.WithDocumentText(documentId, CreateSourceTextFromFile(sourceFile.Path));
            var document3 = solution.Projects.Single().Documents.Single();

            var hasChanges = await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None);
            (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);

            if (saveDocument)
            {
                Assert.False(hasChanges);
                Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            }
            else
            {
                Assert.True(hasChanges);
                Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            }

            ExitBreakState();
            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_FileUpdateNotObservedBeforeDebuggingSessionStart()
        {
            // workspace:     --V0--------------V2-------|--------V3------------------V1--------------|
            // file system:   --V0---------V1-----V2-----|------------------------------V1------------|
            //                   \--build--/      ^save  F5   ^      ^F10 (no change)   ^save         F10 (ok)
            //                                                file watcher: no-op
            // build updates file from V0 -> V1

            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var source2 = "class C1 { void M() { System.Console.WriteLine(2); } }";
            var source3 = "class C1 { void M() { System.Console.WriteLine(3); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(source2);

            using var _ = CreateWorkspace(out var solution, out var service);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document2 = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source2, Encoding.UTF8), filePath: sourceFile.Path);

            var documentId = document2.Id;

            var project = document2.Project;
            solution = project.Solution;

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);

            EnterBreakState(service);

            // user edits the file:
            solution = solution.WithDocumentText(documentId, SourceText.From(source3, Encoding.UTF8));
            var document3 = solution.Projects.Single().Documents.Single();

            // EnC service queries for a document, but the source file on disk doesn't match the PDB

            // We don't report rude edits for out-of-sync documents:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document3, s_noActiveSpans, CancellationToken.None);
            AssertEx.Empty(diagnostics);

            // since the document is out-of-sync we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            AssertEx.Equal(new[] { $"{project.Id} Warning ENC1005: {string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFile.Path)}" }, InspectDiagnostics(emitDiagnostics));

            // undo:
            solution = solution.WithDocumentText(documentId, SourceText.From(source1, Encoding.UTF8));

            var currentDocument = solution.GetDocument(documentId);

            // save (note that this call will fail to match the content with the PDB since it uses the content prior to the actual file write)
            await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(currentDocument, debuggingSession.CancellationToken);
            var (doc, state) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(documentId, currentDocument, CancellationToken.None);
            Assert.Null(doc);
            Assert.Equal(CommittedSolution.DocumentState.OutOfSync, state);
            sourceFile.WriteAllText(source1);

            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));
            (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);

            // the content actually hasn't changed:
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);

            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_AddedFileNotObservedBeforeDebuggingSessionStart()
        {
            // workspace:     ------|----V0---------------|----------
            // file system:   --V0--|---------------------|----------
            //                      F5   ^          ^F10 (no change)
            //                           file watcher observes the file

            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(source1);

            using var _ = CreateWorkspace(out var solution, out var service);

            // the workspace starts with no file
            var project = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40));

            solution = project.Solution;

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            _debuggerService.IsEditAndContinueAvailable = _ => new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.Attach, localizedMessage: "*attached*");

            var debuggingSession = await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);

            // An active statement may be present in the added file since the file exists in the PDB:
            var activeInstruction1 = new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 1);
            var activeSpan1 = GetSpan(source1, "System.Console.WriteLine(1);");
            var sourceText1 = SourceText.From(source1, Encoding.UTF8);
            var activeLineSpan1 = sourceText1.Lines.GetLinePositionSpan(activeSpan1);
            var activeStatements = ImmutableArray.Create(
                new ManagedActiveStatementDebugInfo(
                    activeInstruction1,
                    "test.cs",
                    activeLineSpan1.ToSourceSpan(),
                    ActiveStatementFlags.IsLeafFrame));

            // disallow any edits (attach scenario)
            EnterBreakState(service, activeStatements);

            // File watcher observes the document and adds it to the workspace:

            var document1 = project.AddDocument("test.cs", sourceText1, filePath: sourceFile.Path);
            solution = document1.Project.Solution;

            // We don't report rude edits for the added document:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noActiveSpans, CancellationToken.None);
            AssertEx.Empty(diagnostics);

            // TODO: https://github.com/dotnet/roslyn/issues/49938
            // We currently create the AS map against the committed solution, which may not contain all documents.
            // var spans = await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(document1.Id), CancellationToken.None);
            // AssertEx.Equal(new[] { $"({activeLineSpan1}, IsLeafFrame)" }, spans.Single().Select(s => s.ToString()));

            // No changes.
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);

            AssertEx.Empty(emitDiagnostics);

            EndDebuggingSession(service);
        }

        [Theory]
        [CombinatorialData]
        public async Task BreakMode_ValidSignificantChange_DocumentOutOfSync(bool delayLoad)
        {
            var sourceOnDisk = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(sourceOnDisk);

            using var _ = CreateWorkspace(out var solution, out var service);

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = solution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From("class C1 { void M() { System.Console.WriteLine(0); } }", Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            solution = project.Solution;

            var moduleId = EmitLibrary(sourceOnDisk, sourceFilePath: sourceFile.Path);

            if (!delayLoad)
            {
                LoadLibraryToDebuggee(moduleId);
            }

            var debuggingSession = await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.None);

            EnterBreakState(service);

            // no changes have been made to the project
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            // a file watcher observed a change and updated the document, so it now reflects the content on disk (the code that we compiled):
            solution = solution.WithDocumentText(document1.Id, SourceText.From(sourceOnDisk, Encoding.UTF8));
            var document3 = solution.Projects.Single().Documents.Single();

            var diagnostics = await service.GetDocumentDiagnosticsAsync(document3, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            // the content of the file is now exactly the same as the compiled document, so there is no change to be applied:
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(emitDiagnostics);

            EndDebuggingSession(service);

            Assert.Empty(debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());
        }

        [Theory]
        [CombinatorialData]
        public async Task ValidSignificantChange_EmitSuccessful(bool breakMode, bool commitUpdate)
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var sourceV2 = "class C1 { void M() { System.Console.WriteLine(2); } }";

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, sourceV1);

            var moduleId = EmitAndLoadLibraryToDebuggee(sourceV1);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            if (breakMode)
            {
                EnterBreakState(service);
            }

            var editSession = service.GetTestAccessor().GetEditSession();

            // change the source (valid edit):
            solution = solution.WithDocumentText(document1.Id, SourceText.From(sourceV2, Encoding.UTF8));
            var document2 = solution.GetDocument(document1.Id);

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            ValidateDelta(updates.Updates.Single());

            void ValidateDelta(ManagedModuleUpdate delta)
            {
                // check emitted delta:
                Assert.Empty(delta.ActiveStatements);
                Assert.NotEmpty(delta.ILDelta);
                Assert.NotEmpty(delta.MetadataDelta);
                Assert.NotEmpty(delta.PdbDelta);
                Assert.Equal(0x06000001, delta.UpdatedMethods.Single());
                Assert.Equal(moduleId, delta.Module);
                Assert.Empty(delta.ExceptionRegions);
                Assert.Empty(delta.SequencePoints);
            }

            // the update should be stored on the service:
            var pendingUpdate = editSession.GetTestAccessor().GetPendingSolutionUpdate();
            var (baselineProjectId, newBaseline) = pendingUpdate.EmitBaselines.Single();
            AssertEx.Equal(updates.Updates, pendingUpdate.Deltas);
            Assert.Equal(document2.Project.Id, baselineProjectId);
            Assert.Equal(moduleId, newBaseline.OriginalMetadata.GetModuleVersionId());

            var readers = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
            Assert.Equal(2, readers.Length);
            Assert.NotNull(readers[0]);
            Assert.NotNull(readers[1]);

            if (commitUpdate)
            {
                // all update providers either provided updates or had no change to apply:
                CommitSolutionUpdate(service);

                Assert.Null(editSession.GetTestAccessor().GetPendingSolutionUpdate());

                // no change in non-remappable regions since we didn't have any active statements:
                Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

                var baselineReaders = editSession.DebuggingSession.GetTestAccessor().GetBaselineModuleReaders();
                Assert.Equal(2, baselineReaders.Length);
                Assert.Same(readers[0], baselineReaders[0]);
                Assert.Same(readers[1], baselineReaders[1]);

                // verify that baseline is added:
                Assert.Same(newBaseline, editSession.DebuggingSession.GetTestAccessor().GetProjectEmitBaseline(document2.Project.Id));

                // solution update status after committing an update:
                var commitedUpdateSolutionStatus = await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None);
                Assert.False(commitedUpdateSolutionStatus);
            }
            else
            {
                // another update provider blocked the update:
                service.DiscardSolutionUpdate();

                Assert.Null(editSession.GetTestAccessor().GetPendingSolutionUpdate());

                // solution update status after committing an update:
                var discardedUpdateSolutionStatus = await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None);
                Assert.True(discardedUpdateSolutionStatus);

                (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
                Assert.Empty(emitDiagnostics);
                Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

                ValidateDelta(updates.Updates.Single());
            }

            if (breakMode)
            {
                ExitBreakState();
            }

            EndDebuggingSession(service);

            // open module readers should be disposed when the debugging session ends:
            VerifyReadersDisposed(readers);

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

            if (breakMode)
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0",
                }, _telemetryLog);
            }
            else
            {
                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0",
                }, _telemetryLog);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BreakMode_ValidSignificantChange_EmitSuccessful_UpdateDeferred(bool commitUpdate)
        {
            var dir = Temp.CreateDirectory();

            var sourceV1 = "class C1 { void M1() { int a = 1; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(1); } }";
            var compilationV1 = CSharpTestBase.CreateCompilation(sourceV1, options: TestOptions.DebugDll, targetFramework: DefaultTargetFramework, assemblyName: "lib");

            var (peImage, pdbImage) = compilationV1.EmitToArrays(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
            var moduleMetadata = ModuleMetadata.CreateFromImage(peImage);
            var moduleFile = dir.CreateFile("lib.dll").WriteAllBytes(peImage);
            var pdbFile = dir.CreateFile("lib.pdb").WriteAllBytes(pdbImage);
            var moduleId = moduleMetadata.GetModuleVersionId();

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, sourceV1);

            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path, pdbFile.Path);

            // set up an active statement in the first method, so that we can test preservation of local signature.
            var activeStatements = ImmutableArray.Create(new ManagedActiveStatementDebugInfo(
                new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 0),
                documentName: document1.Name,
                sourceSpan: new SourceSpan(0, 15, 0, 16),
                ActiveStatementFlags.IsLeafFrame));

            await StartDebuggingSessionAsync(service, solution);
            var debuggingSession = service.GetTestAccessor().GetDebuggingSession();

            // module is not loaded:
            EnterBreakState(service, activeStatements);
            var editSession = service.GetTestAccessor().GetEditSession();

            // change the source (valid edit):
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M1() { int a = 1; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = solution.GetDocument(document1.Id);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            Assert.Empty(emitDiagnostics);

            // delta to apply:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(0x06000002, delta.UpdatedMethods.Single());
            Assert.Equal(moduleId, delta.Module);
            Assert.Empty(delta.ExceptionRegions);
            Assert.Empty(delta.SequencePoints);

            // the update should be stored on the service:
            var pendingUpdate = editSession.GetTestAccessor().GetPendingSolutionUpdate();
            var (baselineProjectId, newBaseline) = pendingUpdate.EmitBaselines.Single();

            var readers = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
            Assert.Equal(2, readers.Length);
            Assert.NotNull(readers[0]);
            Assert.NotNull(readers[1]);

            Assert.Equal(document2.Project.Id, baselineProjectId);
            Assert.Equal(moduleId, newBaseline.OriginalMetadata.GetModuleVersionId());

            if (commitUpdate)
            {
                CommitSolutionUpdate(service);
                Assert.Null(editSession.GetTestAccessor().GetPendingSolutionUpdate());

                // no change in non-remappable regions since we didn't have any active statements:
                Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

                // verify that baseline is added:
                Assert.Same(newBaseline, editSession.DebuggingSession.GetTestAccessor().GetProjectEmitBaseline(document2.Project.Id));

                // solution update status after committing an update:
                Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

                ExitBreakState();

                // make another update:
                EnterBreakState(service);

                // Update M1 - this method has an active statement, so we will attempt to preserve the local signature.
                // Since the method hasn't been edited before we'll read the baseline PDB to get the signature token.
                // This validates that the Portable PDB reader can be used (and is not disposed) for a second generation edit.
                var document3 = solution.GetDocument(document1.Id);
                solution = solution.WithDocumentText(document3.Id, SourceText.From("class C1 { void M1() { int a = 3; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
                Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
                Assert.Empty(emitDiagnostics);
            }
            else
            {
                service.DiscardSolutionUpdate();
                Assert.Null(editSession.GetTestAccessor().GetPendingSolutionUpdate());
            }

            ExitBreakState();
            EndDebuggingSession(service);

            // open module readers should be disposed when the debugging session ends:
            VerifyReadersDisposed(readers);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_PartialTypes()
        {
            var sourceA1 = @"
partial class C { int X = 1; void F() { X = 1; } }

partial class D { int U = 1; public D() { } }
partial class D { int W = 1; }

partial class E { int A; public E(int a) { A = a; } }
";
            var sourceB1 = @"
partial class C { int Y = 1; }
partial class E { int B; public E(int a, int b) { A = a; B = new System.Func<int>(() => b)(); } }
";

            var sourceA2 = @"
partial class C { int X = 2; void F() { X = 2; } }

partial class D { int U = 2; }
partial class D { int W = 2; public D() { } }

partial class E { int A = 1; public E(int a) { A = a; } }
";
            var sourceB2 = @"
partial class C { int Y = 2; }
partial class E { int B = 2; public E(int a, int b) { A = a; B = new System.Func<int>(() => b)(); } }
";

            using var _ = CreateWorkspace(out var solution, out var service);
            solution = AddDefaultTestProject(solution, new[] { sourceA1, sourceB1 });
            var project = solution.Projects.Single();

            LoadLibraryToDebuggee(EmitLibrary(new[] { (sourceA1, "test1.cs"), (sourceB1, "test2.cs") }));

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);
            var editSession = service.GetTestAccessor().GetEditSession();

            // change the source (valid edit):
            var documentA = project.Documents.First();
            var documentB = project.Documents.Skip(1).First();
            solution = solution.WithDocumentText(documentA.Id, SourceText.From(sourceA2, Encoding.UTF8));
            solution = solution.WithDocumentText(documentB.Id, SourceText.From(sourceB2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(6, delta.UpdatedMethods.Length);  // F, C.C(), D.D(), E.E(int), E.E(int, int), lambda

            EndDebuggingSession(service);
        }

        private static EditAndContinueLogEntry Row(int rowNumber, TableIndex table, EditAndContinueOperation operation)
            => new(MetadataTokens.Handle(table, rowNumber), operation);

        private static unsafe void VerifyEncLogMetadata(ImmutableArray<byte> delta, params EditAndContinueLogEntry[] expectedRows)
        {
            fixed (byte* ptr = delta.ToArray())
            {
                var reader = new MetadataReader(ptr, delta.Length);
                AssertEx.Equal(expectedRows, reader.GetEditAndContinueLogEntries(), itemInspector: EncLogRowToString);
            }

            static string EncLogRowToString(EditAndContinueLogEntry row)
            {
                TableIndex tableIndex;
                MetadataTokens.TryGetTableIndex(row.Handle.Kind, out tableIndex);

                return string.Format(
                    "Row({0}, TableIndex.{1}, EditAndContinueOperation.{2})",
                    MetadataTokens.GetRowNumber(row.Handle),
                    tableIndex,
                    row.Operation);
            }
        }

        private static void GenerateSource(GeneratorExecutionContext context)
        {
            foreach (var syntaxTree in context.Compilation.SyntaxTrees)
            {
                var fileName = PathUtilities.GetFileName(syntaxTree.FilePath);

                Generate(syntaxTree.GetText().ToString(), fileName);

                if (context.AnalyzerConfigOptions.GetOptions(syntaxTree).TryGetValue("enc_generator_output", out var optionValue))
                {
                    context.AddSource("GeneratedFromOptions_" + fileName, $"class G {{ int X => {optionValue}; }}");
                }
            }

            foreach (var additionalFile in context.AdditionalFiles)
            {
                Generate(additionalFile.GetText()!.ToString(), PathUtilities.GetFileName(additionalFile.Path));
            }

            void Generate(string source, string fileName)
            {
                var generatedSource = GetGeneratedCodeFromMarkedSource(source);
                if (generatedSource != null)
                {
                    context.AddSource($"Generated_{fileName}", generatedSource);
                }
            }
        }

        private static string GetGeneratedCodeFromMarkedSource(string markedSource)
        {
            const string OpeningMarker = "/* GENERATE:";
            const string ClosingMarker = "*/";

            var index = markedSource.IndexOf(OpeningMarker);
            if (index > 0)
            {
                index += OpeningMarker.Length;
                var closing = markedSource.IndexOf(ClosingMarker, index);
                return markedSource[index..closing].Trim();
            }

            return null;
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_SourceGenerators_DocumentUpdate_GeneratedDocumentUpdate()
        {
            var sourceV1 = @"
/* GENERATE: class G { int X => 1; } */

class C { int Y => 1; } 
";
            var sourceV2 = @"
/* GENERATE: class G { int X => 2; } */

class C { int Y => 2; }
";

            var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, sourceV1, generator);

            var moduleId = EmitLibrary(sourceV1, generator: generator);
            LoadLibraryToDebuggee(moduleId);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);
            var editSession = service.GetTestAccessor().GetEditSession();

            // change the source (valid edit)
            solution = solution.WithDocumentText(document1.Id, SourceText.From(sourceV2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(2, delta.UpdatedMethods.Length);

            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_SourceGenerators_DocumentUpdate_GeneratedDocumentUpdate_LineChanges()
        {
            var sourceV1 = @"
/* GENERATE:
class G
{
    int M() 
    {
        return 1;
    }
}
*/
";
            var sourceV2 = @"
/* GENERATE:
class G
{

    int M() 
    {
        return 1;
    }
}
*/
";

            var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, sourceV1, generator);

            var moduleId = EmitLibrary(sourceV1, generator: generator);
            LoadLibraryToDebuggee(moduleId);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);
            var editSession = service.GetTestAccessor().GetEditSession();

            // change the source (valid edit):
            solution = solution.WithDocumentText(document1.Id, SourceText.From(sourceV2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);

            var lineUpdate = delta.SequencePoints.Single();
            AssertEx.Equal(new[] { "3 -> 4" }, lineUpdate.LineUpdates.Select(edit => $"{edit.OldLine} -> {edit.NewLine}"));
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Empty(delta.UpdatedMethods);

            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_SourceGenerators_DocumentUpdate_GeneratedDocumentInsert()
        {
            var sourceV1 = @"
partial class C { int X = 1; } 
";
            var sourceV2 = @"
/* GENERATE: partial class C { int Y = 2; } */

partial class C { int X = 1; }
";

            var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, sourceV1, generator);

            var moduleId = EmitLibrary(sourceV1, generator: generator);
            LoadLibraryToDebuggee(moduleId);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);
            var editSession = service.GetTestAccessor().GetEditSession();

            // change the source (valid edit):
            solution = solution.WithDocumentText(document1.Id, SourceText.From(sourceV2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(1, delta.UpdatedMethods.Length); // constructor update

            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_SourceGenerators_AdditionalDocumentUpdate()
        {
            var source = @"
class C { int Y => 1; } 
";

            var additionalSourceV1 = @"
/* GENERATE: class G { int X => 1; } */
";
            var additionalSourceV2 = @"
/* GENERATE: class G { int X => 2; } */
";

            var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, source, generator, additionalFileText: additionalSourceV1);

            var moduleId = EmitLibrary(source, generator: generator, additionalFileText: additionalSourceV1);
            LoadLibraryToDebuggee(moduleId);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);
            var editSession = service.GetTestAccessor().GetEditSession();

            // change the additional source (valid edit):
            var additionalDocument1 = solution.Projects.Single().AdditionalDocuments.Single();
            solution = solution.WithAdditionalDocumentText(additionalDocument1.Id, SourceText.From(additionalSourceV2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(1, delta.UpdatedMethods.Length);

            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_SourceGenerators_AnalyzerConfigUpdate()
        {
            var source = @"
class C { int Y => 1; } 
";

            var configV1 = new[] { ("enc_generator_output", "1") };
            var configV2 = new[] { ("enc_generator_output", "2") };

            var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, source, generator, analyzerConfig: configV1);

            var moduleId = EmitLibrary(source, generator: generator, analyzerOptions: configV1);
            LoadLibraryToDebuggee(moduleId);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);
            var editSession = service.GetTestAccessor().GetEditSession();

            // change the additional source (valid edit):
            var configDocument1 = solution.Projects.Single().AnalyzerConfigDocuments.Single();
            solution = solution.WithAnalyzerConfigDocumentText(configDocument1.Id, GetAnalyzerConfigText(configV2));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(1, delta.UpdatedMethods.Length);

            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_SourceGenerators_DocumentRemove()
        {
            var source1 = "";

            var generator = new TestSourceGenerator()
            {
                ExecuteImpl = context => context.AddSource("generated", $"class G {{ int X => {context.Compilation.SyntaxTrees.Count()}; }}")
            };

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, source1, generator);

            var moduleId = EmitLibrary(source1, generator: generator);
            LoadLibraryToDebuggee(moduleId);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service);
            var editSession = service.GetTestAccessor().GetEditSession();

            // remove the source document (valid edit):
            solution = document1.Project.Solution.RemoveDocument(document1.Id);

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(1, delta.UpdatedMethods.Length);

            EndDebuggingSession(service);
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

            var compilationA = CSharpTestBase.CreateCompilation(source1, options: TestOptions.DebugDll, targetFramework: DefaultTargetFramework, assemblyName: "A");
            var compilationB = CSharpTestBase.CreateCompilation(source1, options: TestOptions.DebugDll, targetFramework: DefaultTargetFramework, assemblyName: "B");

            var (peImageA, pdbImageA) = compilationA.EmitToArrays(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
            var moduleMetadataA = ModuleMetadata.CreateFromImage(peImageA);
            var moduleFileA = Temp.CreateFile("A.dll").WriteAllBytes(peImageA);
            var pdbFileA = dir.CreateFile("A.pdb").WriteAllBytes(pdbImageA);
            var moduleIdA = moduleMetadataA.GetModuleVersionId();

            var (peImageB, pdbImageB) = compilationB.EmitToArrays(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
            var moduleMetadataB = ModuleMetadata.CreateFromImage(peImageB);
            var moduleFileB = dir.CreateFile("B.dll").WriteAllBytes(peImageB);
            var pdbFileB = dir.CreateFile("B.pdb").WriteAllBytes(pdbImageB);
            var moduleIdB = moduleMetadataB.GetModuleVersionId();

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var documentA) = AddDefaultTestProject(solution, source1);
            var projectA = documentA.Project;

            var projectB = solution.AddProject("B", "A", "C#").AddMetadataReferences(projectA.MetadataReferences).AddDocument("DocB", source1, filePath: "DocB.cs").Project;
            solution = projectB.Solution;

            _mockCompilationOutputsProvider = project =>
                (project.Id == projectA.Id) ? new CompilationOutputFiles(moduleFileA.Path, pdbFileA.Path) :
                (project.Id == projectB.Id) ? new CompilationOutputFiles(moduleFileB.Path, pdbFileB.Path) :
                throw ExceptionUtilities.UnexpectedValue(project);

            // only module A is loaded
            LoadLibraryToDebuggee(moduleIdA);

            await StartDebuggingSessionAsync(service, solution);
            var debuggingSession = service.GetTestAccessor().GetDebuggingSession();

            EnterBreakState(service);
            var editSession = service.GetTestAccessor().GetEditSession();

            //
            // First update.
            //

            solution = solution.WithDocumentText(projectA.Documents.Single().Id, SourceText.From(source2, Encoding.UTF8));
            solution = solution.WithDocumentText(projectB.Documents.Single().Id, SourceText.From(source2, Encoding.UTF8));

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            Assert.Empty(emitDiagnostics);

            var deltaA = updates.Updates.Single(d => d.Module == moduleIdA);
            var deltaB = updates.Updates.Single(d => d.Module == moduleIdB);
            Assert.Equal(2, updates.Updates.Length);

            // the update should be stored on the service:
            var pendingUpdate = editSession.GetTestAccessor().GetPendingSolutionUpdate();
            var (_, newBaselineA1) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectA.Id);
            var (_, newBaselineB1) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectB.Id);

            var baselineA0 = newBaselineA1.GetInitialEmitBaseline();
            var baselineB0 = newBaselineB1.GetInitialEmitBaseline();

            var readers = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
            Assert.Equal(4, readers.Length);
            Assert.False(readers.Any(r => r is null));

            Assert.Equal(moduleIdA, newBaselineA1.OriginalMetadata.GetModuleVersionId());
            Assert.Equal(moduleIdB, newBaselineB1.OriginalMetadata.GetModuleVersionId());

            CommitSolutionUpdate(service);
            Assert.Null(editSession.GetTestAccessor().GetPendingSolutionUpdate());

            // no change in non-remappable regions since we didn't have any active statements:
            Assert.Empty(debuggingSession.NonRemappableRegions);

            // verify that baseline is added for both modules:
            Assert.Same(newBaselineA1, debuggingSession.GetTestAccessor().GetProjectEmitBaseline(projectA.Id));
            Assert.Same(newBaselineB1, debuggingSession.GetTestAccessor().GetProjectEmitBaseline(projectB.Id));

            // solution update status after committing an update:
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            ExitBreakState();
            EnterBreakState(service);
            editSession = service.GetTestAccessor().GetEditSession();

            //
            // Second update.
            //

            solution = solution.WithDocumentText(projectA.Documents.Single().Id, SourceText.From(source3, Encoding.UTF8));
            solution = solution.WithDocumentText(projectB.Documents.Single().Id, SourceText.From(source3, Encoding.UTF8));

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            Assert.Empty(emitDiagnostics);

            deltaA = updates.Updates.Single(d => d.Module == moduleIdA);
            deltaB = updates.Updates.Single(d => d.Module == moduleIdB);
            Assert.Equal(2, updates.Updates.Length);

            // the update should be stored on the service:
            pendingUpdate = editSession.GetTestAccessor().GetPendingSolutionUpdate();
            var (_, newBaselineA2) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectA.Id);
            var (_, newBaselineB2) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectB.Id);

            Assert.NotSame(newBaselineA1, newBaselineA2);
            Assert.NotSame(newBaselineB1, newBaselineB2);
            Assert.Same(baselineA0, newBaselineA2.GetInitialEmitBaseline());
            Assert.Same(baselineB0, newBaselineB2.GetInitialEmitBaseline());
            Assert.Same(baselineA0.OriginalMetadata, newBaselineA2.OriginalMetadata);
            Assert.Same(baselineB0.OriginalMetadata, newBaselineB2.OriginalMetadata);

            // no new module readers:
            var baselineReaders = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
            AssertEx.Equal(readers, baselineReaders);

            CommitSolutionUpdate(service);
            Assert.Null(editSession.GetTestAccessor().GetPendingSolutionUpdate());

            // no change in non-remappable regions since we didn't have any active statements:
            Assert.Empty(debuggingSession.NonRemappableRegions);

            // module readers tracked:
            baselineReaders = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
            AssertEx.Equal(readers, baselineReaders);

            // verify that baseline is updated for both modules:
            Assert.Same(newBaselineA2, debuggingSession.GetTestAccessor().GetProjectEmitBaseline(projectA.Id));
            Assert.Same(newBaselineB2, debuggingSession.GetTestAccessor().GetProjectEmitBaseline(projectB.Id));

            // solution update status after committing an update:
            Assert.False(await service.HasChangesAsync(solution, s_noActiveSpans, sourceFilePath: null, CancellationToken.None));

            ExitBreakState();
            EndDebuggingSession(service);

            // open deferred module readers should be dispose when the debugging session ends:
            VerifyReadersDisposed(readers);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_BaselineCreationFailed_NoStream()
        {
            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.NewGuid())
            {
                OpenPdbStreamImpl = () => null,
                OpenAssemblyStreamImpl = () => null,
            };

            await StartDebuggingSessionAsync(service, solution);

            // module not loaded
            EnterBreakState(service);

            // change the source (valid edit):
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            AssertEx.Equal(new[] { $"{document1.Project.Id} Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "test-pdb", new FileNotFoundException().Message)}" }, InspectDiagnostics(emitDiagnostics));
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_BaselineCreationFailed_AssemblyReadError()
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var compilationV1 = CSharpTestBase.CreateCompilationWithMscorlib40(sourceV1, options: TestOptions.DebugDll, assemblyName: "lib");

            var pdbStream = new MemoryStream();
            var peImage = compilationV1.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, sourceV1);

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.NewGuid())
            {
                OpenPdbStreamImpl = () => pdbStream,
                OpenAssemblyStreamImpl = () => throw new IOException("*message*"),
            };

            await StartDebuggingSessionAsync(service, solution);

            // module not loaded
            EnterBreakState(service);

            // change the source (valid edit):
            var document1 = solution.Projects.Single().Documents.Single();
            solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            AssertEx.Equal(new[] { $"{document.Project.Id} Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "test-assembly", "*message*")}" }, InspectDiagnostics(emitDiagnostics));
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);

            EndDebuggingSession(service);

            AssertEx.Equal(new[]
            {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                    "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC1001"
                }, _telemetryLog);
        }

        [Fact]
        public async Task ActiveStatements()
        {
            var sourceV1 = "class C { void F() { G(1); } void G(int a) => System.Console.WriteLine(1); }";
            var sourceV2 = "class C { int x; void F() { G(2); G(1); } void G(int a) => System.Console.WriteLine(2); }";

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, sourceV1);

            var activeSpan11 = GetSpan(sourceV1, "G(1);");
            var activeSpan12 = GetSpan(sourceV1, "System.Console.WriteLine(1)");
            var activeSpan21 = GetSpan(sourceV2, "G(2); G(1);");
            var activeSpan22 = GetSpan(sourceV2, "System.Console.WriteLine(2)");
            var adjustedActiveSpan1 = GetSpan(sourceV2, "G(2);");
            var adjustedActiveSpan2 = GetSpan(sourceV2, "System.Console.WriteLine(2)");

            var documentId = document1.Id;
            var documentPath = document1.FilePath;

            var sourceTextV1 = document1.GetTextSynchronously(CancellationToken.None);
            var sourceTextV2 = SourceText.From(sourceV2, Encoding.UTF8);

            var activeLineSpan11 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan11);
            var activeLineSpan12 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan12);
            var activeLineSpan21 = sourceTextV2.Lines.GetLinePositionSpan(activeSpan21);
            var activeLineSpan22 = sourceTextV2.Lines.GetLinePositionSpan(activeSpan22);
            var adjustedActiveLineSpan1 = sourceTextV2.Lines.GetLinePositionSpan(adjustedActiveSpan1);
            var adjustedActiveLineSpan2 = sourceTextV2.Lines.GetLinePositionSpan(adjustedActiveSpan2);

            // default if not called in a break state
            Assert.True((await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(document1.Id), CancellationToken.None)).IsDefault);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            // default if not called in a break state
            Assert.True((await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(document1.Id), CancellationToken.None)).IsDefault);

            var moduleId = Guid.NewGuid();
            var activeInstruction1 = new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 1);
            var activeInstruction2 = new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000002, version: 1), ilOffset: 1);

            var activeStatements = ImmutableArray.Create(
                new ManagedActiveStatementDebugInfo(
                    activeInstruction1,
                    documentPath,
                    activeLineSpan11.ToSourceSpan(),
                    ActiveStatementFlags.IsNonLeafFrame),
                new ManagedActiveStatementDebugInfo(
                    activeInstruction2,
                    documentPath,
                    activeLineSpan12.ToSourceSpan(),
                    ActiveStatementFlags.IsLeafFrame));

            EnterBreakState(service, activeStatements);
            var editSession = service.GetTestAccessor().GetEditSession();

            var activeStatementSpan11 = new ActiveStatementSpan(0, activeLineSpan11, ActiveStatementFlags.IsNonLeafFrame, unmappedDocumentId: null);
            var activeStatementSpan12 = new ActiveStatementSpan(1, activeLineSpan12, ActiveStatementFlags.IsLeafFrame, unmappedDocumentId: null);

            var baseSpans = await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(document1.Id), CancellationToken.None);
            AssertEx.Equal(new[]
            {
               activeStatementSpan11,
               activeStatementSpan12
            }, baseSpans.Single());

            var trackedActiveSpans1 = ImmutableArray.Create(activeStatementSpan11, activeStatementSpan12);

            var currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document1, (_, _, _) => new(trackedActiveSpans1), CancellationToken.None);
            AssertEx.Equal(trackedActiveSpans1, currentSpans);

            Assert.Equal(activeLineSpan11,
                await service.GetCurrentActiveStatementPositionAsync(document1.Project.Solution, (_, _, _) => new(trackedActiveSpans1), activeInstruction1, CancellationToken.None));

            Assert.Equal(activeLineSpan12,
                await service.GetCurrentActiveStatementPositionAsync(document1.Project.Solution, (_, _, _) => new(trackedActiveSpans1), activeInstruction2, CancellationToken.None));

            // change the source (valid edit):
            solution = solution.WithDocumentText(documentId, sourceTextV2);
            var document2 = solution.GetDocument(documentId);

            // tracking span update triggered by the edit:
            var activeStatementSpan21 = new ActiveStatementSpan(0, activeLineSpan21, ActiveStatementFlags.IsNonLeafFrame, unmappedDocumentId: null);
            var activeStatementSpan22 = new ActiveStatementSpan(1, activeLineSpan22, ActiveStatementFlags.IsLeafFrame, unmappedDocumentId: null);
            var trackedActiveSpans2 = ImmutableArray.Create(activeStatementSpan21, activeStatementSpan22);

            currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document2, (_, _, _) => new(trackedActiveSpans2), CancellationToken.None);
            AssertEx.Equal(new[] { adjustedActiveLineSpan1, adjustedActiveLineSpan2 }, currentSpans.Select(s => s.LineSpan));

            Assert.Equal(adjustedActiveLineSpan1,
                await service.GetCurrentActiveStatementPositionAsync(solution, (_, _, _) => new(trackedActiveSpans2), activeInstruction1, CancellationToken.None));

            Assert.Equal(adjustedActiveLineSpan2,
                await service.GetCurrentActiveStatementPositionAsync(solution, (_, _, _) => new(trackedActiveSpans2), activeInstruction2, CancellationToken.None));
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

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, sourceV1);

            var activeSpan11 = GetSpan(sourceV1, "G(1)");
            var activeSpan12 = GetSpan(sourceV1, "System.Console.WriteLine(1)");

            var documentId = document1.Id;
            var documentFilePath = document1.FilePath;

            var sourceTextV1 = await document1.GetTextAsync(CancellationToken.None);
            var sourceTextV2 = SourceText.From(sourceV2, Encoding.UTF8);

            var activeLineSpan11 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan11);
            var activeLineSpan12 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan12);

            var debuggingSession = await StartDebuggingSessionAsync(
                service,
                solution,
                isOutOfSync ? CommittedSolution.DocumentState.OutOfSync : CommittedSolution.DocumentState.MatchesBuildOutput);

            var moduleId = Guid.NewGuid();
            var activeInstruction1 = new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 1);
            var activeInstruction2 = new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000002, version: 1), ilOffset: 1);

            var activeStatements = ImmutableArray.Create(
                new ManagedActiveStatementDebugInfo(
                    activeInstruction1,
                    documentFilePath,
                    activeLineSpan11.ToSourceSpan(),
                    ActiveStatementFlags.IsNonLeafFrame),
                new ManagedActiveStatementDebugInfo(
                    activeInstruction2,
                    documentFilePath,
                    activeLineSpan12.ToSourceSpan(),
                    ActiveStatementFlags.IsLeafFrame));

            EnterBreakState(service, activeStatements);
            var editSession = service.GetTestAccessor().GetEditSession();

            var baseSpans = (await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(documentId), CancellationToken.None)).Single();
            AssertEx.Equal(new[]
            {
                new ActiveStatementSpan(0, activeLineSpan11, ActiveStatementFlags.IsNonLeafFrame, unmappedDocumentId: null),
                new ActiveStatementSpan(1, activeLineSpan12, ActiveStatementFlags.IsLeafFrame, unmappedDocumentId: null)
            }, baseSpans);

            // change the source (valid edit):
            solution = solution.WithDocumentText(documentId, sourceTextV2);
            var document2 = solution.GetDocument(documentId);

            // no adjustments made due to syntax error or out-of-sync document:
            var currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document2, (_, _, _) => ValueTaskFactory.FromResult(baseSpans), CancellationToken.None);
            AssertEx.Equal(new[] { activeLineSpan11, activeLineSpan12 }, currentSpans.Select(s => s.LineSpan));

            var currentSpan1 = await service.GetCurrentActiveStatementPositionAsync(solution, (_, _, _) => ValueTaskFactory.FromResult(baseSpans), activeInstruction1, CancellationToken.None);
            var currentSpan2 = await service.GetCurrentActiveStatementPositionAsync(solution, (_, _, _) => ValueTaskFactory.FromResult(baseSpans), activeInstruction2, CancellationToken.None);
            if (isOutOfSync)
            {
                Assert.Equal(baseSpans[0].LineSpan, currentSpan1.Value);
                Assert.Equal(baseSpans[1].LineSpan, currentSpan2.Value);
            }
            else
            {
                Assert.Null(currentSpan1);
                Assert.Null(currentSpan2);
            }
        }

        [Fact]
        public async Task ActiveStatements_ForeignDocument()
        {
            var composition = FeaturesTestCompositions.Features.AddParts(typeof(DummyLanguageService));

            using var _ = CreateWorkspace(out var solution, out var service, new[] { typeof(DummyLanguageService) });

            var project = solution.AddProject("dummy_proj", "dummy_proj", DummyLanguageService.LanguageName);
            var document = project.AddDocument("test", SourceText.From("dummy1"));
            solution = document.Project.Solution;

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            var activeStatements = ImmutableArray.Create(
                new ManagedActiveStatementDebugInfo(
                    new ManagedInstructionId(new ManagedMethodId(Guid.Empty, token: 0x06000001, version: 1), ilOffset: 0),
                    documentName: document.Name,
                    sourceSpan: new SourceSpan(0, 1, 0, 2),
                    ActiveStatementFlags.IsNonLeafFrame));

            EnterBreakState(service, activeStatements);

            // active statements are not tracked in non-Roslyn projects:
            var currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(currentSpans);

            var baseSpans = await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(document.Id), CancellationToken.None);
            Assert.Empty(baseSpans.Single());
        }

        [Fact, WorkItem(24320, "https://github.com/dotnet/roslyn/issues/24320")]
        public async Task ActiveStatements_LinkedDocuments()
        {
            var markedSources = new[]
            {
@"class Test1
{
    static void Main() => <AS:2>Project2::Test1.F();</AS:2>
    static void F() => <AS:1>Project4::Test2.M();</AS:1>
}",
@"class Test2 { static void M() => <AS:0>Console.WriteLine();</AS:0> }"
            };

            var module1 = Guid.NewGuid();
            var module2 = Guid.NewGuid();
            var module4 = Guid.NewGuid();

            var debugInfos = GetActiveStatementDebugInfosCSharp(
                markedSources,
                methodRowIds: new[] { 1, 2, 1 },
                modules: new[] { module4, module2, module1 });

            // Project1: Test1.cs, Test2.cs      
            // Project2: Test1.cs (link from P1)
            // Project3: Test1.cs (link from P1)
            // Project4: Test2.cs (link from P1)

            using var _ = CreateWorkspace(out var solution, out var service);
            solution = AddDefaultTestProject(solution, ActiveStatementsDescription.ClearTags(markedSources));

            var documents = solution.Projects.Single().Documents;
            var doc1 = documents.First();
            var doc2 = documents.Skip(1).First();
            var text1 = await doc1.GetTextAsync();
            var text2 = await doc2.GetTextAsync();

            DocumentId AddProjectAndLinkDocument(string projectName, Document doc, SourceText text)
            {
                var p = solution.AddProject(projectName, projectName, "C#");
                var linkedDocId = DocumentId.CreateNewId(p.Id, projectName + "->" + doc.Name);
                solution = p.Solution.AddDocument(linkedDocId, doc.Name, text, filePath: doc.FilePath);
                return linkedDocId;
            }

            var docId3 = AddProjectAndLinkDocument("Project2", doc1, text1);
            var docId4 = AddProjectAndLinkDocument("Project3", doc1, text1);
            var docId5 = AddProjectAndLinkDocument("Project4", doc2, text2);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);
            EnterBreakState(service, debugInfos);

            // Base Active Statements

            var baseActiveStatementsMap = await debuggingSession.EditSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false);
            var documentMap = baseActiveStatementsMap.DocumentPathMap;

            Assert.Equal(2, documentMap.Count);

            AssertEx.Equal(new[]
            {
                $"2: {doc1.FilePath}: (2,32)-(2,52) flags=[MethodUpToDate, IsNonLeafFrame]",
                $"1: {doc1.FilePath}: (3,29)-(3,49) flags=[MethodUpToDate, IsNonLeafFrame]"
            }, documentMap[doc1.FilePath].Select(InspectActiveStatement));

            AssertEx.Equal(new[]
            {
                $"0: {doc2.FilePath}: (0,39)-(0,59) flags=[IsLeafFrame, MethodUpToDate]",
            }, documentMap[doc2.FilePath].Select(InspectActiveStatement));

            Assert.Equal(3, baseActiveStatementsMap.InstructionMap.Count);

            var statements = baseActiveStatementsMap.InstructionMap.Values.OrderBy(v => v.Ordinal).ToArray();
            var s = statements[0];
            Assert.Equal(0x06000001, s.InstructionId.Method.Token);
            Assert.Equal(module4, s.InstructionId.Method.Module);

            s = statements[1];
            Assert.Equal(0x06000002, s.InstructionId.Method.Token);
            Assert.Equal(module2, s.InstructionId.Method.Module);

            s = statements[2];
            Assert.Equal(0x06000001, s.InstructionId.Method.Token);
            Assert.Equal(module1, s.InstructionId.Method.Module);

            var spans = await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(doc1.Id, doc2.Id, docId3, docId4, docId5), CancellationToken.None);

            AssertEx.Equal(new[]
            {
                "(2,32)-(2,52), (3,29)-(3,49)", // test1.cs
                "(0,39)-(0,59)",                // test2.cs
                "(2,32)-(2,52), (3,29)-(3,49)", // link test1.cs
                "(2,32)-(2,52), (3,29)-(3,49)", // link test1.cs
                "(0,39)-(0,59)"                 // link test2.cs
            }, spans.Select(docSpans => string.Join(", ", docSpans.Select(span => span.LineSpan))));
        }

        [Fact]
        public async Task ActiveStatements_OutOfSyncDocuments()
        {
            var markedSource1 =
@"class C
{
    static void M()
    {
        try 
        {
        }
        catch (Exception e)
        {
            <AS:0>M();</AS:0>
        }
    }
}";
            var source2 =
 @"class C
{
    static void M()
    {
        try 
        {
        }
        catch (Exception e)
        {

            M();
        }
    }
}";

            var markedSources = new[] { markedSource1 };

            var thread1 = Guid.NewGuid();

            // Thread1 stack trace: F (AS:0 leaf)

            var debugInfos = GetActiveStatementDebugInfosCSharp(
                markedSources,
                methodRowIds: new[] { 1 },
                ilOffsets: new[] { 1 },
                flags: new[]
                {
                    ActiveStatementFlags.IsLeafFrame | ActiveStatementFlags.MethodUpToDate
                });

            using var _ = CreateWorkspace(out var solution, out var service);
            solution = AddDefaultTestProject(solution, ActiveStatementsDescription.ClearTags(markedSources));
            var project = solution.Projects.Single();
            var document = project.Documents.Single();

            var debuggingSession = await StartDebuggingSessionAsync(service, solution, initialState: CommittedSolution.DocumentState.OutOfSync);
            EnterBreakState(service, debugInfos);

            // update document to test a changed solution
            solution = solution.WithDocumentText(document.Id, SourceText.From(source2, Encoding.UTF8));
            document = solution.GetDocument(document.Id);

            var baseActiveStatementMap = await debuggingSession.EditSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

            // Active Statements - available in out-of-sync documents, as they reflect the state of the debuggee and not the base document content

            Assert.Single(baseActiveStatementMap.DocumentPathMap);

            AssertEx.Equal(new[]
            {
                $"0: {document.FilePath}: (9,18)-(9,22) flags=[IsLeafFrame, MethodUpToDate]",
            }, baseActiveStatementMap.DocumentPathMap[document.FilePath].Select(InspectActiveStatement));

            Assert.Equal(1, baseActiveStatementMap.InstructionMap.Count);

            var activeStatement1 = baseActiveStatementMap.InstructionMap.Values.OrderBy(v => v.InstructionId.Method.Token).Single();
            Assert.Equal(0x06000001, activeStatement1.InstructionId.Method.Token);
            Assert.Equal(document.FilePath, activeStatement1.FilePath);
            Assert.True(activeStatement1.IsLeaf);

            // Active statement reported as unchanged as the containing document is out-of-sync:
            var baseSpans = await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(document.Id), CancellationToken.None);
            AssertEx.Equal(new[] { $"(9,18)-(9,22)" }, baseSpans.Single().Select(s => s.LineSpan.ToString()));

            // Whether or not an active statement is in an exception region is unknown if the document is out-of-sync:
            Assert.Null(await service.IsActiveStatementInExceptionRegionAsync(solution, activeStatement1.InstructionId, CancellationToken.None));

            // Document got synchronized:
            debuggingSession.LastCommittedSolution.Test_SetDocumentState(document.Id, CommittedSolution.DocumentState.MatchesBuildOutput);

            // New location of the active statement reported:
            baseSpans = await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(document.Id), CancellationToken.None);
            AssertEx.Equal(new[] { $"(10,12)-(10,16)" }, baseSpans.Single().Select(s => s.LineSpan.ToString()));

            Assert.True(await service.IsActiveStatementInExceptionRegionAsync(solution, activeStatement1.InstructionId, CancellationToken.None));
        }

        [Fact]
        public async Task ActiveStatements_SourceGeneratedDocuments_LineDirectives()
        {
            var markedSource1 = @"
/* GENERATE:
class C
{
    void F()
    {
#line 1 ""a.razor""
       <AS:0>F();</AS:0>
#line default
    }
}
*/
";
            var markedSource2 = @"
/* GENERATE:
class C
{
    void F()
    {
#line 2 ""a.razor""
       <AS:0>F();</AS:0>
#line default
    }
}
*/
";
            var source1 = ActiveStatementsDescription.ClearTags(markedSource1);
            var source2 = ActiveStatementsDescription.ClearTags(markedSource2);

            var additionalFileSourceV1 = @"
       xxxxxxxxxxxxxxxxx
";

            var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document1) = AddDefaultTestProject(solution, source1, generator, additionalFileText: additionalFileSourceV1);

            var generatedDocument1 = (await solution.Projects.Single().GetSourceGeneratedDocumentsAsync().ConfigureAwait(false)).Single();

            var moduleId = EmitLibrary(source1, generator: generator, additionalFileText: additionalFileSourceV1);
            LoadLibraryToDebuggee(moduleId);

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            EnterBreakState(service, GetActiveStatementDebugInfosCSharp(
                new[] { GetGeneratedCodeFromMarkedSource(markedSource1) },
                filePaths: new[] { generatedDocument1.FilePath },
                modules: new[] { moduleId },
                methodRowIds: new[] { 1 },
                methodVersions: new[] { 1 },
                flags: new[]
                {
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsLeafFrame
                }));

            var editSession = service.GetTestAccessor().GetEditSession();

            // change the source (valid edit)
            solution = solution.WithDocumentText(document1.Id, SourceText.From(source2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Empty(delta.UpdatedMethods);

            AssertEx.Equal(new[]
            {
                "a.razor: [0 -> 1]"
            }, delta.SequencePoints.Inspect());

            EndDebuggingSession(service);
        }

        /// <summary>
        /// Scenario:
        /// F5 a program that has function F that calls G. G has a long-running loop, which starts executing.
        /// The user makes following operations:
        /// 1) Break, edit F from version 1 to version 2, continue (change is applied), G is still running in its loop
        ///    Function remapping is produced for F v1 -> F v2.
        /// 2) Hot-reload edit F (without breaking) to version 3.
        ///    Function remapping is produced for F v2 -> F v3 based on the last set of active statements calculated for F v2.
        ///    Assume that the execution did not progress since the last resume.
        ///    These active statements will likely not match the actual runtime active statements, 
        ///    however F v2 will never be remapped since it was hot-reloaded and not EnC'd.
        ///    This remapping is needed for mapping from F v1 to F v3.
        /// 3) Break. Update F to v4.
        /// </summary>
        [Fact, WorkItem(52100, "https://github.com/dotnet/roslyn/issues/52100")]
        public async Task BreakStateRemappingFollowedUpByRunStateUpdate()
        {
            var markedSourceV1 =
@"class Test
{
    static bool B() => true;

    static void G() { while (B()); <AS:0>}</AS:0>

    static void F()
    {
        /*insert1[1]*/B();/*insert2[5]*/B();/*insert3[10]*/B();
        <AS:1>G();</AS:1>
    }
}";
            var markedSourceV2 = Update(markedSourceV1, marker: "1");
            var markedSourceV3 = Update(markedSourceV2, marker: "2");
            var markedSourceV4 = Update(markedSourceV3, marker: "3");

            var moduleId = EmitAndLoadLibraryToDebuggee(ActiveStatementsDescription.ClearTags(markedSourceV1));

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, ActiveStatementsDescription.ClearTags(markedSourceV1));
            var documentId = document.Id;

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            // EnC update F v1 -> v2

            EnterBreakState(service, GetActiveStatementDebugInfosCSharp(
                new[] { markedSourceV1 },
                modules: new[] { moduleId, moduleId },
                methodRowIds: new[] { 2, 3 },
                methodVersions: new[] { 1, 1 },
                flags: new[]
                {
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsLeafFrame,    // G
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsNonLeafFrame, // F
                }));

            solution = solution.WithDocumentText(documentId, SourceText.From(ActiveStatementsDescription.ClearTags(markedSourceV2), Encoding.UTF8));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(0x06000003, updates.Updates.Single().UpdatedMethods.Single());
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            CommitSolutionUpdate(service);

            AssertEx.Equal(new[]
            {
                $"0x06000003 v1 | AS {document.FilePath}: (9,14)-(9,18) δ=1",
            }, InspectNonRemappableRegions(debuggingSession.NonRemappableRegions));

            ExitBreakState();

            // Hot Reload update F v2 -> v3

            solution = solution.WithDocumentText(documentId, SourceText.From(ActiveStatementsDescription.ClearTags(markedSourceV3), Encoding.UTF8));

            (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(0x06000003, updates.Updates.Single().UpdatedMethods.Single());
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            CommitSolutionUpdate(service);

            AssertEx.Equal(new[]
            {
                $"0x06000003 v1 | AS {document.FilePath}: (9,14)-(9,18) δ=1",
            }, InspectNonRemappableRegions(debuggingSession.NonRemappableRegions));

            // EnC update F v3 -> v4

            EnterBreakState(service, GetActiveStatementDebugInfosCSharp(
                new[] { markedSourceV1 },       // matches F v1    
                modules: new[] { moduleId, moduleId },
                methodRowIds: new[] { 2, 3 },
                methodVersions: new[] { 1, 1 }, // frame F v1 is still executing (G has not returned)
                flags: new[]
                {
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsLeafFrame,    // G
                    ActiveStatementFlags.IsNonLeafFrame,                                       // F - not up-to-date anymore
                }));

            solution = solution.WithDocumentText(documentId, SourceText.From(ActiveStatementsDescription.ClearTags(markedSourceV4), Encoding.UTF8));

            (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(0x06000003, updates.Updates.Single().UpdatedMethods.Single());
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            CommitSolutionUpdate(service);

            // TODO: https://github.com/dotnet/roslyn/issues/52100
            // this is incorrect. correct value is: 0x06000003 v1 | AS (9,14)-(9,18) δ=16
            AssertEx.Equal(new[]
            {
                $"0x06000003 v1 | AS {document.FilePath}: (9,14)-(9,18) δ=5"
            }, InspectNonRemappableRegions(debuggingSession.NonRemappableRegions));

            ExitBreakState();
        }

        /// <summary>
        /// Scenario:
        /// - F5
        /// - edit, but not apply the edits
        /// - break
        /// </summary>
        [Fact]
        public async Task BreakInPresenceOfUnappliedChanges()
        {
            var markedSource1 =
@"class Test
{
    static bool B() => true;
    static void G() { while (B()); <AS:0>}</AS:0>

    static void F()
    {
        <AS:1>G();</AS:1>
    }
}";

            var markedSource2 =
@"class Test
{
    static bool B() => true;
    static void G() { while (B()); <AS:0>}</AS:0>

    static void F()
    {
        B();
        <AS:1>G();</AS:1>
    }
}";

            var markedSource3 =
@"class Test
{
    static bool B() => true;
    static void G() { while (B()); <AS:0>}</AS:0>

    static void F()
    {
        B();
        B();
        <AS:1>G();</AS:1>
    }
}";

            var moduleId = EmitAndLoadLibraryToDebuggee(ActiveStatementsDescription.ClearTags(markedSource1));

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, ActiveStatementsDescription.ClearTags(markedSource1));
            var documentId = document.Id;

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            // Update to snapshot 2, but don't apply

            solution = solution.WithDocumentText(documentId, SourceText.From(ActiveStatementsDescription.ClearTags(markedSource2), Encoding.UTF8));

            // EnC update F v2 -> v3

            EnterBreakState(service, GetActiveStatementDebugInfosCSharp(
                new[] { markedSource1 },
                modules: new[] { moduleId, moduleId },
                methodRowIds: new[] { 2, 3 },
                methodVersions: new[] { 1, 1 },
                flags: new[]
                {
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsLeafFrame,    // G
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsNonLeafFrame, // F
                }));

            // check that the active statement is mapped correctly to snapshot v2:
            var expectedSpanG1 = new LinePositionSpan(new LinePosition(3, 41), new LinePosition(3, 42));
            var expectedSpanF1 = new LinePositionSpan(new LinePosition(8, 14), new LinePosition(8, 18));

            var activeInstructionF1 = new ManagedInstructionId(new ManagedMethodId(moduleId, 0x06000003, version: 1), ilOffset: 0);
            var span = await service.GetCurrentActiveStatementPositionAsync(solution, s_noActiveSpans, activeInstructionF1, CancellationToken.None);
            Assert.Equal(expectedSpanF1, span.Value);

            var spans = (await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(documentId), CancellationToken.None)).Single();
            AssertEx.Equal(new[]
            {
                new ActiveStatementSpan(0, expectedSpanG1, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsLeafFrame, documentId),
                new ActiveStatementSpan(1, expectedSpanF1, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsNonLeafFrame, documentId)
            }, spans);

            solution = solution.WithDocumentText(documentId, SourceText.From(ActiveStatementsDescription.ClearTags(markedSource3), Encoding.UTF8));

            // check that the active statement is mapped correctly to snapshot v3:
            var expectedSpanG2 = new LinePositionSpan(new LinePosition(3, 41), new LinePosition(3, 42));
            var expectedSpanF2 = new LinePositionSpan(new LinePosition(9, 14), new LinePosition(9, 18));

            span = await service.GetCurrentActiveStatementPositionAsync(solution, s_noActiveSpans, activeInstructionF1, CancellationToken.None);
            Assert.Equal(expectedSpanF2, span);

            spans = (await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(documentId), CancellationToken.None)).Single();
            AssertEx.Equal(new[]
            {
                new ActiveStatementSpan(0, expectedSpanG2, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsLeafFrame, documentId),
                new ActiveStatementSpan(1, expectedSpanF2, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsNonLeafFrame, documentId)
            }, spans);

            // no rude edits:
            var document1 = solution.GetDocument(documentId);
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(diagnostics);

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(0x06000003, updates.Updates.Single().UpdatedMethods.Single());
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            CommitSolutionUpdate(service);

            AssertEx.Equal(new[]
            {
                $"0x06000003 v1 | AS {document.FilePath}: (7,14)-(7,18) δ=2",
            }, InspectNonRemappableRegions(debuggingSession.NonRemappableRegions));

            ExitBreakState();
        }

        /// <summary>
        /// Scenario:
        /// - F5
        /// - edit and apply edit that deletes non-leaf active statement
        /// - break
        /// </summary>
        [Fact, WorkItem(52100, "https://github.com/dotnet/roslyn/issues/52100")]
        public async Task BreakAfterRunModeChangeDeletesNonLeafActiveStatement()
        {
            var markedSource1 =
@"class Test
{
    static bool B() => true;
    static void G() { while (B()); <AS:0>}</AS:0>

    static void F()
    {
        <AS:1>G();</AS:1>
    }
}";

            var markedSource2 =
@"class Test
{
    static bool B() => true;
    static void G() { while (B()); <AS:0>}</AS:0>

    static void F()
    {
    }
}";
            var moduleId = EmitAndLoadLibraryToDebuggee(ActiveStatementsDescription.ClearTags(markedSource1));

            using var _ = CreateWorkspace(out var solution, out var service);
            (solution, var document) = AddDefaultTestProject(solution, ActiveStatementsDescription.ClearTags(markedSource1));
            var documentId = document.Id;

            var debuggingSession = await StartDebuggingSessionAsync(service, solution);

            // Apply update: F v1 -> v2.

            solution = solution.WithDocumentText(documentId, SourceText.From(ActiveStatementsDescription.ClearTags(markedSource2), Encoding.UTF8));

            var (updates, emitDiagnostics) = await EmitSolutionUpdateAsync(service, solution);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(0x06000003, updates.Updates.Single().UpdatedMethods.Single());
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            CommitSolutionUpdate(service);

            // Break

            EnterBreakState(service, GetActiveStatementDebugInfosCSharp(
                new[] { markedSource1 },
                modules: new[] { moduleId, moduleId },
                methodRowIds: new[] { 2, 3 },
                methodVersions: new[] { 1, 1 },  // frame F v1 is still executing (G has not returned)
                flags: new[]
                {
                    ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsLeafFrame,    // G
                    ActiveStatementFlags.IsNonLeafFrame, // F
                }));

            // check that the active statement is mapped correctly to snapshot v2:
            var expectedSpanF1 = new LinePositionSpan(new LinePosition(7, 14), new LinePosition(7, 18));
            var expectedSpanG1 = new LinePositionSpan(new LinePosition(3, 41), new LinePosition(3, 42));

            var activeInstructionF1 = new ManagedInstructionId(new ManagedMethodId(moduleId, 0x06000003, version: 1), ilOffset: 0);
            var span = await service.GetCurrentActiveStatementPositionAsync(solution, s_noActiveSpans, activeInstructionF1, CancellationToken.None);
            Assert.Equal(expectedSpanF1, span);

            var spans = (await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(documentId), CancellationToken.None)).Single();
            AssertEx.Equal(new[]
            {
                new ActiveStatementSpan(0, expectedSpanG1, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.IsLeafFrame, unmappedDocumentId: null),

                // TODO: https://github.com/dotnet/roslyn/issues/52100
                // This is incorrect: the active statement shouldn't be reported since it has been deleted.
                // We need the debugger to mark the method version as replaced by run-mode update.
                new ActiveStatementSpan(1, expectedSpanF1, ActiveStatementFlags.IsNonLeafFrame, unmappedDocumentId: null)
            }, spans);

            ExitBreakState();
        }

        [Fact]
        public async Task WatchHotReloadServiceTest()
        {
            var source1 = "class C { void M() { System.Console.WriteLine(1); } }";
            var source2 = "class C { void M() { System.Console.WriteLine(2); } }";
            var source3 = "class C { void X() { System.Console.WriteLine(2); } }";

            var dir = Temp.CreateDirectory();
            var sourceFileA = dir.CreateFile("A.cs").WriteAllText(source1);
            var moduleId = EmitLibrary(source1, sourceFileA.Path, Encoding.UTF8, "Proj");

            using var workspace = CreateWorkspace(out var solution, out var encService);

            var projectP = solution.
                AddProject("P", "P", LanguageNames.CSharp).
                WithMetadataReferences(TargetFrameworkUtil.GetReferences(DefaultTargetFramework));

            solution = projectP.Solution;

            var documentIdA = DocumentId.CreateNewId(projectP.Id, debugName: "A");
            solution = solution.AddDocument(DocumentInfo.Create(
                id: documentIdA,
                name: "A",
                loader: new FileTextLoader(sourceFileA.Path, Encoding.UTF8),
                filePath: sourceFileA.Path));

            var hotReload = new WatchHotReloadService(workspace.Services);

            await hotReload.StartSessionAsync(solution, CancellationToken.None);
            var debuggingSession = encService.GetTestAccessor().GetDebuggingSession();

            var matchingDocuments = debuggingSession.LastCommittedSolution.Test_GetDocumentStates();
            AssertEx.Equal(new[]
            {
                "(A, MatchesBuildOutput)"
            }, matchingDocuments.Select(e => (solution.GetDocument(e.id).Name, e.state)).OrderBy(e => e.Name).Select(e => e.ToString()));

            solution = solution.WithDocumentText(documentIdA, SourceText.From(source2, Encoding.UTF8));

            var result = await hotReload.EmitSolutionUpdateAsync(solution, CancellationToken.None);
            Assert.Empty(result.diagnostics);
            Assert.Equal(1, result.updates.Length);

            solution = solution.WithDocumentText(documentIdA, SourceText.From(source3, Encoding.UTF8));

            result = await hotReload.EmitSolutionUpdateAsync(solution, CancellationToken.None);
            AssertEx.Equal(
                new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                result.diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            Assert.Empty(result.updates);

            hotReload.EndSession();
        }

        [Fact]
        public void ParseCapabilities()
        {
            var capabilities = ImmutableArray.Create("Baseline");

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.True(service.HasFlag(EditAndContinueCapabilities.Baseline));
            Assert.False(service.HasFlag(EditAndContinueCapabilities.NewTypeDefinition));
        }

        [Fact]
        public void ParseCapabilities_CaseSensitive()
        {
            var capabilities = ImmutableArray.Create("BaseLine");

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.False(service.HasFlag(EditAndContinueCapabilities.Baseline));
        }

        [Fact]
        public void ParseCapabilities_IgnoreInvalid()
        {
            var capabilities = ImmutableArray.Create("Baseline", "Invalid", "NewTypeDefinition");

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.True(service.HasFlag(EditAndContinueCapabilities.Baseline));
            Assert.True(service.HasFlag(EditAndContinueCapabilities.NewTypeDefinition));
        }

        [Fact]
        public void ParseCapabilities_IgnoreInvalidNumeric()
        {
            var capabilities = ImmutableArray.Create("Baseline", "90", "NewTypeDefinition");

            var service = EditAndContinueWorkspaceService.ParseCapabilities(capabilities);

            Assert.True(service.HasFlag(EditAndContinueCapabilities.Baseline));
            Assert.True(service.HasFlag(EditAndContinueCapabilities.NewTypeDefinition));
        }
    }
}
