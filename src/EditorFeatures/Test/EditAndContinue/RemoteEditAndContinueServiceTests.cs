// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UnitTests;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.EditAndContinue
{
    [UseExportProvider]
    public class RemoteEditAndContinueServiceTests
    {
        private static string Inspect(DiagnosticData d)
            => $"[{d.ProjectId}] {d.Severity} {d.Id}:" +
                (!string.IsNullOrWhiteSpace(d.DataLocation.UnmappedFileSpan.Path) ? $" {d.DataLocation.UnmappedFileSpan.Path}({d.DataLocation.UnmappedFileSpan.StartLinePosition.Line}, {d.DataLocation.UnmappedFileSpan.StartLinePosition.Character}, {d.DataLocation.UnmappedFileSpan.EndLinePosition.Line}, {d.DataLocation.UnmappedFileSpan.EndLinePosition.Character}):" : "") +
                $" {d.Message}";

        [Theory]
        [CombinatorialData]
        public async Task Proxy(TestHost testHost)
        {
            var localComposition = EditorTestCompositions.EditorFeatures.WithTestHostParts(testHost)
                .AddExcludedPartTypes(typeof(DiagnosticAnalyzerService))
                .AddParts(typeof(MockDiagnosticAnalyzerService), typeof(NoCompilationLanguageService));

            if (testHost == TestHost.InProcess)
            {
                localComposition = localComposition
                    .AddExcludedPartTypes(typeof(EditAndContinueService))
                    .AddParts(typeof(MockEditAndContinueWorkspaceService));
            }

            using var localWorkspace = new TestWorkspace(composition: localComposition);

            var globalOptions = localWorkspace.GetService<IGlobalOptionService>();

            MockEditAndContinueWorkspaceService mockEncService;
            var clientProvider = (InProcRemoteHostClientProvider?)localWorkspace.Services.GetService<IRemoteHostClientProvider>();
            if (testHost == TestHost.InProcess)
            {
                Assert.Null(clientProvider);

                mockEncService = (MockEditAndContinueWorkspaceService)localWorkspace.GetService<IEditAndContinueService>();
            }
            else
            {
                Assert.NotNull(clientProvider);
                clientProvider!.AdditionalRemoteParts = [typeof(MockEditAndContinueWorkspaceService)];
                clientProvider!.ExcludedRemoteParts = [typeof(EditAndContinueService)];

                var client = await InProcRemoteHostClient.GetTestClientAsync(localWorkspace);
                var remoteWorkspace = client.TestData.WorkspaceManager.GetWorkspace();
                mockEncService = (MockEditAndContinueWorkspaceService)remoteWorkspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;
            }

            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);
            var inProcOnlyProjectId = ProjectId.CreateNewId();
            var inProcOnlyDocumentId = DocumentId.CreateNewId(inProcOnlyProjectId);

            await localWorkspace.ChangeSolutionAsync(localWorkspace.CurrentSolution
                .AddProject(projectId, "proj", "proj", LanguageNames.CSharp)
                .AddMetadataReferences(projectId, TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40))
                .AddDocument(documentId, "test.cs", SourceText.From("class C { }", Encoding.UTF8), filePath: "test.cs")
                .AddProject(inProcOnlyProjectId, "in-proc-only", "in-proc-only.proj", NoCompilationConstants.LanguageName)
                .AddDocument(inProcOnlyDocumentId, "test", "text"));

            var solution = localWorkspace.CurrentSolution;
            var project = solution.GetRequiredProject(projectId);
            var inProcOnlyProject = solution.GetRequiredProject(inProcOnlyProjectId);
            var document = solution.GetRequiredDocument(documentId);
            var inProcOnlyDocument = solution.GetRequiredDocument(inProcOnlyDocumentId);
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);

            var mockDiagnosticService = (MockDiagnosticAnalyzerService)localWorkspace.GetService<IDiagnosticAnalyzerService>();

            void VerifyReanalyzeInvocation(ImmutableArray<DocumentId> documentIds)
            {
                AssertEx.Equal(documentIds, mockDiagnosticService.DocumentsToReanalyze);
                mockDiagnosticService.DocumentsToReanalyze.Clear();
            }

            var diagnosticUpdateSource = new EditAndContinueDiagnosticUpdateSource();
            var emitDiagnosticsUpdated = new List<DiagnosticsUpdatedArgs>();
            var emitDiagnosticsClearedCount = 0;
            diagnosticUpdateSource.DiagnosticsUpdated += (object sender, ImmutableArray<DiagnosticsUpdatedArgs> args) => emitDiagnosticsUpdated.AddRange(args);
            diagnosticUpdateSource.DiagnosticsCleared += (object sender, EventArgs args) => emitDiagnosticsClearedCount++;

            var span1 = new LinePositionSpan(new LinePosition(1, 2), new LinePosition(1, 5));
            var moduleId1 = new Guid("{44444444-1111-1111-1111-111111111111}");
            var methodId1 = new ManagedMethodId(moduleId1, token: 0x06000003, version: 2);
            var instructionId1 = new ManagedInstructionId(methodId1, ilOffset: 10);

            var as1 = new ManagedActiveStatementDebugInfo(
                instructionId1,
                documentName: "test.cs",
                span1.ToSourceSpan(),
                flags: ActiveStatementFlags.LeafFrame | ActiveStatementFlags.PartiallyExecuted);

            var methodId2 = new ManagedModuleMethodId(token: 0x06000002, version: 1);

            var exceptionRegionUpdate1 = new ManagedExceptionRegionUpdate(
                methodId2,
                delta: 1,
                newSpan: new SourceSpan(1, 2, 1, 5));

            var activeSpans1 = ImmutableArray.Create(
                new ActiveStatementSpan(0, new LinePositionSpan(new LinePosition(1, 2), new LinePosition(3, 4)), ActiveStatementFlags.NonLeafFrame, documentId));

            var activeStatementSpanProvider = new ActiveStatementSpanProvider((documentId, path, cancellationToken) =>
            {
                Assert.Equal(documentId, documentId);
                Assert.Equal("test.cs", path);
                return new(activeSpans1);
            });

            var diagnosticDescriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.AddingTypeRuntimeCapabilityRequired);
            var diagnostic = Diagnostic.Create(diagnosticDescriptor, Location.Create(syntaxTree, TextSpan.FromBounds(1, 1)));

            var proxy = new RemoteEditAndContinueServiceProxy(localWorkspace);

            // StartDebuggingSession

            IManagedHotReloadService? remoteDebuggeeModuleMetadataProvider = null;

            var debuggingSession = mockEncService.StartDebuggingSessionImpl = (solution, debuggerService, sourceTextProvider, captureMatchingDocuments, captureAllMatchingDocuments, reportDiagnostics) =>
            {
                Assert.Equal("proj", solution.GetRequiredProject(projectId).Name);
                AssertEx.Equal(new[] { documentId }, captureMatchingDocuments);
                Assert.False(captureAllMatchingDocuments);
                Assert.True(reportDiagnostics);

                remoteDebuggeeModuleMetadataProvider = debuggerService;
                return new DebuggingSessionId(1);
            };

            var sessionProxy = await proxy.StartDebuggingSessionAsync(
                localWorkspace.CurrentSolution,
                debuggerService: new MockManagedEditAndContinueDebuggerService()
                {
                    IsEditAndContinueAvailable = _ => new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.NotAllowedForModule, "can't do enc"),
                    GetActiveStatementsImpl = () => ImmutableArray.Create(as1)
                },
                sourceTextProvider: NullPdbMatchingSourceTextProvider.Instance,
                captureMatchingDocuments: ImmutableArray.Create(documentId),
                captureAllMatchingDocuments: false,
                reportDiagnostics: true,
                CancellationToken.None);

            Contract.ThrowIfNull(sessionProxy);

            // BreakStateChanged

            mockEncService.BreakStateOrCapabilitiesChangedImpl = (bool? inBreakState, out ImmutableArray<DocumentId> documentsToReanalyze) =>
            {
                Assert.True(inBreakState);
                documentsToReanalyze = ImmutableArray.Create(documentId);
            };

            await sessionProxy.BreakStateOrCapabilitiesChangedAsync(mockDiagnosticService, diagnosticUpdateSource, inBreakState: true, CancellationToken.None);
            VerifyReanalyzeInvocation(ImmutableArray.Create(documentId));

            Assert.Equal(1, emitDiagnosticsClearedCount);
            emitDiagnosticsClearedCount = 0;

            var activeStatement = (await remoteDebuggeeModuleMetadataProvider!.GetActiveStatementsAsync(CancellationToken.None)).Single();
            Assert.Equal(as1.ActiveInstruction, activeStatement.ActiveInstruction);
            Assert.Equal(as1.SourceSpan, activeStatement.SourceSpan);
            Assert.Equal(as1.Flags, activeStatement.Flags);

            var availability = await remoteDebuggeeModuleMetadataProvider!.GetAvailabilityAsync(moduleId1, CancellationToken.None);
            Assert.Equal(new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.NotAllowedForModule, "can't do enc"), availability);

            // EmitSolutionUpdate

            var diagnosticDescriptor1 = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);

            mockEncService.EmitSolutionUpdateImpl = (solution, activeStatementSpanProvider) =>
            {
                var project = solution.GetRequiredProject(projectId);
                Assert.Equal("proj", project.Name);
                AssertEx.Equal(activeSpans1, activeStatementSpanProvider(documentId, "test.cs", CancellationToken.None).AsTask().Result);

                var deltas = ImmutableArray.Create(new ManagedHotReloadUpdate(
                    module: moduleId1,
                    moduleName: "mod",
                    ilDelta: ImmutableArray.Create<byte>(1, 2),
                    metadataDelta: ImmutableArray.Create<byte>(3, 4),
                    pdbDelta: ImmutableArray.Create<byte>(5, 6),
                    updatedMethods: ImmutableArray.Create(0x06000001),
                    updatedTypes: ImmutableArray.Create(0x02000001),
                    sequencePoints: ImmutableArray.Create(new SequencePointUpdates("file.cs", ImmutableArray.Create(new SourceLineUpdate(1, 2)))),
                    activeStatements: ImmutableArray.Create(new ManagedActiveStatementUpdate(instructionId1.Method.Method, instructionId1.ILOffset, span1.ToSourceSpan())),
                    exceptionRegions: ImmutableArray.Create(exceptionRegionUpdate1),
                    requiredCapabilities: EditAndContinueCapabilities.Baseline.ToStringArray()));

                var syntaxTree = solution.GetRequiredDocument(documentId).GetSyntaxTreeSynchronously(CancellationToken.None)!;

                var documentDiagnostic = Diagnostic.Create(diagnosticDescriptor1, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)), new[] { "doc", "some error" });
                var projectDiagnostic = Diagnostic.Create(diagnosticDescriptor1, Location.None, new[] { "proj", "some error" });
                var syntaxError = Diagnostic.Create(diagnosticDescriptor1, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)), new[] { "doc", "syntax error" });

                var updates = new ModuleUpdates(ModuleUpdateStatus.Ready, deltas);
                var diagnostics = ImmutableArray.Create(new ProjectDiagnostics(project.Id, ImmutableArray.Create(documentDiagnostic, projectDiagnostic)));
                var documentsWithRudeEdits = ImmutableArray.Create((documentId, ImmutableArray<RudeEditDiagnostic>.Empty));

                return new()
                {
                    ModuleUpdates = updates,
                    Diagnostics = diagnostics,
                    RudeEdits = documentsWithRudeEdits,
                    SyntaxError = syntaxError
                };
            };

            var (updates, _, _, syntaxErrorData) = await sessionProxy.EmitSolutionUpdateAsync(localWorkspace.CurrentSolution, activeStatementSpanProvider, mockDiagnosticService, diagnosticUpdateSource, CancellationToken.None);
            AssertEx.Equal($"[{projectId}] Error ENC1001: test.cs(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "syntax error")}", Inspect(syntaxErrorData!));

            VerifyReanalyzeInvocation(ImmutableArray.Create(documentId));

            Assert.Equal(ModuleUpdateStatus.Ready, updates.Status);

            Assert.Equal(1, emitDiagnosticsClearedCount);
            emitDiagnosticsClearedCount = 0;

            AssertEx.Equal(new[]
            {
                $"[{projectId}] Error ENC1001: test.cs(0, 1, 0, 2): {string.Format(FeaturesResources.ErrorReadingFile, "doc", "some error")}",
                $"[{projectId}] Error ENC1001: {string.Format(FeaturesResources.ErrorReadingFile, "proj", "some error")}"
            }, emitDiagnosticsUpdated.Select(update => Inspect(update.Diagnostics.Single())));

            emitDiagnosticsUpdated.Clear();

            var delta = updates.Updates.Single();
            Assert.Equal(moduleId1, delta.Module);
            AssertEx.Equal(new byte[] { 1, 2 }, delta.ILDelta);
            AssertEx.Equal(new byte[] { 3, 4 }, delta.MetadataDelta);
            AssertEx.Equal(new byte[] { 5, 6 }, delta.PdbDelta);
            AssertEx.Equal(new[] { 0x06000001 }, delta.UpdatedMethods);
            AssertEx.Equal(new[] { 0x02000001 }, delta.UpdatedTypes);

            var lineEdit = delta.SequencePoints.Single();
            Assert.Equal("file.cs", lineEdit.FileName);
            AssertEx.Equal(new[] { new SourceLineUpdate(1, 2) }, lineEdit.LineUpdates);
            Assert.Equal(exceptionRegionUpdate1, delta.ExceptionRegions.Single());

            var activeStatements = delta.ActiveStatements.Single();
            Assert.Equal(instructionId1.Method.Method, activeStatements.Method);
            Assert.Equal(instructionId1.ILOffset, activeStatements.ILOffset);
            Assert.Equal(span1, activeStatements.NewSpan.ToLinePositionSpan());

            // CommitSolutionUpdate

            mockEncService.CommitSolutionUpdateImpl = (out ImmutableArray<DocumentId> documentsToReanalyze) =>
            {
                documentsToReanalyze = ImmutableArray.Create(documentId);
            };

            await sessionProxy.CommitSolutionUpdateAsync(mockDiagnosticService, CancellationToken.None);
            VerifyReanalyzeInvocation(ImmutableArray.Create(documentId));

            // DiscardSolutionUpdate

            var called = false;
            mockEncService.DiscardSolutionUpdateImpl = () => called = true;
            await sessionProxy.DiscardSolutionUpdateAsync(CancellationToken.None);
            Assert.True(called);

            // GetBaseActiveStatementSpans

            var activeStatementSpan1 = new ActiveStatementSpan(0, span1, ActiveStatementFlags.NonLeafFrame | ActiveStatementFlags.PartiallyExecuted, unmappedDocumentId: documentId);

            mockEncService.GetBaseActiveStatementSpansImpl = (solution, documentIds) =>
            {
                AssertEx.Equal(new[] { documentId, inProcOnlyDocumentId }, documentIds);
                return ImmutableArray.Create(ImmutableArray.Create(activeStatementSpan1));
            };

            var baseActiveSpans = await sessionProxy.GetBaseActiveStatementSpansAsync(localWorkspace.CurrentSolution, ImmutableArray.Create(documentId, inProcOnlyDocumentId), CancellationToken.None);
            Assert.Equal(activeStatementSpan1, baseActiveSpans.Single().Single());

            // GetDocumentActiveStatementSpans

            mockEncService.GetAdjustedActiveStatementSpansImpl = (document, activeStatementSpanProvider) =>
            {
                Assert.Equal("test.cs", document.Name);
                AssertEx.Equal(activeSpans1, activeStatementSpanProvider(documentId, "test.cs", CancellationToken.None).AsTask().Result);
                return ImmutableArray.Create(activeStatementSpan1);
            };

            Assert.Empty(await sessionProxy.GetAdjustedActiveStatementSpansAsync(inProcOnlyDocument, activeStatementSpanProvider, CancellationToken.None));

            var documentActiveSpans = await sessionProxy.GetAdjustedActiveStatementSpansAsync(document, activeStatementSpanProvider, CancellationToken.None);
            Assert.Equal(activeStatementSpan1, documentActiveSpans.Single());

            // GetDocumentActiveStatementSpans (default array)

            mockEncService.GetAdjustedActiveStatementSpansImpl = (document, _) => default;

            documentActiveSpans = await sessionProxy.GetAdjustedActiveStatementSpansAsync(document, activeStatementSpanProvider, CancellationToken.None);
            Assert.True(documentActiveSpans.IsDefault);

            // GetDocumentDiagnosticsAsync

            mockEncService.GetDocumentDiagnosticsImpl = (document, activeStatementProvider) => ImmutableArray.Create(diagnostic);

            Assert.Empty(await proxy.GetDocumentDiagnosticsAsync(inProcOnlyDocument, inProcOnlyDocument, activeStatementSpanProvider, CancellationToken.None));
            Assert.Equal(diagnostic.GetMessage(), (await proxy.GetDocumentDiagnosticsAsync(document, document, activeStatementSpanProvider, CancellationToken.None)).Single().GetMessage());

            // EndDebuggingSession

            mockEncService.EndDebuggingSessionImpl = (out ImmutableArray<DocumentId> documentsToReanalyze) =>
            {
                documentsToReanalyze = ImmutableArray.Create(documentId);
            };

            await sessionProxy.EndDebuggingSessionAsync(solution, diagnosticUpdateSource, mockDiagnosticService, CancellationToken.None);
            VerifyReanalyzeInvocation(ImmutableArray.Create(documentId));
            Assert.Equal(1, emitDiagnosticsClearedCount);
            emitDiagnosticsClearedCount = 0;
            Assert.Empty(emitDiagnosticsUpdated);
        }
    }
}
