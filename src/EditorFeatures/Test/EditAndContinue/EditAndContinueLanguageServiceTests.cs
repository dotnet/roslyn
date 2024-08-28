// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.BrokeredServices.UnitTests;
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
public class EditAndContinueLanguageServiceTests : EditAndContinueWorkspaceTestBase
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

    private class TestSourceTextContainer : SourceTextContainer
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
            .AddExcludedPartTypes(typeof(EditAndContinueService))
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

        DocumentId documentId;
        await localWorkspace.ChangeSolutionAsync(localWorkspace.CurrentSolution
            .AddTestProject("proj", out var projectId).Solution
            .AddMetadataReferences(projectId, TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40))
            .AddDocument(documentId = DocumentId.CreateNewId(projectId), "test.cs", SourceText.From("class C { }", Encoding.UTF8), filePath: "test.cs"));

        var solution = localWorkspace.CurrentSolution;
        var project = solution.GetRequiredProject(projectId);
        var document = solution.GetRequiredDocument(documentId);
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);

        var sessionState = localWorkspace.GetService<IEditAndContinueSessionTracker>();
        var diagnosticRefresher = localWorkspace.GetService<IDiagnosticsRefresher>();
        var observedDiagnosticVersion = diagnosticRefresher.GlobalStateVersion;

        // StartDebuggingSession

        var debuggingSession = mockEncService.StartDebuggingSessionImpl = (_, _, _, _, _, _) => new DebuggingSessionId(1);

        Assert.False(sessionState.IsSessionActive);
        Assert.Empty(sessionState.ApplyChangesDiagnostics);

        await localService.StartSessionAsync(CancellationToken.None);

        Assert.True(sessionState.IsSessionActive);
        Assert.Empty(sessionState.ApplyChangesDiagnostics);

        // EnterBreakStateAsync

        mockEncService.BreakStateOrCapabilitiesChangedImpl = (bool? inBreakState) =>
        {
            Assert.True(inBreakState);
        };

        await localService.EnterBreakStateAsync(CancellationToken.None);

        Assert.Equal(++observedDiagnosticVersion, diagnosticRefresher.GlobalStateVersion);
        Assert.Empty(sessionState.ApplyChangesDiagnostics);
        Assert.True(sessionState.IsSessionActive);

        // EmitSolutionUpdate

        var diagnosticDescriptor1 = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);

        mockEncService.EmitSolutionUpdateImpl = (solution, _) =>
        {
            var syntaxTree = solution.GetRequiredDocument(documentId).GetSyntaxTreeSynchronously(CancellationToken.None)!;

            var documentDiagnostic = CodeAnalysis.Diagnostic.Create(diagnosticDescriptor1, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)), ["doc", "error 1"]);
            var projectDiagnostic = CodeAnalysis.Diagnostic.Create(diagnosticDescriptor1, Location.None, ["proj", "error 2"]);
            var syntaxError = CodeAnalysis.Diagnostic.Create(diagnosticDescriptor1, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)), ["doc", "syntax error 3"]);

            return new()
            {
                ModuleUpdates = new ModuleUpdates(ModuleUpdateStatus.Ready, []),
                Diagnostics = [new ProjectDiagnostics(project.Id, [documentDiagnostic, projectDiagnostic])],
                RudeEdits = [(documentId, [new RudeEditDiagnostic(RudeEditKind.Delete, TextSpan.FromBounds(2, 3), arguments: ["x"])])],
                SyntaxError = syntaxError
            };
        };

        var updates = await localService.GetUpdatesAsync(CancellationToken.None);

        Assert.Equal(++observedDiagnosticVersion, diagnosticRefresher.GlobalStateVersion);

        AssertEx.Equal(
        [
            $"Error ENC1001: test.cs(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "error 1")}",
            $"Error ENC1001: proj.csproj(0, 0, 0, 0): {string.Format(FeaturesResources.ErrorReadingFile, "proj", "error 2")}"
        ], sessionState.ApplyChangesDiagnostics.Select(Inspect));

        AssertEx.Equal(
        [
            $"Error ENC1001: test.cs(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "error 1")}",
            $"Error ENC1001: proj.csproj(0, 0, 0, 0): {string.Format(FeaturesResources.ErrorReadingFile, "proj", "error 2")}",
            $"Error ENC1001: test.cs(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "syntax error 3")}",
            $"RestartRequired ENC0033: test.cs(0, 2, 0, 3): {string.Format(FeaturesResources.Deleting_0_requires_restarting_the_application, "x")}"
        ], updates.Diagnostics.Select(Inspect));

        Assert.True(sessionState.IsSessionActive);

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
        var source2 = "class C1 { void M() { System.Console.WriteLine(\"b\"); } }";
        var source3 = "class C1 { void M() { System.Console.WriteLine(\"c\"); } }";

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

        var moduleId = EmitAndLoadLibraryToDebuggee(source1, sourceFilePath: sourceFile.Path);

        // hydrate document text and overwrite file content:
        var document1 = await solution.GetDocument(documentId).GetTextAsync();
        File.WriteAllText(sourceFile.Path, source2, Encoding.UTF8);

        await languageService.StartSessionAsync(CancellationToken.None);
        await languageService.EnterBreakStateAsync(CancellationToken.None);

        workspace.OnDocumentOpened(documentId, new TestSourceTextContainer()
        {
            Text = SourceText.From(source3, Encoding.UTF8, SourceHashAlgorithm.Sha1)
        });

        await workspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        var (key, (documentState, version)) = sourceTextProvider.GetTestAccessor().GetDocumentsWithChangedLoaderByPath().Single();
        Assert.Equal(sourceFile.Path, key);
        Assert.Equal(solution.WorkspaceVersion, version);
        Assert.Equal(source1, (await documentState.GetTextAsync(CancellationToken.None)).ToString());

        // check committed document status:
        var debuggingSession = service.GetTestAccessor().GetActiveDebuggingSessions().Single();
        var (document, state) = await debuggingSession.LastCommittedSolution.GetDocumentAndStateAsync(documentId, currentDocument: null, CancellationToken.None);
        var text = await document.GetTextAsync();
        Assert.Equal(CommittedSolution.DocumentState.MatchesBuildOutput, state);
        Assert.Equal(source1, (await document.GetTextAsync(CancellationToken.None)).ToString());

        await languageService.EndSessionAsync(CancellationToken.None);
    }
}
