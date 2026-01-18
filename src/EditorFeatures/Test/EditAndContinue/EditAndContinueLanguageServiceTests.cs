// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.BrokeredServices.UnitTests;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using DebuggerContracts = Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.EditAndContinue;

[UseExportProvider]
public sealed class EditAndContinueLanguageServiceTests : EditAndContinueWorkspaceTestBase
{
    private static string Inspect(DiagnosticData d)
        => $"{d.Severity} {d.Id}:" +
            (!string.IsNullOrWhiteSpace(d.DataLocation.UnmappedFileSpan.Path) ? $" {d.DataLocation.UnmappedFileSpan.Path}({d.DataLocation.UnmappedFileSpan.StartLinePosition.Line}, {d.DataLocation.UnmappedFileSpan.StartLinePosition.Character}, {d.DataLocation.UnmappedFileSpan.EndLinePosition.Line}, {d.DataLocation.UnmappedFileSpan.EndLinePosition.Character}):" : "") +
            $" {d.Message}";

    private static string Inspect(DebuggerContracts.ManagedHotReloadDiagnostic d)
        => $"{d.Severity} {d.Id}:" +
            (!string.IsNullOrWhiteSpace(d.FilePath) ? $" {d.FilePath}({d.Span.StartLine}, {d.Span.StartColumn}, {d.Span.EndLine}, {d.Span.EndColumn}):" : "") +
            $" {d.Message}";

    private TestWorkspace CreateEditorWorkspace(out Solution solution, out EditAndContinueService service, out EditAndContinueLanguageService languageService, Type[] additionalParts = null)
    {
        var composition = EditorTestCompositions.EditorFeatures
            .AddExcludedPartTypes(typeof(ServiceBrokerProvider))
            .AddParts(
                typeof(MockHostWorkspaceProvider),
                typeof(MockManagedHotReloadService),
                typeof(MockServiceBrokerProvider))
            .AddParts(additionalParts);

        var workspace = new TestWorkspace(composition: composition, solutionTelemetryId: s_solutionTelemetryId);

        var sourceTextProvider = (PdbMatchingSourceTextProvider)workspace.ExportProvider.GetExports<IEventListener>().Single(e => e.Value is PdbMatchingSourceTextProvider).Value;
        var listenerProvider = workspace.GetService<MockWorkspaceEventListenerProvider>();
        listenerProvider.EventListeners = [sourceTextProvider];

        ((MockServiceBroker)workspace.GetService<IServiceBrokerProvider>().ServiceBroker).CreateService = t => t switch
        {
            _ when t == typeof(Microsoft.VisualStudio.Debugger.Contracts.HotReload.IHotReloadLogger) => new MockHotReloadLogger(),
            _ => throw ExceptionUtilities.UnexpectedValue(t)
        };

        ((MockHostWorkspaceProvider)workspace.GetService<IHostWorkspaceProvider>()).Workspace = workspace;

        solution = workspace.CurrentSolution;
        service = GetEditAndContinueService(workspace);
        languageService = workspace.GetService<EditAndContinueLanguageService>();
        return workspace;
    }

    private sealed class TestSourceTextContainer : SourceTextContainer
    {
        public SourceText Text { get; set; }

        public override SourceText CurrentText => Text;

#pragma warning disable CS0067
        public override event EventHandler<TextChangeEventArgs> TextChanged;
#pragma warning restore
    }

    [Theory, CombinatorialData]
    public async Task Test(bool commitChanges)
    {
        var localComposition = EditorTestCompositions.LanguageServerProtocolEditorFeatures
            .AddExcludedPartTypes(
                typeof(EditAndContinueService),
                typeof(ServiceBrokerProvider))
            .AddParts(
                typeof(NoCompilationLanguageService),
                typeof(MockHostWorkspaceProvider),
                typeof(MockServiceBrokerProvider),
                typeof(MockEditAndContinueService),
                typeof(MockManagedHotReloadService));

        using var localWorkspace = new TestWorkspace(composition: localComposition);

        var globalOptions = localWorkspace.GetService<IGlobalOptionService>();
        ((MockHostWorkspaceProvider)localWorkspace.GetService<IHostWorkspaceProvider>()).Workspace = localWorkspace;

        ((MockServiceBroker)localWorkspace.GetService<IServiceBrokerProvider>().ServiceBroker).CreateService = t => t switch
        {
            _ when t == typeof(DebuggerContracts.IHotReloadLogger) => new MockHotReloadLogger(),
            _ => throw ExceptionUtilities.UnexpectedValue(t)
        };

        MockEditAndContinueService mockEncService;

        mockEncService = (MockEditAndContinueService)localWorkspace.GetService<IEditAndContinueService>();

        var localService = localWorkspace.GetService<EditAndContinueLanguageService>();

        await localWorkspace.ChangeSolutionAsync(localWorkspace.CurrentSolution
            .AddTestProject("proj", out var projectId)
            .AddTestDocument("class C { }", "test.cs", out var documentId).Project.Solution);

        var solution = localWorkspace.CurrentSolution;
        var project = solution.GetRequiredProject(projectId);
        var document = solution.GetRequiredDocument(documentId);
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);

        var sessionState = localWorkspace.GetService<IEditAndContinueSessionTracker>();
        var diagnosticRefresher = localWorkspace.GetService<IDiagnosticsRefresher>();
        var observedDiagnosticVersion = diagnosticRefresher.GlobalStateVersion;

        // StartDebuggingSession

        var debuggingSession = mockEncService.StartDebuggingSessionImpl = (_, _, _, _) => new DebuggingSessionId(1);

        Assert.False(sessionState.IsSessionActive);
        Assert.Empty(sessionState.ApplyChangesDiagnostics);

        await localService.StartSessionAsync(CancellationToken.None);

        Assert.True(sessionState.IsSessionActive);
        Assert.Empty(sessionState.ApplyChangesDiagnostics);

        // EnterBreakStateAsync

        mockEncService.BreakStateOrCapabilitiesChangedImpl = inBreakState =>
        {
            Assert.True(inBreakState);
        };

        await localService.EnterBreakStateAsync(CancellationToken.None);

        Assert.Equal(++observedDiagnosticVersion, diagnosticRefresher.GlobalStateVersion);
        Assert.Empty(sessionState.ApplyChangesDiagnostics);
        Assert.True(sessionState.IsSessionActive);

        // EmitSolutionUpdate

        var errorReadingFileDescriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);
        var moduleErrorDescriptor = EditAndContinueDiagnosticDescriptors.GetModuleDiagnosticDescriptor(Contracts.EditAndContinue.ManagedHotReloadAvailabilityStatus.Optimized);
        var syntaxErrorDescriptor = new DiagnosticDescriptor("CS0001", "Syntax error", "Syntax error", "Compiler", DiagnosticSeverity.Error, isEnabledByDefault: true);
        var compilerHiddenDescriptor = new DiagnosticDescriptor("CS0002", "Hidden", "Emit Hidden", "Compiler", DiagnosticSeverity.Hidden, isEnabledByDefault: true);
        var compilerInfoDescriptor = new DiagnosticDescriptor("CS0003", "Info", "Emit Info", "Compiler", DiagnosticSeverity.Info, isEnabledByDefault: true);
        var compilerWarningDescriptor = new DiagnosticDescriptor("CS0004", "Emit Warning", "Emit Warning", "Compiler", DiagnosticSeverity.Warning, isEnabledByDefault: true);
        var compilerErrorDescriptor = new DiagnosticDescriptor("CS0005", "Emit Error", "Emit Error", "Compiler", DiagnosticSeverity.Error, isEnabledByDefault: true);

        mockEncService.EmitSolutionUpdateImpl = (solution, _, _) =>
        {
            var syntaxTree = solution.GetRequiredDocument(documentId).GetSyntaxTreeSynchronously(CancellationToken.None)!;

            var documentDiagnostic = CodeAnalysis.Diagnostic.Create(errorReadingFileDescriptor, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)), ["doc", "error 1"]);
            var projectDiagnostic = CodeAnalysis.Diagnostic.Create(errorReadingFileDescriptor, Location.None, ["proj", "error 2"]);
            var moduleError = CodeAnalysis.Diagnostic.Create(moduleErrorDescriptor, Location.None, ["proj", "module error"]);
            var syntaxError = CodeAnalysis.Diagnostic.Create(syntaxErrorDescriptor, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)));
            var compilerDocHidden = CodeAnalysis.Diagnostic.Create(compilerHiddenDescriptor, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)));
            var compilerDocInfo = CodeAnalysis.Diagnostic.Create(compilerInfoDescriptor, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)));
            var compilerDocWarning = CodeAnalysis.Diagnostic.Create(compilerWarningDescriptor, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)));
            var compilerDocError = CodeAnalysis.Diagnostic.Create(compilerErrorDescriptor, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)));
            var compilerProjectHidden = CodeAnalysis.Diagnostic.Create(compilerHiddenDescriptor, Location.None);
            var compilerProjectInfo = CodeAnalysis.Diagnostic.Create(compilerInfoDescriptor, Location.None);
            var compilerProjectWarning = CodeAnalysis.Diagnostic.Create(compilerWarningDescriptor, Location.None);
            var compilerProjectError = CodeAnalysis.Diagnostic.Create(compilerErrorDescriptor, Location.None);
            var rudeEditDiagnostic = new RudeEditDiagnostic(RudeEditKind.Delete, TextSpan.FromBounds(2, 3), arguments: ["x"]).ToDiagnostic(syntaxTree);
            var deletedDocumentRudeEdit = new RudeEditDiagnostic(RudeEditKind.Delete, TextSpan.FromBounds(2, 3), arguments: ["<deleted>"]).ToDiagnostic(tree: null);

            return new()
            {
                Solution = solution,
                ModuleUpdates = new ModuleUpdates(ModuleUpdateStatus.Ready, []),
                Diagnostics =
                [
                    new ProjectDiagnostics(
                        project.Id,
                        [
                            documentDiagnostic,
                            projectDiagnostic,
                            moduleError,
                            rudeEditDiagnostic,
                            deletedDocumentRudeEdit,
                            compilerDocError,
                            compilerDocWarning,
                            compilerDocHidden,
                            compilerDocInfo,
                            compilerProjectError,
                            compilerProjectWarning,
                            compilerProjectHidden,
                            compilerProjectInfo,
                        ])
                ],
                SyntaxError = syntaxError,
                ProjectsToRebuild = [projectId],
                ProjectsToRestart = ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty.Add(projectId, []),
                ProjectsToRedeploy = [projectId],
            };
        };

        var runningProjectInfo = new DebuggerContracts.RunningProjectInfo()
        {
            ProjectInstanceId = new DebuggerContracts.ProjectInstanceId(project.FilePath, "net10.0"),
            RestartAutomatically = false,
        };

        var updates = await localService.GetUpdatesAsync(runningProjects: [runningProjectInfo], CancellationToken.None);

        Assert.Equal(++observedDiagnosticVersion, diagnosticRefresher.GlobalStateVersion);

        AssertEx.Equal(
        [
            $"Error ENC1001: {document.FilePath}(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "error 1")}",
            $"Error ENC1001: {project.FilePath}(0, 0, 0, 0): {string.Format(FeaturesResources.ErrorReadingFile, "proj", "error 2")}",
            $"Error ENC2012: {project.FilePath}(0, 0, 0, 0): {string.Format(FeaturesResources.EditAndContinueDisallowedByProject, "proj", "module error")}",
            $"Error ENC0033: {project.FilePath}(0, 0, 0, 0): {string.Format(FeaturesResources.Deleting_0_requires_restarting_the_application, "<deleted>")}",
            $"Error CS0005: {document.FilePath}(0, 1, 0, 2): Emit Error",
            $"Warning CS0004: {document.FilePath}(0, 1, 0, 2): Emit Warning",
            $"Error CS0005: {project.FilePath}(0, 0, 0, 0): Emit Error",
            $"Warning CS0004: {project.FilePath}(0, 0, 0, 0): Emit Warning",
        ], sessionState.ApplyChangesDiagnostics.Select(Inspect));

        AssertEx.Equal(
        [
            $"RestartRequired ENC1001: {document.FilePath}(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "error 1")}",
            $"RestartRequired ENC1001: {project.FilePath}(0, 0, 0, 0): {string.Format(FeaturesResources.ErrorReadingFile, "proj", "error 2")}",
            $"RestartRequired ENC2012: {project.FilePath}(0, 0, 0, 0): {string.Format(FeaturesResources.EditAndContinueDisallowedByProject, "proj", "module error")}",
            $"RestartRequired ENC0033: {document.FilePath}(0, 2, 0, 3): {string.Format(FeaturesResources.Deleting_0_requires_restarting_the_application, "x")}",
            $"RestartRequired ENC0033: {project.FilePath}(0, 0, 0, 0): {string.Format(FeaturesResources.Deleting_0_requires_restarting_the_application, "<deleted>")}",
            $"Error CS0005: {document.FilePath}(0, 1, 0, 2): Emit Error",
            $"Warning CS0004: {document.FilePath}(0, 1, 0, 2): Emit Warning",
            $"Error CS0005: {project.FilePath}(0, 0, 0, 0): Emit Error",
            $"Warning CS0004: {project.FilePath}(0, 0, 0, 0): Emit Warning",
            $"Error CS0001: {document.FilePath}(0, 1, 0, 2): Syntax error",
        ], updates.Diagnostics.Select(Inspect));

        var moduleId = Guid.NewGuid();
        var methodId = new ManagedModuleMethodId(token: 0x06000001, version: 2);

        mockEncService.EmitSolutionUpdateImpl = (solution, _, _) =>
        {
            var syntaxTree = solution.GetRequiredDocument(documentId).GetSyntaxTreeSynchronously(CancellationToken.None)!;

            return new()
            {
                Solution = solution,
                ModuleUpdates = new ModuleUpdates(
                    ModuleUpdateStatus.Ready,
                    [
                        new ManagedHotReloadUpdate(
                            moduleId,
                            "module.dll",
                            project.Id,
                            ilDelta: [1],
                            metadataDelta: [2],
                            pdbDelta: [3],
                            updatedTypes: [0x02000001],
                            requiredCapabilities: ["Baseline"],
                            updatedMethods: [0x06000002],
                            sequencePoints: [new SequencePointUpdates("file.cs", [new SourceLineUpdate(1, 2)])],
                            activeStatements: [new ManagedActiveStatementUpdate(methodId, ilOffset: 1, new(1, 2, 3, 4))],
                            exceptionRegions: [new ManagedExceptionRegionUpdate(methodId, delta: 1, new(10, 20, 30, 40))])
                    ]),
                Diagnostics = [],
                SyntaxError = null,
                ProjectsToRebuild = [],
                ProjectsToRestart = ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty,
                ProjectsToRedeploy = [],
            };
        };

        updates = await localService.GetUpdatesAsync(runningProjects: [runningProjectInfo], CancellationToken.None);

        Assert.Equal(++observedDiagnosticVersion, diagnosticRefresher.GlobalStateVersion);

        var update = updates.Updates.Single();
        Assert.Equal(moduleId, update.Module);
        Assert.Equal("module.dll", update.ModuleName);
        AssertEx.SequenceEqual([(byte)1], update.ILDelta);
        AssertEx.SequenceEqual([(byte)2], update.MetadataDelta);
        AssertEx.SequenceEqual([(byte)3], update.PdbDelta);
        AssertEx.SequenceEqual([0x02000001], update.UpdatedTypes);
        AssertEx.SequenceEqual(["Baseline"], update.RequiredCapabilities);
        AssertEx.SequenceEqual([0x06000002], update.UpdatedMethods);

        var sequencePoint = update.SequencePoints.Single();
        Assert.Equal("file.cs", sequencePoint.FileName);
        AssertEx.SequenceEqual(["1->2"], sequencePoint.LineUpdates.Select(u => $"{u.OldLine}->{u.NewLine}"));

        var activeStatement = update.ActiveStatements.Single();
        Assert.Equal(0x06000001, activeStatement.Method.Token);
        Assert.Equal(2, activeStatement.Method.Version);
        Assert.Equal(1, activeStatement.ILOffset);
        Assert.Equal(new(1, 2, 3, 4), activeStatement.NewSpan);

        var exceptionRegion = update.ExceptionRegions.Single();
        Assert.Equal(0x06000001, exceptionRegion.Method.Token);
        Assert.Equal(2, exceptionRegion.Method.Version);
        Assert.Equal(1, exceptionRegion.Delta);
        Assert.Equal(new(10, 20, 30, 40), exceptionRegion.NewSpan);

        Assert.True(sessionState.IsSessionActive);

#pragma warning disable CS0612 // Type or member is obsolete
        // validate that obsolete overload does not throw for empty array:
        _ = await localService.GetUpdatesAsync(runningProjects: ImmutableArray<string>.Empty, CancellationToken.None);
        Assert.Equal(++observedDiagnosticVersion, diagnosticRefresher.GlobalStateVersion);
#pragma warning restore

        if (commitChanges)
        {
            // CommitUpdatesAsync

            var called = false;
            mockEncService.CommitSolutionUpdateImpl = () => called = true;
            await localService.CommitUpdatesAsync(CancellationToken.None);
            Assert.True(called);
        }
        else
        {
            // DiscardUpdatesAsync

            var called = false;
            mockEncService.DiscardSolutionUpdateImpl = () => called = true;
            await localService.DiscardUpdatesAsync(CancellationToken.None);
            Assert.True(called);
        }

        Assert.True(sessionState.IsSessionActive);

        // EndSessionAsync

        await localService.EndSessionAsync(CancellationToken.None);

        Assert.Equal(++observedDiagnosticVersion, diagnosticRefresher.GlobalStateVersion);
        Assert.Empty(sessionState.ApplyChangesDiagnostics);
        Assert.False(sessionState.IsSessionActive);
    }

    [Fact]
    public async Task DefaultPdbMatchingSourceTextProvider()
    {
        var source1 = "class C1 { void M() { System.Console.WriteLine(\"a\"); } }";
        var dir = Temp.CreateDirectory();
        var sourceFile = dir.CreateFile("test.cs").WriteAllText(source1, Encoding.UTF8);

        using var workspace = CreateEditorWorkspace(out var solution, out var service, out var languageService);
        var sourceTextProvider = workspace.GetService<PdbMatchingSourceTextProvider>();

        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        solution = solution.
            AddProject(projectId, "test", "test", LanguageNames.CSharp).
            WithProjectChecksumAlgorithm(projectId, SourceHashAlgorithms.Default).
            AddMetadataReferences(projectId, TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
            AddDocument(DocumentInfo.Create(
                documentId,
                name: "test.cs",
                loader: new WorkspaceFileTextLoader(workspace.Services.SolutionServices, sourceFile.Path, Encoding.UTF8),
                filePath: sourceFile.Path));

        Assert.True(workspace.SetCurrentSolution(_ => solution, WorkspaceChangeKind.SolutionAdded));
        solution = workspace.CurrentSolution;

        var moduleId = EmitAndLoadLibraryToDebuggee(projectId, source1, sourceFilePath: sourceFile.Path);

        // hydrate document text and overwrite file content:
        var document1 = solution.GetRequiredDocument(documentId);
        _ = await document1.GetTextAsync(CancellationToken.None);

        File.WriteAllText(sourceFile.Path, "class C1 { void M() { System.Console.WriteLine(\"b\"); } }", Encoding.UTF8);

        await languageService.StartSessionAsync(CancellationToken.None);
        await languageService.EnterBreakStateAsync(CancellationToken.None);

        workspace.OnDocumentOpened(documentId, new TestSourceTextContainer()
        {
            Text = SourceText.From("class C1 { void M() { System.Console.WriteLine(\"c\"); } }", Encoding.UTF8, SourceHashAlgorithm.Sha1)
        });

        await workspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        var (key, (documentState, version)) = sourceTextProvider.GetTestAccessor().GetDocumentsWithChangedLoaderByPath().Single();
        Assert.Equal(sourceFile.Path, key);
        Assert.Equal(solution.SolutionStateContentVersion, version);
        Assert.Equal(source1, documentState.GetTextSynchronously(CancellationToken.None).ToString());

        // check committed document status:
        var debuggingSession = service.GetTestAccessor().GetActiveDebuggingSessions().Single();
        var (document, state) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(document1, CancellationToken.None);
        var text = await document.GetTextAsync();
        Assert.Equal(CommittedSolution.DocumentState.MatchesBuildOutput, state);
        Assert.Equal(source1, document.GetTextSynchronously(CancellationToken.None).ToString());

        await languageService.EndSessionAsync(CancellationToken.None);
    }
}
