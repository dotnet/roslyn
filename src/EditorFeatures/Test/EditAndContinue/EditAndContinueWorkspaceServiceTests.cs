﻿// Licensed to the .NET Foundation under one or more agreements.
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
    [UseExportProvider]
    public sealed partial class EditAndContinueWorkspaceServiceTests : TestBase
    {
        private static readonly TestComposition s_composition = FeaturesTestCompositions.Features;

        private static readonly SolutionActiveStatementSpanProvider s_noSolutionActiveSpans =
            (_, _) => new(ImmutableArray<TextSpan>.Empty);

        private static readonly DocumentActiveStatementSpanProvider s_noDocumentActiveSpans =
            _ => new(ImmutableArray<TextSpan>.Empty);

        private const TargetFramework DefaultTargetFramework = TargetFramework.NetStandard20;

        private Func<Project, CompilationOutputs> _mockCompilationOutputsProvider;
        private readonly List<string> _telemetryLog;
        private int _telemetryId;

        private readonly MockManagedEditAndContinueDebuggerService _loadedModulesProvider;

        public EditAndContinueWorkspaceServiceTests()
        {
            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.NewGuid());
            _telemetryLog = new List<string>();

            _loadedModulesProvider = new MockManagedEditAndContinueDebuggerService()
            {
                LoadedModules = new Dictionary<Guid, ManagedEditAndContinueAvailability>()
            };
        }

        private static TestWorkspace CreateWorkspace(Type[] additionalParts = null)
            => new(composition: s_composition.AddParts(additionalParts));

        private static SourceText GetAnalyzerConfigText((string key, string value)[] analyzerConfig)
            => SourceText.From("[*.*]" + Environment.NewLine + string.Join(Environment.NewLine, analyzerConfig.Select(c => $"{c.key} = {c.value}")));

        private static Project AddDefaultTestProject(
            TestWorkspace workspace,
            string source,
            ISourceGenerator generator = null,
            string additionalFileText = null,
            (string key, string value)[] analyzerConfig = null)
        {
            return AddDefaultTestProject(workspace, new[] { source }, generator, additionalFileText, analyzerConfig);
        }

        private static Project AddDefaultTestProject(
            TestWorkspace workspace,
            string[] sources,
            ISourceGenerator generator = null,
            string additionalFileText = null,
            (string key, string value)[] analyzerConfig = null)
        {
            var solution = workspace.CurrentSolution;

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

            workspace.ChangeSolution(document.Project.Solution);
            return workspace.CurrentSolution.GetProject(document.Project.Id);
        }

        private EditAndContinueWorkspaceService CreateEditAndContinueService(Workspace workspace)
        {
            return new EditAndContinueWorkspaceService(
                workspace,
                _mockCompilationOutputsProvider,
                testReportTelemetry: data => EditAndContinueWorkspaceService.LogDebuggingSessionTelemetry(data, (id, message) => _telemetryLog.Add($"{id}: {message.GetMessage()}"), () => ++_telemetryId));
        }

        private static EditSession StartEditSession(
            EditAndContinueWorkspaceService service,
            ImmutableArray<ManagedActiveStatementDebugInfo> activeStatements = default,
            IManagedEditAndContinueDebuggerService loadedModules = null,
            ImmutableArray<DocumentId> documentsWithRunModeDiagnostics = default)
        {
            service.StartEditSession(
                loadedModules ?? new MockManagedEditAndContinueDebuggerService()
                {
                    // all modules are considered loaded by default:
                    IsEditAndContinueAvailable = _ => new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.Available),

                    GetActiveStatementsImpl = () => activeStatements.NullToEmpty(),
                },
                out var documentsToReanalyze);

            AssertEx.Equal(documentsWithRunModeDiagnostics.NullToEmpty(), documentsToReanalyze);

            return service.Test_GetEditSession();
        }

        private static void EndEditSession(EditAndContinueWorkspaceService service, ImmutableArray<DocumentId> documentsWithRudeEdits = default)
        {
            service.EndEditSession(out var documentsToReanalyze);
            AssertEx.Equal(documentsWithRudeEdits.NullToEmpty(), documentsToReanalyze);
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

        private static void EndDebuggingSession(EditAndContinueWorkspaceService service, ImmutableArray<DocumentId> documentsWithRunModeDiagnostics = default)
        {
            service.EndDebuggingSession(out var documentsToReanalyze);
            AssertEx.Equal(documentsWithRunModeDiagnostics.NullToEmpty(), documentsToReanalyze);
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

        private Guid EmitAndLoadLibraryToDebuggee(string source, string sourceFilePath = "test1.cs", Encoding encoding = null, string assemblyName = "")
        {
            var moduleId = EmitLibrary(source, sourceFilePath, encoding, assemblyName);
            LoadLibraryToDebuggee(moduleId);
            return moduleId;
        }

        private void LoadLibraryToDebuggee(Guid moduleId, ManagedEditAndContinueAvailability availability = default)
        {
            _loadedModulesProvider.LoadedModules.Add(moduleId, availability);
        }

        private Guid EmitLibrary(
            string source,
            string sourceFilePath = "test1.cs",
            Encoding encoding = null,
            string assemblyName = "",
            DebugInformationFormat pdbFormat = DebugInformationFormat.PortablePdb,
            ISourceGenerator generator = null,
            string additionalFileText = null,
            IEnumerable<(string, string)> analyzerOptions = null)
        {
            return EmitLibrary(new[] { (source, sourceFilePath) }, encoding, assemblyName, pdbFormat, generator, additionalFileText, analyzerOptions);
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

        [Fact]
        public async Task RunMode_ProjectThatDoesNotSupportEnC()
        {
            using var workspace = CreateWorkspace(new[] { typeof(DummyLanguageService) });
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, "class C1 { void M() { System.Console.WriteLine(1); } }");

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

            EndDebuggingSession(service);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task RunMode_ProjectNotBuilt()
        {
            using (var workspace = CreateWorkspace())
            {
                var project = AddDefaultTestProject(workspace, "class C1 { void M() { System.Console.WriteLine(1); } }");

                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.Empty);

                var service = CreateEditAndContinueService(workspace);

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

            using (var workspace = CreateWorkspace())
            {
                var project = AddDefaultTestProject(workspace, "class C1 { void M() { System.Console.WriteLine(1); } }");

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

                var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
                Assert.Empty(updates.Updates);
                Assert.Empty(emitDiagnostics);
            }
        }

        [Fact]
        public async Task RunMode_DocumentOutOfSync()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using var workspace = CreateWorkspace();

            var project = AddDefaultTestProject(workspace, "class C1 { void M() { System.Console.WriteLine(1); } }");
            var service = CreateEditAndContinueService(workspace);

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

            EndDebuggingSession(service, documentsWithRunModeDiagnostics: ImmutableArray.Create(document1.Id));

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task RunMode_FileAdded()
        {
            var sourceA = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var sourceB = "class C2 {}";

            var sourceFileA = Temp.CreateFile().WriteAllText(sourceA);
            var sourceFileB = Temp.CreateFile().WriteAllText(sourceB);

            using var workspace = new TestWorkspace(composition: s_composition);

            var documentA = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(sourceA, Encoding.UTF8), filePath: sourceFileA.Path);

            workspace.ChangeSolution(documentA.Project.Solution);

            // Source B will be added while debugging.
            EmitAndLoadLibraryToDebuggee(sourceA, sourceFilePath: sourceFileA.Path);

            var project = documentA.Project;
            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            // add a source file:
            var documentB = project.AddDocument("file2.cs", SourceText.From(sourceB), filePath: sourceFileB.Path);
            workspace.ChangeSolution(documentB.Project.Solution);

            // no changes in document1:
            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(documentA, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics1);

            // update in document2:
            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(documentB, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "ENC1003" }, diagnostics2.Select(d => d.Id));

            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            EndDebuggingSession(service, documentsWithRunModeDiagnostics: ImmutableArray.Create(documentB.Id));

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task RunMode_Diagnostics()
        {
            var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

            using (var workspace = CreateWorkspace())
            {
                var project = AddDefaultTestProject(workspace, "class C1 { void M() { System.Console.WriteLine(1); } }");
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

                var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
                Assert.Empty(updates.Updates);
                Assert.Empty(emitDiagnostics);

                diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(new[] { "ENC1003" }, diagnostics.Select(d => d.Id));

                EndDebuggingSession(service, documentsWithRunModeDiagnostics: ImmutableArray.Create(document2.Id));

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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, source);

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

            EndDebuggingSession(service);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=0|EmptySessionCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_ProjectThatDoesNotSupportEnC()
        {
            using (var workspace = CreateWorkspace(new[] { typeof(DummyLanguageService) }))
            {
                var solution = workspace.CurrentSolution;
                var project = solution.AddProject("dummy_proj", "dummy_proj", DummyLanguageService.LanguageName);
                var document = project.AddDocument("test", SourceText.From("dummy1"));
                workspace.ChangeSolution(document.Project.Solution);

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);
                StartEditSession(service);

                // change the source:
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("dummy2"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // validate solution update status and emit:
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
                Assert.Empty(updates.Updates);
                Assert.Empty(emitDiagnostics);
            }
        }

        [Fact]
        public async Task BreakMode_DesignTimeOnlyDocument_Dynamic()
        {
            using var workspace = CreateWorkspace();

            var project = AddDefaultTestProject(workspace, "class C {}");

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
            StartEditSession(service);

            // change the source:
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single(d => d.Id == documentInfo.Id);
            workspace.ChangeDocument(document1.Id, SourceText.From("class E {}"));

            // validate solution update status and emit:
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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

            using var workspace = CreateWorkspace();

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
            var moduleId = EmitLibrary(sourceA, sourceFilePath: sourceFileA.Path);

            if (!delayLoad)
            {
                LoadLibraryToDebuggee(moduleId);
            }

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            StartEditSession(service);

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

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(emitDiagnostics);

            if (delayLoad)
            {
                LoadLibraryToDebuggee(moduleId);

                // validate solution update status and emit:
                Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
                Assert.Empty(emitDiagnostics);
            }

            EndEditSession(service);
            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ErrorReadingModuleFile()
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

            using (var workspace = CreateWorkspace())
            {
                var project = AddDefaultTestProject(workspace, "class C1 { void M() { System.Console.WriteLine(1); } }");

                _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path);

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);
                StartEditSession(service);

                // change the source:
                var document1 = project.Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }"));
                var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

                // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
                var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Empty(diagnostics);

                Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

                var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
                Assert.Empty(updates.Updates);
                AssertEx.Equal(new[] { $"{project.Id} Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, moduleFile.Path, expectedErrorMessage)}" }, InspectDiagnostics(emitDiagnostics));

                EndEditSession(service);
                EndDebuggingSession(service);

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

            using var workspace = CreateWorkspace();

            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId)
            {
                OpenPdbStreamImpl = () =>
                {
                    throw new IOException("Error");
                }
            };

            var service = CreateEditAndContinueService(workspace);
            StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);
            StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the source:
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.GetDocument(document1.Id);

            // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // an error occurred so we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            AssertEx.Equal(new[] { $"{project.Id} Warning ENC1006: {string.Format(FeaturesResources.UnableToReadSourceFileOrPdb, sourceFile.Path)}" }, InspectDiagnostics(emitDiagnostics));

            EndEditSession(service);
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

            using var workspace = CreateWorkspace();

            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(DefaultTargetFramework)).
                AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);
            StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);
            StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the source:
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.GetDocument(document1.Id);

            using var fileLock = File.Open(sourceFile.Path, FileMode.Open, FileAccess.Read, FileShare.None);

            // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // an error occurred so we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            // try apply changes:
            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            AssertEx.Equal(new[] { $"{project.Id} Warning ENC1006: {string.Format(FeaturesResources.UnableToReadSourceFileOrPdb, sourceFile.Path)}" }, InspectDiagnostics(emitDiagnostics));

            fileLock.Dispose();

            // try apply changes again:
            (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            Assert.NotEmpty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndEditSession(service);
            EndDebuggingSession(service);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0"
            }, _telemetryLog);
        }

        [Fact]
        public async Task BreakMode_FileAdded()
        {
            var sourceA = "class C1 { void M() { System.Console.WriteLine(1); } }";
            var sourceB = "class C2 {}";

            var sourceFileA = Temp.CreateFile().WriteAllText(sourceA);
            var sourceFileB = Temp.CreateFile().WriteAllText(sourceB);

            using var workspace = new TestWorkspace(composition: s_composition);

            var documentA = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(sourceA, Encoding.UTF8), filePath: sourceFileA.Path);

            workspace.ChangeSolution(documentA.Project.Solution);

            // Source B will be added while debugging.
            EmitAndLoadLibraryToDebuggee(sourceA, sourceFilePath: sourceFileA.Path);

            var project = documentA.Project;

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);
            StartEditSession(service);

            // add a source file:
            var documentB = project.AddDocument("file2.cs", SourceText.From(sourceB, Encoding.UTF8), filePath: sourceFileB.Path);
            workspace.ChangeSolution(documentB.Project.Solution);
            documentB = workspace.CurrentSolution.GetDocument(documentB.Id);

            var diagnostics2 = await service.GetDocumentDiagnosticsAsync(documentB, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics2);

            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            EndEditSession(service);
            EndDebuggingSession(service);

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0"
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
            using (var workspace = CreateWorkspace())
            {
                var project = AddDefaultTestProject(workspace, source1);
                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId);

                LoadLibraryToDebuggee(moduleId, new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.NotAllowedForRuntime, "*message*"));

                var service = CreateEditAndContinueService(workspace);

                var debuggingSession = StartDebuggingSession(service);

                StartEditSession(service, loadedModules: _loadedModulesProvider);

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

                var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
                Assert.Empty(updates.Updates);
                AssertEx.Equal(new[] { $"{project.Id} Error ENC2016: {string.Format(FeaturesResources.EditAndContinueDisallowedByProject, project.Name, "*message*")}" }, InspectDiagnostics(emitDiagnostics));

                EndEditSession(service);
                EndDebuggingSession(service);

                AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

                AssertEx.Equal(new[]
                {
                    "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                    "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1",
                    "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC2016"
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

            using var workspace = CreateWorkspace();

            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, encoding), filePath: sourceFile.Path);

            var documentId = document1.Id;

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path, encoding: encoding);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            StartEditSession(service, loadedModules: _loadedModulesProvider);

            // Emulate opening the file, which will trigger "out-of-sync" check.
            // Since we find content matching the PDB checksum we update the committed solution with this source text.
            // If we used wrong encoding this would lead to a false change detected below.
            var currentDocument = workspace.CurrentSolution.GetDocument(documentId);
            await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(currentDocument, debuggingSession.CancellationToken).ConfigureAwait(false);

            // EnC service queries for a document, which triggers read of the source file from disk.
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            EndEditSession(service);
            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_RudeEdits()
        {
            var moduleId = Guid.NewGuid();

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, "class C1 { void M() { System.Console.WriteLine(1); } }");
            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId);

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service);

            StartEditSession(service);

            // change the source (rude edit):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M1() { System.Console.WriteLine(1); } }"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics1.Select(d => $"{d.Id}: {d.GetMessage()}"));

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndEditSession(service, documentsWithRudeEdits: ImmutableArray.Create(document2.Id));
            EndDebuggingSession(service);

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

            AssertEx.Equal(new[]
            {
                "Debugging_EncSession: SessionId=1|SessionCount=1|EmptySessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=0",
                "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=2|RudeEditKind=20|RudeEditSyntaxKind=8875|RudeEditBlocking=True"
            }, _telemetryLog);
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, sourceV1, generator: generator);

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service);
            StartEditSession(service);

            // change the source:
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From(sourceV2, Encoding.UTF8));

            var generatedDocument = (await workspace.CurrentSolution.Projects.Single().GetSourceGeneratedDocumentsAsync().ConfigureAwait(false)).Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(generatedDocument, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.property_) },
                diagnostics1.Select(d => $"{d.Id}: {d.GetMessage()}"));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndEditSession(service, documentsWithRudeEdits: ImmutableArray.Create(generatedDocument.Id));
            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_RudeEdits_DocumentOutOfSync()
        {
            var source0 = "class C1 { void M() { System.Console.WriteLine(0); } }";
            var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs");

            using var workspace = CreateWorkspace();

            var project = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40));

            workspace.ChangeSolution(project.Solution);

            // compile with source0:
            var moduleId = EmitAndLoadLibraryToDebuggee(source0, sourceFilePath: sourceFile.Path);

            // update the file with source1 before session starts:
            sourceFile.WriteAllText(source1);

            // source1 is reflected in workspace before session starts:
            var document1 = project.AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);
            workspace.ChangeSolution(document1.Project.Solution);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the source (rude edit):
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void RenamedMethod() { System.Console.WriteLine(1); } }"));
            var document2 = workspace.CurrentSolution.GetDocument(document1.Id);

            // no Rude Edits, since the document is out-of-sync
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // since the document is out-of-sync we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            AssertEx.Equal(new[] { $"{project.Id} Warning ENC1005: {string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFile.Path)}" }, InspectDiagnostics(emitDiagnostics));

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

            (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndEditSession(service, documentsWithRudeEdits: ImmutableArray.Create(document2.Id));
            EndDebuggingSession(service);

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

            using var workspace = CreateWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);

            // do not initialize the document state - we will detect the state based on the PDB content.
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            StartEditSession(service, loadedModules: _loadedModulesProvider);

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

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndEditSession(service, documentsWithRudeEdits: ImmutableArray.Create(document2.Id));
            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_RudeEdits_DelayLoadedModule()
        {
            var source1 = "class C { public void M() { } }";
            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1);

            using var workspace = CreateWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source1, Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var moduleId = EmitLibrary(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);

            // do not initialize the document state - we will detect the state based on the PDB content.
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            StartEditSession(service);

            // change the source (rude edit) before the library is loaded:
            workspace.ChangeDocument(document1.Id, SourceText.From("class C { public void Renamed() { } }"));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // Rude Edits reported:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(
                new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            // load library to the debuggee:
            LoadLibraryToDebuggee(moduleId);

            // Rude Edits still reported:
            diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(
                new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_will_prevent_the_debug_session_from_continuing, FeaturesResources.method) },
                diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndEditSession(service, documentsWithRudeEdits: ImmutableArray.Create(document2.Id));
            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_SyntaxError()
        {
            var moduleId = Guid.NewGuid();

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, "class C1 { void M() { System.Console.WriteLine(1); } }");
            _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(moduleId);

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service);

            StartEditSession(service);

            // change the source (compilation error):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { "));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // compilation errors are not reported via EnC diagnostic analyzer:
            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            EndEditSession(service);
            EndDebuggingSession(service);

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, sourceV1);

            var moduleId = EmitAndLoadLibraryToDebuggee(sourceV1);

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service);

            StartEditSession(service, loadedModules: _loadedModulesProvider);

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

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
            Assert.Empty(updates.Updates);

            // TODO: https://github.com/dotnet/roslyn/issues/36061
            // Semantic errors should not be reported in emit diagnostics.

            AssertEx.Equal(new[] { $"{project.Id} Error CS0266: {string.Format(CSharpResources.ERR_NoImplicitConvCast, "long", "int")}" }, InspectDiagnostics(emitDiagnostics));

            EndEditSession(service);
            EndDebuggingSession(service);

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
            using var workspace = CreateWorkspace();
            workspace.ChangeSolution(workspace.CurrentSolution.
                AddProject("A", "A", "C#").
                AddDocument("A.cs", "class Program { void Main() { System.Console.WriteLine(1); } }", filePath: "A.cs").Project.Solution.
                AddProject("B", "B", "C#").
                AddDocument("Common.cs", "class Common {}", filePath: "Common.cs").Project.
                AddDocument("B.cs", "class B {}", filePath: "B.cs").Project.Solution.
                AddProject("C", "C", "C#").
                AddDocument("Common.cs", "class Common {}", filePath: "Common.cs").Project.
                AddDocument("C.cs", "class C {}", filePath: "C.cs").Project.Solution);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);
            StartEditSession(service);

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

            EndEditSession(service);
            EndDebuggingSession(service);
        }

        [Fact]
        public async Task BreakMode_ValidSignificantChange_EmitError()
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var workspace = CreateWorkspace();

            var project = AddDefaultTestProject(workspace, sourceV1);
            EmitAndLoadLibraryToDebuggee(sourceV1);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the source (valid edit but passing no encoding to emulate emit error):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", encoding: null));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[] { $"{project.Id} Error CS8055: {string.Format(CSharpResources.ERR_EncodinglessSyntaxTree)}" }, InspectDiagnostics(emitDiagnostics));

            // no emitted delta:
            Assert.Empty(updates.Updates);

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

            EndEditSession(service);
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

            using var workspace = CreateWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(DefaultTargetFramework)).
                AddDocument("test.cs", SourceText.From("class C1 { void M() { System.Console.WriteLine(0); } }", Encoding.UTF8), filePath: sourceFile.Path);

            var documentId = document1.Id;

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            StartEditSession(service, loadedModules: _loadedModulesProvider);

            // The user opens the source file and changes the source before Roslyn receives file watcher event.
            var source2 = "class C1 { void M() { System.Console.WriteLine(2); } }";
            workspace.ChangeDocument(documentId, SourceText.From(source2, Encoding.UTF8));
            var document2 = workspace.CurrentSolution.GetDocument(documentId);

            // Save the document:
            if (saveDocument)
            {
                await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(document2, debuggingSession.CancellationToken).ConfigureAwait(false);
                sourceFile.WriteAllText(source2);
            }

            // EnC service queries for a document, which triggers read of the source file from disk.
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));
            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(emitDiagnostics);

            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            service.CommitSolutionUpdate();

            EndEditSession(service);

            StartEditSession(service, loadedModules: _loadedModulesProvider);

            // file watcher updates the workspace:
            workspace.ChangeDocument(documentId, CreateSourceTextFromFile(sourceFile.Path));
            var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var hasChanges = await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false);
            (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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

            EndEditSession(service);
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

            using var workspace = CreateWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document2 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From(source2, Encoding.UTF8), filePath: sourceFile.Path);

            var documentId = document2.Id;

            var project = document2.Project;
            workspace.ChangeSolution(project.Solution);

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            StartEditSession(service, loadedModules: _loadedModulesProvider);

            // user edits the file:
            workspace.ChangeDocument(documentId, SourceText.From(source3, Encoding.UTF8));
            var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // EnC service queries for a document, but the source file on disk doesn't match the PDB

            // We don't report rude edits for out-of-sync documents:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document3, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics);

            // since the document is out-of-sync we need to call update to determine whether we have changes to apply or not:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            AssertEx.Equal(new[] { $"{project.Id} Warning ENC1005: {string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFile.Path)}" }, InspectDiagnostics(emitDiagnostics));

            // undo:
            workspace.ChangeDocument(documentId, SourceText.From(source1, Encoding.UTF8));

            var currentDocument = workspace.CurrentSolution.GetDocument(documentId);

            // save (note that this call will fail to match the content with the PDB since it uses the content prior to the actual file write)
            await debuggingSession.LastCommittedSolution.OnSourceFileUpdatedAsync(currentDocument, debuggingSession.CancellationToken).ConfigureAwait(false);
            var (doc, state) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(documentId, currentDocument, CancellationToken.None).ConfigureAwait(false);
            Assert.Null(doc);
            Assert.Equal(CommittedSolution.DocumentState.OutOfSync, state);
            sourceFile.WriteAllText(source1);

            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));
            (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(emitDiagnostics);

            // the content actually hasn't changed:
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);

            EndEditSession(service);
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

            using var workspace = new TestWorkspace(composition: s_composition);

            // the workspace starts with no file
            var project = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40));

            workspace.ChangeSolution(project.Solution);

            var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

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
            StartEditSession(
                service,
                activeStatements,
                loadedModules: new MockManagedEditAndContinueDebuggerService()
                {
                    IsEditAndContinueAvailable = _ => new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.Attach, localizedMessage: "*attached*")
                });

            // File watcher observes the document and adds it to the workspace:

            var document1 = project.AddDocument("test.cs", sourceText1, filePath: sourceFile.Path);
            workspace.ChangeSolution(document1.Project.Solution);

            // We don't report rude edits for the added document:
            var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics);

            // TODO: https://github.com/dotnet/roslyn/issues/49938
            // We currently create the AS map against the committed solution, which may not contain all documents.
            // var spans = await service.GetBaseActiveStatementSpansAsync(workspace.CurrentSolution, ImmutableArray.Create(document1.Id), CancellationToken.None).ConfigureAwait(false);
            // AssertEx.Equal(new[] { $"({activeLineSpan1}, IsLeafFrame)" }, spans.Single().Select(s => s.ToString()));

            // No changes.
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);

            AssertEx.Empty(emitDiagnostics);

            EndEditSession(service);
            EndDebuggingSession(service);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BreakMode_ValidSignificantChange_DocumentOutOfSync(bool delayLoad)
        {
            var sourceOnDisk = "class C1 { void M() { System.Console.WriteLine(1); } }";

            var dir = Temp.CreateDirectory();
            var sourceFile = dir.CreateFile("test.cs").WriteAllText(sourceOnDisk);

            using var workspace = CreateWorkspace();

            // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
            var document1 = workspace.CurrentSolution.
                AddProject("test", "test", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From("class C1 { void M() { System.Console.WriteLine(0); } }", Encoding.UTF8), filePath: sourceFile.Path);

            var project = document1.Project;
            workspace.ChangeSolution(project.Solution);

            var moduleId = EmitLibrary(sourceOnDisk, sourceFilePath: sourceFile.Path);

            if (!delayLoad)
            {
                LoadLibraryToDebuggee(moduleId);
            }

            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service, initialState: CommittedSolution.DocumentState.None);

            StartEditSession(service);

            // no changes have been made to the project
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(updates.Updates);
            Assert.Empty(emitDiagnostics);

            // a file watcher observed a change and updated the document, so it now reflects the content on disk (the code that we compiled):
            workspace.ChangeDocument(document1.Id, SourceText.From(sourceOnDisk, Encoding.UTF8));
            var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics = await service.GetDocumentDiagnosticsAsync(document3, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(diagnostics);

            // the content of the file is now exactly the same as the compiled document, so there is no change to be applied:
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.None, updates.Status);
            Assert.Empty(emitDiagnostics);

            EndEditSession(service);
            EndDebuggingSession(service);

            Assert.Empty(debuggingSession.Test_GetModulesPreparedForUpdate());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BreakMode_ValidSignificantChange_EmitSuccessful(bool commitUpdate)
        {
            var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, sourceV1);

            var moduleId = EmitAndLoadLibraryToDebuggee(sourceV1);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service);
            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the source (valid edit):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            AssertEx.Empty(diagnostics1);

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(0x06000001, delta.UpdatedMethods.Single());
            Assert.Equal(moduleId, delta.Module);
            Assert.Empty(delta.ExceptionRegions);
            Assert.Empty(delta.SequencePoints);

            // the update should be stored on the service:
            var pendingUpdate = editSession.Test_GetPendingSolutionUpdate();
            var (baselineProjectId, newBaseline) = pendingUpdate.EmitBaselines.Single();
            AssertEx.Equal(updates.Updates, pendingUpdate.Deltas);
            Assert.Equal(project.Id, baselineProjectId);
            Assert.Equal(moduleId, newBaseline.OriginalMetadata.GetModuleVersionId());

            var readers = pendingUpdate.ModuleReaders;
            Assert.Equal(2, readers.Length);
            Assert.NotNull(readers[0]);
            Assert.NotNull(readers[1]);

            if (commitUpdate)
            {
                // all update providers either provided updates or had no change to apply:
                service.CommitSolutionUpdate();

                Assert.Null(editSession.Test_GetPendingSolutionUpdate());

                // no change in non-remappable regions since we didn't have any active statements:
                Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

                var baselineReaders = editSession.DebuggingSession.GetBaselineModuleReaders();
                Assert.Equal(2, baselineReaders.Length);
                Assert.Same(readers[0], baselineReaders[0]);
                Assert.Same(readers[1], baselineReaders[1]);

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

            EndEditSession(service);
            EndDebuggingSession(service);

            // open module readers should be disposed when the debugging session ends:
            VerifyReadersDisposed(readers);

            AssertEx.SetEqual(new[] { moduleId }, debuggingSession.Test_GetModulesPreparedForUpdate());

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
            var compilationV1 = CSharpTestBase.CreateCompilation(sourceV1, options: TestOptions.DebugDll, targetFramework: DefaultTargetFramework, assemblyName: "lib");

            var (peImage, pdbImage) = compilationV1.EmitToArrays(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
            var moduleMetadata = ModuleMetadata.CreateFromImage(peImage);
            var moduleFile = dir.CreateFile("lib.dll").WriteAllBytes(peImage);
            var pdbFile = dir.CreateFile("lib.pdb").WriteAllBytes(pdbImage);
            var moduleId = moduleMetadata.GetModuleVersionId();

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, sourceV1);

            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            _mockCompilationOutputsProvider = _ => new CompilationOutputFiles(moduleFile.Path, pdbFile.Path);

            // set up an active statement in the first method, so that we can test preservation of local signature.
            var activeStatements = ImmutableArray.Create(new ManagedActiveStatementDebugInfo(
                new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 0),
                documentName: document1.Name,
                sourceSpan: new SourceSpan(0, 15, 0, 16),
                ActiveStatementFlags.IsLeafFrame));

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            // module is not loaded:
            var editSession = StartEditSession(service, activeStatements, loadedModules: _loadedModulesProvider);

            // change the source (valid edit):
            workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M1() { int a = 1; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }", Encoding.UTF8));
            var document2 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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

                EndEditSession(service);

                // make another update:
                StartEditSession(service);

                // Update M1 - this method has an active statement, so we will attempt to preserve the local signature.
                // Since the method hasn't been edited before we'll read the baseline PDB to get the signature token.
                // This validates that the Portable PDB reader can be used (and is not disposed) for a second generation edit.
                var document3 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document3.Id, SourceText.From("class C1 { void M1() { int a = 3; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
                Assert.Empty(emitDiagnostics);

                EndEditSession(service);
                EndDebuggingSession(service);

                // open module readers should be disposed when the debugging session ends:
                VerifyReadersDisposed(readers);
            }
            else
            {
                service.DiscardSolutionUpdate();
                Assert.Null(editSession.Test_GetPendingSolutionUpdate());

                // no open module readers since we didn't defer any module update:
                Assert.Empty(editSession.DebuggingSession.GetBaselineModuleReaders());

                VerifyReadersDisposed(readers);

                EndEditSession(service);
                EndDebuggingSession(service);
            }
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, new[] { sourceA1, sourceB1 });

            LoadLibraryToDebuggee(EmitLibrary(new[] { (sourceA1, "test1.cs"), (sourceB1, "test2.cs") }));

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service);
            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the source (valid edit):
            var documentA = project.Documents.First();
            var documentB = project.Documents.Skip(1).First();
            workspace.ChangeDocument(documentA.Id, SourceText.From(sourceA2, Encoding.UTF8));
            workspace.ChangeDocument(documentB.Id, SourceText.From(sourceB2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(6, delta.UpdatedMethods.Length);  // F, C.C(), D.D(), E.E(int), E.E(int, int), lambda

            EndEditSession(service);
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
            const string OpeningMarker = "/* GENERATE:";
            const string ClosingMarker = "*/";

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
                var index = source.IndexOf(OpeningMarker);
                if (index > 0)
                {
                    index += OpeningMarker.Length;
                    var closing = source.IndexOf(ClosingMarker, index);
                    context.AddSource($"Generated_{fileName}", source[index..closing].Trim());
                }
            }
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, sourceV1, generator);

            var moduleId = EmitLibrary(sourceV1, generator: generator);
            LoadLibraryToDebuggee(moduleId);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service);
            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the source (valid edit):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From(sourceV2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(2, delta.UpdatedMethods.Length);

            EndEditSession(service);
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, sourceV1, generator);

            var moduleId = EmitLibrary(sourceV1, generator: generator);
            LoadLibraryToDebuggee(moduleId);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service);
            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the source (valid edit):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From(sourceV2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
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

            EndEditSession(service);
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, sourceV1, generator);

            var moduleId = EmitLibrary(sourceV1, generator: generator);
            LoadLibraryToDebuggee(moduleId);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service);
            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the source (valid edit):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeDocument(document1.Id, SourceText.From(sourceV2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(1, delta.UpdatedMethods.Length); // constructor update

            EndEditSession(service);
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, source, generator, additionalFileText: additionalSourceV1);

            var moduleId = EmitLibrary(source, generator: generator, additionalFileText: additionalSourceV1);
            LoadLibraryToDebuggee(moduleId);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service);
            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the additional source (valid edit):
            var additionalDocument1 = workspace.CurrentSolution.Projects.Single().AdditionalDocuments.Single();
            workspace.ChangeAdditionalDocument(additionalDocument1.Id, SourceText.From(additionalSourceV2, Encoding.UTF8));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(1, delta.UpdatedMethods.Length);

            EndEditSession(service);
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, source, generator, analyzerConfig: configV1);

            var moduleId = EmitLibrary(source, generator: generator, analyzerOptions: configV1);
            LoadLibraryToDebuggee(moduleId);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service);
            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            // change the additional source (valid edit):
            var configDocument1 = workspace.CurrentSolution.Projects.Single().AnalyzerConfigDocuments.Single();
            workspace.ChangeAnalyzerConfigDocument(configDocument1.Id, GetAnalyzerConfigText(configV2));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(1, delta.UpdatedMethods.Length);

            EndEditSession(service);
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, source1, generator);

            var moduleId = EmitLibrary(source1, generator: generator);
            LoadLibraryToDebuggee(moduleId);

            var service = CreateEditAndContinueService(workspace);
            var debuggingSession = StartDebuggingSession(service);
            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            // remove the source document (valid edit):
            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
            workspace.ChangeSolution(document1.Project.Solution.RemoveDocument(document1.Id));

            // validate solution update status and emit:
            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(emitDiagnostics);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

            // check emitted delta:
            var delta = updates.Updates.Single();
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(1, delta.UpdatedMethods.Length);

            EndEditSession(service);
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

            using var workspace = CreateWorkspace();
            var projectA = AddDefaultTestProject(workspace, source1);

            var projectB = workspace.CurrentSolution.AddProject("B", "A", "C#").AddMetadataReferences(projectA.MetadataReferences).AddDocument("DocB", source1, filePath: "DocB.cs").Project;
            workspace.ChangeSolution(projectB.Solution);

            _mockCompilationOutputsProvider = project =>
                (project.Id == projectA.Id) ? new CompilationOutputFiles(moduleFileA.Path, pdbFileA.Path) :
                (project.Id == projectB.Id) ? new CompilationOutputFiles(moduleFileB.Path, pdbFileB.Path) :
                throw ExceptionUtilities.UnexpectedValue(project);

            // only module A is loaded
            LoadLibraryToDebuggee(moduleIdA);

            var service = CreateEditAndContinueService(workspace);

            StartDebuggingSession(service);

            var editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            //
            // First update.
            //

            workspace.ChangeDocument(projectA.Documents.Single().Id, SourceText.From(source2, Encoding.UTF8));
            workspace.ChangeDocument(projectB.Documents.Single().Id, SourceText.From(source2, Encoding.UTF8));

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            Assert.Empty(emitDiagnostics);

            var deltaA = updates.Updates.Single(d => d.Module == moduleIdA);
            var deltaB = updates.Updates.Single(d => d.Module == moduleIdB);
            Assert.Equal(2, updates.Updates.Length);

            // the update should be stored on the service:
            var pendingUpdate = editSession.Test_GetPendingSolutionUpdate();
            var (_, newBaselineA1) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectA.Id);
            var (_, newBaselineB1) = pendingUpdate.EmitBaselines.Single(b => b.ProjectId == projectB.Id);

            var baselineA0 = newBaselineA1.GetInitialEmitBaseline();
            var baselineB0 = newBaselineB1.GetInitialEmitBaseline();

            var readers = pendingUpdate.ModuleReaders;
            Assert.Equal(4, readers.Length);
            Assert.False(readers.Any(r => r is null));

            Assert.Equal(moduleIdA, newBaselineA1.OriginalMetadata.GetModuleVersionId());
            Assert.Equal(moduleIdB, newBaselineB1.OriginalMetadata.GetModuleVersionId());

            service.CommitSolutionUpdate();
            Assert.Null(editSession.Test_GetPendingSolutionUpdate());

            // no change in non-remappable regions since we didn't have any active statements:
            Assert.Empty(editSession.DebuggingSession.NonRemappableRegions);

            // deferred module readers tracked:
            var baselineReaders = editSession.DebuggingSession.GetBaselineModuleReaders();
            AssertEx.Equal(readers, baselineReaders);

            // verify that baseline is added for both modules:
            Assert.Same(newBaselineA1, editSession.DebuggingSession.Test_GetProjectEmitBaseline(projectA.Id));
            Assert.Same(newBaselineB1, editSession.DebuggingSession.Test_GetProjectEmitBaseline(projectB.Id));

            // solution update status after committing an update:
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            EndEditSession(service);
            editSession = StartEditSession(service, loadedModules: _loadedModulesProvider);

            //
            // Second update.
            //

            workspace.ChangeDocument(projectA.Documents.Single().Id, SourceText.From(source3, Encoding.UTF8));
            workspace.ChangeDocument(projectB.Documents.Single().Id, SourceText.From(source3, Encoding.UTF8));

            // validate solution update status and emit:
            Assert.True(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);
            Assert.Empty(emitDiagnostics);

            deltaA = updates.Updates.Single(d => d.Module == moduleIdA);
            deltaB = updates.Updates.Single(d => d.Module == moduleIdB);
            Assert.Equal(2, updates.Updates.Length);

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
            AssertEx.Equal(readers, baselineReaders);

            // verify that baseline is updated for both modules:
            Assert.Same(newBaselineA2, editSession.DebuggingSession.Test_GetProjectEmitBaseline(projectA.Id));
            Assert.Same(newBaselineB2, editSession.DebuggingSession.Test_GetProjectEmitBaseline(projectB.Id));

            // solution update status after committing an update:
            Assert.False(await service.HasChangesAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, sourceFilePath: null, CancellationToken.None).ConfigureAwait(false));

            EndEditSession(service);

            EndDebuggingSession(service);

            // open deferred module readers should be dispose when the debugging session ends:
            VerifyReadersDisposed(readers);
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
            using (var workspace = CreateWorkspace())
            {
                var project = AddDefaultTestProject(workspace, "class C1 { void M() { System.Console.WriteLine(1); } }");

                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.NewGuid())
                {
                    OpenPdbStreamImpl = () => null,
                    OpenAssemblyStreamImpl = () => null,
                };

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                // module not loaded
                StartEditSession(service, loadedModules: _loadedModulesProvider);

                // change the source (valid edit):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(new[] { $"{project.Id} Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "test-pdb", new FileNotFoundException().Message)}" }, InspectDiagnostics(emitDiagnostics));
                Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);
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

            using (var workspace = CreateWorkspace())
            {
                var project = AddDefaultTestProject(workspace, sourceV1);

                _mockCompilationOutputsProvider = _ => new MockCompilationOutputs(Guid.NewGuid())
                {
                    OpenPdbStreamImpl = () => pdbStream,
                    OpenAssemblyStreamImpl = () => throw new IOException("*message*"),
                };

                var service = CreateEditAndContinueService(workspace);

                StartDebuggingSession(service);

                // module not loaded
                StartEditSession(service, loadedModules: _loadedModulesProvider);

                // change the source (valid edit):
                var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();
                workspace.ChangeDocument(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", Encoding.UTF8));

                var (updates, emitDiagnostics) = await service.EmitSolutionUpdateAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, CancellationToken.None).ConfigureAwait(false);
                AssertEx.Equal(new[] { $"{project.Id} Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "test-assembly", "*message*")}" }, InspectDiagnostics(emitDiagnostics));
                Assert.Equal(ManagedModuleUpdateStatus.Blocked, updates.Status);

                EndEditSession(service);
                EndDebuggingSession(service);

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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, sourceV1);

            var activeSpan11 = GetSpan(sourceV1, "G(1);");
            var activeSpan12 = GetSpan(sourceV1, "System.Console.WriteLine(1)");
            var activeSpan21 = GetSpan(sourceV2, "G(2); G(1);");
            var activeSpan22 = GetSpan(sourceV2, "System.Console.WriteLine(2)");
            var adjustedActiveSpan1 = GetSpan(sourceV2, "G(2);");
            var adjustedActiveSpan2 = GetSpan(sourceV2, "System.Console.WriteLine(2)");

            var document1 = project.Documents.Single();
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

            var service = CreateEditAndContinueService(workspace);

            // default if called outside of edit session
            Assert.True((await service.GetBaseActiveStatementSpansAsync(workspace.CurrentSolution, ImmutableArray.Create(document1.Id), CancellationToken.None).ConfigureAwait(false)).IsDefault);

            var debuggingSession = StartDebuggingSession(service);

            // default if called outside of edit session
            Assert.True((await service.GetBaseActiveStatementSpansAsync(workspace.CurrentSolution, ImmutableArray.Create(document1.Id), CancellationToken.None).ConfigureAwait(false)).IsDefault);

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

            var editSession = StartEditSession(service, activeStatements);

            var baseSpans = await service.GetBaseActiveStatementSpansAsync(workspace.CurrentSolution, ImmutableArray.Create(document1.Id), CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"({activeLineSpan11}, IsNonLeafFrame)",
                $"({activeLineSpan12}, IsLeafFrame)"
            }, baseSpans.Single().Select(s => s.ToString()));

            var trackedActiveSpans1 = ImmutableArray.Create(activeSpan11, activeSpan12);

            var currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document1, (_) => new(trackedActiveSpans1), CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"({activeLineSpan11}, IsNonLeafFrame)",
                $"({activeLineSpan12}, IsLeafFrame)"
            }, currentSpans.Select(s => s.ToString()));

            Assert.Equal(activeLineSpan11,
                await service.GetCurrentActiveStatementPositionAsync(document1.Project.Solution, (_, _) => new(trackedActiveSpans1), activeInstruction1, CancellationToken.None).ConfigureAwait(false));

            Assert.Equal(activeLineSpan12,
                await service.GetCurrentActiveStatementPositionAsync(document1.Project.Solution, (_, _) => new(trackedActiveSpans1), activeInstruction2, CancellationToken.None).ConfigureAwait(false));

            // change the source (valid edit):
            workspace.ChangeDocument(documentId, sourceTextV2);
            var document2 = workspace.CurrentSolution.GetDocument(documentId);

            // tracking span update triggered by the edit:
            var trackedActiveSpans2 = ImmutableArray.Create(activeSpan21, activeSpan22);

            baseSpans = await service.GetBaseActiveStatementSpansAsync(workspace.CurrentSolution, ImmutableArray.Create(document2.Id), CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"({activeLineSpan11}, IsNonLeafFrame)",
                $"({activeLineSpan12}, IsLeafFrame)"
            }, baseSpans.Single().Select(s => s.ToString()));

            currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document2, _ => new(trackedActiveSpans2), CancellationToken.None).ConfigureAwait(false);
            AssertEx.Equal(new[]
            {
                $"({adjustedActiveLineSpan1}, IsNonLeafFrame)",
                $"({adjustedActiveLineSpan2}, IsLeafFrame)"
            }, currentSpans.Select(s => s.ToString()));

            Assert.Equal(adjustedActiveLineSpan1,
                await service.GetCurrentActiveStatementPositionAsync(workspace.CurrentSolution, (_, _) => new(trackedActiveSpans2), activeInstruction1, CancellationToken.None).ConfigureAwait(false));

            Assert.Equal(adjustedActiveLineSpan2,
                await service.GetCurrentActiveStatementPositionAsync(workspace.CurrentSolution, (_, _) => new(trackedActiveSpans2), activeInstruction2, CancellationToken.None).ConfigureAwait(false));
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

            using var workspace = CreateWorkspace();
            var project = AddDefaultTestProject(workspace, sourceV1);

            var activeSpan11 = GetSpan(sourceV1, "G(1)");
            var activeSpan12 = GetSpan(sourceV1, "System.Console.WriteLine(1)");

            var document1 = project.Documents.Single();
            var documentId = document1.Id;
            var documentFilePath = document1.FilePath;

            var sourceTextV1 = await document1.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
            var sourceTextV2 = SourceText.From(sourceV2, Encoding.UTF8);

            var activeLineSpan11 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan11);
            var activeLineSpan12 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan12);
            var service = CreateEditAndContinueService(workspace);

            var debuggingSession = StartDebuggingSession(service,
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

            var editSession = StartEditSession(service, activeStatements);

            // change the source (valid edit):
            workspace.ChangeDocument(documentId, sourceTextV2);
            var document2 = workspace.CurrentSolution.GetDocument(documentId);

            var baseSpans = await service.GetBaseActiveStatementSpansAsync(workspace.CurrentSolution, ImmutableArray.Create(documentId), CancellationToken.None).ConfigureAwait(false);

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

            Assert.Null(await service.GetCurrentActiveStatementPositionAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, activeInstruction1, CancellationToken.None).ConfigureAwait(false));
            Assert.Null(await service.GetCurrentActiveStatementPositionAsync(workspace.CurrentSolution, s_noSolutionActiveSpans, activeInstruction2, CancellationToken.None).ConfigureAwait(false));
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
                new ManagedActiveStatementDebugInfo(
                    new ManagedInstructionId(new ManagedMethodId(Guid.Empty, token: 0x06000001, version: 1), ilOffset: 0),
                    documentName: document.Name,
                    sourceSpan: new SourceSpan(0, 1, 0, 2),
                    ActiveStatementFlags.IsNonLeafFrame));

            StartEditSession(service, activeStatements);

            // active statements are tracked not in non-Roslyn projects:
            var currentSpans = await service.GetAdjustedActiveStatementSpansAsync(document, s_noDocumentActiveSpans, CancellationToken.None).ConfigureAwait(false);
            Assert.True(currentSpans.IsDefault);

            var baseSpans = await service.GetBaseActiveStatementSpansAsync(workspace.CurrentSolution, ImmutableArray.Create(document.Id), CancellationToken.None).ConfigureAwait(false);
            Assert.Empty(baseSpans.Single());
        }
    }
}
