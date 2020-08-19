// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;
using System.Composition;
using Roslyn.Test.Utilities.Remote;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Test.Utilities.RemoteHost;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Composition;
using System.Composition.Hosting;
using System.Collections.Generic;

namespace Roslyn.VisualStudio.Next.UnitTests.EditAndContinue
{
    [UseExportProvider]
    public class RemoteEditAndContinueServiceTests
    {
        [ExportWorkspaceServiceFactory(typeof(IEditAndContinueWorkspaceService), ServiceLayer.Test), Shared]
        internal sealed class MockEncServiceFactory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public MockEncServiceFactory()
            {
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
                => new MockEditAndContinueWorkspaceService();
        }

        private sealed class TestEditSessionCallback : IRemoteEditAndContinueService.IStartEditSessionCallback
        {
            private readonly ActiveStatementDebugInfo _info;

            public TestEditSessionCallback(ActiveStatementDebugInfo info)
            {
                _info = info;
            }

            public Task<ImmutableArray<ActiveStatementDebugInfo.Data>> GetActiveStatementsAsync(CancellationToken cancellationToken)
                => Task.FromResult(ImmutableArray.Create(_info.Serialize()));

            public Task<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
                => Task.FromResult(((int, string?)?)(1, "can't do enc"));

            public Task PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
                => Task.CompletedTask;
        }

        [Fact]
        public async Task Proxy()
        {
            var composition = EditorTestCompositions.EditorFeatures.WithTestHostParts(TestHost.OutOfProcess);

            using var workspace = new TestWorkspace(exportProvider: exportProviderFactory.CreateExportProvider());

            var options = workspace.Services.GetRequiredService<IOptionService>();
            options.SetOptions(options.GetOptions().WithChangedOption(RemoteHostOptions.RemoteHostTest, true));

            workspace.ChangeSolution(workspace.CurrentSolution.
                AddProject("proj", "proj", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From("class C { }", Encoding.UTF8), filePath: "test.cs").Project.Solution);

            var solution = workspace.CurrentSolution;
            var project = solution.Projects.Single();
            var document = project.Documents.Single();

            var clientProvider = (InProcRemoteHostClientProvider)workspace.Services.GetService<IRemoteHostClientProvider>();
            clientProvider.AdditionalRemoteParts = new[] { typeof(MockEncServiceFactory) };

            var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);
            var remoteWorkspace = client.GetRemoteWorkspace();
            var mockEncService = (MockEditAndContinueWorkspaceService)remoteWorkspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();

            var mockDiagnosticService = new Mock<IDiagnosticAnalyzerService>(MockBehavior.Strict);
            mockDiagnosticService.Setup(s => s.Reanalyze(It.IsAny<Workspace>(), It.IsAny<IEnumerable<ProjectId>>(), It.IsAny<IEnumerable<DocumentId>>(), It.IsAny<bool>()));

            void VerifyReanalyzeInvocation(ImmutableArray<DocumentId> documentIds)
               => mockDiagnosticService.Invocations.VerifyAndClear((nameof(IDiagnosticAnalyzerService.Reanalyze), new object[] { workspace, null, documentIds, false }));

            var diagnosticUpdateSource = new EditAndContinueDiagnosticUpdateSource();
            var emitDiagnosticsUpdated = new List<DiagnosticsUpdatedArgs>();
            var emitDiagnosticsClearedCount = 0;
            diagnosticUpdateSource.DiagnosticsUpdated += (object sender, DiagnosticsUpdatedArgs args) => emitDiagnosticsUpdated.Add(args);
            diagnosticUpdateSource.DiagnosticsCleared += (object sender, EventArgs args) => emitDiagnosticsClearedCount++;

            var span1 = new LinePositionSpan(new LinePosition(1, 2), new LinePosition(1, 5));
            var threadId1 = new Guid("{22222222-1111-1111-1111-111111111111}");
            var threadId2 = new Guid("{33333333-1111-1111-1111-111111111111}");
            var moduleId1 = new Guid("{44444444-1111-1111-1111-111111111111}");
            var instructionId1 = new ActiveInstructionId(moduleId1, methodToken: 0x06000003, methodVersion: 2, ilOffset: 10);

            var as1 = new ActiveStatementDebugInfo(
                instructionId1,
                documentName: "test.cs",
                span1,
                threadIds: ImmutableArray.Create(threadId1, threadId2),
                flags: ActiveStatementFlags.IsLeafFrame | ActiveStatementFlags.PartiallyExecuted);

            var methodId1 = new ActiveMethodId(new Guid("{11111111-1111-1111-1111-111111111111}"), token: 0x06000002, version: 1);

            var region1 = new NonRemappableRegion(
                new LinePositionSpan(new LinePosition(1, 2), new LinePosition(1, 5)),
                lineDelta: 1,
                isExceptionRegion: true);

            var document1 = workspace.CurrentSolution.Projects.Single().Documents.Single();

            var proxy = new RemoteEditAndContinueServiceProxy(workspace);

            // StartDebuggingSession

            mockEncService.StartDebuggingSessionImpl = solution =>
            {
                Assert.Equal("proj", solution.Projects.Single().Name);
            };

            await proxy.StartDebuggingSessionAsync(CancellationToken.None).ConfigureAwait(false);

            // StartEditSession

            ActiveStatementProvider? remoteActiveStatementProvider = null;
            IDebuggeeModuleMetadataProvider? remoteDebuggeeModuleMetadataProvider = null;
            mockEncService.StartEditSessionImpl = (ActiveStatementProvider activeStatementProvider, IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider, out ImmutableArray<DocumentId> documentsToReanalyze) =>
            {
                remoteActiveStatementProvider = activeStatementProvider;
                remoteDebuggeeModuleMetadataProvider = debuggeeModuleMetadataProvider;
                documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            };

            var callback = new TestEditSessionCallback(as1);
            await proxy.StartEditSessionAsync(mockDiagnosticService.Object, callback, CancellationToken.None).ConfigureAwait(false);
            VerifyReanalyzeInvocation(ImmutableArray<DocumentId>.Empty);

            var activeStatement = (await remoteActiveStatementProvider!(CancellationToken.None).ConfigureAwait(false)).Single();
            Assert.Equal(as1.InstructionId, activeStatement.InstructionId);
            Assert.Equal(as1.LinePositionSpan, activeStatement.LinePositionSpan);
            AssertEx.Equal(as1.ThreadIds, activeStatement.ThreadIds);
            Assert.Equal(as1.Flags, activeStatement.Flags);

            var availability = await remoteDebuggeeModuleMetadataProvider!.GetEncAvailabilityAsync(moduleId1, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal<(int, string?)?>((1, "can't do enc"), availability);

            // EndEditSession

            mockEncService.EndEditSessionImpl = (out ImmutableArray<DocumentId> documentsToReanalyze) =>
            {
                documentsToReanalyze = ImmutableArray.Create(document.Id);
            };

            await proxy.EndEditSessionAsync(mockDiagnosticService.Object, CancellationToken.None).ConfigureAwait(false);
            VerifyReanalyzeInvocation(ImmutableArray.Create(document.Id));

            // EndDebuggingSession

            mockEncService.EndDebuggingSessionImpl = (out ImmutableArray<DocumentId> documentsToReanalyze) =>
            {
                documentsToReanalyze = ImmutableArray.Create(document.Id);
            };

            await proxy.EndDebuggingSessionAsync(diagnosticUpdateSource, mockDiagnosticService.Object, CancellationToken.None).ConfigureAwait(false);
            VerifyReanalyzeInvocation(ImmutableArray.Create(document.Id));
            Assert.Equal(1, emitDiagnosticsClearedCount);
            emitDiagnosticsClearedCount = 0;
            Assert.Empty(emitDiagnosticsUpdated);

            // HasChanges

            mockEncService.HasChangesAsyncImpl = (solution, sourceFilePath) =>
            {
                Assert.Equal("proj", solution.Projects.Single().Name);
                Assert.Equal("test.cs", sourceFilePath);
                return true;
            };

            Assert.True(await proxy.HasChangesAsync("test.cs", CancellationToken.None).ConfigureAwait(false));

            // EmitSolutionUpdate

            var diagnosticDescriptor1 = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);

            mockEncService.EmitSolutionUpdateAsyncImpl = solution =>
            {
                var project = solution.Projects.Single();
                Assert.Equal("proj", project.Name);

                var deltas = ImmutableArray.Create(new Deltas(
                    mvid: moduleId1,
                    il: ImmutableArray.Create<byte>(1, 2),
                    metadata: ImmutableArray.Create<byte>(3, 4),
                    pdb: ImmutableArray.Create<byte>(5, 6),
                    updatedMethods: ImmutableArray.Create(0x06000001),
                    lineEdits: ImmutableArray.Create(("file.cs", ImmutableArray.Create(new LineChange(1, 2)))),
                    nonRemappableRegions: ImmutableArray.Create((methodId1, region1)),
                    activeStatementsInUpdatedMethods: ImmutableArray.Create((threadId1, instructionId1, span1))));

                var syntaxTree = project.Documents.Single().GetSyntaxTreeSynchronously(CancellationToken.None);

                var documentDiagnostic = Diagnostic.Create(diagnosticDescriptor1, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)), new[] { "doc", "some error" });
                var projectDiagnostic = Diagnostic.Create(diagnosticDescriptor1, Location.None, new[] { "proj", "some error" });
                var solutionDiagnostic = Diagnostic.Create(diagnosticDescriptor1, Location.None, new[] { "sol", "some error" });

                var diagnostics = ImmutableArray.Create(
                    (project.Id, ImmutableArray.Create(documentDiagnostic)),
                    (project.Id, ImmutableArray.Create(projectDiagnostic)),
                    (null, ImmutableArray.Create(solutionDiagnostic)));

                return (SolutionUpdateStatus.Ready, deltas, diagnostics);
            };

            var (status, deltas) = await proxy.EmitSolutionUpdateAsync(diagnosticUpdateSource, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Ready, status);

            Assert.Equal(1, emitDiagnosticsClearedCount);
            emitDiagnosticsClearedCount = 0;

            AssertEx.Equal(new[]
            {
                $"[{project.Id}] Error ENC1001: test.cs(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "some error")}",
                $"[{project.Id}] Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "proj", "some error")}",
                $"[] Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "sol", "some error")}",
            },
            emitDiagnosticsUpdated.Select(update =>
            {
                var d = update.Diagnostics.Single();
                return $"[{d.ProjectId}] {d.Severity} {d.Id}:" +
                       (d.DataLocation != null ? $" {d.DataLocation.OriginalFilePath}({d.DataLocation.OriginalStartLine}, {d.DataLocation.OriginalStartColumn}, {d.DataLocation.OriginalEndLine}, {d.DataLocation.OriginalEndColumn}):" : "") +
                       $" {d.Message}";
            }));

            var delta = deltas.Single();
            Assert.Equal(moduleId1, delta.Mvid);
            AssertEx.Equal(new byte[] { 1, 2 }, delta.IL);
            AssertEx.Equal(new byte[] { 3, 4 }, delta.Metadata);
            AssertEx.Equal(new byte[] { 5, 6 }, delta.Pdb);
            AssertEx.Equal(new[] { 0x06000001 }, delta.UpdatedMethods);

            var lineEdit = delta.LineEdits.Single();
            Assert.Equal("file.cs", lineEdit.SourceFilePath);
            AssertEx.Equal(new[] { new LineChange(1, 2) }, lineEdit.Deltas);
            Assert.Equal((methodId1, region1), delta.NonRemappableRegions.Single());

            var activeStatements = delta.ActiveStatementsInUpdatedMethods.Single();
            Assert.Equal(threadId1, activeStatements.ThreadId);
            Assert.Equal(instructionId1, activeStatements.OldInstructionId);
            Assert.Equal(span1, activeStatements.NewSpan);

            // CommitUpdates

            await proxy.CommitUpdatesAsync(CancellationToken.None).ConfigureAwait(false);

            // DiscardUpdates

            await proxy.DiscardUpdatesAsync(CancellationToken.None).ConfigureAwait(false);

            // GetCurrentActiveStatementPosition

            mockEncService.GetCurrentActiveStatementPositionAsyncImpl = (solution, instructionId) =>
            {
                Assert.Equal("proj", solution.Projects.Single().Name);
                Assert.Equal(instructionId1, instructionId);
                return new LinePositionSpan(new LinePosition(1, 2), new LinePosition(1, 5));
            };

            Assert.Equal(span1, await proxy.GetCurrentActiveStatementPositionAsync(moduleId1, methodToken: 0x06000003, methodVersion: 2, ilOffset: 10, CancellationToken.None).ConfigureAwait(false));

            // IsActiveStatementInExceptionRegion

            mockEncService.IsActiveStatementInExceptionRegionAsyncImpl = instructionId =>
            {
                Assert.Equal(instructionId1, instructionId);
                return true;
            };

            Assert.True(await proxy.IsActiveStatementInExceptionRegionAsync(moduleId1, methodToken: 0x06000003, methodVersion: 2, ilOffset: 10, CancellationToken.None).ConfigureAwait(false));

            // GetBaseActiveStatementSpans

            mockEncService.GetBaseActiveStatementSpansAsyncImpl = (documentIds) =>
            {
                AssertEx.Equal(new[] { document1.Id }, documentIds);
                return ImmutableArray.Create(ImmutableArray.Create((span1, ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.PartiallyExecuted)));
            };

            var baseActiveSpans = await proxy.GetBaseActiveStatementSpansAsync(ImmutableArray.Create(document1.Id), CancellationToken.None).ConfigureAwait(false);
            Assert.Equal((span1, ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.PartiallyExecuted), baseActiveSpans.Single().Single());

            // GetDocumentActiveStatementSpans

            mockEncService.GetDocumentActiveStatementSpansAsyncImpl = (document) =>
            {
                Assert.Equal("test.cs", document.Name);
                return ImmutableArray.Create((span1, ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.PartiallyExecuted));
            };

            var documentActiveSpans = await proxy.GetDocumentActiveStatementSpansAsync(document1, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal((span1, ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.PartiallyExecuted), documentActiveSpans.Single());

            // GetDocumentActiveStatementSpans (default array)

            mockEncService.GetDocumentActiveStatementSpansAsyncImpl = (document) => default;

            documentActiveSpans = await proxy.GetDocumentActiveStatementSpansAsync(document1, CancellationToken.None).ConfigureAwait(false);
            Assert.True(documentActiveSpans.IsDefault);

            // OnSourceFileUpdatedAsync

            mockEncService.OnSourceFileUpdatedImpl = documentId => Assert.Equal(document.Id, documentId);

            await proxy.OnSourceFileUpdatedAsync(document.Id, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
