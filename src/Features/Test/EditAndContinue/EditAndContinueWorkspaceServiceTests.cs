// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

using static ActiveStatementTestHelpers;

[UseExportProvider]
public sealed class EditAndContinueWorkspaceServiceTests : EditAndContinueWorkspaceTestBase
{
    [Fact]
    public async Task StartDebuggingSession_CapturingDocuments()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var encodingA = Encoding.BigEndianUnicode;
        var encodingB = Encoding.Unicode;
        var encodingC = Encoding.GetEncoding("SJIS");
        var encodingE = Encoding.UTF8;

        var sourceA1 = "class A {}";
        var sourceB1 = "class B { int F() => 1; }";
        var sourceC1 = "class C { const char L = 'ワ'; }";
        var sourceD1 = "dummy code";
        var sourceE1 = "class E { }";
        var sourceBytesA1 = encodingA.GetBytesWithPreamble(sourceA1);
        var sourceBytesB1 = encodingB.GetBytesWithPreamble(sourceB1);
        var sourceBytesC1 = encodingC.GetBytesWithPreamble(sourceC1);
        var sourceBytesD1 = Encoding.UTF8.GetBytesWithPreamble(sourceD1);
        var sourceBytesE1 = encodingE.GetBytesWithPreamble(sourceE1);

        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("A.cs").WriteAllBytes(sourceBytesA1);
        var sourceFileB = dir.CreateFile("B.cs").WriteAllBytes(sourceBytesB1);
        var sourceFileC = dir.CreateFile("C.cs").WriteAllBytes(sourceBytesC1);
        var sourceFileD = dir.CreateFile("dummy").WriteAllBytes(sourceBytesD1);
        var sourceFileE = dir.CreateFile("E.cs").WriteAllBytes(sourceBytesE1);
        var sourceTreeA1 = SyntaxFactory.ParseSyntaxTree(SourceText.From(sourceBytesA1, sourceBytesA1.Length, encodingA, SourceHashAlgorithms.Default), TestOptions.Regular, sourceFileA.Path);
        var sourceTreeB1 = SyntaxFactory.ParseSyntaxTree(SourceText.From(sourceBytesB1, sourceBytesB1.Length, encodingB, SourceHashAlgorithms.Default), TestOptions.Regular, sourceFileB.Path);
        var sourceTreeC1 = SyntaxFactory.ParseSyntaxTree(SourceText.From(sourceBytesC1, sourceBytesC1.Length, encodingC, SourceHashAlgorithm.Sha1), TestOptions.Regular, sourceFileC.Path);

        var projectPId = ProjectId.CreateNewId("P");

        // E is not included in the compilation:
        var compilation = CSharpTestBase.CreateCompilation([sourceTreeA1, sourceTreeB1, sourceTreeC1], options: TestOptions.DebugDll, targetFramework: DefaultTargetFramework, assemblyName: "P");
        EmitLibrary(projectPId, compilation);

        // change content of B on disk:
        sourceFileB.WriteAllText("class B { int F() => 2; }", encodingB);

        // prepare workspace as if it was loaded from project files:
        using var _ = CreateWorkspace(out var solution, out var service, [typeof(NoCompilationLanguageService)]);

        solution = solution
            .AddTestProject("P", LanguageNames.CSharp, id: projectPId).Solution
            .WithProjectChecksumAlgorithm(projectPId, SourceHashAlgorithm.Sha1);

        var documentIdA = DocumentId.CreateNewId(projectPId, debugName: "A");
        solution = solution.AddDocument(DocumentInfo.Create(
            id: documentIdA,
            name: "A",
            loader: new WorkspaceFileTextLoader(solution.Services, sourceFileA.Path, encodingA),
            filePath: sourceFileA.Path));

        var documentIdB = DocumentId.CreateNewId(projectPId, debugName: "B");
        solution = solution.AddDocument(DocumentInfo.Create(
            id: documentIdB,
            name: "B",
            loader: new WorkspaceFileTextLoader(solution.Services, sourceFileB.Path, encodingB),
            filePath: sourceFileB.Path));

        var documentIdC = DocumentId.CreateNewId(projectPId, debugName: "C");
        solution = solution.AddDocument(DocumentInfo.Create(
            id: documentIdC,
            name: "C",
            loader: new WorkspaceFileTextLoader(solution.Services, sourceFileC.Path, encodingC),
            filePath: sourceFileC.Path));

        var documentIdE = DocumentId.CreateNewId(projectPId, debugName: "E");
        solution = solution.AddDocument(DocumentInfo.Create(
            id: documentIdE,
            name: "E",
            loader: new WorkspaceFileTextLoader(solution.Services, sourceFileE.Path, encodingE),
            filePath: sourceFileE.Path));

        // check that we are testing documents whose hash algorithm does not match the PDB (but the hash itself does):
        Assert.Equal(SourceHashAlgorithm.Sha1, solution.GetDocument(documentIdA).GetTextSynchronously(default).ChecksumAlgorithm);
        Assert.Equal(SourceHashAlgorithm.Sha1, solution.GetDocument(documentIdB).GetTextSynchronously(default).ChecksumAlgorithm);
        Assert.Equal(SourceHashAlgorithm.Sha1, solution.GetDocument(documentIdC).GetTextSynchronously(default).ChecksumAlgorithm);
        Assert.Equal(SourceHashAlgorithm.Sha1, solution.GetDocument(documentIdE).GetTextSynchronously(default).ChecksumAlgorithm);

        // design-time-only document with and without absolute path:
        solution = solution.
            AddDocument(CreateDesignTimeOnlyDocument(projectPId, name: "dt1.cs", path: Path.Combine(dir.Path, "dt1.cs"))).
            AddDocument(CreateDesignTimeOnlyDocument(projectPId, name: "dt2.cs", path: "dt2.cs"));

        // project that does not support EnC - the contents of documents in this project shouldn't be loaded:
        var projectQ = solution.AddTestProject("Q", NoCompilationConstants.LanguageName);
        solution = projectQ.Solution;

        solution = solution.AddDocument(DocumentInfo.Create(
            id: DocumentId.CreateNewId(projectQ.Id, debugName: "D"),
            name: "D",
            loader: new FailingTextLoader(),
            filePath: sourceFileD.Path));

        var sessionId = service.StartDebuggingSession(solution, _debuggerService, NullPdbMatchingSourceTextProvider.Instance, reportDiagnostics: true);
        var debuggingSession = service.GetTestAccessor().GetDebuggingSession(sessionId);

        Assert.Empty(debuggingSession.LastCommittedSolution.Test_GetDocumentStates());

        // change content of B on disk again:
        sourceFileB.WriteAllText("class B { int F() => 3; }", encodingB);
        solution = solution.WithDocumentTextLoader(documentIdB, new WorkspaceFileTextLoader(solution.Services, sourceFileB.Path, encodingB), PreservationMode.PreserveValue);

        EnterBreakState(debuggingSession);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        // TODO: warning reported https://github.com/dotnet/roslyn/issues/78125
        // AssertEx.Equal([$"P.csproj: (0,0)-(0,0): Warning ENC1005: {string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFileB.Path)}"], InspectDiagnostics(results.Diagnostics));

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ProjectNotBuilt()
    {
        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

        var debuggingSession = StartDebuggingSession(service, solution);

        // no changes:
        var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics);

        // change the source:
        solution = solution.WithDocumentText(document1.Id, CreateText("class C1 { void M() { System.Console.WriteLine(2); } }"));
        var document2 = solution.GetDocument(document1.Id);

        diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics);

        // changes in the project are ignored:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.Equal(
        [
            $"proj: {document1.FilePath}: (0,0)-(0,0): Warning ENC1008: {string.Format(FeaturesResources.Changing_source_file_0_in_a_stale_project_has_no_effect_until_the_project_is_rebuit, document1.FilePath)}"
        ], InspectDiagnostics(results.Diagnostics));

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task DifferentDocumentWithSameContent()
    {
        var source = "class C1 { void M1() { System.Console.WriteLine(1); } }";
        var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, source);

        solution = solution.WithProjectOutputFilePath(document.Project.Id, moduleFile.Path);
        _mockCompilationOutputs.Add(document.Project.Id, new CompilationOutputFiles(moduleFile.Path));

        var debuggingSession = StartDebuggingSession(service, solution);

        // update the document
        var document1 = solution.GetDocument(document.Id);
        solution = solution.WithDocumentText(document.Id, CreateText(source));
        var document2 = solution.GetDocument(document.Id);

        Assert.Equal(document1.Id, document2.Id);
        Assert.NotSame(document1, document2);

        var diagnostics2 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics2);

        // validate solution update status and emit - changes made during run mode are ignored:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);

        EndDebuggingSession(debuggingSession);

        AssertEx.Equal(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1"
        ], _telemetryLog);
    }

    [Theory, CombinatorialData]
    public async Task ProjectThatDoesNotSupportEnC_Language(bool breakMode)
    {
        using var _ = CreateWorkspace(out var solution, out var service, [typeof(NoCompilationLanguageService)]);
        var project = solution.AddProject("dummy_proj", "dummy_proj", NoCompilationConstants.LanguageName);
        var document = project.AddDocument("test", "dummy1");
        solution = document.Project.Solution;

        var debuggingSession = StartDebuggingSession(service, solution);
        if (breakMode)
        {
            EnterBreakState(debuggingSession);
        }

        // no changes:
        var document1 = solution.Projects.Single().Documents.Single();
        var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics);

        // change the source:
        solution = solution.WithDocumentText(document1.Id, CreateText("dummy2"));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        var document2 = solution.GetDocument(document1.Id);
        diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ProjectThatDoesNotSupportEnC_Settings_OptimizationLevel()
    {
        using var _ = CreateWorkspace(out var solution, out var service);

        var source1 = """
            class C
            {
            }
            """;
        var sourceFile = Temp.CreateFile().WriteAllText(source1, Encoding.UTF8);

        solution = solution.
            AddTestProject("test", out var projectId).
                WithCompilationOptions<CSharpCompilationOptions>(options => options.WithOptimizationLevel(OptimizationLevel.Release)).
            AddTestDocument(source1, sourceFile.Path, out var documentId).Project.Solution;

        var project = solution.GetRequiredProject(projectId);
        EmitAndLoadLibraryToDebuggee(project);

        // Debugger reports error when trying to emit change for optimized module. We do not report another rude edit.
        _debuggerService.IsEditAndContinueAvailable = _ => new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.Optimized, localizedMessage: "*optimized*");

        var debuggingSession = StartDebuggingSession(service, solution);

        // change the source:
        solution = solution.WithDocumentText(documentId, CreateText("""
            class C
            {
                public void F() {}
            }
            """));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        AssertEx.Equal(
        [
            $"test: {sourceFile.Path}: (2,0)-(2,22): Error ENC2012: {string.Format(FeaturesResources.EditAndContinueDisallowedByProject, ["test", "*optimized*"])}"
        ], InspectDiagnostics(results.Diagnostics));
    }

    [Fact]
    public async Task ProjectWithoutEffectiveGeneratedFilesOutputDirectory()
    {
        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, """
            /* GENERATE: class G { int F(int x) => 1; } */
            
            class C { int Y => 1; }
            """, generator: generator);
        solution = solution.WithProjectCompilationOutputInfo(document.Project.Id, new CompilationOutputInfo(assemblyPath: null, generatedFilesOutputDirectory: null));

        var debuggingSession = StartDebuggingSession(service, solution);
        EnterBreakState(debuggingSession);

        // change the source:
        var document1 = solution.Projects.Single().Documents.Single();
        solution = solution.WithDocumentText(document1.Id, CreateText("""
            /* GENERATE: class G { int F() => 1; } */

            class C { int Y => 2; }
            """));

        var generatedDocument = (await solution.Projects.Single().GetSourceGeneratedDocumentsAsync()).Single();

        var diagnostics1 = await service.GetDocumentDiagnosticsAsync(generatedDocument, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics1);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        EndDebuggingSession(debuggingSession);
    }

    [Theory]
    [CombinatorialData]
    internal async Task Project_ParseOptions(
        [CombinatorialValues(ProjectSettingKind.LangVersion, ProjectSettingKind.Features, ProjectSettingKind.DefineConstants)] ProjectSettingKind settingKind,
        [CombinatorialValues(LanguageNames.CSharp, LanguageNames.VisualBasic)] string language)
    {
        var source = language == LanguageNames.CSharp
            ? """
              class C;
              """
            : """
              Class C
              End Class
              """;

        var sourceFile1 = Temp.CreateFile().WriteAllText(source, Encoding.UTF8);

        using var w = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", language, out var projectId).
            AddTestDocument(source, sourceFile1.Path, out var documentId).Project.Solution;

        var project = solution.GetRequiredProject(projectId);

        EmitAndLoadLibraryToDebuggee(project);

        var debuggingSession = StartDebuggingSession(service, solution);

        var (code, settingName) = settingKind switch
        {
            ProjectSettingKind.LangVersion => ("ENC1100", "LangVersion"),
            ProjectSettingKind.Features => ("ENC1101", "Features"),
            ProjectSettingKind.DefineConstants => ("ENC1102", "DefineConstants"),
            _ => throw ExceptionUtilities.UnexpectedValue(settingKind)
        };

        ParseOptions newOptions;
        string oldValue;
        string newValue;

        if (language == LanguageNames.CSharp)
        {
            var oldOptions = (CSharpParseOptions)project.ParseOptions;

            (newOptions, oldValue, newValue) = settingKind switch
            {
                ProjectSettingKind.LangVersion => (oldOptions.WithLanguageVersion(CSharp.LanguageVersion.CSharp11), "default", "11.0"),
                ProjectSettingKind.Features => (oldOptions.WithFeatures([new("f1", "1"), new("f2", "2")]), "noRefSafetyRulesAttribute=true", "f1=1,f2=2"),
                ProjectSettingKind.DefineConstants => (oldOptions.WithPreprocessorSymbols("S1", "S2"), "", "S1,S2"),
                _ => throw ExceptionUtilities.UnexpectedValue(settingKind)
            };
        }
        else
        {
            var oldOptions = (VisualBasic.VisualBasicParseOptions)project.ParseOptions;

            (newOptions, oldValue, newValue) = settingKind switch
            {
                ProjectSettingKind.LangVersion => (oldOptions.WithLanguageVersion(VisualBasic.LanguageVersion.VisualBasic11), "default", "11"),
                ProjectSettingKind.Features => (oldOptions.WithFeatures([new("f1", "1"), new("f2", "2")]), "", "f1=1,f2=2"),
                ProjectSettingKind.DefineConstants => (oldOptions.WithPreprocessorSymbols(new("S1", 1), new("S2", 2)), "_MYTYPE=Empty", "S1=1,S2=2"),
                _ => throw ExceptionUtilities.UnexpectedValue(settingKind)
            };
        }

        solution = project.WithParseOptions(newOptions).Solution;

        // Rude edits not reported for document, will be reported when emitting updates:
        var diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetRequiredDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        AssertEx.Equal(
        [
            $"test: <no location>: Error {code}: {string.Format(FeaturesResources.Changing_project_setting_0_from_1_to_2_requires_restarting_the_application, settingName, oldValue, newValue)}"
        ], InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();

        EndDebuggingSession(debuggingSession);
    }

    [Theory]
    [CombinatorialData]
    internal async Task Project_CompilationOptions(
        [CombinatorialValues(
            ProjectSettingKind.CheckForOverflowUnderflow,
            ProjectSettingKind.OutputType,
            ProjectSettingKind.StartupObject,
            ProjectSettingKind.ModuleAssemblyName,
            ProjectSettingKind.Platform,
            ProjectSettingKind.OptimizationLevel
        )] ProjectSettingKind settingKind,
        [CombinatorialValues(LanguageNames.CSharp, LanguageNames.VisualBasic)] string language)
    {
        var source = language == LanguageNames.CSharp
            ? """
              class C;
              """
            : """
              Class C
              End Class
              """;

        var sourceFile1 = Temp.CreateFile().WriteAllText(source, Encoding.UTF8);

        using var w = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", language, out var projectId).
            AddTestDocument(source, sourceFile1.Path, out var documentId).Project.Solution;

        var project = solution.GetRequiredProject(projectId);

        EmitAndLoadLibraryToDebuggee(project);

        var debuggingSession = StartDebuggingSession(service, solution);

        var (code, settingName, isWarning) = settingKind switch
        {
            ProjectSettingKind.CheckForOverflowUnderflow => ("ENC1103", "CheckForOverflowUnderflow", isWarning: false),
            ProjectSettingKind.OutputType => ("ENC1104", "OutputType", isWarning: true),
            ProjectSettingKind.StartupObject => ("ENC1105", "StartupObject", isWarning: true),
            ProjectSettingKind.ModuleAssemblyName => ("ENC1109", "ModuleAssemblyName", isWarning: false),
            ProjectSettingKind.Platform => ("ENC1111", "Platform", isWarning: true),
            ProjectSettingKind.OptimizationLevel => ("ENC1112", "OptimizationLevel", isWarning: false),
            _ => throw ExceptionUtilities.UnexpectedValue(settingKind)
        };

        CompilationOptions newOptions;
        string oldValue;
        string newValue;

        var oldOptions = project.CompilationOptions;
        var defaultOverflowChecks = oldOptions.CheckOverflow;

        (newOptions, oldValue, newValue) = settingKind switch
        {
            ProjectSettingKind.CheckForOverflowUnderflow => (oldOptions.WithOverflowChecks(!defaultOverflowChecks), defaultOverflowChecks.ToString(), (!defaultOverflowChecks).ToString()),
            ProjectSettingKind.OutputType => (oldOptions.WithOutputKind(OutputKind.WindowsRuntimeApplication), "Library", "AppContainerExe"),
            ProjectSettingKind.StartupObject => (oldOptions.WithMainTypeName("NewProgram"), $"<{FeaturesResources.@default}>", "NewProgram"),
            ProjectSettingKind.ModuleAssemblyName => (oldOptions.WithModuleName("mod"), $"<{FeaturesResources.@default}>", "mod"),
            ProjectSettingKind.Platform => (oldOptions.WithPlatform(Platform.Arm64), "AnyCpu", "Arm64"),
            ProjectSettingKind.OptimizationLevel => (oldOptions.WithOptimizationLevel(OptimizationLevel.Release), "Debug", "Release"),
            _ => throw ExceptionUtilities.UnexpectedValue(settingKind)
        };

        solution = project.WithCompilationOptions(newOptions).Solution;

        // Rude edits not reported for document, will be reported when emitting updates:
        var diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetRequiredDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(isWarning ? ModuleUpdateStatus.None : ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        AssertEx.Equal(
        [
            $"test: <no location>: {(isWarning ? "Warning" : "Error")} {code}: {string.Format(FeaturesResources.Changing_project_setting_0_from_1_to_2_requires_restarting_the_application, settingName, oldValue, newValue)}"
        ], InspectDiagnostics(results.Diagnostics));

        if (!isWarning)
        {
            debuggingSession.DiscardSolutionUpdate();
        }

        EndDebuggingSession(debuggingSession);
    }

    [Theory]
    [InlineData(ProjectSettingKind.RootNamespace)]
    [InlineData(ProjectSettingKind.OptionStrict)]
    [InlineData(ProjectSettingKind.OptionInfer)]
    [InlineData(ProjectSettingKind.OptionExplicit)]
    [InlineData(ProjectSettingKind.OptionCompare)]
    internal async Task Project_CompilationOptions_VB(ProjectSettingKind settingKind)
    {
        var source = """
            Class C
            End Class
            """;

        var sourceFile1 = Temp.CreateFile().WriteAllText(source, Encoding.UTF8);

        using var w = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", LanguageNames.VisualBasic, out var projectId).
            AddTestDocument(source, sourceFile1.Path, out var documentId).Project.Solution;

        var project = solution.GetRequiredProject(projectId);

        EmitAndLoadLibraryToDebuggee(project);

        var debuggingSession = StartDebuggingSession(service, solution);

        var (code, settingName) = settingKind switch
        {
            ProjectSettingKind.RootNamespace => ("ENC1150", "RootNamespace"),
            ProjectSettingKind.OptionStrict => ("ENC1151", "OptionStrict"),
            ProjectSettingKind.OptionInfer => ("ENC1152", "OptionInfer"),
            ProjectSettingKind.OptionExplicit => ("ENC1153", "OptionExplicit"),
            ProjectSettingKind.OptionCompare => ("ENC1154", "OptionCompare"),
            _ => throw ExceptionUtilities.UnexpectedValue(settingKind)
        };

        CompilationOptions newOptions;
        string oldValue;
        string newValue;

        var oldOptions = (VisualBasic.VisualBasicCompilationOptions)project.CompilationOptions;

        (newOptions, oldValue, newValue) = settingKind switch
        {
            ProjectSettingKind.RootNamespace => (oldOptions.WithRootNamespace("N"), "", "N"),
            ProjectSettingKind.OptionStrict => (oldOptions.WithOptionStrict(VisualBasic.OptionStrict.On), "Off", "On"),
            ProjectSettingKind.OptionInfer => (oldOptions.WithOptionInfer(false), "On", "Off"),
            ProjectSettingKind.OptionExplicit => (oldOptions.WithOptionExplicit(false), "On", "Off"),
            ProjectSettingKind.OptionCompare => (oldOptions.WithOptionCompareText(true), "Binary", "Text"),
            _ => throw ExceptionUtilities.UnexpectedValue(settingKind)
        };

        solution = project.WithCompilationOptions(newOptions).Solution;

        // Rude edits not reported for document, will be reported when emitting updates:
        var diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetRequiredDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        AssertEx.Equal(
        [
            $"test: <no location>: Error {code}: {string.Format(FeaturesResources.Changing_project_setting_0_from_1_to_2_requires_restarting_the_application, settingName, oldValue, newValue)}"
        ], InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ProjectWithoutChanges()
    {
        using var _ = CreateWorkspace(out var solution, out var service);

        var source = """
            class A
            {
            }
            """;

        var sourceFile = Temp.CreateFile().WriteAllText(source, Encoding.UTF8);

        solution = solution.
            AddTestProject("A", out var projectId).
            AddTestDocument(source, sourceFile.Path).Project.Solution;

        var project = solution.GetRequiredProject(projectId);
        EmitAndLoadLibraryToDebuggee(project);

        var debuggingSession = StartDebuggingSession(service, solution);

        var newRefOutPath = Path.Combine(TempRoot.Root, "newRef");
        solution = solution.WithProjectOutputRefFilePath(projectId, newRefOutPath);

        // No changes pertinent to EnC were made to the project, we should not read its outputs:
        var mockOutputs = (MockCompilationOutputs)_mockCompilationOutputs[projectId];

        mockOutputs.OpenAssemblyStreamImpl = () =>
        {
            Assert.Fail("Should not open assembly");
            return null;
        };

        mockOutputs.OpenPdbStreamImpl = () =>
        {
            Assert.Fail("Should not open PDB");
            return null;
        };

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);
    }

    private static MetadataReference EmitLibraryReference(Version version, bool useStrongName = false)
    {
        var libSource = $$"""
            [assembly: System.Reflection.AssemblyVersion("{{version}}")]
            public class Lib;
            """;

        var libCompilation = CSharpCompilation.Create(
            assemblyName: "Lib",
            syntaxTrees: [SyntaxFactory.ParseSyntaxTree(libSource, options: TestOptions.Regular, path: "Lib.cs", Encoding.UTF8)],
            references: TargetFrameworkUtil.GetReferences(TargetFramework.NetStandard20),
            useStrongName ? TestOptions.SigningDebugDll.WithCryptoKeyFile(SigningTestHelpers.KeyPairFile) : TestOptions.DebugDll);

        return libCompilation.EmitToImageReference();
    }

    [Fact]
    internal async Task Project_MetadataReferences()
    {
        var source = "class C;";
        var sourceFile = Temp.CreateFile().WriteAllText(source, Encoding.UTF8);

        var libV1 = EmitLibraryReference(new Version(1, 0, 0, 0));
        var libV2 = EmitLibraryReference(new Version(2, 0, 0, 0));

        using var w = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", out var projectId).
                AddMetadataReference(libV1).
            AddTestDocument(source, sourceFile.Path, out var documentId).Project.Solution;

        var project = solution.GetRequiredProject(projectId);

        EmitAndLoadLibraryToDebuggee(project);

        var debuggingSession = StartDebuggingSession(service, solution);

        // change version of the dependency:
        solution = project.RemoveMetadataReference(libV1).AddMetadataReference(libV2).Solution;

        // Rude edits not reported for document, will be reported when emitting updates:
        var diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetRequiredDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        AssertEx.Equal(
        [
            $"test: <no location>: Error ENC1099: {string.Format(
                FeaturesResources.Changing_project_or_package_reference_caused_the_identity_of_referenced_assembly_to_change_from_0_to_1_which_requires_restarting_the_application,
                "Lib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "Lib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null")}"
        ], InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    internal async Task Project_MetadataReferences_RemoveAdd()
    {
        var source = "class C;";
        var sourceFile = Temp.CreateFile().WriteAllText(source, Encoding.UTF8);

        var libV1 = EmitLibraryReference(new Version(1, 0, 0, 0));
        var libV2 = EmitLibraryReference(new Version(2, 0, 0, 0));

        using var w = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", out var projectId).
                AddMetadataReference(libV1).
            AddTestDocument(source, sourceFile.Path, out var documentId).Project.Solution;

        var project = solution.GetRequiredProject(projectId);

        EmitAndLoadLibraryToDebuggee(project);

        var debuggingSession = StartDebuggingSession(service, solution);

        // remove dependency:
        solution = project.RemoveMetadataReference(libV1).Solution;

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        // add newer version:
        solution = project.AddMetadataReference(libV2).Solution;

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        AssertEx.Equal(
        [
            $"test: <no location>: Error ENC1099: {string.Format(
                FeaturesResources.Changing_project_or_package_reference_caused_the_identity_of_referenced_assembly_to_change_from_0_to_1_which_requires_restarting_the_application,
                "Lib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
                "Lib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=null")}"
        ], InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    internal async Task Project_MetadataReferences_Add()
    {
        var source = "class C;";
        var sourceFile = Temp.CreateFile().WriteAllText(source, Encoding.UTF8);

        var libV1 = EmitLibraryReference(new Version(1, 0, 0, 0));

        using var w = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", out var projectId).
            AddTestDocument(source, sourceFile.Path, out var documentId).Project.Solution;

        var project = solution.GetRequiredProject(projectId);

        EmitAndLoadLibraryToDebuggee(project);

        var debuggingSession = StartDebuggingSession(service, solution);

        // add dependency:
        solution = project.AddMetadataReference(libV1).Solution;

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    internal async Task Project_MetadataReferences_MultipleVersions()
    {
        var source = "class C;";
        var sourceFile = Temp.CreateFile().WriteAllText(source, Encoding.UTF8);

        var libV1 = EmitLibraryReference(new Version(1, 0, 0, 0), useStrongName: true);
        var libV2 = EmitLibraryReference(new Version(2, 0, 0, 0), useStrongName: true);
        var libV3 = EmitLibraryReference(new Version(3, 0, 0, 0), useStrongName: true);

        using var w = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", out var projectId).
                AddMetadataReference(libV1).
                AddMetadataReference(libV2).
            AddTestDocument(source, sourceFile.Path, out var documentId).Project.Solution;

        var project = solution.GetRequiredProject(projectId);

        EmitAndLoadLibraryToDebuggee(project);

        var debuggingSession = StartDebuggingSession(service, solution);

        // add version 3:
        solution = project.AddMetadataReference(libV3).Solution;

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        AssertEx.Equal(
        [
            $"test: <no location>: Error ENC1098: {string.Format(
                FeaturesResources.Project_references_mutliple_assemblies_of_the_same_simple_name_0_1_Changing_a_reference_to_such_an_assembly_requires_restarting_the_application,
                "Lib",
                "'Lib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2', 'Lib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2'")}"
        ], InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task DesignTimeOnlyDocument()
    {
        var moduleFile = Temp.CreateFile().WriteAllBytes(TestResources.Basic.Members);

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

        var projectId = document1.Project.Id;
        var documentInfo = CreateDesignTimeOnlyDocument(projectId);
        solution = solution.WithProjectOutputFilePath(projectId, moduleFile.Path).AddDocument(documentInfo);

        _mockCompilationOutputs.Add(projectId, new CompilationOutputFiles(moduleFile.Path));

        var debuggingSession = StartDebuggingSession(service, solution);

        // update a design-time-only source file:
        solution = solution.WithDocumentText(documentInfo.Id, CreateText("class UpdatedC2 {}"));
        var document2 = solution.GetDocument(documentInfo.Id);

        // no updates:
        var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics);

        // validate solution update status and emit - changes made in design-time-only documents are ignored:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);

        EndDebuggingSession(debuggingSession);

        AssertEx.Equal(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1"
        ], _telemetryLog);
    }

    [Fact]
    public async Task DesignTimeOnlyDocument_Dynamic()
    {
        using var _ = CreateWorkspace(out var solution, out var service);

        (solution, var document) = AddDefaultTestProject(solution, "class C {}");

        var sourceText = CreateText("class D {}");
        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(document.Project.Id),
            name: "design-time-only.cs",
            loader: TextLoader.From(TextAndVersion.Create(sourceText, VersionStamp.Create(), "design-time-only.cs")),
            filePath: "design-time-only.cs",
            isGenerated: false)
            .WithDesignTimeOnly(true);

        solution = solution.AddDocument(documentInfo);

        var debuggingSession = StartDebuggingSession(service, solution);
        EnterBreakState(debuggingSession);

        // change the source:
        var document1 = solution.GetDocument(documentInfo.Id);
        solution = solution.WithDocumentText(document1.Id, CreateText("class E {}"));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);
    }

    [Theory, CombinatorialData]
    public async Task DesignTimeOnlyDocument_Wpf(
        [CombinatorialValues(LanguageNames.CSharp, LanguageNames.VisualBasic)] string language,
        bool delayLoad,
        bool designTimeOnlyAddedAfterSessionStarts)
    {
        var source = "class A { }";
        var sourceDesignTimeOnly = (language == LanguageNames.CSharp) ? "class B { }" : "Class C : End Class";
        var sourceDesignTimeOnly2 = (language == LanguageNames.CSharp) ? "class B2 { }" : "Class C2 : End Class";

        var dir = Temp.CreateDirectory();

        var extension = (language == LanguageNames.CSharp) ? ".cs" : ".vb";

        var sourceFileName = "a" + extension;
        var sourceFilePath = dir.CreateFile(sourceFileName).WriteAllText(source, Encoding.UTF8).Path;

        var designTimeOnlyFileName = "b.g.i" + extension;
        var designTimeOnlyFilePath = Path.Combine(dir.Path, designTimeOnlyFileName);

        using var _ = CreateWorkspace(out var solution, out var service);

        // The workspace starts with
        // [added == false] a version of the source that's not updated with the output of single file generator (or design-time build):
        // [added == true] without the output of single file generator (design-time build has not completed)

        solution = solution.
            AddTestProject("test", language, out var projectId).
            AddTestDocument(source, path: sourceFilePath, out var documentId).Project.Solution;

        var designTimeOnlyDocumentId = DocumentId.CreateNewId(projectId);

        if (!designTimeOnlyAddedAfterSessionStarts)
        {
            solution = solution.AddDocument(designTimeOnlyDocumentId, designTimeOnlyFileName, SourceText.From(sourceDesignTimeOnly, Encoding.UTF8), filePath: designTimeOnlyFilePath);
        }

        // only compile actual source document, not design-time-only document:
        var moduleId = EmitLibrary(projectId, source, sourceFilePath: sourceFilePath);

        if (!delayLoad)
        {
            LoadLibraryToDebuggee(moduleId);
        }

        // make sure renames are not supported:
        _debuggerService.GetCapabilitiesImpl = () => ["Baseline"];

        var sessionId = service.StartDebuggingSession(solution, _debuggerService, NullPdbMatchingSourceTextProvider.Instance, reportDiagnostics: true);
        var debuggingSession = service.GetTestAccessor().GetDebuggingSession(sessionId);

        if (designTimeOnlyAddedAfterSessionStarts)
        {
            solution = solution.AddDocument(designTimeOnlyDocumentId, designTimeOnlyFileName, SourceText.From(sourceDesignTimeOnly, Encoding.UTF8), filePath: designTimeOnlyFilePath);
        }

        var activeLineSpan = new LinePositionSpan(new(0, 0), new(0, 1));
        var activeStatements = ImmutableArray.Create(
            new ManagedActiveStatementDebugInfo(
                new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 1),
                designTimeOnlyFilePath,
                activeLineSpan.ToSourceSpan(),
                ActiveStatementFlags.NonLeafFrame | ActiveStatementFlags.MethodUpToDate));

        EnterBreakState(debuggingSession, activeStatements);

        // change the source (rude edit):
        solution = solution.WithDocumentText(designTimeOnlyDocumentId, CreateText(sourceDesignTimeOnly2));

        var designTimeOnlyDocument2 = solution.GetDocument(designTimeOnlyDocumentId);

        Assert.False(designTimeOnlyDocument2.State.SupportsEditAndContinue());
        Assert.True(designTimeOnlyDocument2.Project.SupportsEditAndContinue());

        var activeStatementMap = await debuggingSession.EditSession.BaseActiveStatements.GetValueAsync(CancellationToken.None);
        Assert.NotEmpty(activeStatementMap.DocumentPathMap);

        // Active statements in design-time documents should be left unchanged.
        var asSpans = await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [designTimeOnlyDocumentId], CancellationToken.None);
        Assert.Empty(asSpans.Single());

        // no Rude Edits reported:
        Assert.Empty(await service.GetDocumentDiagnosticsAsync(designTimeOnlyDocument2, s_noActiveSpans, CancellationToken.None));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);

        if (delayLoad)
        {
            LoadLibraryToDebuggee(moduleId);

            // validate solution update status and emit:
            results = await EmitSolutionUpdateAsync(debuggingSession, solution);
            Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
            Assert.Empty(results.Diagnostics);
        }

        EndDebuggingSession(debuggingSession);
    }

    [Theory, CombinatorialData]
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

        var source2 = "class C { void M() { System.Console.WriteLine(2); } }";

        using var _w = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, "class C { void M() { System.Console.WriteLine(1); } }");

        var projectId = document1.Project.Id;
        _mockCompilationOutputs.Add(projectId, new CompilationOutputFiles(moduleFile.Path));

        var debuggingSession = StartDebuggingSession(service, solution);

        if (breakMode)
        {
            EnterBreakState(debuggingSession);
        }

        // change the source:
        solution = solution.WithDocumentText(document1.Id, CreateText(source2));
        var document2 = solution.GetDocument(document1.Id);

        // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(docDiagnostics);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.Equal(
            [$"proj: <no location>: Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, moduleFile.Path, expectedErrorMessage)}"],
            InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();

        // correct the error:
        EmitLibrary(projectId, source2);

        var results2 = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results2.ModuleUpdates.Status);
        Assert.Empty(results2.Diagnostics);

        CommitSolutionUpdate(debuggingSession);

        if (breakMode)
        {
            ExitBreakState(debuggingSession);
        }

        EndDebuggingSession(debuggingSession);

        if (breakMode)
        {
            AssertEx.SequenceEqual(
            [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=3",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges={6A6F7270-0000-4000-8000-000000000000}|ProjectIdsWithUpdatedBaselines=",
                "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC1001"
            ], _telemetryLog);
        }
        else
        {
            AssertEx.SequenceEqual(
            [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=1|EmptyHotReloadSessionCount=1",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges={6A6F7270-0000-4000-8000-000000000000}|ProjectIdsWithUpdatedBaselines=",
                "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC1001"
            ], _telemetryLog);
        }
    }

    [Fact]
    public async Task ErrorReadingPdbFile()
    {
        var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        var document1 = AddEmptyTestProject(solution).
            AddDocument("a.cs", CreateText(source1), filePath: sourceFile.Path);

        var project = document1.Project;
        solution = project.Solution;

        var moduleId = EmitAndLoadLibraryToDebuggee(project.Id, source1, sourceFilePath: sourceFile.Path);

        _mockCompilationOutputs[project.Id] = new MockCompilationOutputs(moduleId)
        {
            OpenPdbStreamImpl = () =>
            {
                throw new IOException("Error");
            }
        };

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);
        EnterBreakState(debuggingSession);

        // change the source:
        solution = solution.WithDocumentText(document1.Id, CreateText("class C1 { void M() { System.Console.WriteLine(2); } }"));
        var document2 = solution.GetDocument(document1.Id);

        // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(docDiagnostics);

        // an error occurred so we need to call update to determine whether we have changes to apply or not:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.Equal(
            [$"proj: {document2.FilePath}: (0,0)-(0,0): Error ENC1006: {string.Format(FeaturesResources.UnableToReadSourceFileOrPdb, sourceFile.Path)}"],
            InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);

        AssertEx.Equal(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=1|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1"
        ], _telemetryLog);
    }

    [Fact]
    public async Task ErrorReadingSourceFile()
    {
        var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        var document1 = solution.
            AddTestProject("test").
            AddDocument("a.cs", SourceText.From(source1, Encoding.UTF8, SourceHashAlgorithm.Sha1), filePath: sourceFile.Path);

        var project = document1.Project;
        solution = project.Solution;

        var moduleId = EmitAndLoadLibraryToDebuggee(project.Id, source1, sourceFilePath: sourceFile.Path, checksumAlgorithm: SourceHashAlgorithms.Default);

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);
        EnterBreakState(debuggingSession);

        // change the source:
        solution = solution.WithDocumentText(document1.Id, CreateText("class C1 { void M() { System.Console.WriteLine(2); } }"));
        var document2 = solution.GetDocument(document1.Id);

        using var fileLock = File.Open(sourceFile.Path, FileMode.Open, FileAccess.Read, FileShare.None);

        // error not reported here since it might be intermittent and will be reported if the issue persist when applying the update:
        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(docDiagnostics);

        // an error occurred so we need to call update to determine whether we have changes to apply or not:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.Equal(
            [$"test: {document1.FilePath}: (0,0)-(0,0): Error ENC1006: {string.Format(FeaturesResources.UnableToReadSourceFileOrPdb, sourceFile.Path)}"],
            InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();
        fileLock.Dispose();

        // try apply changes again:
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.NotEmpty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);

        AssertEx.SequenceEqual(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1",
            "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines=",
            "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC1006"
        ], _telemetryLog);
    }

    [Theory, CombinatorialData]
    public async Task Document_Add(bool breakMode)
    {
        var sourceA = "class C1 { void M() { System.Console.WriteLine(1); } }";
        var sourceB = "class C2 {}";

        var sourceFileA = Temp.CreateFile().WriteAllText(sourceA, Encoding.UTF8);
        var sourceFileB = Temp.CreateFile().WriteAllText(sourceB, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        var documentA = solution.
            AddTestProject("test").
            AddDocument("test.cs", CreateText(sourceA), filePath: sourceFileA.Path);

        var project = documentA.Project;
        solution = project.Solution;

        // Source B will be added while debugging.
        EmitAndLoadLibraryToDebuggee(project.Id, sourceA, sourceFilePath: sourceFileA.Path);

        var debuggingSession = StartDebuggingSession(service, solution);

        if (breakMode)
        {
            EnterBreakState(debuggingSession);
        }

        // add a source file:
        var documentB = project.AddTestDocument(sourceB, path: sourceFileB.Path);
        solution = documentB.Project.Solution;
        documentB = solution.GetRequiredDocument(documentB.Id);

        var diagnostics2 = await service.GetDocumentDiagnosticsAsync(documentB, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics2);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        debuggingSession.DiscardSolutionUpdate();

        if (breakMode)
        {
            ExitBreakState(debuggingSession);
        }

        EndDebuggingSession(debuggingSession);

        if (breakMode)
        {
            AssertEx.Equal(
            [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=2",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines="
            ], _telemetryLog);
        }
        else
        {
            AssertEx.Equal(
            [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=1|EmptyHotReloadSessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines="
            ], _telemetryLog);
        }
    }

    [Fact]
    public async Task Document_Delete()
    {
        var sourceA = "partial class C { void F() { System.Console.WriteLine(1); } }";
        var sourceB = "partial class C { void G() { System.Console.WriteLine(2); } }";

        var sourceFileA = Temp.CreateFile().WriteAllText(sourceA, Encoding.UTF8);
        var sourceFileB = Temp.CreateFile().WriteAllText(sourceB, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", out var projectId).
            AddTestDocument(sourceA, sourceFileA.Path).Project.
            AddTestDocument(sourceB, sourceFileA.Path, out var documentBId).Project.Solution;

        EmitAndLoadLibraryToDebuggee(solution.Projects.Single());

        var debuggingSession = StartDebuggingSession(service, solution);

        // delete B:
        solution = solution.RemoveDocument(documentBId);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.NotEmpty(results.ModuleUpdates.Updates);

        debuggingSession.DiscardSolutionUpdate();

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task Document_RenameAndUpdate()
    {
        var source1 = "class C { void F() { System.Console.WriteLine(1); } }";
        var source2 = "class C { void F() { System.Console.WriteLine(2); } }";

        var sourceFile1 = Temp.CreateFile().WriteAllText(source1, Encoding.UTF8);

        using var w = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", out var projectId).
            AddTestDocument(source1, sourceFile1.Path, out var documentBId).Project.Solution;

        EmitAndLoadLibraryToDebuggee(solution.Projects.Single());

        var debuggingSession = StartDebuggingSession(service, solution);

        // delete:
        solution = solution.RemoveDocument(documentBId);

        // add:
        var sourceFile2 = Temp.CreateFile().WriteAllText(source2, Encoding.UTF8);
        solution = solution.AddTestDocument(projectId, source2, sourceFile2.Path, out var document2Id).Project.Solution;

        var diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetRequiredDocument(document2Id), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.NotEmpty(results.ModuleUpdates.Updates);

        debuggingSession.DiscardSolutionUpdate();

        EndDebuggingSession(debuggingSession);
    }

    /// <summary>
    /// <code>
    ///                         F5   build
    ///                              complete
    ///                         │    │
    /// Workspace    ═════0═════╪════╪══════════1═══
    ///                   ▲     │               ▲ src file watcher
    ///                   │     │               │
    /// dll/pdb      ═0═══╪═════╪════1══════════╪═══
    ///                   │     │    ▲          │
    ///               ┌───┘     │    │          │
    ///               │      ┌──┼────┴──────────┘
    /// Source file  ═0══════1══╪═══════════════════
    ///                         │
    /// Committed    ═══════════╪════0══════════1═══
    /// solution
    /// </code>
    /// </summary>
    [Theory, CombinatorialData]
    public async Task ModuleDisallowsEditAndContinue_NoChanges(bool breakMode)
    {
        var source0 = "class C1 { void M() { System.Console.WriteLine(0); } }";
        var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("a.cs");

        using var _ = CreateWorkspace(out var solution, out var service);

        var project = solution.
            AddProject("test", "test", LanguageNames.CSharp).
            AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40));

        solution = project.Solution;

        // compile with source1:
        var moduleId = EmitLibrary(project.Id, source1, sourceFilePath: sourceFile.Path);
        LoadLibraryToDebuggee(moduleId, new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.NotAllowedForRuntime, "*message*"));

        // update the file with source1 before session starts:
        sourceFile.WriteAllText(source1, Encoding.UTF8);

        // source0 is loaded to workspace before session starts:
        var document0 = project.AddTestDocument(source0, path: sourceFile.Path);
        solution = document0.Project.Solution;

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);

        if (breakMode)
        {
            EnterBreakState(debuggingSession);
        }

        // workspace is updated to new version after build completed and the session started:
        solution = solution.WithDocumentText(document0.Id, CreateText(source1));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        if (breakMode)
        {
            ExitBreakState(debuggingSession);
        }

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ModuleDisallowsEditAndContinue_SourceGenerator_NoChanges()
    {
        var moduleId = Guid.NewGuid();

        var source1 = @"/* GENERATE class C1 { void M() { System.Console.WriteLine(1); } } */";
        var source2 = source1;

        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, source1, generator: generator);

        _mockCompilationOutputs.Add(document.Project.Id, new MockCompilationOutputs(moduleId));

        LoadLibraryToDebuggee(moduleId, new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.NotAllowedForRuntime, "*message*"));

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // update document with the same content:
        var document1 = solution.Projects.Single().Documents.Single();
        solution = solution.WithDocumentText(document1.Id, CreateText(source2));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ModuleDisallowsEditAndContinue()
    {
        var moduleId = Guid.NewGuid();
        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, """

            class C1
            {
              void M()
              {
                System.Console.WriteLine(1);
                System.Console.WriteLine(2);
                System.Console.WriteLine(3);
              }
            }
            """, generator: generator);

        _mockCompilationOutputs.Add(document.Project.Id, new MockCompilationOutputs(moduleId));

        LoadLibraryToDebuggee(moduleId, new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.NotAllowedForRuntime, "*message*"));

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the source:
        var document1 = solution.Projects.Single().Documents.Single();
        solution = solution.WithDocumentText(document1.Id, CreateText("""

            class C1
            {
              void M()
              {
                System.Console.WriteLine(9);
                System.Console.WriteLine();
                System.Console.WriteLine(30);
              }
            }
            """));
        var document2 = solution.GetDocument(document1.Id);

        // We do not report module diagnostics until emit.
        // This is to make the analysis deterministic (not dependent on the current state of the debuggee).
        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(docDiagnostics);

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.Equal(
            [$"proj: {document2.FilePath}: (5,0)-(5,32): Error ENC2016: {string.Format(FeaturesResources.EditAndContinueDisallowedByProject, document2.Project.Name, "*message*")}"],
            InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);

        AssertEx.SetEqual([moduleId], debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

        AssertEx.Equal(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1",
            "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines=",
            "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC2016"
        ], _telemetryLog);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/81930")]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2067885")]
    public async Task Encodings(bool matchingContent)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var editorSource = "class C1 { public void こんにちは() {} }";
        var fileSource = matchingContent ? editorSource : "class C1 { public virtual void こんにちは() {} }";

        // encoding from the editor (e.g selected by the user in the IDE, or used implicitly by LSP):
        var editorEncoding = Encoding.UTF8;

        // The actual encoding used by the compiler is either detected from the file content itself (e.g. Unicode encodings)
        // or it can also be set in the project via CodePage property.

        // TODO: https://github.com/dotnet/roslyn/issues/81930
        // We do not currently account for CodePage property value and thus an encoding such as Shift-JIS that can't be detected
        // from the file content does not work.
        // var compilerEncoding = Encoding.GetEncoding("SJIS");
        var compilerEncoding = Encoding.Unicode;

        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("test.cs").WriteAllText(fileSource, compilerEncoding);

        using var workspace = CreateWorkspace(out var solution, out var service);

        DocumentId documentId;

        solution = solution.
            AddTestProject("test", out var projectId).Solution.
            WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithm.Sha1).
            AddDocument(documentId = DocumentId.CreateNewId(projectId), "test.cs", SourceText.From(editorSource, editorEncoding, SourceHashAlgorithm.Sha1), filePath: sourceFile.Path);

        // use different checksum alg to trigger PdbMatchingSourceTextProvider call:
        var moduleId = EmitAndLoadLibraryToDebuggee(projectId, fileSource, sourceFilePath: sourceFile.Path, encoding: compilerEncoding, checksumAlgorithm: SourceHashAlgorithm.Sha256);

        var sourceTextProviderCalled = false;
        var sourceTextProvider = new MockPdbMatchingSourceTextProvider()
        {
            TryGetMatchingSourceTextImpl = (filePath, requiredChecksum, checksumAlgorithm) =>
            {
                sourceTextProviderCalled = true;

                // fall back to reading the file content:
                return null;
            }
        };

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None, sourceTextProvider);

        EnterBreakState(debuggingSession);

        var document = solution.GetRequiredDocument(documentId);
        var (committedDocument, state) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(document, CancellationToken.None);

        Assert.True(sourceTextProviderCalled);
        Assert.Equal(CommittedSolution.DocumentState.MatchesBuildOutput, state);

        if (matchingContent)
        {
            // The file text content matches the document text, hence we reuse the existing document:
            var documentText = await document.GetTextAsync(CancellationToken.None);
            var committedText = await committedDocument.GetTextAsync(CancellationToken.None);

            Assert.Equal(committedText.ToString(), documentText.ToString());
            Assert.True(committedText.ContentEquals(documentText));
            Assert.Same(document, committedDocument);
        }
        else
        {
            var committedText = await committedDocument.GetTextAsync(CancellationToken.None);

            // We have now baseline document whose encoding differs from the current document.
            // The content is the same though, so semantics is the same.
            Assert.Equal(compilerEncoding.WebName, committedText.Encoding.WebName);
            Assert.Equal(fileSource, committedText.ToString());

            var diagnostics = await debuggingSession.GetDocumentDiagnosticsAsync(document, s_noActiveSpans, CancellationToken.None);
            AssertEx.Equal(
                [$"{document.FilePath}: (0,11)-(0,30): Error ENC0004: {string.Format(FeaturesResources.Updating_the_modifiers_of_0_requires_restarting_the_application, FeaturesResources.method)}"],
                InspectDiagnostics(diagnostics));
        }

        EndDebuggingSession(debuggingSession);
    }

    [Theory, CombinatorialData]
    public async Task RudeEdits(bool breakMode)
    {
        var moduleId = Guid.NewGuid();

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

        _mockCompilationOutputs.Add(document.Project.Id, new MockCompilationOutputs(moduleId));

        var debuggingSession = StartDebuggingSession(service, solution);

        if (breakMode)
        {
            EnterBreakState(debuggingSession);
        }

        // change the source (rude edit):
        var document1 = solution.Projects.Single().Documents.Single();
        solution = solution.WithDocumentText(document1.Id, CreateText("class C1 { void M<T>() { System.Console.WriteLine(1); } }"));
        var document2 = solution.GetDocument(document1.Id);

        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(
            [$"{document2.FilePath}: (0,18)-(0,19): Error ENC0110: {string.Format(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.method)}"],
            InspectDiagnostics(docDiagnostics));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.SequenceEqual(["ENC0110"], InspectDiagnosticIds(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();

        if (breakMode)
        {
            ExitBreakState(debuggingSession);
            EndDebuggingSession(debuggingSession);
        }
        else
        {
            EndDebuggingSession(debuggingSession);
        }

        AssertEx.SetEqual([moduleId], debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

        if (breakMode)
        {
            AssertEx.SequenceEqual(
            [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=2",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=0|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines=",
                "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=2|RudeEditKind=110|RudeEditSyntaxKind=8910|RudeEditBlocking=True|RudeEditProjectId={6A6F7270-0000-4000-8000-000000000000}"
            ], _telemetryLog);
        }
        else
        {
            AssertEx.SequenceEqual(
            [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=1|EmptyHotReloadSessionCount=0",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=0|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines=",
                "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=2|RudeEditKind=110|RudeEditSyntaxKind=8910|RudeEditBlocking=True|RudeEditProjectId={6A6F7270-0000-4000-8000-000000000000}"
            ], _telemetryLog);
        }
    }

    [Fact]
    public async Task DeferredApplyChangeWithActiveStatementRudeEdits()
    {
        var source1 = "class C { void M() { System.Console.WriteLine(1); } }";
        var source2 = "class C { void M() { System.Console.WriteLine(2); } }";

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, source1);

        var moduleId = EmitAndLoadLibraryToDebuggee(document.Project.Id, source1);

        var debuggingSession = StartDebuggingSession(service, solution);

        var activeLineSpan1 = CreateText(source1).Lines.GetLinePositionSpan(GetSpan(source1, "System.Console.WriteLine(1);"));
        var activeLineSpan2 = CreateText(source2).Lines.GetLinePositionSpan(GetSpan(source2, "System.Console.WriteLine(2);"));

        var activeStatements = ImmutableArray.Create(
            new ManagedActiveStatementDebugInfo(
                new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 1),
                document.FilePath,
                activeLineSpan1.ToSourceSpan(),
                ActiveStatementFlags.NonLeafFrame | ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.PartiallyExecuted));

        EnterBreakState(debuggingSession, activeStatements);

        // change the source (rude edit):
        solution = solution.WithDocumentText(document.Id, CreateText(source2));
        var document2 = solution.GetDocument(document.Id);

        var diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(["ENC0001: " + string.Format(FeaturesResources.Updating_an_active_statement_requires_restarting_the_application)],
            diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

        // exit break state without applying the change:
        ExitBreakState(debuggingSession);

        diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        // enter break state again (with the same active statements)

        EnterBreakState(debuggingSession, activeStatements);

        diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(["ENC0001: " + string.Format(FeaturesResources.Updating_an_active_statement_requires_restarting_the_application)],
            diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

        // exit break state without applying the change:
        ExitBreakState(debuggingSession);

        // apply the change:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.NotEmpty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        CommitSolutionUpdate(debuggingSession);

        EnterBreakState(debuggingSession, activeStatements);

        // no rude edits - changes have been applied
        diagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        ExitBreakState(debuggingSession);
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task RudeEdits_SourceGenerators()
    {
        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, """

            /* GENERATE: class G { int X1() => 1; } */

            class C { int Y => 1; }

            """, generator: generator);

        // mock project build:
        _mockCompilationOutputs.Add(document.Project.Id, new MockCompilationOutputs(Guid.NewGuid()));

        var debuggingSession = StartDebuggingSession(service, solution);
        EnterBreakState(debuggingSession);

        // change the source:
        var document1 = solution.Projects.Single().Documents.Single();
        solution = solution.WithDocumentText(document1.Id, CreateText("""

            /* GENERATE: class G { int X1<T>() => 1; } */

            class C { int Y => 2; }

            """));

        var generatedDocument = (await solution.Projects.Single().GetSourceGeneratedDocumentsAsync()).Single();

        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(generatedDocument, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(
            [$"{generatedDocument.FilePath}: (0,17)-(0,18): Error ENC0110: {string.Format(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.method)}"],
            InspectDiagnostics(docDiagnostics));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.Equal(["ENC0110"], InspectDiagnosticIds(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Theory(Skip = "https://github.com/dotnet/roslyn/issues/79589")]
    [CombinatorialData]
    public async Task RudeEdits_DocumentOutOfSync(bool breakMode)
    {
        var source0 = "class C1 { void M() { System.Console.WriteLine(0); } }";
        var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("a.cs");

        using var _ = CreateWorkspace(out var solution, out var service);

        var project = AddEmptyTestProject(solution);
        solution = project.Solution;
        var projectId = project.Id;

        // compile with source0:
        var moduleId = EmitAndLoadLibraryToDebuggee(projectId, source0, sourceFilePath: sourceFile.Path);

        // update the file with source1 before session starts:
        sourceFile.WriteAllText(source1, Encoding.UTF8);

        // source1 is reflected in workspace before session starts:
        var document1 = project.AddTestDocument(source1, path: sourceFile.Path);
        solution = document1.Project.Solution;
        var documentId = document1.Id;

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);

        if (breakMode)
        {
            EnterBreakState(debuggingSession);
        }

        // change the source (rude edit):
        solution = solution.WithDocumentText(documentId, CreateText("class C1 { void M<T>() { System.Console.WriteLine(1); } }"));
        var document2 = solution.GetDocument(documentId);

        // no Rude Edits, since the document is out-of-sync
        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(docDiagnostics);

        //  the document is out-of-sync, so no rude edits reported:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        // project is stale:
        Assert.True(debuggingSession.LastCommittedSolution.StaleProjects.ContainsKey(projectId));

        // TODO: warning reported https://github.com/dotnet/roslyn/issues/78125
        // AssertEx.Equal([$"proj.csproj: (0,0)-(0,0): Warning ENC1005: {string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFile.Path)}"], InspectDiagnostics(results.Diagnostics));

        // We do not reload the content of out-of-sync file for analyzer query.
        // We don't check if the content on disk has been updated to match either.
        sourceFile.WriteAllText(source0, Encoding.UTF8);
        docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(docDiagnostics);

        // rebuild triggers reload of out-of-sync file content:
        moduleId = EmitAndLoadLibraryToDebuggee(projectId, source0, sourceFilePath: sourceFile.Path);

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);

        // project has been unstaled:
        Assert.False(debuggingSession.LastCommittedSolution.StaleProjects.ContainsKey(projectId));

        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        if (breakMode)
        {
            ExitBreakState(debuggingSession);
        }

        EndDebuggingSession(debuggingSession);

        if (breakMode)
        {
            AssertEx.SequenceEqual(
            [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=1|HotReloadSessionCount=0|EmptyHotReloadSessionCount=2"
            ], _telemetryLog);
        }
        else
        {
            AssertEx.SequenceEqual(
            [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1"
            ], _telemetryLog);
        }
    }

    [Fact]
    public async Task RudeEdits_DocumentWithoutSequencePoints()
    {
        var source1 = "abstract class C { public abstract void M(); }";
        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
        var document1 = solution.
            AddTestProject("test").
            AddDocument("test.cs", CreateText(source1), filePath: sourceFile.Path);

        var project = document1.Project;
        solution = project.Solution;

        var moduleId = EmitAndLoadLibraryToDebuggee(project.Id, source1, sourceFilePath: sourceFile.Path);

        // do not initialize the document state - we will detect the state based on the PDB content.
        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);

        EnterBreakState(debuggingSession);

        // change the source (rude edit since the base document content matches the PDB checksum, so the document is not out-of-sync):
        solution = solution.WithDocumentText(document1.Id, CreateText("abstract class C { public abstract void M(); public abstract void N(); }"));
        var document2 = solution.Projects.Single().Documents.Single();

        // Rude Edits reported:
        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(
            [$"{document1.FilePath}: (0,45)-(0,69): Error ENC0023: {string.Format(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_requires_restarting_the_application, FeaturesResources.method)}"],
            InspectDiagnostics(docDiagnostics));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.SequenceEqual(["ENC0023"], InspectDiagnosticIds(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task RudeEdits_DelayLoadedModule()
    {
        var source1 = "class C { public void M() { } }";
        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("a.cs").WriteAllText(source1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
        var document1 = solution.
            AddTestProject("test").
            AddDocument("test.cs", CreateText(source1), filePath: sourceFile.Path);

        var project = document1.Project;
        solution = project.Solution;

        var moduleId = EmitLibrary(project.Id, source1, sourceFilePath: sourceFile.Path);

        // do not initialize the document state - we will detect the state based on the PDB content.
        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);

        EnterBreakState(debuggingSession);

        // change the source (rude edit) before the library is loaded:
        solution = solution.WithDocumentText(document1.Id, CreateText("class C { public void M<T>() { } }"));
        var document2 = solution.Projects.Single().Documents.Single();

        // Rude Edits reported:
        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(
            [$"{document2.FilePath}: (0,24)-(0,25): Error ENC0110: {string.Format(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.method)}"],
            InspectDiagnostics(docDiagnostics));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.SequenceEqual(["ENC0110"], InspectDiagnosticIds(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();

        // load library to the debuggee:
        LoadLibraryToDebuggee(moduleId);

        // Rude Edits still reported:
        docDiagnostics = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(
            [$"{document2.FilePath}: (0,24)-(0,25): Error ENC0110: {string.Format(FeaturesResources.Changing_the_signature_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.method)}"],
            InspectDiagnostics(docDiagnostics));

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.SequenceEqual(["ENC0110"], InspectDiagnosticIds(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Theory]
    [CombinatorialData]
    public async Task RudeEdits_UpdateBaseline(bool validChangeBeforeRudeEdit)
    {
        var source3 = "abstract class C { void F() {} public abstract void G(); }";
        var projectDir = Temp.CreateDirectory();

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, "abstract class C { }", projectDir);

        var documentId = document.Id;
        var projectId = document.Project.Id;
        var moduleId = EmitAndLoadLibraryToDebuggee(document);
        var sourceFilePath = document.FilePath;

        var debuggingSession = StartDebuggingSession(service, solution);

        EmitSolutionUpdateResults results;
        var readers = ImmutableArray<IDisposable>.Empty;

        // change the source (valid edit):
        if (validChangeBeforeRudeEdit)
        {
            solution = solution.WithDocumentText(documentId, CreateText("abstract class C { void F() {} }"));

            results = await EmitSolutionUpdateAsync(debuggingSession, solution);
            Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
            Assert.Empty(results.ProjectsToRebuild);
            Assert.Empty(results.ProjectsToRestart);

            // baseline should be present:
            readers = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
            Assert.NotEmpty(readers);
            Assert.True(debuggingSession.GetTestAccessor().HasProjectEmitBaseline(projectId));

            CommitSolutionUpdate(debuggingSession);
        }
        else
        {
            // baseline is not present:
            Assert.Empty(debuggingSession.GetTestAccessor().GetBaselineModuleReaders());
            Assert.False(debuggingSession.GetTestAccessor().HasProjectEmitBaseline(projectId));
        }

        // change the source (rude edit):
        solution = solution.WithDocumentText(documentId, CreateText(source3));

        // Rude Edits reported:
        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetRequiredDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(
            [$"{document.FilePath}: (0,31)-(0,55): Error ENC0023: {string.Format(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_requires_restarting_the_application, FeaturesResources.method)}"],
            InspectDiagnostics(docDiagnostics));

        // validate solution update status and emit:
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        AssertEx.SequenceEqual(["ENC0023"], InspectDiagnosticIds(results.GetAllDiagnostics()));
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        AssertEx.Equal([projectId], results.ProjectsToRebuild);
        AssertEx.Equal([projectId], results.ProjectsToRestart.Keys);

        // assuming user approved restart and rebuild:
        CommitSolutionUpdate(debuggingSession);

        // rebuild and restart:
        _debuggerService.LoadedModules.Remove(moduleId);
        File.WriteAllText(sourceFilePath, source3, Encoding.UTF8);
        moduleId = EmitAndLoadLibraryToDebuggee(solution.GetRequiredDocument(documentId));

        if (validChangeBeforeRudeEdit)
        {
            // baseline should be removed:
            Assert.False(debuggingSession.GetTestAccessor().HasProjectEmitBaseline(projectId));

            // all readers created for the module must be disposed, so we can rebuild the module:
            VerifyReadersDisposed(readers);
        }

        // no rude edits reported:
        Assert.Empty(await service.GetDocumentDiagnosticsAsync(solution.GetRequiredDocument(documentId), s_noActiveSpans, CancellationToken.None));

        // change the source (valid edit):
        solution = solution.WithDocumentText(documentId, CreateText("abstract class C { void F() {} public abstract void G(); void H() {} }"));

        // no rude edits:
        Assert.Empty(await service.GetDocumentDiagnosticsAsync(solution.GetRequiredDocument(documentId), s_noActiveSpans, CancellationToken.None));

        // apply valid change:
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        CommitSolutionUpdate(debuggingSession);

        EndDebuggingSession(debuggingSession);

        AssertEx.SequenceEqual(validChangeBeforeRudeEdit
            ? [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=3|EmptyHotReloadSessionCount=1",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges={6A6F7270-0000-4000-8000-000000000000}|ProjectIdsWithUpdatedBaselines=",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=3|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=0|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines={6A6F7270-0000-4000-8000-000000000000}",
                "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=3|RudeEditKind=23|RudeEditSyntaxKind=8875|RudeEditBlocking=True|RudeEditProjectId={6A6F7270-0000-4000-8000-000000000000}",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=4|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges={6A6F7270-0000-4000-8000-000000000000}|ProjectIdsWithUpdatedBaselines=",
            ]
            :
            [
                "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=2|EmptyHotReloadSessionCount=1",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=True|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=1|EmitDeltaErrorIdCount=0|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines={6A6F7270-0000-4000-8000-000000000000}",
                "Debugging_EncSession_EditSession_RudeEdit: SessionId=1|EditSessionId=2|RudeEditKind=23|RudeEditSyntaxKind=8875|RudeEditBlocking=True|RudeEditProjectId={6A6F7270-0000-4000-8000-000000000000}",
                "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=3|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges={6A6F7270-0000-4000-8000-000000000000}|ProjectIdsWithUpdatedBaselines=",
            ],
            _telemetryLog);
    }

    [Fact]
    public async Task SyntaxError()
    {
        var moduleId = Guid.NewGuid();

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

        _mockCompilationOutputs.Add(document.Project.Id, new MockCompilationOutputs(moduleId));

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the source (compilation error):
        var document1 = solution.Projects.Single().Documents.Single();
        solution = solution.WithDocumentText(document1.Id, CreateText("class C1 { void M() { "));
        var document2 = solution.Projects.Single().Documents.Single();

        // compilation errors are not reported via EnC diagnostic analyzer:
        var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics1);

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Blocked, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        EndDebuggingSession(debuggingSession);

        AssertEx.SetEqual([moduleId], debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

        AssertEx.Equal(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1",
            "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=True|HadRudeEdits=False|HadValidChanges=False|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines="
        ], _telemetryLog);
    }

    [Fact]
    public async Task SemanticDiagnostics()
    {
        var sourceV1 = """
            class C1 { void M() { System.Console.WriteLine(1); } }
            """;

        var sourceV2 = """
            global using System;
            using System;
            
            class C1 { void M() { int i = 0L; System.Console.WriteLine(i); } }
            """;

        var globalUsings = """
            global using System;
            """;

        var sourceFile = Temp.CreateFile("a.cs").WriteAllText(sourceV1, Encoding.UTF8);
        var globalUsingsFile = Temp.CreateFile("global.cs").WriteAllText(globalUsings, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        solution = solution.
            AddTestProject("test", out var projectId).
            AddTestDocument(sourceV1, sourceFile.Path, out var documentId).Project.
            AddTestDocument(globalUsings, globalUsingsFile.Path).Project.Solution;

        var moduleId = EmitAndLoadLibraryToDebuggee(projectId, sourceV1);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the source (compilation error):
        var document1 = solution.GetRequiredDocument(documentId);
        solution = solution.WithDocumentText(document1.Id, CreateText(sourceV2));
        var document2 = solution.GetRequiredDocument(documentId);

        // compilation errors are not reported via EnC diagnostic analyzer:
        var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics1);

        // The EnC analyzer does not check for and block on all semantic errors as they are already reported by diagnostic analyzer.
        // Blocking update on semantic errors would be possible, but the status check is only an optimization to avoid emitting.
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Blocked, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);

        // Only errors and warnings are reported:
        AssertEx.Equal(
        [
            $"test: {document2.FilePath}: (1,6)-(1,12): Warning CS0105: {string.Format(CSharpResources.WRN_DuplicateUsing, "System")}",
            $"test: {document2.FilePath}: (3,30)-(3,32): Error CS0266: {string.Format(CSharpResources.ERR_NoImplicitConvCast, "long", "int")}",
            // Not reported: Hidden CS8933: The using directive for 'System' appeared previously as global using
        ], InspectDiagnostics(results.Diagnostics));

        EndDebuggingSession(debuggingSession);

        AssertEx.SetEqual([moduleId], debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

        AssertEx.SequenceEqual(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1",
            "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines=",
            "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=CS0266"
        ], _telemetryLog);
    }

    [Fact]
    public async Task HasChanges()
    {
        using var _ = CreateWorkspace(out var solution, out var service, [typeof(NoCompilationLanguageService)]);

        var pathA = Path.Combine(TempRoot.Root, "A.cs");
        var pathB = Path.Combine(TempRoot.Root, "B.cs");
        var pathC = Path.Combine(TempRoot.Root, "C.cs");
        var pathD = Path.Combine(TempRoot.Root, "D.cs");
        var pathX = Path.Combine(TempRoot.Root, "X");
        var pathY = Path.Combine(TempRoot.Root, "Y");
        var pathCommon = Path.Combine(TempRoot.Root, "Common.cs");

        var projectAId = ProjectId.CreateNewId("A");
        var projectBId = ProjectId.CreateNewId("B");
        var projectCId = ProjectId.CreateNewId("C");

        solution = solution.
            AddTestProject("A", id: projectAId).
            AddDocument("A.cs", "class Program { void Main() { System.Console.WriteLine(1); } }", filePath: pathA).Project.Solution.
            AddTestProject("B", id: projectBId).
            AddDocument("Common.cs", "class Common {}", filePath: pathCommon).Project.
            AddDocument("B.cs", "class B {}", filePath: pathB).Project.Solution.
            AddTestProject("C", id: projectCId).
            AddDocument("Common.cs", "class Common {}", filePath: pathCommon).Project.
            AddDocument("C.cs", "class C {}", filePath: pathC).Project.Solution;

        var debuggingSession = StartDebuggingSession(service, solution);
        EnterBreakState(debuggingSession);

        // mock project build:
        _mockCompilationOutputs.Add(projectAId, new MockCompilationOutputs(Guid.NewGuid()));
        _mockCompilationOutputs.Add(projectBId, new MockCompilationOutputs(Guid.NewGuid()));
        _mockCompilationOutputs.Add(projectCId, new MockCompilationOutputs(Guid.NewGuid()));

        // change C.cs to have a compilation error:
        var oldSolution = solution;
        var projectC = solution.GetProjectsByName("C").Single();
        var documentC = projectC.Documents.Single(d => d.Name == "C.cs");
        solution = solution.WithDocumentText(documentC.Id, CreateText("class C { void M() { "));

        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, CancellationToken.None));

        Assert.False(await EditSession.HasChangesAsync(oldSolution, solution, sourceFilePath: pathCommon, CancellationToken.None));
        Assert.False(await EditSession.HasChangesAsync(oldSolution, solution, sourceFilePath: pathB, CancellationToken.None));
        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, sourceFilePath: pathC, CancellationToken.None));
        Assert.False(await EditSession.HasChangesAsync(oldSolution, solution, sourceFilePath: "NonexistentFile.cs", CancellationToken.None));

        // All projects must have no errors.
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Blocked, results.ModuleUpdates.Status);

        // add a project:

        oldSolution = solution;
        var projectD = solution.AddTestProject("D");
        solution = projectD.Solution;

        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, CancellationToken.None));

        // remove a project:
        Assert.True(await EditSession.HasChangesAsync(solution, solution.RemoveProject(projectD.Id), CancellationToken.None));

        // add a project that doesn't support EnC:

        oldSolution = solution;
        var projectE = solution.AddTestProject("E", language: NoCompilationConstants.LanguageName);
        solution = projectE.Solution;

        Assert.False(await EditSession.HasChangesAsync(oldSolution, solution, CancellationToken.None));

        // remove a project that doesn't support EnC:
        Assert.False(await EditSession.HasChangesAsync(solution, oldSolution, CancellationToken.None));

        EndDebuggingSession(debuggingSession);
    }

    [Theory, CombinatorialData]
    public async Task HasChanges_Documents(TextDocumentKind documentKind)
    {
        using var _ = CreateWorkspace(out var solution, out var service);
        var log = new TraceLog("Test");

        var pathX = Path.Combine(TempRoot.Root, "X.cs");
        var pathA = Path.Combine(TempRoot.Root, "A.cs");

        var generatorExecutionCount = 0;
        var generator = new TestSourceGenerator()
        {
            ExecuteImpl = context =>
            {
                switch (documentKind)
                {
                    case TextDocumentKind.Document:
                        context.AddSource("Generated.cs", context.Compilation.SyntaxTrees.SingleOrDefault(t => t.FilePath.EndsWith("X.cs"))?.ToString() ?? "none");
                        break;

                    case TextDocumentKind.AdditionalDocument:
                        context.AddSource("Generated.cs", context.AdditionalFiles.FirstOrDefault()?.GetText().ToString() ?? "none");
                        break;

                    case TextDocumentKind.AnalyzerConfigDocument:
                        var syntaxTree = context.Compilation.SyntaxTrees.Single(t => t.FilePath.EndsWith("A.cs"));
                        var content = context.AnalyzerConfigOptions.GetOptions(syntaxTree).TryGetValue("x", out var optionValue) ? optionValue.ToString() : "none";

                        context.AddSource("Generated.cs", content);
                        break;
                }

                generatorExecutionCount++;
            }
        };

        var project = solution
            .AddTestProject("A")
            .AddTestDocument(source: "", path: pathA)
            .Project;

        var projectId = project.Id;
        solution = project.Solution.AddAnalyzerReference(projectId, new TestGeneratorReference(generator));
        project = solution.GetRequiredProject(projectId);
        var generatedDocument = (await project.GetSourceGeneratedDocumentsAsync()).Single();
        var generatedDocumentId = generatedDocument.Id;

        var debuggingSession = StartDebuggingSession(service, solution);
        EnterBreakState(debuggingSession);

        Assert.Equal(1, generatorExecutionCount);
        var projectDifferences = new ProjectDifferences();

        //
        // Add document
        //

        generatorExecutionCount = 0;
        var oldSolution = solution;
        var documentId = DocumentId.CreateNewId(projectId);
        solution = documentKind switch
        {
            TextDocumentKind.Document => solution.AddDocument(documentId, "X", CreateText("xxx"), filePath: pathX),
            TextDocumentKind.AdditionalDocument => solution.AddAdditionalDocument(documentId, "X", CreateText("xxx"), filePath: pathX),
            TextDocumentKind.AnalyzerConfigDocument => solution.AddAnalyzerConfigDocument(documentId, "X", GetAnalyzerConfigText([("x", "1")]), filePath: pathX),
            _ => throw ExceptionUtilities.Unreachable(),
        };
        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, CancellationToken.None));
        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, pathX, CancellationToken.None));

        // always returns false for source generated files:
        Assert.False(await EditSession.HasChangesAsync(oldSolution, solution, generatedDocument.FilePath, CancellationToken.None));

        // generator is not executed since we already know the solution changed without inspecting generated files:
        Assert.Equal(0, generatorExecutionCount);

        AssertEx.Equal([generatedDocumentId],
            await EditSession.GetChangedDocumentsAsync(log, oldSolution.GetProject(projectId), solution.GetProject(projectId), CancellationToken.None).ToImmutableArrayAsync(CancellationToken.None));

        var diagnostics = new ArrayBuilder<Diagnostic>();
        await EditSession.GetProjectDifferencesAsync(log, oldSolution.GetProject(projectId), solution.GetProject(projectId), projectDifferences, diagnostics, CancellationToken.None);
        Assert.Empty(diagnostics);
        Assert.Empty(projectDifferences.DeletedDocuments);
        AssertEx.Equal(documentKind == TextDocumentKind.Document ? [documentId, generatedDocumentId] : [generatedDocumentId], projectDifferences.ChangedOrAddedDocuments.Select(d => d.Id));

        Assert.Equal(1, generatorExecutionCount);

        //
        // Update document to a different document snapshot but the same content
        //

        generatorExecutionCount = 0;
        oldSolution = solution;

        solution = documentKind switch
        {
            TextDocumentKind.Document => solution.WithDocumentText(documentId, CreateText("xxx")),
            TextDocumentKind.AdditionalDocument => solution.WithAdditionalDocumentText(documentId, CreateText("xxx")),
            TextDocumentKind.AnalyzerConfigDocument => solution.WithAnalyzerConfigDocumentText(documentId, GetAnalyzerConfigText([("x", "1")])),
            _ => throw ExceptionUtilities.Unreachable(),
        };
        Assert.False(await EditSession.HasChangesAsync(oldSolution, solution, CancellationToken.None));
        Assert.False(await EditSession.HasChangesAsync(oldSolution, solution, pathX, CancellationToken.None));

        Assert.Equal(0, generatorExecutionCount);

        // source generator infrastructure compares content and reuses state if it matches (SourceGeneratedDocumentState.WithUpdatedGeneratedContent):
        AssertEx.Equal(documentKind == TextDocumentKind.Document ? new[] { documentId } : [],
            await EditSession.GetChangedDocumentsAsync(log, oldSolution.GetProject(projectId), solution.GetProject(projectId), CancellationToken.None).ToImmutableArrayAsync(CancellationToken.None));

        await EditSession.GetProjectDifferencesAsync(log, oldSolution.GetProject(projectId), solution.GetProject(projectId), projectDifferences, diagnostics, CancellationToken.None);
        Assert.Empty(diagnostics);
        Assert.True(projectDifferences.IsEmpty);

        Assert.Equal(1, generatorExecutionCount);

        //
        // Update document content
        //

        generatorExecutionCount = 0;
        oldSolution = solution;
        solution = documentKind switch
        {
            TextDocumentKind.Document => solution.WithDocumentText(documentId, CreateText("xxx-changed")),
            TextDocumentKind.AdditionalDocument => solution.WithAdditionalDocumentText(documentId, CreateText("xxx-changed")),
            TextDocumentKind.AnalyzerConfigDocument => solution.WithAnalyzerConfigDocumentText(documentId, GetAnalyzerConfigText([("x", "2")])),
            _ => throw ExceptionUtilities.Unreachable(),
        };
        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, CancellationToken.None));
        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, pathX, CancellationToken.None));

        AssertEx.Equal(documentKind == TextDocumentKind.Document ? [documentId, generatedDocumentId] : [generatedDocumentId],
            await EditSession.GetChangedDocumentsAsync(log, oldSolution.GetProject(projectId), solution.GetProject(projectId), CancellationToken.None).ToImmutableArrayAsync(CancellationToken.None));

        await EditSession.GetProjectDifferencesAsync(log, oldSolution.GetProject(projectId), solution.GetProject(projectId), projectDifferences, diagnostics, CancellationToken.None);
        Assert.Empty(diagnostics);
        Assert.Empty(projectDifferences.DeletedDocuments);
        AssertEx.Equal(documentKind == TextDocumentKind.Document ? [documentId, generatedDocumentId] : [generatedDocumentId], projectDifferences.ChangedOrAddedDocuments.Select(d => d.Id));

        Assert.Equal(1, generatorExecutionCount);

        //
        // Remove document
        //

        generatorExecutionCount = 0;
        oldSolution = solution;
        solution = documentKind switch
        {
            TextDocumentKind.Document => solution.RemoveDocument(documentId),
            TextDocumentKind.AdditionalDocument => solution.RemoveAdditionalDocument(documentId),
            TextDocumentKind.AnalyzerConfigDocument => solution.RemoveAnalyzerConfigDocument(documentId),
            _ => throw ExceptionUtilities.Unreachable(),
        };
        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, CancellationToken.None));
        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, pathX, CancellationToken.None));

        Assert.Equal(0, generatorExecutionCount);

        AssertEx.Equal([generatedDocumentId],
            await EditSession.GetChangedDocumentsAsync(log, oldSolution.GetProject(projectId), solution.GetProject(projectId), CancellationToken.None).ToImmutableArrayAsync(CancellationToken.None));

        await EditSession.GetProjectDifferencesAsync(log, oldSolution.GetProject(projectId), solution.GetProject(projectId), projectDifferences, diagnostics, CancellationToken.None);
        Assert.Empty(diagnostics);

        if (documentKind == TextDocumentKind.Document)
        {
            AssertEx.Equal([documentId], projectDifferences.DeletedDocuments.Select(d => d.Id));
        }
        else
        {
            AssertEx.Empty(projectDifferences.DeletedDocuments);
        }

        AssertEx.Equal([generatedDocumentId], projectDifferences.ChangedOrAddedDocuments.Select(d => d.Id));

        Assert.Equal(1, generatorExecutionCount);
    }

    [Fact]
    public async Task HasChanges_SourceGeneratorFailure()
    {
        using var _ = CreateWorkspace(out var solution, out var service);

        var log = new TraceLog("Test");

        var pathA = Path.Combine(TempRoot.Root, "A.txt");

        var generatorExecutionCount = 0;
        var generator = new TestSourceGenerator()
        {
            ExecuteImpl = context =>
            {
                generatorExecutionCount++;

                var additionalText = context.AdditionalFiles.Single().GetText().ToString();
                if (additionalText.Contains("updated"))
                {
                    throw new InvalidOperationException("Source generator failed");
                }

                context.AddSource("generated.cs", SourceText.From("generated: " + additionalText, Encoding.UTF8, SourceHashAlgorithm.Sha256));
            }
        };

        var project = solution
            .AddTestProject("A")
            .AddAdditionalDocument("A.txt", "text", filePath: pathA)
            .Project;

        var projectId = project.Id;
        solution = project.Solution.AddAnalyzerReference(projectId, new TestGeneratorReference(generator));
        project = solution.GetRequiredProject(projectId);
        var aId = project.AdditionalDocumentIds.Single();

        var generatedDocuments = await project.Solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(project.State, CancellationToken.None);

        var generatedText = generatedDocuments.States.Single().Value.GetTextSynchronously(CancellationToken.None).ToString();
        AssertEx.AreEqual("generated: text", generatedText);
        Assert.Equal(1, generatorExecutionCount);

        var generatorDiagnostics = await solution.CompilationState.GetSourceGeneratorDiagnosticsAsync(project.State, CancellationToken.None);
        Assert.Empty(generatorDiagnostics);

        var debuggingSession = StartDebuggingSession(service, solution);
        EnterBreakState(debuggingSession);

        var diffences = new ProjectDifferences();

        //
        // Update document content
        //

        var oldSolution = solution;
        var oldProject = project;
        solution = solution.WithAdditionalDocumentText(aId, CreateText("updated text"));
        project = solution.GetRequiredProject(projectId);

        // Change in additional file is detected:
        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, CancellationToken.None));

        // No changed source documents since the generator failed:
        AssertEx.Empty(await EditSession.GetChangedDocumentsAsync(log, oldProject, project, CancellationToken.None).ToImmutableArrayAsync(CancellationToken.None));

        var diagnostics = new ArrayBuilder<Diagnostic>();
        await EditSession.GetProjectDifferencesAsync(log, oldProject, project, diffences, diagnostics, CancellationToken.None);
        Assert.Contains("System.InvalidOperationException: Source generator failed", diagnostics.Single().GetMessage());
        AssertEx.Empty(diffences.ChangedOrAddedDocuments);
        AssertEx.Equal(["generated.cs"], diffences.DeletedDocuments.Select(d => d.Name));

        Assert.Equal(2, generatorExecutionCount);

        generatedDocuments = await solution.CompilationState.GetSourceGeneratedDocumentStatesAsync(project.State, CancellationToken.None);
        Assert.Empty(generatedDocuments.States);

        generatorDiagnostics = await solution.CompilationState.GetSourceGeneratorDiagnosticsAsync(project.State, CancellationToken.None);
        Assert.Contains("System.InvalidOperationException: Source generator failed", generatorDiagnostics.Single().GetMessage());
    }

    [Fact]
    public async Task HasChanges_Settings()
    {
        using var _ = CreateWorkspace(out var solution, out var service);

        var sourcePath = Path.Combine(TempRoot.Root, "A.cs");

        solution = solution.
            AddTestProject("A", out var projectId).
            AddTestDocument("class C;", sourcePath).Project.Solution;

        var debuggingSession = StartDebuggingSession(service, solution);
        EnterBreakState(debuggingSession);

        var oldSolution = solution;
        solution = solution
            .GetRequiredProject(projectId)
            .WithCompilationOptions<CSharpCompilationOptions>(options => options.WithOverflowChecks(!options.CheckOverflow)).Solution;

        Assert.True(await EditSession.HasChangesAsync(oldSolution, solution, CancellationToken.None));
        Assert.False(await EditSession.HasChangesAsync(oldSolution, solution, sourceFilePath: sourcePath, CancellationToken.None));

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task HasChanges_References()
    {
        using var _ = CreateWorkspace(out var solution0, out var service);

        solution0 = solution0.
            AddTestProject("A", out var projectAId).Solution.
            AddTestProject("B", out var projectBId).Solution;

        var debuggingSession = StartDebuggingSession(service, solution0);
        EnterBreakState(debuggingSession);

        // add metadata reference:
        var solution1 = solution0.AddMetadataReference(projectAId, TestReferences.MetadataTests.InterfaceAndClass.CSClasses01);
        Assert.True(await EditSession.HasChangesAsync(solution0, solution1, CancellationToken.None));

        // add project reference:
        var solution2 = solution1.AddProjectReferences(projectBId, [new ProjectReference(projectAId)]);
        Assert.True(await EditSession.HasChangesAsync(solution1, solution2, CancellationToken.None));

        // change reference properties:
        var solution3 = solution2.WithProjectReferences(projectBId, [new ProjectReference(projectAId, aliases: ["A"])]);
        Assert.True(await EditSession.HasChangesAsync(solution2, solution3, CancellationToken.None));

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task Project_Add()
    {
        var sourceA1 = "class A { void M() { System.Console.WriteLine(1); } }";
        var sourceB1 = "class B { int F() => 1; }";

        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("a.cs").WriteAllText(sourceA1, Encoding.UTF8);
        var sourceFileB = dir.CreateFile("b.cs").WriteAllText(sourceB1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        solution = solution
            .AddTestProject("A", out var projectAId)
                .AddTestDocument(sourceA1, path: sourceFileA.Path, out var documentAId).Project.Solution;

        EmitAndLoadLibraryToDebuggee(solution.GetRequiredProject(projectAId));

        var debuggingSession = StartDebuggingSession(service, solution);

        // Add project B and a project reference A -> B
        solution = solution
            .AddTestProject("B", out var projectBId)
                .AddTestDocument(sourceB1, path: sourceFileB.Path, out var documentBId).Project.Solution
           .AddProjectReference(projectAId, new ProjectReference(projectBId));

        var runningProjects = ImmutableDictionary<ProjectId, RunningProjectOptions>.Empty
            .Add(projectAId, new RunningProjectOptions() { RestartWhenChangesHaveNoEffect = false });

        var results = await debuggingSession.EmitSolutionUpdateAsync(solution, runningProjects, s_noActiveSpans, CancellationToken.None);

        AssertEx.Empty(results.ProjectsToRestart);
        AssertEx.Equal([projectBId], results.ProjectsToRebuild);
        AssertEx.Equal([projectAId], results.ProjectsToRedeploy);
        AssertEx.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        AssertEx.Empty(results.ModuleUpdates.Updates);

        CommitSolutionUpdate(debuggingSession);
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ProjectReference_Add()
    {
        var sourceA1 = "class A { void M() { System.Console.WriteLine(1); } }";
        var sourceB1 = "class B { int F() => 1; }";

        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("a.cs").WriteAllText(sourceA1, Encoding.UTF8);
        var sourceFileB = dir.CreateFile("b.cs").WriteAllText(sourceB1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        solution = solution
            .AddTestProject("A", out var projectAId)
                .AddTestDocument(sourceA1, path: sourceFileA.Path, out var documentAId).Project.Solution
            .AddTestProject("B", out var projectBId)
                .AddTestDocument(sourceB1, path: sourceFileB.Path, out var documentBId).Project.Solution;

        EmitAndLoadLibraryToDebuggee(solution.GetRequiredProject(projectAId));
        EmitAndLoadLibraryToDebuggee(solution.GetRequiredProject(projectBId));

        var debuggingSession = StartDebuggingSession(service, solution);

        // Add project reference A -> B
        solution = solution.AddProjectReference(projectAId, new ProjectReference(projectBId));

        var runningProjects = ImmutableDictionary<ProjectId, RunningProjectOptions>.Empty
            .Add(projectAId, new RunningProjectOptions() { RestartWhenChangesHaveNoEffect = false });

        var results = await debuggingSession.EmitSolutionUpdateAsync(solution, runningProjects, s_noActiveSpans, CancellationToken.None);

        AssertEx.Empty(results.ProjectsToRestart);
        AssertEx.Empty(results.ProjectsToRebuild);
        AssertEx.Equal([projectAId], results.ProjectsToRedeploy);
        AssertEx.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        AssertEx.Empty(results.ModuleUpdates.Updates);

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1371694")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/1204")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/79423")]
    public async Task Project_Add_BinaryAlreadyLoaded()
    {
        // Project A:
        var sourceA1 = "class A { void M() { System.Console.WriteLine(1); } }";

        // Project B (baseline, but not loaded into solution):
        var sourceB1 = "class B { int F() => 1; }";
        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("a.cs").WriteAllText(sourceA1, Encoding.UTF8);
        var sourceFileB = dir.CreateFile("b.cs").WriteAllText(sourceB1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        solution = solution
            .AddTestProject("A", out var projectAId)
                .AddTestDocument(sourceA1, path: sourceFileA.Path, out var documentAId).Project.Solution;

        var projectBId = ProjectId.CreateNewId("B");

        var mvidA = EmitAndLoadLibraryToDebuggee(projectAId, sourceA1, sourceFilePath: sourceFileA.Path, assemblyName: "A");
        var mvidB = EmitAndLoadLibraryToDebuggee(projectBId, sourceB1, sourceFilePath: sourceFileB.Path, assemblyName: "B");

        var debuggingSession = StartDebuggingSession(service, solution);

        // An active statement may be present in the added file since the file exists in the PDB:
        var activeLineSpanA1 = CreateText(sourceA1).Lines.GetLinePositionSpan(GetSpan(sourceA1, "System.Console.WriteLine(1);"));
        var activeLineSpanB1 = CreateText(sourceB1).Lines.GetLinePositionSpan(GetSpan(sourceB1, "1"));

        var activeStatements = ImmutableArray.Create(
            new ManagedActiveStatementDebugInfo(
                new ManagedInstructionId(new ManagedMethodId(mvidA, token: 0x06000001, version: 1), ilOffset: 1),
                sourceFileA.Path,
                activeLineSpanA1.ToSourceSpan(),
                ActiveStatementFlags.LeafFrame | ActiveStatementFlags.MethodUpToDate),
            new ManagedActiveStatementDebugInfo(
                new ManagedInstructionId(new ManagedMethodId(mvidB, token: 0x06000001, version: 1), ilOffset: 1),
                sourceFileB.Path,
                activeLineSpanB1.ToSourceSpan(),
                ActiveStatementFlags.LeafFrame | ActiveStatementFlags.MethodUpToDate));

        EnterBreakState(debuggingSession, activeStatements);

        // add project that matches assembly B and update the document:

        solution = solution.
            AddTestProject("B", id: projectBId).
            AddTestDocument("class B { virtual int F() => 1; }", path: sourceFileB.Path, out var documentBId).Project.Solution;

        var documentB2 = solution.GetRequiredDocument(documentBId);

        var baseSpans = await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [documentAId, documentBId], CancellationToken.None);
        AssertEx.Equal(
        [
            activeLineSpanA1.ToString(),
            activeLineSpanB1.ToString(),
        ], baseSpans.Select(spans => spans.IsEmpty ? "<empty>" : string.Join(",", spans.Select(s => s.LineSpan.ToString()))));

        var trackedActiveSpans = ImmutableArray.Create(
            new ActiveStatementSpan(new ActiveStatementId(0), activeLineSpanB1, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame));

        var currentSpans = await debuggingSession.GetAdjustedActiveStatementSpansAsync(documentB2, (_, _, _) => new(trackedActiveSpans), CancellationToken.None);
        // TODO: https://github.com/dotnet/roslyn/issues/79423
        // AssertEx.Equal(trackedActiveSpans, currentSpans);
        Assert.Empty(currentSpans);

        var diagnostics = await service.GetDocumentDiagnosticsAsync(documentB2, s_noActiveSpans, CancellationToken.None);

        // TODO: https://github.com/dotnet/roslyn/issues/1204
        //AssertEx.Equal(
        //    new[] { "ENC0020: " + string.Format(FeaturesResources.Renaming_0_requires_restarting_the_application, FeaturesResources.method) },
        //    diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));
        Assert.Empty(diagnostics);

        // update document with a valid change:
        solution = solution.WithDocumentText(documentB2.Id, CreateText("class B { int F() => 2; }"));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);

        // TODO: https://github.com/dotnet/roslyn/issues/1204
        // verify valid update
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);

        ExitBreakState(debuggingSession);

        EndDebuggingSession(debuggingSession);
    }

    [Theory, CombinatorialData]
    public async Task Capabilities(bool breakState)
    {
        var source1 = "class C { void M() { } }";
        using var _ = CreateWorkspace(out var solution, out var service);
        solution = AddDefaultTestProject(solution, [source1]);
        var documentId = solution.Projects.Single().Documents.Single().Id;

        EmitAndLoadLibraryToDebuggee(documentId.ProjectId, source1);

        // attached to processes that allow updating custom attributes:
        _debuggerService.GetCapabilitiesImpl = () => ["Baseline", "ChangeCustomAttributes"];

        // F5
        var debuggingSession = StartDebuggingSession(service, solution);

        // update document:
        solution = solution.WithDocumentText(documentId, CreateText("[System.Obsolete]class C { void M() { } }"));

        var diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        if (breakState)
        {
            EnterBreakState(debuggingSession);
        }

        diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        // attach to additional processes - at least one process that does not allow updating custom attributes:
        if (breakState)
        {
            ExitBreakState(debuggingSession);
        }

        _debuggerService.GetCapabilitiesImpl = () => ["Baseline"];

        if (breakState)
        {
            EnterBreakState(debuggingSession);
        }
        else
        {
            CapabilitiesChanged(debuggingSession);
        }

        diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(["ENC0101: " + string.Format(FeaturesResources.Updating_the_attributes_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.class_)],
           diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

        if (breakState)
        {
            ExitBreakState(debuggingSession);
        }

        diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(["ENC0101: " + string.Format(FeaturesResources.Updating_the_attributes_of_0_requires_restarting_the_application_because_it_is_not_supported_by_the_runtime, FeaturesResources.class_)],
           diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));

        // detach from processes that do not allow updating custom attributes:
        _debuggerService.GetCapabilitiesImpl = () => ["Baseline", "ChangeCustomAttributes"];

        if (breakState)
        {
            EnterBreakState(debuggingSession);
        }
        else
        {
            CapabilitiesChanged(debuggingSession);
        }

        diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        if (breakState)
        {
            ExitBreakState(debuggingSession);
        }

        EndDebuggingSession(debuggingSession);

        AssertEx.Equal(
        [
            $"Debugging_EncSession: SolutionSessionId={{00000000-AAAA-AAAA-AAAA-000000000000}}|SessionId=1|SessionCount=0|EmptySessionCount={(breakState ? 3 : 0)}|HotReloadSessionCount=0|EmptyHotReloadSessionCount={(breakState ? 4 : 3)}"
        ], _telemetryLog);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/56431")]
    public async Task Capabilities_NoTypesEmitted()
    {
        var sourceV1 = """

            /* GENERATE:
            class G
            {
                int M()
                {
                    return 1;
                }
            }
            */

            """;
        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, sourceV1, generator: generator);

        var moduleId = EmitLibrary(document1.Project.Id, sourceV1, generatorProject: document1.Project);
        LoadLibraryToDebuggee(moduleId);

        // attached to processes that doesn't allow creating new types
        _debuggerService.GetCapabilitiesImpl = () => ["Baseline"];

        var debuggingSession = StartDebuggingSession(service, solution);

        // change the source
        solution = solution.WithDocumentText(document1.Id, CreateText("""

            /* GENERATE:
            class G
            {
                // a change that won't cause a type to be emitted
                int M()
                {
                    return 1;
                }
            }
            */

            """));

        // validate solution update status and emit
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check that no types have been updated. this used to throw
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.UpdatedTypes);

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task Capabilities_SynthesizedNewType()
    {
        var source1 = "class C { void M() { } }";
        using var _ = CreateWorkspace(out var solution, out var service);
        solution = AddDefaultTestProject(solution, [source1]);
        var project = solution.Projects.Single();
        solution = solution.WithProjectParseOptions(project.Id, new CSharpParseOptions(LanguageVersion.CSharp10));
        var documentId = solution.Projects.Single().Documents.Single().Id;

        EmitAndLoadLibraryToDebuggee(project.Id, source1);

        // attached to processes that doesn't allow creating new types
        _debuggerService.GetCapabilitiesImpl = () => ["Baseline"];

        // F5
        var debuggingSession = StartDebuggingSession(service, solution);

        // update document:
        solution = solution.WithDocumentText(documentId, CreateText("class C { void M() { var x = new { Goo = 1 }; } }"));
        var document2 = solution.Projects.Single().Documents.Single();

        // These errors aren't reported as document diagnostics
        var diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        // They are reported as emit diagnostics
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        AssertEx.Equal([$"proj: <no location>: Error ENC1007: {FeaturesResources.ChangesRequiredSynthesizedType}"], InspectDiagnostics(results.Diagnostics));

        // no emitted delta:
        Assert.Empty(results.ModuleUpdates.Updates);

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    [WorkItem("https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems/edit/2631743")]
    public async Task Capabilities_ExplicitInterfaceImplementation()
    {
        var source1 = "class C { System.Collections.Generic.IEnumerable<int> M() => [1]; }";
        var source2 = "class C { System.Collections.Generic.IEnumerable<int> M() => [2]; }";

        using var _ = CreateWorkspace(out var solution, out var service);
        solution = AddDefaultTestProject(solution, [source1]);
        var project = solution.Projects.Single();
        var documentId = solution.Projects.Single().Documents.Single().Id;

        EmitAndLoadLibraryToDebuggee(project.Id, source1);

        // attached to processes that doesn't allow creating new types
        _debuggerService.GetCapabilitiesImpl = () => EditAndContinueTestVerifier.NetFrameworkCapabilities.ToStringArray();

        // F5
        var debuggingSession = StartDebuggingSession(service, solution);

        // update document:
        solution = solution.WithDocumentText(documentId, CreateText(source2));
        var document2 = solution.Projects.Single().Documents.Single();

        // The error isn't reported as document diagnostic:
        var diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        // The error is reported as emit diagnostic:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        AssertEx.Equal([$"proj: <no location>: Error CS9346: {CSharpResources.ERR_EncUpdateRequiresEmittingExplicitInterfaceImplementationNotSupportedByTheRuntime}"], InspectDiagnostics(results.Diagnostics));

        // no emitted delta:
        Assert.Empty(results.ModuleUpdates.Updates);

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidSignificantChange_EmitError()
    {
        var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

        using var _ = CreateWorkspace(out var solution, out var service);

        (solution, var document) = AddDefaultTestProject(solution, sourceV1);
        EmitAndLoadLibraryToDebuggee(document.Project.Id, sourceV1);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the source (valid edit but passing no encoding to emulate emit error):
        var document1 = solution.Projects.Single().Documents.Single();
        solution = solution.WithDocumentText(document1.Id, SourceText.From("class C1 { void M() { System.Console.WriteLine(2); } }", encoding: null, SourceHashAlgorithms.Default));
        var document2 = solution.Projects.Single().Documents.Single();

        var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics1);

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        AssertEx.Equal([$"proj: {document2.FilePath}: (0,0)-(0,54): Error CS8055: {string.Format(CSharpResources.ERR_EncodinglessSyntaxTree)}"], InspectDiagnostics(results.Diagnostics));

        // no emitted delta:
        Assert.Empty(results.ModuleUpdates.Updates);

        // no pending update:
        Assert.Null(debuggingSession.GetTestAccessor().GetPendingSolutionUpdate());

        Assert.Throws<InvalidOperationException>(() => debuggingSession.CommitSolutionUpdate());
        Assert.Throws<InvalidOperationException>(() => debuggingSession.DiscardSolutionUpdate());

        // no change in non-remappable regions since we didn't have any active statements:
        Assert.Empty(debuggingSession.EditSession.NonRemappableRegions);

        // solution update status after discarding an update (still has update ready):
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Blocked, results.ModuleUpdates.Status);
        AssertEx.Equal([$"proj: {document2.FilePath}: (0,0)-(0,54): Error CS8055: {string.Format(CSharpResources.ERR_EncodinglessSyntaxTree)}"], InspectDiagnostics(results.Diagnostics));

        EndDebuggingSession(debuggingSession);

        AssertEx.Equal(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1",
            "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines=",
            "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=CS8055"
        ], _telemetryLog);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidSignificantChange_ApplyBeforeFileWatcherEvent(bool saveDocument)
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
        var sourceFile = dir.CreateFile("test.cs").WriteAllText(source1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
        var document1 = solution.
            AddTestProject("test").
            AddDocument("test.cs", CreateText("class C1 { void M() { System.Console.WriteLine(0); } }"), filePath: sourceFile.Path);

        var documentId = document1.Id;
        solution = document1.Project.Solution;

        var sourceTextProvider = new MockPdbMatchingSourceTextProvider()
        {
            TryGetMatchingSourceTextImpl = (filePath, requiredChecksum, checksumAlgorithm) =>
            {
                Assert.Equal(sourceFile.Path, filePath);
                AssertEx.Equal(requiredChecksum, CreateText(source1).GetChecksum());
                Assert.Equal(SourceHashAlgorithms.Default, checksumAlgorithm);

                return source1;
            }
        };

        var moduleId = EmitAndLoadLibraryToDebuggee(documentId.ProjectId, source1, sourceFilePath: sourceFile.Path);

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None, sourceTextProvider);

        EnterBreakState(debuggingSession);

        // The user opens the source file and changes the source before Roslyn receives file watcher event.
        var source2 = "class C1 { void M() { System.Console.WriteLine(2); } }";
        solution = solution.WithDocumentText(documentId, CreateText(source2));
        var document2 = solution.GetDocument(documentId);

        // Save the document:
        if (saveDocument)
        {
            sourceFile.WriteAllText(source2, Encoding.UTF8);
        }

        // EnC service queries for a document, which triggers read of the source file from disk.
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);

        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        CommitSolutionUpdate(debuggingSession);

        ExitBreakState(debuggingSession);

        EnterBreakState(debuggingSession);

        // file watcher updates the workspace:
        solution = solution.WithDocumentText(documentId, CreateTextFromFile(sourceFile.Path));
        var document3 = solution.Projects.Single().Documents.Single();

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);

        if (saveDocument)
        {
            Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        }
        else
        {
            Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
            debuggingSession.DiscardSolutionUpdate();
        }

        ExitBreakState(debuggingSession);
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidSignificantChange_FileUpdateNotObservedBeforeDebuggingSessionStart()
    {
        // workspace:     --V0--------------V2-------|--------V3------------------V1--------------|
        // file system:   --V0---------V1-----V2-----|------------------------------V1------------|
        //                   \--build--/      ^save  F5   ^      ^F10 (no change)   ^save         F10 (ok)
        //                                                file watcher: no-op
        // build updates file from V0 -> V1

        var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
        var source2 = "class C1 { void M() { System.Console.WriteLine(2); } }";
        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("test.cs").WriteAllText(source2, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
        var document2 = solution.
            AddTestProject("test").
            AddDocument("test.cs", CreateText(source2), filePath: sourceFile.Path);

        var documentId = document2.Id;

        var project = document2.Project;
        solution = project.Solution;

        var moduleId = EmitAndLoadLibraryToDebuggee(project.Id, source1, sourceFilePath: sourceFile.Path);

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);

        EnterBreakState(debuggingSession);

        // user edits the file:
        solution = solution.WithDocumentText(documentId, CreateText("class C1 { void M() { System.Console.WriteLine(3); } }"));
        var document3 = solution.Projects.Single().Documents.Single();

        // EnC service queries for a document, but the source file on disk doesn't match the PDB

        // We don't report rude edits for out-of-sync documents:
        var diagnostics = await service.GetDocumentDiagnosticsAsync(document3, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        // since the document is out-of-sync we need to call update to determine whether we have changes to apply or not:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);

        // TODO: warning reported https://github.com/dotnet/roslyn/issues/78125
        // AssertEx.Equal([$"test.csproj: (0,0)-(0,0): Warning ENC1005: {string.Format(FeaturesResources.DocumentIsOutOfSyncWithDebuggee, sourceFile.Path)}"], InspectDiagnostics(results.Diagnostics));

        // undo:
        solution = solution.WithDocumentText(documentId, CreateText(source1));

        var currentDocument = solution.GetRequiredDocument(documentId);

        // save (note that this call will fail to match the content with the PDB since it uses the content prior to the actual file write)
        // TODO: await debuggingSession.OnSourceFileUpdatedAsync(currentDocument);
        var (doc, state) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(currentDocument, CancellationToken.None);
        Assert.Null(doc);
        Assert.Equal(CommittedSolution.DocumentState.OutOfSync, state);
        sourceFile.WriteAllText(source1, Encoding.UTF8);

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        AssertEx.Equal(
        [
            $"test: {document3.FilePath}: (0,0)-(0,0): Warning ENC1008: {string.Format(FeaturesResources.Changing_source_file_0_in_a_stale_project_has_no_effect_until_the_project_is_rebuit, sourceFile.Path)}"
        ], InspectDiagnostics(results.Diagnostics));

        // the content actually hasn't changed:
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidSignificantChange_AddedFileNotObservedBeforeDebuggingSessionStart()
    {
        // workspace:     ------|----V0---------------|----------
        // file system:   --V0--|---------------------|----------
        //                      F5   ^          ^F10 (no change)
        //                           file watcher observes the file

        var source1 = "class C1 { void M() { System.Console.WriteLine(1); } }";

        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("test.cs").WriteAllText(source1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        // the workspace starts with no file
        var project = solution.
            AddProject("test", "test", LanguageNames.CSharp).
            AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40));

        solution = project.Solution;

        var moduleId = EmitAndLoadLibraryToDebuggee(project.Id, source1, sourceFilePath: sourceFile.Path);

        _debuggerService.IsEditAndContinueAvailable = _ => new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.Attach, localizedMessage: "*attached*");

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);

        // An active statement may be present in the added file since the file exists in the PDB:
        var activeInstruction1 = new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 1);
        var activeSpan1 = GetSpan(source1, "System.Console.WriteLine(1);");
        var sourceText1 = CreateText(source1);
        var activeLineSpan1 = sourceText1.Lines.GetLinePositionSpan(activeSpan1);
        var activeStatements = ImmutableArray.Create(
            new ManagedActiveStatementDebugInfo(
                activeInstruction1,
                "test.cs",
                activeLineSpan1.ToSourceSpan(),
                ActiveStatementFlags.LeafFrame));

        // disallow any edits (attach scenario)
        EnterBreakState(debuggingSession, activeStatements);

        // File watcher observes the document and adds it to the workspace:

        var document1 = project.AddDocument("test.cs", sourceText1, filePath: sourceFile.Path);
        solution = document1.Project.Solution;

        // We don't report rude edits for the added document:
        var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        // TODO: https://github.com/dotnet/roslyn/issues/49938
        // We currently create the AS map against the committed solution, which may not contain all documents.
        // var spans = await service.GetBaseActiveStatementSpansAsync(solution, ImmutableArray.Create(document1.Id), CancellationToken.None);
        // AssertEx.Equal(new[] { $"({activeLineSpan1}, LeafFrame)" }, spans.Single().Select(s => s.ToString()));

        // No changes.
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);

        AssertEx.Empty(results.Diagnostics);

        EndDebuggingSession(debuggingSession);
    }

    [Theory, CombinatorialData]
    public async Task ValidSignificantChange_DocumentOutOfSync(bool delayLoad)
    {
        var sourceOnDisk = "class C1 { void M() { System.Console.WriteLine(1); } }";

        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("test.cs").WriteAllText(sourceOnDisk, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var service);

        // the workspace starts with a version of the source that's not updated with the output of single file generator (or design-time build):
        var document1 = solution.
            AddProject("test", "test", LanguageNames.CSharp).
            AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
            AddDocument("test.cs", CreateText("class C1 { void M() { System.Console.WriteLine(0); } }"), filePath: sourceFile.Path);

        var project = document1.Project;
        solution = project.Solution;

        var moduleId = EmitLibrary(project.Id, sourceOnDisk, sourceFilePath: sourceFile.Path);

        if (!delayLoad)
        {
            LoadLibraryToDebuggee(moduleId);
        }

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);

        EnterBreakState(debuggingSession);

        // no changes have been made to the project
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.ModuleUpdates.Updates);
        Assert.Empty(results.Diagnostics);

        // a file watcher observed a change and updated the document, so it now reflects the content on disk (the code that we compiled):
        solution = solution.WithDocumentText(document1.Id, CreateText(sourceOnDisk));
        var document3 = solution.Projects.Single().Documents.Single();

        var diagnostics = await service.GetDocumentDiagnosticsAsync(document3, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics);

        // the content of the file is now exactly the same as the compiled document, so there is no change to be applied:
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);

        EndDebuggingSession(debuggingSession);

        Assert.Empty(debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());
    }

    [Theory, CombinatorialData]
    public async Task ValidSignificantChange_EmitSuccessful(bool breakMode, bool commitUpdate)
    {
        var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, sourceV1);

        var moduleId = EmitAndLoadLibraryToDebuggee(document1.Project.Id, sourceV1);

        var debuggingSession = StartDebuggingSession(service, solution);

        if (breakMode)
        {
            EnterBreakState(debuggingSession);
        }

        // change the source (valid edit):
        solution = solution.WithDocumentText(document1.Id, CreateText("class C1 { void M() { System.Console.WriteLine(2); } }"));
        var document2 = solution.GetDocument(document1.Id);

        var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics1);

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        ValidateDelta(results.ModuleUpdates.Updates.Single());

        void ValidateDelta(ManagedHotReloadUpdate delta)
        {
            // check emitted delta:
            Assert.Empty(delta.ActiveStatements);
            Assert.NotEmpty(delta.ILDelta);
            Assert.NotEmpty(delta.MetadataDelta);
            Assert.NotEmpty(delta.PdbDelta);
            Assert.Equal(0x06000001, delta.UpdatedMethods.Single());
            Assert.Equal(0x02000002, delta.UpdatedTypes.Single());
            Assert.Equal(moduleId, delta.Module);
            Assert.Empty(delta.ExceptionRegions);
            Assert.Empty(delta.SequencePoints);
        }

        // the update should be stored on the service:
        var pendingUpdate = debuggingSession.GetTestAccessor().GetPendingSolutionUpdate();
        var newBaseline = pendingUpdate.ProjectBaselines.Single();
        AssertEx.Equal(results.ModuleUpdates.Updates, pendingUpdate.Deltas);
        Assert.Equal(document2.Project.Id, newBaseline.ProjectId);
        Assert.Equal(moduleId, newBaseline.EmitBaseline.OriginalMetadata.GetModuleVersionId());

        var readers = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
        Assert.Equal(2, readers.Length);
        Assert.NotNull(readers[0]);
        Assert.NotNull(readers[1]);

        if (commitUpdate)
        {
            // all update providers either provided updates or had no change to apply:
            CommitSolutionUpdate(debuggingSession);

            Assert.Null(debuggingSession.GetTestAccessor().GetPendingSolutionUpdate());

            // no change in non-remappable regions since we didn't have any active statements:
            Assert.Empty(debuggingSession.EditSession.NonRemappableRegions);

            var baselineReaders = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
            Assert.Equal(2, baselineReaders.Length);
            Assert.Same(readers[0], baselineReaders[0]);
            Assert.Same(readers[1], baselineReaders[1]);

            // verify that baseline is added:
            Assert.Same(newBaseline.EmitBaseline, debuggingSession.GetTestAccessor().GetProjectBaselines(document2.Project.Id).Single().EmitBaseline);

            // solution update status after committing an update:
            results = await EmitSolutionUpdateAsync(debuggingSession, solution);
            Assert.Empty(results.Diagnostics);
            Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        }
        else
        {
            // another update provider blocked the update:
            debuggingSession.DiscardSolutionUpdate();

            Assert.Null(debuggingSession.GetTestAccessor().GetPendingSolutionUpdate());

            // solution update status after committing an update:
            results = await EmitSolutionUpdateAsync(debuggingSession, solution);
            Assert.Empty(results.Diagnostics);
            Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

            ValidateDelta(results.ModuleUpdates.Updates.Single());
            debuggingSession.DiscardSolutionUpdate();
        }

        if (breakMode)
        {
            ExitBreakState(debuggingSession);
        }

        EndDebuggingSession(debuggingSession);

        // open module readers should be disposed when the debugging session ends:
        VerifyReadersDisposed(readers);

        AssertEx.SetEqual([moduleId], debuggingSession.GetTestAccessor().GetModulesPreparedForUpdate());

        if (breakMode)
        {
            AssertEx.SequenceEqual(
            [
                $"Debugging_EncSession: SolutionSessionId={{00000000-AAAA-AAAA-AAAA-000000000000}}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount={(commitUpdate ? 3 : 2)}",
                $"Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges={(commitUpdate ? "{6A6F7270-0000-4000-8000-000000000000}" : "")}|ProjectIdsWithUpdatedBaselines=",
            ], _telemetryLog);
        }
        else
        {
            AssertEx.SequenceEqual(
            [
                $"Debugging_EncSession: SolutionSessionId={{00000000-AAAA-AAAA-AAAA-000000000000}}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=1|EmptyHotReloadSessionCount={(commitUpdate ? 1 : 0)}",
                $"Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=0|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges={(commitUpdate ? "{6A6F7270-0000-4000-8000-000000000000}" : "")}|ProjectIdsWithUpdatedBaselines="
            ], _telemetryLog);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidSignificantChange_EmitSuccessful_UpdateDeferred(bool commitUpdate)
    {
        var dir = Temp.CreateDirectory();

        var sourceV1 = "class C1 { void M1() { int a = 1; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(1); } }";
        var compilationV1 = CSharpTestBase.CreateCompilation(sourceV1, parseOptions: TestOptions.Regular.WithNoRefSafetyRulesAttribute(), options: TestOptions.DebugDll, targetFramework: DefaultTargetFramework, assemblyName: "lib");

        var (peImage, pdbImage) = compilationV1.EmitToArrays(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
        var moduleMetadata = ModuleMetadata.CreateFromImage(peImage);
        var moduleFile = dir.CreateFile("lib.dll").WriteAllBytes(peImage);
        var pdbFile = dir.CreateFile("lib.pdb").WriteAllBytes(pdbImage);
        var moduleId = moduleMetadata.GetModuleVersionId();

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, sourceV1);

        _mockCompilationOutputs.Add(document1.Project.Id, new CompilationOutputFiles(moduleFile.Path, pdbFile.Path));

        // set up an active statement in the first method, so that we can test preservation of local signature.
        var activeStatements = ImmutableArray.Create(new ManagedActiveStatementDebugInfo(
            new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 0),
            documentName: document1.Name,
            sourceSpan: new SourceSpan(0, 15, 0, 16),
            ActiveStatementFlags.LeafFrame));

        var debuggingSession = StartDebuggingSession(service, solution);

        // module is not loaded:
        EnterBreakState(debuggingSession, activeStatements);

        // change the source (valid edit):
        solution = solution.WithDocumentText(document1.Id, CreateText("class C1 { void M1() { int a = 1; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }"));
        var document2 = solution.GetDocument(document1.Id);

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);

        // delta to apply:
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.ActiveStatements);
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Equal(0x06000002, delta.UpdatedMethods.Single());
        Assert.Equal(0x02000002, delta.UpdatedTypes.Single());
        Assert.Equal(moduleId, delta.Module);
        Assert.Empty(delta.ExceptionRegions);
        Assert.Empty(delta.SequencePoints);

        // the update should be stored on the service:
        var pendingUpdate = debuggingSession.GetTestAccessor().GetPendingSolutionUpdate();
        var newBaseline = pendingUpdate.ProjectBaselines.Single();

        var readers = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
        Assert.Equal(2, readers.Length);
        Assert.NotNull(readers[0]);
        Assert.NotNull(readers[1]);

        Assert.Equal(document2.Project.Id, newBaseline.ProjectId);
        Assert.Equal(moduleId, newBaseline.EmitBaseline.OriginalMetadata.GetModuleVersionId());

        if (commitUpdate)
        {
            CommitSolutionUpdate(debuggingSession);
            Assert.Null(debuggingSession.GetTestAccessor().GetPendingSolutionUpdate());

            // no change in non-remappable regions since we didn't have any active statements:
            Assert.Empty(debuggingSession.EditSession.NonRemappableRegions);

            // verify that baseline is added:
            Assert.Same(newBaseline.EmitBaseline, debuggingSession.GetTestAccessor().GetProjectBaselines(document2.Project.Id).Single().EmitBaseline);

            // solution update status after committing an update:
            ExitBreakState(debuggingSession);

            // make another update:
            EnterBreakState(debuggingSession);

            // Update M1 - this method has an active statement, so we will attempt to preserve the local signature.
            // Since the method hasn't been edited before we'll read the baseline PDB to get the signature token.
            // This validates that the Portable PDB reader can be used (and is not disposed) for a second generation edit.
            var document3 = solution.GetDocument(document1.Id);
            solution = solution.WithDocumentText(document3.Id, CreateText("class C1 { void M1() { int a = 3; System.Console.WriteLine(a); } void M2() { System.Console.WriteLine(2); } }"));

            results = await EmitSolutionUpdateAsync(debuggingSession, solution);
            Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
            Assert.Empty(results.Diagnostics);
            debuggingSession.DiscardSolutionUpdate();
        }
        else
        {
            debuggingSession.DiscardSolutionUpdate();
            Assert.Null(debuggingSession.GetTestAccessor().GetPendingSolutionUpdate());
        }

        ExitBreakState(debuggingSession);
        EndDebuggingSession(debuggingSession);

        // open module readers should be disposed when the debugging session ends:
        VerifyReadersDisposed(readers);
    }

    [Fact]
    public async Task ValidSignificantChange_PartialTypes()
    {
        var sourceA1 = """

            partial class C { int X = 1; void F() { X = 1; } }

            partial class D { int U = 1; public D() { } }
            partial class D { int W = 1; }

            partial class E { int A; public E(int a) { A = a; } }

            """;
        var sourceB1 = """

            partial class C { int Y = 1; }
            partial class E { int B; public E(int a, int b) { A = a; B = new System.Func<int>(() => b)(); } }

            """;
        using var _ = CreateWorkspace(out var solution, out var service);
        solution = AddDefaultTestProject(solution, [sourceA1, sourceB1]);
        var project = solution.Projects.Single();

        LoadLibraryToDebuggee(EmitLibrary(project.Id, [(sourceA1, "test1.cs"), (sourceB1, "test2.cs")]));

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the source (valid edit):
        var documentA = project.Documents.First();
        var documentB = project.Documents.Skip(1).First();
        solution = solution.WithDocumentText(documentA.Id, CreateText("""

            partial class C { int X = 2; void F() { X = 2; } }

            partial class D { int U = 2; }
            partial class D { int W = 2; public D() { } }

            partial class E { int A = 1; public E(int a) { A = a; } }

            """));
        solution = solution.WithDocumentText(documentB.Id, CreateText("""

            partial class C { int Y = 2; }
            partial class E { int B = 2; public E(int a, int b) { A = a; B = new System.Func<int>(() => b)(); } }

            """));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check emitted delta:
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.ActiveStatements);
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Equal(6, delta.UpdatedMethods.Length);  // F, C.C(), D.D(), E.E(int), E.E(int, int), lambda
        AssertEx.SetEqual([0x02000002, 0x02000003, 0x02000004, 0x02000005], delta.UpdatedTypes, itemInspector: t => "0x" + t.ToString("X"));

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78244")]
    public async Task MultiProjectUpdates_ValidSignificantChange_RudeEdit()
    {
        using var _ = CreateWorkspace(out var solution, out var service);

        solution = solution
            .AddTestProject("A", out var projectAId)
                .AddTestDocument("""
            using System;

            class A
            {
                static void F()
                {
                    Console.WriteLine(1);
                }
            }
            """, "A.cs", out var documentAId).Project.Solution
            .AddTestProject("B", out var projectBId)
                .AddTestDocument("""
            using System;
            
            interface I
            {
            }
            """, "B.cs", out var documentBId).Project.Solution;

        EmitAndLoadLibraryToDebuggee(solution.GetRequiredDocument(documentAId));
        EmitAndLoadLibraryToDebuggee(solution.GetRequiredDocument(documentBId));

        var debuggingSession = StartDebuggingSession(service, solution);

        // change the source (valid edit in A and rude edit in B):
        solution = solution
            .WithDocumentText(documentAId, CreateText("""
            using System;
            class A
            {
                static void F()
                {
                    Console.WriteLine(2);
                }
            }
            """))
            .WithDocumentText(documentBId, CreateText("""
            using System;

            interface I
            {
                void F() {}
            }
            """));

        // Rude Edit reported:
        var documentB = solution.GetRequiredDocument(documentBId);
        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(documentB, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(
            [$"{documentB.FilePath}: (4,4)-(4,12): Error ENC0023: {string.Format(FeaturesResources.Adding_an_abstract_0_or_overriding_an_inherited_0_requires_restarting_the_application, FeaturesResources.method)}"],
            InspectDiagnostics(docDiagnostics));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);

        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        AssertEx.SequenceEqual(["ENC0023"], InspectDiagnosticIds(results.Diagnostics));

        AssertEx.SetEqual([documentBId.ProjectId], results.ProjectsToRebuild);
        AssertEx.SetEqual([documentBId.ProjectId], results.ProjectsToRestart.Keys);

        var delta = results.ModuleUpdates.Updates.Single();
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Equal(1, delta.UpdatedMethods.Length);

        debuggingSession.DiscardSolutionUpdate();

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/78244")]
    public async Task MultiProjectUpdates_ValidSignificantChange_NoEffectEdit()
    {
        using var _ = CreateWorkspace(out var solution, out var service);

        solution = solution
            .AddTestProject("A", out var projectAId)
                .AddTestDocument("""
            class A
            {
                static void F()
                {
                    System.Console.WriteLine(1);
                }
            }
            """, "A.cs", out var documentAId).Project.Solution
            .AddTestProject("B", out var projectBId)
                .AddTestDocument("""
            class B
            {
                static B()
                {
                    System.Console.WriteLine(10);
                }
            }
            """, "B.cs", out var documentBId).Project.Solution;

        EmitAndLoadLibraryToDebuggee(solution.GetRequiredDocument(documentAId));
        EmitAndLoadLibraryToDebuggee(solution.GetRequiredDocument(documentBId));

        var debuggingSession = StartDebuggingSession(service, solution);

        // change the source (valid edit in A and no-effect edit in B):
        solution = solution
            .WithDocumentText(documentAId, CreateText("""
            class A
            {
                static void F()
                {
                    System.Console.WriteLine(2);
                }
            }
            """))
            .WithDocumentText(documentBId, CreateText("""
            class B
            {
                static B()
                {
                    System.Console.WriteLine(20);
                }
            }
            """));

        // no-effect warning reported:
        var documentB = solution.GetRequiredDocument(documentBId);
        var diagnostics = await service.GetDocumentDiagnosticsAsync(documentB, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(
            [$"{documentB.FilePath}: (2,4)-(2,14): Warning ENC0118: {string.Format(FeaturesResources.Changing_0_might_not_have_any_effect_until_the_application_is_restarted, FeaturesResources.static_constructor)}"],
            InspectDiagnostics(diagnostics));

        // TODO: Set RestartWhenChangesHaveNoEffect=true and AllowPartialUpdate=true
        // https://github.com/dotnet/roslyn/issues/78244
        var runningProjects = ImmutableDictionary<ProjectId, RunningProjectOptions>.Empty
            .Add(projectAId, new RunningProjectOptions() { RestartWhenChangesHaveNoEffect = false })
            .Add(projectBId, new RunningProjectOptions() { RestartWhenChangesHaveNoEffect = false });

        // emit updates:
        var results = await debuggingSession.EmitSolutionUpdateAsync(solution, runningProjects, s_noActiveSpans, CancellationToken.None);

        AssertEx.SetEqual([], results.ProjectsToRestart.Select(p => p.Key.DebugName));
        AssertEx.SequenceEqual(["ENC0118"], InspectDiagnosticIds(results.GetAllDiagnostics()));
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check emitted delta:
        Assert.Equal(2, results.ModuleUpdates.Updates.Length);

        // Process will be restarted, so discard all updates:
        debuggingSession.DiscardSolutionUpdate();

        EndDebuggingSession(debuggingSession);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/72331")]
    internal async Task ValidSignificantChange_SourceGenerators_DocumentUpdate_GeneratedDocumentUpdate(SourceGeneratorExecutionPreference executionPreference)
    {
        var sourceV1 = """

            /* GENERATE: file class G { int X => 1; } */

            class C { int Y => 1; }

            """;
        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var workspace = CreateWorkspace(out var solution, out var service);
        var workspaceConfig = Assert.IsType<TestWorkspaceConfigurationService>(workspace.Services.GetRequiredService<IWorkspaceConfigurationService>());
        workspaceConfig.Options = new WorkspaceConfigurationOptions(executionPreference);

        (solution, var document1) = AddDefaultTestProject(solution, sourceV1, generator: generator);

        var moduleId = EmitLibrary(document1.Project.Id, sourceV1, generatorProject: document1.Project);
        LoadLibraryToDebuggee(moduleId);

        // Trigger initial source generation before debugging session starts.
        // Causes source generator to run on the solution for the first time.
        // Futher compilation access won't automatically trigger source generators,
        // the EnC service has to do so.
        _ = await solution.Projects.Single().GetCompilationAsync(CancellationToken.None);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the source (valid edit)
        solution = solution.WithDocumentText(document1.Id, CreateText("""

            /* GENERATE: file class G { int X => 2; } */

            class C { int Y => 2; }

            """));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check emitted delta:
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.ActiveStatements);
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Equal(2, delta.UpdatedMethods.Length);
        AssertEx.Equal([0x02000002, 0x02000003], delta.UpdatedTypes, itemInspector: t => "0x" + t.ToString("X"));

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/2169491")]
    internal async Task RudeEdit_SourceGenerators_DocumentUpdate_GeneratedDocumentAdd(SourceGeneratorExecutionPreference executionPreference)
    {
        var sourceV1 = """
            class C { int Y => 1; }
            """;
        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var workspace = CreateWorkspace(out var solution, out var service);
        var workspaceConfig = Assert.IsType<TestWorkspaceConfigurationService>(workspace.Services.GetRequiredService<IWorkspaceConfigurationService>());
        workspaceConfig.Options = new WorkspaceConfigurationOptions(executionPreference);

        (solution, var document1) = AddDefaultTestProject(solution, sourceV1, generator: generator);
        var projectId = document1.Project.Id;

        var moduleId = EmitLibrary(projectId, sourceV1, generatorProject: document1.Project);
        LoadLibraryToDebuggee(moduleId);

        // Trigger initial source generation before debugging session starts.
        // Causes source generator to run on the solution for the first time.
        // Futher compilation access won't automatically trigger source generators,
        // the EnC service has to do so.
        _ = await solution.Projects.Single().GetCompilationAsync(CancellationToken.None);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the source (valid edit)
        solution = solution.WithDocumentText(document1.Id, CreateText("""
            /* GENERATE: [assembly: System.Reflection.AssemblyMetadata("X", "Y")] */

            class C { int Y => 2; }
            """));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);

        var generatedFilePath = Path.Combine(
            TempRoot.Root,
            "Microsoft.CodeAnalysis.Test.Utilities",
            "Roslyn.Test.Utilities.TestGenerators.TestSourceGenerator",
            "Generated_test1.cs");

        AssertEx.Equal(
            [$"proj: {generatedFilePath}: (0,0)-(0,56): Error ENC0021: {string.Format(FeaturesResources.Adding_0_requires_restarting_the_application, FeaturesResources.attribute)}"],
            InspectDiagnostics(results.Diagnostics));

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidSignificantChange_SourceGenerators_DocumentUpdate_GeneratedDocumentUpdate_LineChanges()
    {
        var sourceV1 = """

            /* GENERATE:
            class G
            {
                int M()
                {
                    return 1;
                }
            }
            */

            """;
        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, sourceV1, generator: generator);

        var moduleId = EmitLibrary(document1.Project.Id, sourceV1, generatorProject: document1.Project);
        LoadLibraryToDebuggee(moduleId);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the source (valid edit):
        solution = solution.WithDocumentText(document1.Id, CreateText("""

            /* GENERATE:
            class G
            {

                int M()
                {
                    return 1;
                }
            }
            */

            """));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check emitted delta:
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.ActiveStatements);

        var lineUpdate = delta.SequencePoints.Single();
        AssertEx.Equal(["3 -> 4"], lineUpdate.LineUpdates.Select(edit => $"{edit.OldLine} -> {edit.NewLine}"));
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Empty(delta.UpdatedMethods);
        Assert.Empty(delta.UpdatedTypes);

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidSignificantChange_SourceGenerators_DocumentUpdate_GeneratedDocumentInsert()
    {
        var sourceV1 = """

            partial class C { int X = 1; }

            """;
        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, sourceV1, generator: generator);

        var moduleId = EmitLibrary(document1.Project.Id, sourceV1, generatorProject: document1.Project);
        LoadLibraryToDebuggee(moduleId);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the source (valid edit):
        solution = solution.WithDocumentText(document1.Id, CreateText("""

            /* GENERATE: partial class C { int Y = 2; } */

            partial class C { int X = 1; }

            """));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check emitted delta:
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.ActiveStatements);
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Equal(1, delta.UpdatedMethods.Length); // constructor update
        Assert.Equal(0x02000002, delta.UpdatedTypes.Single());

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidSignificantChange_SourceGenerators_AdditionalDocumentUpdate()
    {
        var source = """

            class C { int Y => 1; }

            """;

        var additionalSourceV1 = """

            /* GENERATE: class G { int X => 1; } */

            """;
        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, source, generator: generator, additionalFileText: additionalSourceV1);

        var moduleId = EmitLibrary(document.Project.Id, source, generatorProject: document.Project, additionalFileText: additionalSourceV1);
        LoadLibraryToDebuggee(moduleId);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the additional source (valid edit):
        var additionalDocument1 = solution.Projects.Single().AdditionalDocuments.Single();
        solution = solution.WithAdditionalDocumentText(additionalDocument1.Id, CreateText("""

            /* GENERATE: class G { int X => 2; } */

            """));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check emitted delta:
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.ActiveStatements);
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Equal(1, delta.UpdatedMethods.Length);
        Assert.Equal(0x02000003, delta.UpdatedTypes.Single());

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidSignificantChange_SourceGenerators_AnalyzerConfigUpdate()
    {
        var source = """

            class C { int Y => 1; }

            """;

        var configV1 = new[] { ("enc_generator_output", "1") };
        var configV2 = new[] { ("enc_generator_output", "2") };

        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, source, generator: generator, analyzerConfig: configV1);

        var moduleId = EmitLibrary(document.Project.Id, source, generatorProject: document.Project, analyzerOptions: configV1);
        LoadLibraryToDebuggee(moduleId);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // change the additional source (valid edit):
        var configDocument1 = solution.Projects.Single().AnalyzerConfigDocuments.Single();
        solution = solution.WithAnalyzerConfigDocumentText(configDocument1.Id, GetAnalyzerConfigText(configV2));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check emitted delta:
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.ActiveStatements);
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Equal(1, delta.UpdatedMethods.Length);
        Assert.Equal(0x02000003, delta.UpdatedTypes.Single());

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidSignificantChange_SourceGenerators_DocumentRemove()
    {
        var source1 = "";

        var generator = new TestSourceGenerator()
        {
            ExecuteImpl = context => context.AddSource("generated", $"class G {{ int X => {context.Compilation.SyntaxTrees.Count()}; }}")
        };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, source1, generator: generator);

        var moduleId = EmitLibrary(document1.Project.Id, source1, generatorProject: document1.Project);
        LoadLibraryToDebuggee(moduleId);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        // remove the source document (valid edit):
        solution = document1.Project.Solution.RemoveDocument(document1.Id);

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check emitted delta:
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.ActiveStatements);
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Equal(1, delta.UpdatedMethods.Length);
        Assert.Equal(0x02000002, delta.UpdatedTypes.Single());

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidInsignificantChange()
    {
        var source1 = "class C1 { void M() { /* System.Console.WriteLine(1); */ } }";
        var source2 = "class C1 { void M() { /* System.Console.WriteLine(2); */ } }";

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, source1);

        var moduleId = EmitAndLoadLibraryToDebuggee(document1.Project.Id, source1);

        var debuggingSession = StartDebuggingSession(service, solution);

        // change the source (valid insignificant edit):
        solution = solution.WithDocumentText(document1.Id, CreateText(source2));
        var document2 = solution.GetDocument(document1.Id);

        var diagnostics1 = await service.GetDocumentDiagnosticsAsync(document2, s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics1);

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);

        // solution has been updated:
        var text = await debuggingSession.LastCommittedSolution.GetRequiredProject(document1.Project.Id).GetRequiredDocument(document1.Id).GetTextAsync();
        Assert.Equal(source2, text.ToString());

        EndDebuggingSession(debuggingSession);

        AssertEx.SequenceEqual(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=0|EmptySessionCount=0|HotReloadSessionCount=1|EmptyHotReloadSessionCount=0",
            "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=False|HadValidInsignificantChanges=True|RudeEditsCount=0|EmitDeltaErrorIdCount=0|InBreakState=False|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines="
        ], _telemetryLog);
    }

    [Fact]
    public async Task RudeEdit()
    {
        var source1 = "class C { void M() { } }";
        using var _ = CreateWorkspace(out var solution, out var service);
        solution = AddDefaultTestProject(solution, [source1]);
        var project = solution.Projects.Single();
        solution = solution.WithProjectParseOptions(project.Id, new CSharpParseOptions(LanguageVersion.CSharp10));
        var documentId = solution.Projects.Single().Documents.Single().Id;

        EmitAndLoadLibraryToDebuggee(project.Id, source1);

        // attached to processes that doesn't allow creating new types
        _debuggerService.GetCapabilitiesImpl = () => ["Baseline"];

        // F5
        var debuggingSession = StartDebuggingSession(service, solution);

        // update document:
        solution = solution.WithDocumentText(documentId, CreateText("class C { void M() { var x = new { Goo = 1 }; } }"));
        var document2 = solution.Projects.Single().Documents.Single();

        // These errors aren't reported as document diagnostics
        var diagnostics = await service.GetDocumentDiagnosticsAsync(solution.GetDocument(documentId), s_noActiveSpans, CancellationToken.None);
        AssertEx.Empty(diagnostics);

        // They are reported as emit diagnostics
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        AssertEx.Equal([$"proj: <no location>: Error ENC1007: {FeaturesResources.ChangesRequiredSynthesizedType}"], InspectDiagnostics(results.Diagnostics));

        // no emitted delta:
        Assert.Empty(results.ModuleUpdates.Updates);

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
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

        var projectB = solution.AddTestProject("B").WithAssemblyName("A").
            AddDocument("DocB", source1, filePath: Path.Combine(TempRoot.Root, "DocB.cs")).Project;

        solution = projectB.Solution;

        _mockCompilationOutputs.Add(projectA.Id, new CompilationOutputFiles(moduleFileA.Path, pdbFileA.Path));
        _mockCompilationOutputs.Add(projectB.Id, new CompilationOutputFiles(moduleFileB.Path, pdbFileB.Path));

        // only module A is loaded
        LoadLibraryToDebuggee(moduleIdA);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession);

        //
        // First update.
        //

        solution = solution.WithDocumentText(projectA.Documents.Single().Id, CreateText(source2));
        solution = solution.WithDocumentText(projectB.Documents.Single().Id, CreateText(source2));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);

        var deltaA = results.ModuleUpdates.Updates.Single(d => d.Module == moduleIdA);
        var deltaB = results.ModuleUpdates.Updates.Single(d => d.Module == moduleIdB);
        Assert.Equal(2, results.ModuleUpdates.Updates.Length);

        // the update should be stored on the service:
        var pendingUpdate = debuggingSession.GetTestAccessor().GetPendingSolutionUpdate();
        var newBaselineA1 = pendingUpdate.ProjectBaselines.Single(b => b.ProjectId == projectA.Id).EmitBaseline;
        var newBaselineB1 = pendingUpdate.ProjectBaselines.Single(b => b.ProjectId == projectB.Id).EmitBaseline;

        var baselineA0 = newBaselineA1.GetInitialEmitBaseline();
        var baselineB0 = newBaselineB1.GetInitialEmitBaseline();

        var readers = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
        Assert.Equal(4, readers.Length);
        Assert.False(readers.Any(r => r is null));

        Assert.Equal(moduleIdA, newBaselineA1.OriginalMetadata.GetModuleVersionId());
        Assert.Equal(moduleIdB, newBaselineB1.OriginalMetadata.GetModuleVersionId());

        CommitSolutionUpdate(debuggingSession);
        Assert.Null(debuggingSession.GetTestAccessor().GetPendingSolutionUpdate());

        // no change in non-remappable regions since we didn't have any active statements:
        Assert.Empty(debuggingSession.EditSession.NonRemappableRegions);

        // verify that baseline is added for both modules:
        Assert.Same(newBaselineA1, debuggingSession.GetTestAccessor().GetProjectBaselines(projectA.Id).Single().EmitBaseline);
        Assert.Same(newBaselineB1, debuggingSession.GetTestAccessor().GetProjectBaselines(projectB.Id).Single().EmitBaseline);

        // solution update status after committing an update:results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        ExitBreakState(debuggingSession);
        EnterBreakState(debuggingSession);

        //
        // Second update.
        //

        solution = solution.WithDocumentText(projectA.Documents.Single().Id, CreateText(source3));
        solution = solution.WithDocumentText(projectB.Documents.Single().Id, CreateText(source3));

        // validate solution update status and emit:
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);

        deltaA = results.ModuleUpdates.Updates.Single(d => d.Module == moduleIdA);
        deltaB = results.ModuleUpdates.Updates.Single(d => d.Module == moduleIdB);
        Assert.Equal(2, results.ModuleUpdates.Updates.Length);

        // the update should be stored on the service:
        pendingUpdate = debuggingSession.GetTestAccessor().GetPendingSolutionUpdate();
        var newBaselineA2 = pendingUpdate.ProjectBaselines.Single(b => b.ProjectId == projectA.Id).EmitBaseline;
        var newBaselineB2 = pendingUpdate.ProjectBaselines.Single(b => b.ProjectId == projectB.Id).EmitBaseline;

        Assert.NotSame(newBaselineA1, newBaselineA2);
        Assert.NotSame(newBaselineB1, newBaselineB2);
        Assert.Same(baselineA0, newBaselineA2.GetInitialEmitBaseline());
        Assert.Same(baselineB0, newBaselineB2.GetInitialEmitBaseline());
        Assert.Same(baselineA0.OriginalMetadata, newBaselineA2.OriginalMetadata);
        Assert.Same(baselineB0.OriginalMetadata, newBaselineB2.OriginalMetadata);

        // no new module readers:
        var baselineReaders = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
        AssertEx.Equal(readers, baselineReaders);

        CommitSolutionUpdate(debuggingSession);
        Assert.Null(debuggingSession.GetTestAccessor().GetPendingSolutionUpdate());

        // no change in non-remappable regions since we didn't have any active statements:
        Assert.Empty(debuggingSession.EditSession.NonRemappableRegions);

        // module readers tracked:
        baselineReaders = debuggingSession.GetTestAccessor().GetBaselineModuleReaders();
        AssertEx.Equal(readers, baselineReaders);

        // verify that baseline is updated for both modules:
        Assert.Same(newBaselineA2, debuggingSession.GetTestAccessor().GetProjectBaselines(projectA.Id).Single().EmitBaseline);
        Assert.Same(newBaselineB2, debuggingSession.GetTestAccessor().GetProjectBaselines(projectB.Id).Single().EmitBaseline);

        // solution update status after committing an update:
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);

        ExitBreakState(debuggingSession);
        EndDebuggingSession(debuggingSession);

        // open deferred module readers should be dispose when the debugging session ends:
        VerifyReadersDisposed(readers);
    }

    /// <summary>
    /// Emulates update to Multi-TFM project where only one of the projects has up-to-date binaries.
    /// This may occur when projects sepecify SingleTargetBuildForStartupProjects msbuild property (e.g. MAUI).
    /// </summary>
    [Fact]
    public async Task MultiTargetedPartiallyBuiltProjects()
    {
        var dir = Temp.CreateDirectory();

        var source0 = "class A { void M() { System.Console.WriteLine(0); } }";
        var source1 = "class A { void M() { System.Console.WriteLine(1); } }";
        var source2 = "class A { void M() { System.Console.WriteLine(2); } }";
        var source3 = "class A { void M() { System.Console.WriteLine(3); } }";

        // Create two projects with a shared (linked) document.

        using var _ = CreateWorkspace(out var solution, out var service);

        var sourcePath = dir.CreateFile("Lib.cs").WriteAllText(source1, Encoding.UTF8).Path;

        var documentA = solution.AddTestProject("A", targetFramework: TargetFramework.NetStandard20).WithAssemblyName("A").
            AddDocument("Lib.cs", source1, filePath: sourcePath);

        var documentB = documentA.Project.Solution.AddTestProject("B", targetFramework: TargetFramework.Net90).WithAssemblyName("A").
            AddDocument("Lib.cs", source1, filePath: sourcePath);

        solution = documentB.Project.Solution;
        var projectAId = documentA.Project.Id;
        var projectBId = documentB.Project.Id;

        // target A is built with up-to-date source:
        var mvidA = EmitAndLoadLibraryToDebuggee(projectAId, source1, sourceFilePath: sourcePath, assemblyName: "A", targetFramework: TargetFramework.NetStandard20);

        // target B is built with stale source:
        var mvidB = EmitAndLoadLibraryToDebuggee(projectBId, source0, sourceFilePath: sourcePath, assemblyName: "A", targetFramework: TargetFramework.Net90);

        // force document checksum validation:
        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);

        EnterBreakState(debuggingSession);

        // update source file in the editor (do not save to file on disk):
        var text2 = CreateText(source2);
        solution = solution.WithDocumentText(documentA.Id, text2).WithDocumentText(documentB.Id, text2);

        // delta emitted only for up-to-date project
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);
        AssertEx.SequenceEqual([mvidA], results.ModuleUpdates.Updates.Select(u => u.Module));

        CommitSolutionUpdate(debuggingSession);

        // Stale project baselines should get discarded after committing the update.
        // Unlocks binaries and allows the user to rebuild the project.
        Assert.False(debuggingSession.GetTestAccessor().HasProjectEmitBaseline(projectBId));

        // update source file in the editor (source text is now matching the PDB of project B):
        var text0 = CreateText(source0);
        solution = solution.WithDocumentText(documentA.Id, text0).WithDocumentText(documentB.Id, text0);

        // both projects are up-to-date now, but B hasn't changed w.r.t. baseline:
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);
        AssertEx.SetEqual([mvidA], results.ModuleUpdates.Updates.Select(u => u.Module));

        CommitSolutionUpdate(debuggingSession);

        // update source file in the editor:
        solution = solution.WithDocumentText(documentA.Id, text2).WithDocumentText(documentB.Id, text2);

        // project B is considered stale until rebuilt (even though the document content matches now):
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);
        AssertEx.SetEqual([mvidA], results.ModuleUpdates.Updates.Select(u => u.Module));

        CommitSolutionUpdate(debuggingSession);

        // Save file to disk and rebuild project B.
        // Saving is required so that we can fetch the baseline content for the next delta calculation.
        File.WriteAllText(sourcePath, source2, Encoding.UTF8);
        var mvidB2 = EmitAndLoadLibraryToDebuggee(projectBId, source2, sourceFilePath: sourcePath, assemblyName: "A", targetFramework: TargetFramework.Net90);

        // no changes have been made:

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);

        // update source file in the editor:
        var text3 = CreateText(source3);
        solution = solution.WithDocumentText(documentA.Id, text3).WithDocumentText(documentB.Id, text3);

        // both modules updated now:
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        Assert.Empty(results.Diagnostics);
        AssertEx.SetEqual([mvidA, mvidB2], results.ModuleUpdates.Updates.Select(u => u.Module));

        CommitSolutionUpdate(debuggingSession);
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task MultiTargeted_AllTargetsStale()
    {
        var dir = Temp.CreateDirectory();

        var source0 = "class A { void M() { System.Console.WriteLine(0); } }";
        var source1 = "class A { void M() { System.Console.WriteLine(1); } }";
        var source2 = "class A { void M() { System.Console.WriteLine(2); } }";

        // Create two projects with a shared (linked) document.

        using var _ = CreateWorkspace(out var solution, out var service);

        var sourcePath = dir.CreateFile("Lib.cs").WriteAllText(source1, Encoding.UTF8).Path;

        var documentA = solution.AddTestProject("A", targetFramework: TargetFramework.NetStandard20).WithAssemblyName("A").
            AddDocument("Lib.cs", source1, filePath: sourcePath);

        var documentB = documentA.Project.Solution.AddTestProject("B", targetFramework: TargetFramework.Net90).WithAssemblyName("A").
            AddDocument("Lib.cs", source1, filePath: sourcePath);

        solution = documentB.Project.Solution;
        var projectAId = documentA.Project.Id;
        var projectBId = documentB.Project.Id;

        // target A is built with stale source:
        var mvidA = EmitAndLoadLibraryToDebuggee(projectAId, source0, sourceFilePath: sourcePath, assemblyName: "A", targetFramework: TargetFramework.NetStandard20);

        // target B is built with stale source:
        var mvidB = EmitAndLoadLibraryToDebuggee(projectBId, source0, sourceFilePath: sourcePath, assemblyName: "A", targetFramework: TargetFramework.Net90);

        // force document checksum validation:
        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.None);

        EnterBreakState(debuggingSession);

        // update source file in the editor:
        var text2 = CreateText(source2);
        solution = solution.WithDocumentText(documentA.Id, text2).WithDocumentText(documentB.Id, text2);

        // no updates
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Equal(ModuleUpdateStatus.None, results.ModuleUpdates.Status);
        AssertEx.Equal(
        [
            $"A: {documentA.FilePath}: (0,0)-(0,0): Warning ENC1008: {string.Format(FeaturesResources.Changing_source_file_0_in_a_stale_project_has_no_effect_until_the_project_is_rebuit, sourcePath)}",
            $"B: {documentB.FilePath}: (0,0)-(0,0): Warning ENC1008: {string.Format(FeaturesResources.Changing_source_file_0_in_a_stale_project_has_no_effect_until_the_project_is_rebuit, sourcePath)}"
        ], InspectDiagnostics(results.Diagnostics));

        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task ValidSignificantChange_BaselineCreationFailed_NoStream()
    {
        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, "class C1 { void M() { System.Console.WriteLine(1); } }");

        _mockCompilationOutputs.Add(document1.Project.Id, new MockCompilationOutputs(Guid.NewGuid())
        {
            OpenPdbStreamImpl = () => null,
            OpenAssemblyStreamImpl = () => null,
        });

        var debuggingSession = StartDebuggingSession(service, solution);

        // module not loaded
        EnterBreakState(debuggingSession);

        // change the source (valid edit):
        solution = solution.WithDocumentText(document1.Id, CreateText("class C1 { void M() { System.Console.WriteLine(2); } }"));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        AssertEx.Equal(
            [$"proj: <no location>: Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "test-pdb", new FileNotFoundException().Message)}"],
            InspectDiagnostics(results.Diagnostics));

        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
    }

    [Fact]
    public async Task ValidSignificantChange_BaselineCreationFailed_AssemblyReadError()
    {
        var sourceV1 = "class C1 { void M() { System.Console.WriteLine(1); } }";
        var compilationV1 = CSharpTestBase.CreateCompilationWithMscorlib40(sourceV1, options: TestOptions.DebugDll, assemblyName: "lib");

        var pdbStream = new MemoryStream();
        var peImage = compilationV1.EmitToArray(new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
        pdbStream.Position = 0;

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, sourceV1);

        _mockCompilationOutputs.Add(document.Project.Id, new MockCompilationOutputs(Guid.NewGuid())
        {
            OpenPdbStreamImpl = () => pdbStream,
            OpenAssemblyStreamImpl = () => throw new IOException("*message*"),
        });

        var debuggingSession = StartDebuggingSession(service, solution);

        // module not loaded
        EnterBreakState(debuggingSession);

        // change the source (valid edit):
        var document1 = solution.Projects.Single().Documents.Single();
        solution = solution.WithDocumentText(document1.Id, CreateText("class C1 { void M() { System.Console.WriteLine(2); } }"));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        AssertEx.Equal(
            [$"proj: <no location>: Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "test-assembly", "*message*")}"],
            InspectDiagnostics(results.Diagnostics));

        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);
        debuggingSession.DiscardSolutionUpdate();

        EndDebuggingSession(debuggingSession);

        AssertEx.Equal(
        [
            "Debugging_EncSession: SolutionSessionId={00000000-AAAA-AAAA-AAAA-000000000000}|SessionId=1|SessionCount=1|EmptySessionCount=0|HotReloadSessionCount=0|EmptyHotReloadSessionCount=1",
            "Debugging_EncSession_EditSession: SessionId=1|EditSessionId=2|HadCompilationErrors=False|HadRudeEdits=False|HadValidChanges=True|HadValidInsignificantChanges=False|RudeEditsCount=0|EmitDeltaErrorIdCount=1|InBreakState=True|Capabilities=31|ProjectIdsWithAppliedChanges=|ProjectIdsWithUpdatedBaselines=",
            "Debugging_EncSession_EditSession_EmitDeltaErrorId: SessionId=1|EditSessionId=2|ErrorId=ENC1001"
        ], _telemetryLog);
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
        var sourceTextV2 = CreateText(sourceV2);

        var activeLineSpan11 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan11);
        var activeLineSpan12 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan12);
        var activeLineSpan21 = sourceTextV2.Lines.GetLinePositionSpan(activeSpan21);
        var activeLineSpan22 = sourceTextV2.Lines.GetLinePositionSpan(activeSpan22);
        var adjustedActiveLineSpan1 = sourceTextV2.Lines.GetLinePositionSpan(adjustedActiveSpan1);
        var adjustedActiveLineSpan2 = sourceTextV2.Lines.GetLinePositionSpan(adjustedActiveSpan2);

        var debuggingSession = StartDebuggingSession(service, solution);

        // default if not called in a break state
        Assert.True((await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [document1.Id], CancellationToken.None)).IsDefault);

        var moduleId = Guid.NewGuid();
        var activeInstruction1 = new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000001, version: 1), ilOffset: 1);
        var activeInstruction2 = new ManagedInstructionId(new ManagedMethodId(moduleId, token: 0x06000002, version: 1), ilOffset: 1);

        var activeStatements = ImmutableArray.Create(
            new ManagedActiveStatementDebugInfo(
                activeInstruction1,
                documentPath,
                activeLineSpan11.ToSourceSpan(),
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame),
            new ManagedActiveStatementDebugInfo(
                activeInstruction2,
                documentPath,
                activeLineSpan12.ToSourceSpan(),
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame));

        EnterBreakState(debuggingSession, activeStatements);

        var activeStatementSpan11 = new ActiveStatementSpan(new ActiveStatementId(0), activeLineSpan11, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame);
        var activeStatementSpan12 = new ActiveStatementSpan(new ActiveStatementId(1), activeLineSpan12, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame);

        var baseSpans = await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [document1.Id], CancellationToken.None);
        AssertEx.Equal(
        [
            activeStatementSpan11,
            activeStatementSpan12
        ], baseSpans.Single());

        var trackedActiveSpans1 = ImmutableArray.Create(activeStatementSpan11, activeStatementSpan12);

        var currentSpans = await debuggingSession.GetAdjustedActiveStatementSpansAsync(document1, (_, _, _) => new(trackedActiveSpans1), CancellationToken.None);
        AssertEx.Equal(trackedActiveSpans1, currentSpans);

        // change the source (valid edit):
        solution = solution.WithDocumentText(documentId, sourceTextV2);
        var document2 = solution.GetDocument(documentId);

        // tracking span update triggered by the edit:
        var activeStatementSpan21 = new ActiveStatementSpan(new ActiveStatementId(0), activeLineSpan21, ActiveStatementFlags.NonLeafFrame);
        var activeStatementSpan22 = new ActiveStatementSpan(new ActiveStatementId(1), activeLineSpan22, ActiveStatementFlags.LeafFrame);
        var trackedActiveSpans2 = ImmutableArray.Create(activeStatementSpan21, activeStatementSpan22);

        currentSpans = await debuggingSession.GetAdjustedActiveStatementSpansAsync(document2, (_, _, _) => new(trackedActiveSpans2), CancellationToken.None);
        AssertEx.Equal([adjustedActiveLineSpan1, adjustedActiveLineSpan2], currentSpans.Select(s => s.LineSpan));
    }

    [Theory, CombinatorialData]
    public async Task ActiveStatements_SyntaxErrorOrOutOfSyncDocument(bool isOutOfSync)
    {
        var sourceV1 = "class C { void F() => G(1); void G(int a) => System.Console.WriteLine(1); }";

        // syntax error (missing ';') unless testing out-of-sync document
        var sourceV2 = isOutOfSync
            ? "class C { int x; void F() => G(1); void G(int a) => System.Console.WriteLine(2); }"
            : "class C { int x void F() => G(1); void G(int a) => System.Console.WriteLine(2); }";

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, sourceV1);

        var activeSpan11 = GetSpan(sourceV1, "G(1)");
        var activeSpan12 = GetSpan(sourceV1, "System.Console.WriteLine(1)");

        var documentId = document1.Id;
        var documentFilePath = document1.FilePath;

        var sourceTextV1 = await document1.GetTextAsync(CancellationToken.None);
        var sourceTextV2 = CreateText(sourceV2);

        var activeLineSpan11 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan11);
        var activeLineSpan12 = sourceTextV1.Lines.GetLinePositionSpan(activeSpan12);

        var debuggingSession = StartDebuggingSession(
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
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame),
            new ManagedActiveStatementDebugInfo(
                activeInstruction2,
                documentFilePath,
                activeLineSpan12.ToSourceSpan(),
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame));

        EnterBreakState(debuggingSession, activeStatements);

        var baseSpans = (await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [documentId], CancellationToken.None)).Single();
        AssertEx.Equal(
        [
            new ActiveStatementSpan(new ActiveStatementId(0), activeLineSpan11, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame),
            new ActiveStatementSpan(new ActiveStatementId(1), activeLineSpan12, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame)
        ], baseSpans);

        // change the source (valid edit):
        solution = solution.WithDocumentText(documentId, sourceTextV2);
        var document2 = solution.GetDocument(documentId);

        // no adjustments made due to syntax error or out-of-sync document:
        var currentSpans = await debuggingSession.GetAdjustedActiveStatementSpansAsync(document2, (_, _, _) => ValueTask.FromResult(baseSpans), CancellationToken.None);
        AssertEx.Equal([activeLineSpan11, activeLineSpan12], currentSpans.Select(s => s.LineSpan));
    }

    [Theory, CombinatorialData]
    public async Task ActiveStatements_ForeignDocument(bool withPath, bool designTimeOnly)
    {
        using var _ = CreateWorkspace(out var solution, out var service, [typeof(NoCompilationLanguageService)]);

        var project = solution.AddProject("dummy_proj", "dummy_proj", designTimeOnly ? LanguageNames.CSharp : NoCompilationConstants.LanguageName);
        var filePath = withPath ? Path.Combine(TempRoot.Root, "test.cs") : null;
        var sourceText = CreateText("dummy1");

        var documentInfo = DocumentInfo.Create(
            DocumentId.CreateNewId(project.Id, "test"),
            name: "test",
            loader: TextLoader.From(TextAndVersion.Create(sourceText, VersionStamp.Create(), filePath)),
            filePath: filePath)
            .WithDesignTimeOnly(designTimeOnly);

        var document = project.Solution.AddDocument(documentInfo).GetDocument(documentInfo.Id);

        solution = document.Project.Solution;

        var debuggingSession = StartDebuggingSession(service, solution);

        var activeStatements = ImmutableArray.Create(
            new ManagedActiveStatementDebugInfo(
                new ManagedInstructionId(new ManagedMethodId(Guid.Empty, token: 0x06000001, version: 1), ilOffset: 0),
                documentName: document.Name,
                sourceSpan: new SourceSpan(0, 1, 0, 2),
                ActiveStatementFlags.NonLeafFrame));

        EnterBreakState(debuggingSession, activeStatements);

        // active statements are not tracked in non-Roslyn projects:
        var currentSpans = await debuggingSession.GetAdjustedActiveStatementSpansAsync(document, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(currentSpans);

        var baseSpans = await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [document.Id], CancellationToken.None);
        Assert.Empty(baseSpans.Single());

        // update solution:
        solution = solution.WithDocumentText(document.Id, CreateText("dummy2"));

        baseSpans = await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [document.Id], CancellationToken.None);
        Assert.Empty(baseSpans.Single());
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24320")]
    public async Task ActiveStatements_LinkedDocuments()
    {
        var markedSources = new[]
        {
            """
            class Test1
            {
                static void Main() => <AS:2>Project2::Test1.F();</AS:2>
                static void F() => <AS:1>Project4::Test2.M();</AS:1>
            }
            """,
@"class Test2 { static void M() => <AS:0>Console.WriteLine();</AS:0> }"
        };

        var module1 = Guid.NewGuid();
        var module2 = Guid.NewGuid();
        var module4 = Guid.NewGuid();

        var debugInfos = GetActiveStatementDebugInfosCSharp(
            markedSources,
            methodRowIds: [1, 2, 1],
            modules: [module4, module2, module1]);

        // Project1: Test1.cs, Test2.cs
        // Project2: Test1.cs (link from P1)
        // Project3: Test1.cs (link from P1)
        // Project4: Test2.cs (link from P1)

        using var _ = CreateWorkspace(out var solution, out var service);
        solution = AddDefaultTestProject(solution, SourceMarkers.Clear(markedSources));

        var documents = solution.Projects.Single().Documents;
        var doc1 = documents.First();
        var doc2 = documents.Skip(1).First();
        var text1 = await doc1.GetTextAsync();
        var text2 = await doc2.GetTextAsync();

        DocumentId AddProjectAndLinkDocument(string projectName, Document doc, SourceText text)
        {
            var p = solution.AddTestProject(projectName);
            var linkedDocId = DocumentId.CreateNewId(p.Id, projectName + "->" + doc.Name);
            solution = p.Solution.AddDocument(linkedDocId, doc.Name, text, filePath: doc.FilePath);
            return linkedDocId;
        }

        var docId3 = AddProjectAndLinkDocument("Project2", doc1, text1);
        var docId4 = AddProjectAndLinkDocument("Project3", doc1, text1);
        var docId5 = AddProjectAndLinkDocument("Project4", doc2, text2);

        var debuggingSession = StartDebuggingSession(service, solution);
        EnterBreakState(debuggingSession, debugInfos);

        // Base Active Statements

        var baseActiveStatementsMap = await debuggingSession.EditSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false);
        var documentMap = baseActiveStatementsMap.DocumentPathMap;

        Assert.Equal(2, documentMap.Count);

        AssertEx.Equal(
        [
            $"2: {doc1.FilePath}: (2,32)-(2,52) flags=[MethodUpToDate, NonLeafFrame]",
            $"1: {doc1.FilePath}: (3,29)-(3,49) flags=[MethodUpToDate, NonLeafFrame]"
        ], documentMap[doc1.FilePath].Select(InspectActiveStatement));

        AssertEx.Equal(
        [
            $"0: {doc2.FilePath}: (0,39)-(0,59) flags=[LeafFrame, MethodUpToDate]",
        ], documentMap[doc2.FilePath].Select(InspectActiveStatement));

        Assert.Equal(3, baseActiveStatementsMap.InstructionMap.Count);

        var statements = baseActiveStatementsMap.InstructionMap.Values.OrderBy(v => v.Id.Ordinal).ToArray();
        var s = statements[0];
        Assert.Equal(0x06000001, s.InstructionId.Method.Token);
        Assert.Equal(module4, s.InstructionId.Method.Module);

        s = statements[1];
        Assert.Equal(0x06000002, s.InstructionId.Method.Token);
        Assert.Equal(module2, s.InstructionId.Method.Module);

        s = statements[2];
        Assert.Equal(0x06000001, s.InstructionId.Method.Token);
        Assert.Equal(module1, s.InstructionId.Method.Module);

        var spans = await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [doc1.Id, doc2.Id, docId3, docId4, docId5], CancellationToken.None);

        AssertEx.Equal(
        [
            "(2,32)-(2,52), (3,29)-(3,49)", // test1.cs
            "(0,39)-(0,59)",                // test2.cs
            "(2,32)-(2,52), (3,29)-(3,49)", // link test1.cs
            "(2,32)-(2,52), (3,29)-(3,49)", // link test1.cs
            "(0,39)-(0,59)"                 // link test2.cs
        ], spans.Select(docSpans => string.Join(", ", docSpans.Select(span => span.LineSpan))));
    }

    [Fact]
    public async Task ActiveStatements_OutOfSyncDocuments()
    {
        var markedSource1 =
            """
            class C
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
            }
            """;
        var markedSources = new[] { markedSource1 };

        var thread1 = Guid.NewGuid();

        // Thread1 stack trace: F (AS:0 leaf)

        var debugInfos = GetActiveStatementDebugInfosCSharp(
            markedSources,
            methodRowIds: [1],
            ilOffsets: [1],
            flags:
            [
                ActiveStatementFlags.LeafFrame | ActiveStatementFlags.MethodUpToDate
            ]);

        using var _ = CreateWorkspace(out var solution, out var service);
        solution = AddDefaultTestProject(solution, SourceMarkers.Clear(markedSources));
        var project = solution.Projects.Single();
        var document = project.Documents.Single();

        var debuggingSession = StartDebuggingSession(service, solution, initialState: CommittedSolution.DocumentState.OutOfSync);
        EnterBreakState(debuggingSession, debugInfos);

        // update document to test a changed solution
        solution = solution.WithDocumentText(document.Id, CreateText("""
            class C
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
            }
            """));
        document = solution.GetDocument(document.Id);

        var baseActiveStatementMap = await debuggingSession.EditSession.BaseActiveStatements.GetValueAsync(CancellationToken.None).ConfigureAwait(false);

        // Active Statements - available in out-of-sync documents, as they reflect the state of the debuggee and not the base document content

        Assert.Single(baseActiveStatementMap.DocumentPathMap);

        AssertEx.Equal(
        [
            $"0: {document.FilePath}: (9,18)-(9,22) flags=[LeafFrame, MethodUpToDate]",
        ], baseActiveStatementMap.DocumentPathMap[document.FilePath].Select(InspectActiveStatement));

        Assert.Equal(1, baseActiveStatementMap.InstructionMap.Count);

        var activeStatement1 = baseActiveStatementMap.InstructionMap.Values.OrderBy(v => v.InstructionId.Method.Token).Single();
        Assert.Equal(0x06000001, activeStatement1.InstructionId.Method.Token);
        Assert.Equal(document.FilePath, activeStatement1.FilePath);
        Assert.True(activeStatement1.IsLeaf);

        // Active statement reported as unchanged as the containing document is out-of-sync:
        var baseSpans = await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [document.Id], CancellationToken.None);
        AssertEx.Equal([$"(9,18)-(9,22)"], baseSpans.Single().Select(s => s.LineSpan.ToString()));

        // Document got synchronized:
        debuggingSession.LastCommittedSolution.Test_SetDocumentState(document.Id, CommittedSolution.DocumentState.MatchesBuildOutput);

        // New location of the active statement reported:
        baseSpans = await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [document.Id], CancellationToken.None);
        AssertEx.Equal([$"(10,12)-(10,16)"], baseSpans.Single().Select(s => s.LineSpan.ToString()));
    }

    [Fact]
    public async Task ActiveStatements_SourceGeneratedDocuments_LineDirectives()
    {
        var markedSource1 = """

            /* GENERATE:
            class C
            {
                void F()
                {
            #line 1 "a.razor"
                   <AS:0>F();</AS:0>
            #line default
                }
            }
            */

            """;
        var markedSource2 = """

            /* GENERATE:
            class C
            {
                void F()
                {
            #line 2 "a.razor"
                   <AS:0>F();</AS:0>
            #line default
                }
            }
            */

            """;
        var source1 = SourceMarkers.Clear(markedSource1);
        var source2 = SourceMarkers.Clear(markedSource2);

        var additionalFileSourceV1 = """

                   xxxxxxxxxxxxxxxxx

            """;

        var generator = new TestSourceGenerator() { ExecuteImpl = GenerateSource };

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document1) = AddDefaultTestProject(solution, source1, generator: generator, additionalFileText: additionalFileSourceV1);

        var generatedDocument1 = (await solution.Projects.Single().GetSourceGeneratedDocumentsAsync().ConfigureAwait(false)).Single();

        var moduleId = EmitLibrary(document1.Project.Id, source1, generatorProject: document1.Project, additionalFileText: additionalFileSourceV1);
        LoadLibraryToDebuggee(moduleId);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession, GetActiveStatementDebugInfosCSharp(
            [GetGeneratedCodeFromMarkedSource(markedSource1)],
            filePaths: [generatedDocument1.FilePath],
            modules: [moduleId],
            methodRowIds: [1],
            methodVersions: [1],
            flags:
            [
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame
            ]));

        // change the source (valid edit)
        solution = solution.WithDocumentText(document1.Id, CreateText(source2));

        // validate solution update status and emit:
        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        // check emitted delta:
        var delta = results.ModuleUpdates.Updates.Single();
        Assert.Empty(delta.ActiveStatements);
        Assert.NotEmpty(delta.ILDelta);
        Assert.NotEmpty(delta.MetadataDelta);
        Assert.NotEmpty(delta.PdbDelta);
        Assert.Empty(delta.UpdatedMethods);
        Assert.Empty(delta.UpdatedTypes);

        AssertEx.Equal(
        [
            "a.razor: [0 -> 1]"
        ], delta.SequencePoints.Inspect());

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/54347")]
    public async Task ActiveStatements_EncSessionFollowedByHotReload()
    {
        var markedSource1 = """

            class C
            {
                int F()
                {
                    try
                    {
                        return 0;
                    }
                    catch
                    {
                        <AS:0>return 1;</AS:0>
                    }
                }
            }

            """;
        var markedSource2 = """

            class C
            {
                int F()
                {
                    try
                    {
                        return 0;
                    }
                    catch
                    {
                        <AS:0>return 2;</AS:0>
                    }
                }
            }

            """;
        var source1 = SourceMarkers.Clear(markedSource1);
        var source2 = SourceMarkers.Clear(markedSource2);

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, source1);

        var moduleId = EmitAndLoadLibraryToDebuggee(document);

        var debuggingSession = StartDebuggingSession(service, solution);

        EnterBreakState(debuggingSession, GetActiveStatementDebugInfosCSharp(
            [markedSource1],
            modules: [moduleId],
            methodRowIds: [1],
            methodVersions: [1],
            flags:
            [
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame
            ]));

        // change the source (rude edit)
        solution = solution.WithDocumentText(document.Id, CreateText(source2));
        document = solution.GetDocument(document.Id);

        var docDiagnostics = await service.GetDocumentDiagnosticsAsync(document, s_noActiveSpans, CancellationToken.None);
        AssertEx.Equal(
            [$"{document.FilePath}: (9,8)-(9,13): Error ENC0063: {string.Format(FeaturesResources.Updating_a_0_around_an_active_statement_requires_restarting_the_application, CSharpFeaturesResources.catch_clause)}"],
            InspectDiagnostics(docDiagnostics));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        AssertEx.SequenceEqual(["ENC0063"], InspectDiagnosticIds(results.Diagnostics));
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        debuggingSession.DiscardSolutionUpdate();

        // undo the change
        solution = solution.WithDocumentText(document.Id, CreateText(source1));
        document = solution.GetDocument(document.Id);

        ExitBreakState(debuggingSession);

        // change the source (now a valid edit since there is no active statement)
        solution = solution.WithDocumentText(document.Id, CreateText(source2));

        docDiagnostics = await service.GetDocumentDiagnosticsAsync(document, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(docDiagnostics);

        // validate solution update status and emit (Hot Reload change):
        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        debuggingSession.DiscardSolutionUpdate();
        EndDebuggingSession(debuggingSession);
    }

    /// <summary>
    /// Scenario:
    /// F5 a program that has function F that calls G. G has a long-running loop, which starts executing.
    /// The user makes following operations:
    /// 1) Break, edit F from version 1 to version 2, continue (change is applied), G is still running in its loop
    ///    Function remapping is produced for F v1 -> F v2.
    /// 2) Hot-reload edit F (without breaking) to version 3.
    ///    Function remapping is not produced for F v2 -> F v3. If G ever returned to F it will be remapped from F v1 -> F v2,
    ///    where F v2 is considered stale code. This is consistent with the semantic of Hot Reload: Hot Reloaded changes do not have
    ///    an effect until the method is called again. In this case the method is not called, it it returned into hence the stale
    ///    version executes.
    /// 3) Break and apply EnC edit. This edit is to F v3 (Hot Reload) of the method. We will produce remapping F v3 -> v4.
    /// </summary>
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52100")]
    public async Task BreakStateRemappingFollowedUpByRunStateUpdate()
    {
        var markedSourceV1 =
            """
            class Test
            {
                static bool B() => true;

                static void G() { while (B()); <AS:0>}</AS:0>

                static void F()
                {
                    /*insert1[1]*/B();/*insert2[5]*/B();/*insert3[10]*/B();
                    <AS:1>G();</AS:1>
                }
            }
            """;
        var markedSourceV2 = Update(markedSourceV1, marker: "1");
        var markedSourceV3 = Update(markedSourceV2, marker: "2");
        var markedSourceV4 = Update(markedSourceV3, marker: "3");

        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, SourceMarkers.Clear(markedSourceV1));
        var documentId = document.Id;

        var moduleId = EmitAndLoadLibraryToDebuggee(document.Project.Id, SourceMarkers.Clear(markedSourceV1));

        var debuggingSession = StartDebuggingSession(service, solution);

        // EnC update F v1 -> v2

        EnterBreakState(debuggingSession, GetActiveStatementDebugInfosCSharp(
            [markedSourceV1],
            modules: [moduleId, moduleId],
            methodRowIds: [2, 3],
            methodVersions: [1, 1],
            flags:
            [
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame,    // G
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame, // F
            ]));

        solution = solution.WithDocumentText(documentId, CreateText(SourceMarkers.Clear(markedSourceV2)));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(0x06000003, results.ModuleUpdates.Updates.Single().UpdatedMethods.Single());
        Assert.Equal(0x02000002, results.ModuleUpdates.Updates.Single().UpdatedTypes.Single());
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        CommitSolutionUpdate(debuggingSession);

        AssertEx.Equal(
        [
            $"0x06000002 v1 | AS {document.FilePath}: (4,41)-(4,42) => (4,41)-(4,42)",
            $"0x06000003 v1 | AS {document.FilePath}: (9,14)-(9,18) => (10,14)-(10,18)",
        ], InspectNonRemappableRegions(debuggingSession.EditSession.NonRemappableRegions));

        ExitBreakState(debuggingSession);

        // Hot Reload update F v2 -> v3

        solution = solution.WithDocumentText(documentId, CreateText(SourceMarkers.Clear(markedSourceV3)));

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(0x06000003, results.ModuleUpdates.Updates.Single().UpdatedMethods.Single());
        Assert.Equal(0x02000002, results.ModuleUpdates.Updates.Single().UpdatedTypes.Single());
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        CommitSolutionUpdate(debuggingSession);

        // the regions remain unchanged
        AssertEx.Equal(
        [
            $"0x06000002 v1 | AS {document.FilePath}: (4,41)-(4,42) => (4,41)-(4,42)",
            $"0x06000003 v1 | AS {document.FilePath}: (9,14)-(9,18) => (10,14)-(10,18)",
        ], InspectNonRemappableRegions(debuggingSession.EditSession.NonRemappableRegions));

        // EnC update F v3 -> v4

        EnterBreakState(debuggingSession, GetActiveStatementDebugInfosCSharp(
            [markedSourceV1],       // matches F v1
            modules: [moduleId, moduleId],
            methodRowIds: [2, 3],
            methodVersions: [1, 1], // frame F v1 is still executing (G has not returned)
            flags:
            [
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame,    // G
                ActiveStatementFlags.Stale | ActiveStatementFlags.NonLeafFrame,        // F - not up-to-date anymore and since F v1 is followed by F v3 (hot-reload) it is now stale
            ]));

        var spans = (await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [documentId], CancellationToken.None)).Single();
        AssertEx.Equal(
        [
            new ActiveStatementSpan(new ActiveStatementId(0), new LinePositionSpan(new(4, 41), new(4, 42)), ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame),
        ], spans);

        solution = solution.WithDocumentText(documentId, CreateText(SourceMarkers.Clear(markedSourceV4)));

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(0x06000003, results.ModuleUpdates.Updates.Single().UpdatedMethods.Single());
        Assert.Equal(0x02000002, results.ModuleUpdates.Updates.Single().UpdatedTypes.Single());
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        CommitSolutionUpdate(debuggingSession);

        // Stale active statement region is gone.
        AssertEx.Equal(
        [
            $"0x06000002 v1 | AS {document.FilePath}: (4,41)-(4,42) => (4,41)-(4,42)",
        ], InspectNonRemappableRegions(debuggingSession.EditSession.NonRemappableRegions));

        ExitBreakState(debuggingSession);
        EndDebuggingSession(debuggingSession);
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
            """
            class Test
            {
                static bool B() => true;
                static void G() { while (B()); <AS:0>}</AS:0>

                static void F()
                {
                    <AS:1>G();</AS:1>
                }
            }
            """;
        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, SourceMarkers.Clear(markedSource1));
        var documentId = document.Id;

        var moduleId = EmitAndLoadLibraryToDebuggee(document.Project.Id, SourceMarkers.Clear(markedSource1));

        var debuggingSession = StartDebuggingSession(service, solution);

        // Update to snapshot 2, but don't apply

        solution = solution.WithDocumentText(documentId, CreateText(SourceMarkers.Clear("""
            class Test
            {
                static bool B() => true;
                static void G() { while (B()); <AS:0>}</AS:0>

                static void F()
                {
                    B();
                    <AS:1>G();</AS:1>
                }
            }
            """)));

        // EnC update F v2 -> v3

        EnterBreakState(debuggingSession, GetActiveStatementDebugInfosCSharp(
            [markedSource1],
            modules: [moduleId, moduleId],
            methodRowIds: [2, 3],
            methodVersions: [1, 1],
            flags:
            [
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame,    // G
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame, // F
            ]));

        // check that the active statement is mapped correctly to snapshot v2:
        var expectedSpanG1 = new LinePositionSpan(new LinePosition(3, 41), new LinePosition(3, 42));
        var expectedSpanF1 = new LinePositionSpan(new LinePosition(8, 14), new LinePosition(8, 18));

        var spans = (await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [documentId], CancellationToken.None)).Single();
        AssertEx.Equal(
        [
            new ActiveStatementSpan(new ActiveStatementId(0), expectedSpanG1, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame, documentId),
            new ActiveStatementSpan(new ActiveStatementId(1), expectedSpanF1, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame, documentId)
        ], spans);

        solution = solution.WithDocumentText(documentId, CreateText(SourceMarkers.Clear("""
            class Test
            {
                static bool B() => true;
                static void G() { while (B()); <AS:0>}</AS:0>

                static void F()
                {
                    B();
                    B();
                    <AS:1>G();</AS:1>
                }
            }
            """)));

        // check that the active statement is mapped correctly to snapshot v3:
        var expectedSpanG2 = new LinePositionSpan(new LinePosition(3, 41), new LinePosition(3, 42));
        var expectedSpanF2 = new LinePositionSpan(new LinePosition(9, 14), new LinePosition(9, 18));

        spans = (await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [documentId], CancellationToken.None)).Single();
        AssertEx.Equal(
        [
            new ActiveStatementSpan(new ActiveStatementId(0), expectedSpanG2, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame, documentId),
            new ActiveStatementSpan(new ActiveStatementId(1), expectedSpanF2, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.NonLeafFrame, documentId)
        ], spans);

        // no rude edits:
        var document1 = solution.GetDocument(documentId);
        var diagnostics = await service.GetDocumentDiagnosticsAsync(document1, s_noActiveSpans, CancellationToken.None);
        Assert.Empty(diagnostics);

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(0x06000003, results.ModuleUpdates.Updates.Single().UpdatedMethods.Single());
        Assert.Equal(0x02000002, results.ModuleUpdates.Updates.Single().UpdatedTypes.Single());
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        CommitSolutionUpdate(debuggingSession);

        AssertEx.Equal(
        [
            $"0x06000002 v1 | AS {document.FilePath}: (3,41)-(3,42) => (3,41)-(3,42)",
            $"0x06000003 v1 | AS {document.FilePath}: (7,14)-(7,18) => (9,14)-(9,18)",
        ], InspectNonRemappableRegions(debuggingSession.EditSession.NonRemappableRegions));

        ExitBreakState(debuggingSession);
        EndDebuggingSession(debuggingSession);
    }

    /// <summary>
    /// Scenario:
    /// - F5
    /// - edit and apply edit that deletes non-leaf active statement
    /// - break
    /// </summary>
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52100")]
    public async Task BreakAfterRunModeChangeDeletesNonLeafActiveStatement()
    {
        var markedSource1 =
            """
            class Test
            {
                static bool B() => true;
                static void G() { while (B()); <AS:0>}</AS:0>

                static void F()
                {
                    <AS:1>G();</AS:1>
                }
            }
            """;
        using var _ = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, SourceMarkers.Clear(markedSource1));
        var documentId = document.Id;

        var moduleId = EmitAndLoadLibraryToDebuggee(document.Project.Id, SourceMarkers.Clear(markedSource1));

        var debuggingSession = StartDebuggingSession(service, solution);

        // Apply update: F v1 -> v2.

        solution = solution.WithDocumentText(documentId, CreateText(SourceMarkers.Clear("""
            class Test
            {
                static bool B() => true;
                static void G() { while (B()); <AS:0>}</AS:0>

                static void F()
                {
                }
            }
            """)));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(0x06000003, results.ModuleUpdates.Updates.Single().UpdatedMethods.Single());
        Assert.Equal(0x02000002, results.ModuleUpdates.Updates.Single().UpdatedTypes.Single());
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        CommitSolutionUpdate(debuggingSession);

        // Break

        EnterBreakState(debuggingSession, GetActiveStatementDebugInfosCSharp(
            [markedSource1],
            modules: [moduleId, moduleId],
            methodRowIds: [2, 3],
            methodVersions: [1, 1],  // frame F v1 is still executing (G has not returned)
            flags:
            [
                ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame,    // G
                ActiveStatementFlags.NonLeafFrame, // F
            ]));

        // check that the active statement is mapped correctly to snapshot v2:
        var expectedSpanG1 = new LinePositionSpan(new LinePosition(3, 41), new LinePosition(3, 42));

        var spans = (await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [documentId], CancellationToken.None)).Single();
        AssertEx.Equal(
        [
            new ActiveStatementSpan(new ActiveStatementId(0), expectedSpanG1, ActiveStatementFlags.MethodUpToDate | ActiveStatementFlags.LeafFrame)
            // active statement in F has been deleted
        ], spans);

        ExitBreakState(debuggingSession);
        EndDebuggingSession(debuggingSession);
    }

    /// <summary>
    /// Scenario:
    /// - F5
    /// - edit source and apply code fix to move type to file XYZ.cs
    /// - continue execution
    /// </summary>
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72984")]
    public async Task EditAndContinueAfterApplyingMoveTypeToFileCodeFix()
    {
        const string movedType = "AnotherClass";

        const string source = $$"""
            class {{movedType}}
            {
                public const bool True = true;
            }

            class Test
            {
                static bool B() => {{movedType}}.True;
                static void G() { while (B()); }

                static void F()
                {
                    G();
                }
            }
            """;
        using var workspace = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, source);
        var documentId = document.Id;
        var oldProject = document.Project;

        var moduleId = EmitAndLoadLibraryToDebuggee(document.Project.Id, source);

        var debuggingSession = StartDebuggingSession(service, solution);

        // Apply code fix: Move type to AnotherClass.cs

        var moveTypeService = document.GetLanguageService<IMoveTypeService>();
        var root = await document.GetSyntaxRootAsync();
        var span = root.DescendantTokens()
            .Where(s => s.Text is movedType)
            .FirstOrDefault()
            .Span;
        var modifiedSolution = await moveTypeService.GetModifiedSolutionAsync(document, span, MoveTypeOperationKind.MoveType, cancellationToken: default);

        // Apply edit on remaining document: source after code fix -> modifiedSource

        modifiedSolution = modifiedSolution.WithDocumentText(document.Id, CreateText($$"""
            class Test
            {
                static bool A() => {{movedType}}.True;
                static bool B() => A();
                static void G() { while (B()); }

                static void F()
                {
                    H();
                }

                static void H()
                {
                    G();
                }
            }
            """));

        var newProject = modifiedSolution.GetProject(oldProject.Id);
        Assert.Equal(1, oldProject.DocumentIds.Count);
        Assert.Equal(2, newProject.DocumentIds.Count);

        var results = await EmitSolutionUpdateAsync(debuggingSession, modifiedSolution);
        Assert.Empty(results.Diagnostics);
        Assert.False(results.ModuleUpdates.Updates.IsEmpty);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        CommitSolutionUpdate(debuggingSession);
        EndDebuggingSession(debuggingSession);
    }

    [Fact]
    public async Task MultiSession()
    {
        var source1 = "class C { void M() { System.Console.WriteLine(); } }";
        var source3 = "class C { void M() { WriteLine(2); } }";

        var dir = Temp.CreateDirectory();
        var sourceFileA = dir.CreateFile("A.cs").WriteAllText(source1, Encoding.UTF8);

        using var _ = CreateWorkspace(out var solution, out var encService);

        var projectP = solution.AddTestProject("P");

        solution = projectP.Solution;

        var moduleId = EmitLibrary(projectP.Id, source1, sourceFileA.Path, assemblyName: "Proj");

        var documentIdA = DocumentId.CreateNewId(projectP.Id, debugName: "A");
        solution = solution.AddDocument(DocumentInfo.Create(
            id: documentIdA,
            name: "A",
            loader: new WorkspaceFileTextLoader(solution.Services, sourceFileA.Path, Encoding.UTF8),
            filePath: sourceFileA.Path));

        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var sessionId = encService.StartDebuggingSession(
                solution,
                _debuggerService,
                NullPdbMatchingSourceTextProvider.Instance,
                reportDiagnostics: true);

            var solution1 = solution.WithDocumentText(documentIdA, CreateText("class C { void M() { System.Console.WriteLine(" + i + "); } }"));

            var result1 = await encService.EmitSolutionUpdateAsync(sessionId, solution1, runningProjects: ImmutableDictionary<ProjectId, RunningProjectOptions>.Empty, s_noActiveSpans, CancellationToken.None);
            Assert.Empty(result1.Diagnostics);
            Assert.Equal(1, result1.ModuleUpdates.Updates.Length);
            encService.DiscardSolutionUpdate(sessionId);

            var solution2 = solution1.WithDocumentText(documentIdA, CreateText(source3));

            var result2 = await encService.EmitSolutionUpdateAsync(sessionId, solution2, runningProjects: ImmutableDictionary<ProjectId, RunningProjectOptions>.Empty, s_noActiveSpans, CancellationToken.None);
            Assert.Equal("CS0103", result2.Diagnostics.Single().Diagnostics.Single().Id);
            Assert.Empty(result2.ModuleUpdates.Updates);

            encService.EndDebuggingSession(sessionId);
        });

        await Task.WhenAll(tasks);

        Assert.Empty(encService.GetTestAccessor().GetActiveDebuggingSessions());
    }

    [Fact]
    public async Task Disposal()
    {
        using var _1 = CreateWorkspace(out var solution, out var service);
        (solution, var document) = AddDefaultTestProject(solution, "class C { }");

        var debuggingSession = StartDebuggingSession(service, solution);

        EndDebuggingSession(debuggingSession);

        // The folling methods shall not be called after the debugging session ended.
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await debuggingSession.EmitSolutionUpdateAsync(solution, runningProjects: ImmutableDictionary<ProjectId, RunningProjectOptions>.Empty, s_noActiveSpans, CancellationToken.None));
        Assert.Throws<ObjectDisposedException>(() => debuggingSession.BreakStateOrCapabilitiesChanged(inBreakState: true));
        Assert.Throws<ObjectDisposedException>(() => debuggingSession.DiscardSolutionUpdate());
        Assert.Throws<ObjectDisposedException>(() => debuggingSession.CommitSolutionUpdate());
        Assert.Throws<ObjectDisposedException>(() => debuggingSession.EndSession(out _));

        // The following methods can be called at any point in time, so we must handle race with dispose gracefully.
        Assert.Empty(await debuggingSession.GetDocumentDiagnosticsAsync(document, s_noActiveSpans, CancellationToken.None));
        Assert.Empty(await debuggingSession.GetAdjustedActiveStatementSpansAsync(document, s_noActiveSpans, CancellationToken.None));
        Assert.True((await debuggingSession.GetBaseActiveStatementSpansAsync(solution, [], CancellationToken.None)).IsDefault);
    }

    [Fact]
    public async Task MultipleSharedLibraryBaselines()
    {
        var libSource1 = """
            public class C
            {
                public int F() => 1;
            }
            """;

        var libSource2 = """
            public class C
            {
                public int F() => 2;
            }
            """;
        using var workspace = CreateWorkspace(out var solution, out var service);

        (solution, var document) = AddDefaultTestProject(solution, libSource1);
        var documentId = document.Id;
        var oldProject = document.Project;

        var debuggingSession = StartDebuggingSession(service, solution);

        // Build and load two versions of the libary.
        // This happens when projects A and B both reference Lib project and copy the assembly to their respective output directories and
        // 1) A is built and launched
        // 2) an update is made to the library
        // 3) B is built and launched
        // 
        // At this point we have two baselines for the Lib project (two mvids).
        // 
        // The update to the library source also produces delta to be applied to V1 of the library.

        var libMvid1 = EmitAndLoadLibraryToDebuggee(oldProject.Id, libSource1);

        // lib source is updated:
        solution = solution.WithDocumentText(documentId, CreateText(libSource2));

        var results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        var update = results.ModuleUpdates.Updates.Single();
        GetModuleIds(update.MetadataDelta, out var updateModuleId, out var baseId, out var genId, out var gen);
        Assert.Equal(libMvid1, updateModuleId);
        Assert.Equal(1, gen);
        Assert.Equal(default, baseId);
        Assert.NotEqual(default, genId);

        CommitSolutionUpdate(debuggingSession);

        // B is launched:
        var libMvid2 = EmitAndLoadLibraryToDebuggee(oldProject.Id, libSource2);

        // lib source is updated:
        solution = solution.WithDocumentText(documentId, CreateText("""
            public class C
            {
                public int F() => 3;
            }
            """));

        results = await EmitSolutionUpdateAsync(debuggingSession, solution);
        Assert.Empty(results.Diagnostics);
        Assert.Equal(ModuleUpdateStatus.Ready, results.ModuleUpdates.Status);

        Assert.Equal(2, results.ModuleUpdates.Updates.Length);
        var libUpdate1 = results.ModuleUpdates.Updates.Single(u => u.Module == libMvid1);
        var libUpdate2 = results.ModuleUpdates.Updates.Single(u => u.Module == libMvid2);

        // update of the original library should chain to its previous delta:
        GetModuleIds(libUpdate1.MetadataDelta, out var updateModuleId1, out var baseId1, out var genId1, out var gen1);
        Assert.Equal(libMvid1, updateModuleId1);
        Assert.Equal(genId, baseId1);
        Assert.Equal(2, gen1);
        Assert.NotEqual(default, genId1);

        // update of the rebuilt library should be the first in the new chain:
        GetModuleIds(libUpdate2.MetadataDelta, out var updateModuleId2, out var baseId2, out var genId2, out var gen2);
        Assert.Equal(libMvid2, updateModuleId2);
        Assert.Equal(default, baseId2);
        Assert.Equal(1, gen2);
        Assert.NotEqual(default, genId2);

        CommitSolutionUpdate(debuggingSession);
        EndDebuggingSession(debuggingSession);

        static void GetModuleIds(
            ImmutableArray<byte> metadataDelta,
            out Guid mvid,
            out Guid baseId,
            out Guid generationId,
            out int generation)
        {
            using var readerProvider = MetadataReaderProvider.FromMetadataImage(metadataDelta);
            var reader = readerProvider.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            baseId = reader.GetGuid(moduleDef.BaseGenerationId);
            generationId = reader.GetGuid(moduleDef.GenerationId);
            mvid = reader.GetGuid(moduleDef.Mvid);
            generation = moduleDef.Generation;
        }
    }
}
