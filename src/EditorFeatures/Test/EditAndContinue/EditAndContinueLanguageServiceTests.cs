// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using DebuggerContracts = Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Roslyn.VisualStudio.Next.UnitTests.EditAndContinue
{
    [UseExportProvider]
    public class EditAndContinueLanguageServiceTests
    {
        private static string Inspect(DiagnosticData d)
            => $"{d.Severity} {d.Id}:" +
                (!string.IsNullOrWhiteSpace(d.DataLocation.UnmappedFileSpan.Path) ? $" {d.DataLocation.UnmappedFileSpan.Path}({d.DataLocation.UnmappedFileSpan.StartLinePosition.Line}, {d.DataLocation.UnmappedFileSpan.StartLinePosition.Character}, {d.DataLocation.UnmappedFileSpan.EndLinePosition.Line}, {d.DataLocation.UnmappedFileSpan.EndLinePosition.Character}):" : "") +
                $" {d.Message}";

        private static string Inspect(DebuggerContracts.ManagedHotReloadDiagnostic d)
            => $"{d.Severity} {d.Id}:" +
                (!string.IsNullOrWhiteSpace(d.FilePath) ? $" {d.FilePath}({d.Span.StartLine}, {d.Span.StartColumn}, {d.Span.EndLine}, {d.Span.EndColumn}):" : "") +
                $" {d.Message}";

        [Theory, CombinatorialData]
        public async Task Test(bool commitChanges)
        {
            var localComposition = EditorTestCompositions.LanguageServerProtocolEditorFeatures
                .AddExcludedPartTypes(typeof(EditAndContinueService))
                .AddParts(
                    typeof(NoCompilationLanguageService),
                    typeof(MockHostWorkspaceProvider),
                    typeof(MockServiceBrokerProvider),
                    typeof(MockEditAndContinueWorkspaceService),
                    typeof(MockManagedHotReloadService));

            using var localWorkspace = new TestWorkspace(composition: localComposition);

            var globalOptions = localWorkspace.GetService<IGlobalOptionService>();
            ((MockHostWorkspaceProvider)localWorkspace.GetService<IHostWorkspaceProvider>()).Workspace = localWorkspace;

            ((MockServiceBroker)localWorkspace.GetService<IServiceBrokerProvider>().ServiceBroker).CreateService = t => t switch
            {
                _ when t == typeof(DebuggerContracts.IHotReloadLogger) => new MockHotReloadLogger(),
                _ => throw ExceptionUtilities.UnexpectedValue(t)
            };

            MockEditAndContinueWorkspaceService mockEncService;

            mockEncService = (MockEditAndContinueWorkspaceService)localWorkspace.GetService<IEditAndContinueService>();

            var localService = localWorkspace.GetService<EditAndContinueLanguageService>();

            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            await localWorkspace.ChangeSolutionAsync(localWorkspace.CurrentSolution
                .AddProject(projectId, "proj", "proj", LanguageNames.CSharp)
                .AddMetadataReferences(projectId, TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40))
                .AddDocument(documentId, "test.cs", SourceText.From("class C { }", Encoding.UTF8), filePath: "test.cs"));

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

            mockEncService.BreakStateOrCapabilitiesChangedImpl = (bool? inBreakState, out ImmutableArray<DocumentId> documentsToReanalyze) =>
            {
                Assert.True(inBreakState);
                documentsToReanalyze = [];
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

                var documentDiagnostic = Diagnostic.Create(diagnosticDescriptor1, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)), ["doc", "error 1"]);
                var projectDiagnostic = Diagnostic.Create(diagnosticDescriptor1, Location.None, ["proj", "error 2"]);
                var syntaxError = Diagnostic.Create(diagnosticDescriptor1, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)), ["doc", "syntax error 3"]);

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
                $"Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "proj", "error 2")}"
            ], sessionState.ApplyChangesDiagnostics.Select(Inspect));

            AssertEx.Equal(
            [
                $"Error ENC1001: test.cs(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "error 1")}",
                $"Error ENC1001: test.cs(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "syntax error 3")}",
                $"RestartRequired ENC0033: test.cs(0, 2, 0, 3): {string.Format(FeaturesResources.Deleting_0_requires_restarting_the_application, "x")}"
            ], updates.Diagnostics.Select(Inspect));

            Assert.True(sessionState.IsSessionActive);

            if (commitChanges)
            {
                // CommitUpdatesAsync

                mockEncService.CommitSolutionUpdateImpl = (out ImmutableArray<DocumentId> documentsToReanalyze) =>
                {
                    documentsToReanalyze = ImmutableArray.Create(documentId);
                };

                await localService.CommitUpdatesAsync(CancellationToken.None);
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

            mockEncService.EndDebuggingSessionImpl = (out ImmutableArray<DocumentId> documentsToReanalyze) =>
            {
                documentsToReanalyze = ImmutableArray.Create(documentId);
            };

            await localService.EndSessionAsync(CancellationToken.None);

            Assert.Equal(++observedDiagnosticVersion, diagnosticRefresher.GlobalStateVersion);
            Assert.Empty(sessionState.ApplyChangesDiagnostics);
            Assert.False(sessionState.IsSessionActive);
        }
    }
}
