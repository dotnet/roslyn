// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

public abstract class EditAndContinueWorkspaceTestBase : TestBase, IDisposable
{
    private protected static readonly Guid s_solutionTelemetryId = Guid.Parse("00000000-AAAA-AAAA-AAAA-000000000000");
    private protected static readonly Guid s_defaultProjectTelemetryId = Guid.Parse("00000000-AAAA-AAAA-AAAA-111111111111");
    private protected static readonly Regex s_timePropertiesRegex = new("[|](EmitDifferenceMilliseconds|TotalAnalysisMilliseconds)=[0-9]+");

    private protected static readonly ActiveStatementSpanProvider s_noActiveSpans =
        (_, _, _) => new([]);

    private protected const TargetFramework DefaultTargetFramework = TargetFramework.NetLatest;

    private protected readonly Dictionary<ProjectId, CompilationOutputs> _mockCompilationOutputs = [];
    private protected readonly List<string> _telemetryLog = [];
    private protected int _telemetryId;

    private protected readonly MockManagedEditAndContinueDebuggerService _debuggerService = new()
    {
        LoadedModules = []
    };

    /// <summary>
    /// Streams that are verified to be disposed at the end of the debug session (by default).
    /// </summary>
    private ImmutableList<Stream> _disposalVerifiedStreams = [];

    public override void Dispose()
    {
        base.Dispose();

        foreach (var stream in _disposalVerifiedStreams)
        {
            Assert.False(stream.CanRead);
        }
    }

    internal TestWorkspace CreateWorkspace(out Solution solution, out EditAndContinueService service, Type[]? additionalParts = null)
    {
        var composition = FeaturesTestCompositions.Features
            .AddParts(typeof(TestWorkspaceConfigurationService))
            .AddParts(additionalParts);

        var workspace = new TestWorkspace(composition: composition, solutionTelemetryId: s_solutionTelemetryId);
        solution = workspace.CurrentSolution;
        service = GetEditAndContinueService(workspace);
        return workspace;
    }

    internal static SourceText GetAnalyzerConfigText((string key, string value)[] analyzerConfig)
        => CreateText("[*.*]" + Environment.NewLine + string.Join(Environment.NewLine, analyzerConfig.Select(c => $"{c.key} = {c.value}")));

    internal static (Solution, Document) AddDefaultTestProject(
        Solution solution,
        string source,
        TempDirectory? projectDirectory = null,
        ISourceGenerator? generator = null,
        string? additionalFileText = null,
        (string key, string value)[]? analyzerConfig = null)
    {
        solution = AddDefaultTestProject(solution, [source], projectDirectory, generator, additionalFileText, analyzerConfig);
        return (solution, solution.Projects.Single().Documents.Single());
    }

    internal static Project AddEmptyTestProject(Solution solution)
        => solution
            .AddTestProject("proj")
            .WithMetadataReferences(TargetFrameworkUtil.GetReferences(DefaultTargetFramework));

    internal static Solution AddDefaultTestProject(
        Solution solution,
        string[] sources,
        TempDirectory? projectDirectory = null,
        ISourceGenerator? generator = null,
        string? additionalFileText = null,
        (string key, string value)[]? analyzerConfig = null)
    {
        Assert.NotEmpty(sources);

        var project = AddEmptyTestProject(solution);
        solution = project.Solution;

        if (generator != null)
        {
            solution = solution.AddAnalyzerReference(project.Id, new TestGeneratorReference(generator));
        }

        if (additionalFileText != null)
        {
            solution = solution.AddAdditionalDocument(DocumentId.CreateNewId(project.Id), "additional", CreateText(additionalFileText));
        }

        if (analyzerConfig != null)
        {
            var fileName = "config";
            var text = GetAnalyzerConfigText(analyzerConfig);
            var filePath = GetFilePath(fileName, text.ToString());

            solution = solution.AddAnalyzerConfigDocument(
                DocumentId.CreateNewId(project.Id),
                name: "config",
                text,
                filePath: filePath);
        }

        Document? document = null;
        var i = 1;
        foreach (var source in sources)
        {
            var fileName = $"test{i++}.cs";
            var filePath = GetFilePath(fileName, source);

            document = solution.GetRequiredProject(project.Id).
                AddDocument(fileName, CreateText(source), filePath: filePath);

            solution = document.Project.Solution;
        }

        Debug.Assert(document != null);
        return document.Project.Solution;

        string GetFilePath(string fileName, string content)
            => projectDirectory?.CreateFile(fileName).WriteAllText(content, Encoding.UTF8).Path ?? Path.Combine(TempRoot.Root, fileName);
    }

    internal EditAndContinueService GetEditAndContinueService(TestWorkspace workspace)
    {
        var service = (EditAndContinueService)workspace.GetService<IEditAndContinueService>();
        var accessor = service.GetTestAccessor();

        // Empty guid means project is not built.
        accessor.SetOutputProvider(project => _mockCompilationOutputs.GetValueOrDefault(project.Id, null) ?? new MockCompilationOutputs(Guid.Empty));

        return service;
    }

    internal DebuggingSession StartDebuggingSession(
        EditAndContinueService service,
        Solution solution,
        CommittedSolution.DocumentState initialState = CommittedSolution.DocumentState.MatchesBuildOutput,
        IPdbMatchingSourceTextProvider? sourceTextProvider = null)
    {
        var sessionId = service.StartDebuggingSession(
            solution,
            _debuggerService,
            sourceTextProvider: sourceTextProvider ?? NullPdbMatchingSourceTextProvider.Instance,
            reportDiagnostics: true);

        var session = service.GetTestAccessor().GetDebuggingSession(sessionId);

        if (initialState != CommittedSolution.DocumentState.None)
        {
            EditAndContinueTestVerifier.SetDocumentsState(session, solution, initialState);
        }

        session.GetTestAccessor().SetTelemetryLogger((id, message) => _telemetryLog.Add($"{id}: {s_timePropertiesRegex.Replace(message.GetMessage(), "")}"), () => ++_telemetryId);

        return session;
    }

    internal void EnterBreakState(
        DebuggingSession session,
        ImmutableArray<ManagedActiveStatementDebugInfo> activeStatements = default)
    {
        _debuggerService.GetActiveStatementsImpl = () => activeStatements.NullToEmpty();
        session.BreakStateOrCapabilitiesChanged(inBreakState: true);
    }

    internal void ExitBreakState(
        DebuggingSession session)
    {
        _debuggerService.GetActiveStatementsImpl = () => [];
        session.BreakStateOrCapabilitiesChanged(inBreakState: false);
    }

    internal static void CapabilitiesChanged(DebuggingSession session)
        => session.BreakStateOrCapabilitiesChanged(inBreakState: null);

    internal static void CommitSolutionUpdate(DebuggingSession session)
        => session.CommitSolutionUpdate();

    internal static void DiscardSolutionUpdate(DebuggingSession session)
        => session.DiscardSolutionUpdate();

    internal static void EndDebuggingSession(DebuggingSession session)
        => session.EndSession(out _);

    internal static async ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(
        DebuggingSession session,
        Solution solution,
        ActiveStatementSpanProvider? activeStatementSpanProvider = null)
    {
        var runningProjects = solution.ProjectIds.ToImmutableDictionary(
            keySelector: id => id,
            elementSelector: id => new RunningProjectOptions() { RestartWhenChangesHaveNoEffect = false });

        var results = await session.EmitSolutionUpdateAsync(solution, runningProjects, activeStatementSpanProvider ?? s_noActiveSpans, CancellationToken.None);

        var hasTransientError = results.Diagnostics.SelectMany(pd => pd.Diagnostics).Any(d => d.IsEncDiagnostic() && d.Severity == DiagnosticSeverity.Error);

        Assert.Equal(hasTransientError, results.ProjectsToRestart.Any());
        Assert.Equal(hasTransientError, results.ProjectsToRebuild.Any());

        return results;
    }

    internal static IEnumerable<string> InspectDiagnostics(ImmutableArray<ProjectDiagnostics> actual)
        => actual.SelectMany(pd => pd.Diagnostics.Select(d => $"{pd.ProjectId.DebugName}: {InspectDiagnostic(d)}")).Order();

    internal static IEnumerable<string> InspectDiagnostics(ImmutableArray<(ProjectId project, ImmutableArray<Diagnostic> diagnostics)> diagnostics)
        => diagnostics.SelectMany(pd => pd.diagnostics.Select(d => $"{pd.project.DebugName}: {InspectDiagnostic(d)}")).Order();

    internal static string InspectDiagnostic(Diagnostic actual)
        => $"{Inspect(actual.Location)}: {actual.Severity} {actual.Id}: {actual.GetMessage()}";

    internal static string Inspect(Location actual)
        => actual.GetLineSpan() is { IsValid: true } span ? span.ToString() : "<no location>";

    internal static IEnumerable<string> InspectDiagnostics(ImmutableArray<Diagnostic> actual)
        => actual.Select(InspectDiagnostic);

    internal static IEnumerable<string> InspectDiagnosticIds(ImmutableArray<DiagnosticData> actual)
        => actual.Select(d => d.Id);

    internal static IEnumerable<string> InspectDiagnosticIds(ImmutableArray<ProjectDiagnostics> actual)
        => InspectDiagnosticIds(actual.SelectMany(pd => pd.Diagnostics));

    internal static IEnumerable<string> InspectDiagnosticIds(IEnumerable<Diagnostic> actual)
        => actual.Select(d => d.Id);

    internal static Guid ReadModuleVersionId(Stream stream)
    {
        using var peReader = new PEReader(stream);
        var metadataReader = peReader.GetMetadataReader();
        var mvidHandle = metadataReader.GetModuleDefinition().Mvid;
        return metadataReader.GetGuid(mvidHandle);
    }

    internal Guid EmitAndLoadLibraryToDebuggee(Document document, TargetFramework targetFramework = DefaultTargetFramework)
        => EmitAndLoadLibraryToDebuggee(document.Project, targetFramework);

    internal Guid EmitAndLoadLibraryToDebuggee(Project project, TargetFramework targetFramework = DefaultTargetFramework)
        => LoadLibraryToDebuggee(EmitLibrary(project, targetFramework));

    internal Guid EmitAndLoadLibraryToDebuggee(
        ProjectId projectId,
        string source,
        string language = LanguageNames.CSharp,
        string? sourceFilePath = null,
        Encoding? encoding = null,
        SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithms.Default,
        string? assemblyName = null,
        TargetFramework targetFramework = DefaultTargetFramework)
        => LoadLibraryToDebuggee(EmitLibrary(projectId, source, sourceFilePath, language, encoding, checksumAlgorithm, assemblyName, targetFramework: targetFramework));

    internal Guid LoadLibraryToDebuggee(Guid moduleId, ManagedHotReloadAvailability availability = default)
    {
        _debuggerService.LoadedModules!.Add(moduleId, availability);
        return moduleId;
    }

    internal Guid EmitLibrary(Project project, TargetFramework targetFramework = DefaultTargetFramework)
        => EmitLibrary(
            project.Id,
            project.Documents.Select(d => (d.GetTextSynchronously(CancellationToken.None), d.FilePath ?? throw ExceptionUtilities.UnexpectedValue(null))),
            project.Language,
            project.AssemblyName,
            targetFramework: targetFramework);

    internal Guid EmitLibrary(
        ProjectId projectId,
        string source,
        string? sourceFilePath = null,
        string language = LanguageNames.CSharp,
        Encoding? encoding = null,
        SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithms.Default,
        string? assemblyName = null,
        DebugInformationFormat pdbFormat = DebugInformationFormat.PortablePdb,
        Project? generatorProject = null,
        string? additionalFileText = null,
        IEnumerable<(string, string)>? analyzerOptions = null,
        TargetFramework targetFramework = DefaultTargetFramework)
        => EmitLibrary(
            projectId,
            [(source, sourceFilePath ?? Path.Combine(TempRoot.Root, "test1.cs"))],
            language,
            encoding,
            checksumAlgorithm,
            assemblyName,
            pdbFormat,
            generatorProject,
            additionalFileText,
            analyzerOptions,
            targetFramework);

    internal Guid EmitLibrary(
        ProjectId projectId,
        (string content, string filePath)[] sources,
        string language = LanguageNames.CSharp,
        Encoding? encoding = null,
        SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithms.Default,
        string? assemblyName = null,
        DebugInformationFormat pdbFormat = DebugInformationFormat.PortablePdb,
        Project? generatorProject = null,
        string? additionalFileText = null,
        IEnumerable<(string, string)>? analyzerOptions = null,
        TargetFramework targetFramework = DefaultTargetFramework)
    {
        return EmitLibrary(
            projectId,
            sources.Select(source => (CreateText(source.content, encoding, checksumAlgorithm), source.filePath)),
            language,
            assemblyName,
            pdbFormat,
            generatorProject,
            additionalFileText,
            analyzerOptions,
            targetFramework);
    }

    internal static SourceText CreateText(string source, Encoding? encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithms.Default)
    {
        encoding ??= Encoding.UTF8;
        return SourceText.From(new MemoryStream(encoding.GetBytesWithPreamble(source)), encoding, checksumAlgorithm);
    }

    internal Guid EmitLibrary(
        ProjectId projectId,
        IEnumerable<(SourceText text, string filePath)> sources,
        string language = LanguageNames.CSharp,
        string? assemblyName = null,
        DebugInformationFormat pdbFormat = DebugInformationFormat.PortablePdb,
        Project? generatorProject = null,
        string? additionalFileText = null,
        IEnumerable<(string, string)>? analyzerOptions = null,
        TargetFramework targetFramework = DefaultTargetFramework)
    {
        Compilation compilation;
        CSharpParseOptions? csParseOptions = null;
        VisualBasic.VisualBasicParseOptions? vbParseOptions = null;

        var references = TargetFrameworkUtil.GetReferences(targetFramework);
        assemblyName ??= "TestAssembly";

        if (language == LanguageNames.CSharp)
        {
            csParseOptions = TestOptions.RegularPreview.WithNoRefSafetyRulesAttribute();
            var trees = sources.Select(source => SyntaxFactory.ParseSyntaxTree(source.text, csParseOptions, source.filePath));
            compilation = CSharpCompilation.Create(assemblyName, trees, references, TestOptions.DebugDll);
        }
        else
        {
            vbParseOptions = VisualBasic.UnitTests.TestOptions.Regular;
            var trees = sources.Select(source => VisualBasic.SyntaxFactory.ParseSyntaxTree(source.text, vbParseOptions, source.filePath));
            compilation = VisualBasic.VisualBasicCompilation.Create(assemblyName, trees, references, VisualBasic.UnitTests.TestOptions.DebugDll);
        }

        if (generatorProject != null)
        {
            var generators = generatorProject.AnalyzerReferences.SelectMany(r => r.GetGenerators(language: generatorProject.Language));
            var driverOptions = new GeneratorDriverOptions(baseDirectory: generatorProject.CompilationOutputInfo.GetEffectiveGeneratedFilesOutputDirectory());

            var optionsProvider = (analyzerOptions != null) ? new EditAndContinueTestAnalyzerConfigOptionsProvider(analyzerOptions) : null;
            var additionalTexts = (additionalFileText != null) ? new[] { new InMemoryAdditionalText("additional_file", additionalFileText) } : null;

            GeneratorDriver generatorDriver;
            if (language == LanguageNames.CSharp)
            {
                generatorDriver = CSharpGeneratorDriver.Create(
                    generators,
                    additionalTexts,
                    csParseOptions,
                    optionsProvider,
                    driverOptions);
            }
            else
            {
                generatorDriver = VisualBasic.VisualBasicGeneratorDriver.Create(
                    [.. generators],
                    additionalTexts.ToImmutableArrayOrEmpty<AdditionalText>(),
                    vbParseOptions,
                    optionsProvider,
                    driverOptions);
            }

            generatorDriver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);
            generatorDiagnostics.Verify();
            compilation = outputCompilation;
        }

        return EmitLibrary(projectId, compilation, pdbFormat);
    }

    internal Guid EmitLibrary(ProjectId projectId, Compilation compilation, DebugInformationFormat pdbFormat = DebugInformationFormat.PortablePdb)
    {
        var (peImage, pdbImage) = compilation.EmitToArrays(new EmitOptions(debugInformationFormat: pdbFormat));

        var dir = Temp.CreateDirectory();
        dir.CreateFile(compilation.AssemblyName + ".dll").WriteAllBytes(peImage.ToArray());
        dir.CreateFile(compilation.AssemblyName + ".pdb").WriteAllBytes(pdbImage.ToArray());

        var symReader = SymReaderTestHelpers.OpenDummySymReader(pdbImage);

        var moduleMetadata = ModuleMetadata.CreateFromImage(peImage);
        var moduleId = moduleMetadata.GetModuleVersionId();

        // Associate the binaries with the project.
        // Note that in some scenarios the projectId may have already been associated with a moduleId,
        // and the assembly file was overwritten with a new one.

        _mockCompilationOutputs[projectId] = new MockCompilationOutputs(moduleId)
        {
            OpenAssemblyStreamImpl = () =>
            {
                var stream = new MemoryStream();
                ImmutableInterlocked.Update(ref _disposalVerifiedStreams, s => s.Add(stream));
                peImage.WriteToStream(stream);
                stream.Position = 0;
                return stream;
            },
            OpenPdbStreamImpl = () =>
            {
                var stream = new MemoryStream();
                ImmutableInterlocked.Update(ref _disposalVerifiedStreams, s => s.Add(stream));
                pdbImage.WriteToStream(stream);
                stream.Position = 0;
                return stream;
            }
        };

        return moduleId;
    }

    internal static SourceText CreateText(string source)
        => SourceText.From(source, Encoding.UTF8, SourceHashAlgorithms.Default);

    internal static SourceText CreateTextFromFile(string path)
    {
        using var stream = File.OpenRead(path);
        return SourceText.From(stream, Encoding.UTF8, SourceHashAlgorithms.Default);
    }

    internal static TextSpan GetSpan(string str, string substr)
        => new(str.IndexOf(substr), substr.Length);

    internal static void VerifyReadersDisposed(IEnumerable<IDisposable> readers)
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

    internal static DocumentInfo CreateDesignTimeOnlyDocument(ProjectId projectId, string name = "design-time-only.cs", string path = "design-time-only.cs")
    {
        var sourceText = CreateText("class DTO {}");
        return DocumentInfo.Create(
            DocumentId.CreateNewId(projectId, name),
            name: name,
            folders: [],
            sourceCodeKind: SourceCodeKind.Regular,
            loader: TextLoader.From(TextAndVersion.Create(sourceText, VersionStamp.Create(), path)),
            filePath: path,
            isGenerated: false)
            .WithDesignTimeOnly(true);
    }

    internal sealed class FailingTextLoader : TextLoader
    {
        public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
        {
            Assert.True(false, $"Content of document should never be loaded");
            throw ExceptionUtilities.Unreachable();
        }
    }

    internal static EditAndContinueLogEntry Row(int rowNumber, TableIndex table, EditAndContinueOperation operation)
        => new(MetadataTokens.Handle(table, rowNumber), operation);

    internal static unsafe void VerifyEncLogMetadata(ImmutableArray<byte> delta, params EditAndContinueLogEntry[] expectedRows)
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

    internal static void GenerateSource(GeneratorExecutionContext context)
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

    internal static string? GetGeneratedCodeFromMarkedSource(string markedSource)
    {
        const string OpeningMarker = "/* GENERATE:";
        const string ClosingMarker = "*/";

        var index = markedSource.IndexOf(OpeningMarker);
        if (index >= 0)
        {
            index += OpeningMarker.Length;
            var closing = markedSource.IndexOf(ClosingMarker, index);
            return markedSource[index..closing].Trim();
        }

        return null;
    }
}
