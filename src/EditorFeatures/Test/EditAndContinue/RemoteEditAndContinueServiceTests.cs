﻿// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

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

        [Theory, CombinatorialData]
        public async Task Proxy(TestHost testHost)
        {
            var localComposition = EditorTestCompositions.EditorFeatures.WithTestHostParts(testHost);
            if (testHost == TestHost.InProcess)
            {
                localComposition = localComposition.AddParts(typeof(MockEncServiceFactory));
            }

            using var localWorkspace = new TestWorkspace(composition: localComposition);

            MockEditAndContinueWorkspaceService mockEncService;
            var clientProvider = (InProcRemoteHostClientProvider?)localWorkspace.Services.GetService<IRemoteHostClientProvider>();
            if (testHost == TestHost.InProcess)
            {
                Assert.Null(clientProvider);

                mockEncService = (MockEditAndContinueWorkspaceService)localWorkspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
            }
            else
            {
                Assert.NotNull(clientProvider);
                clientProvider!.AdditionalRemoteParts = new[] { typeof(MockEncServiceFactory) };

                var client = await InProcRemoteHostClient.GetTestClientAsync(localWorkspace).ConfigureAwait(false);
                var remoteWorkspace = client.TestData.WorkspaceManager.GetWorkspace();
                mockEncService = (MockEditAndContinueWorkspaceService)remoteWorkspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();
            }

            localWorkspace.ChangeSolution(localWorkspace.CurrentSolution.
                AddProject("proj", "proj", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From("class C { }", Encoding.UTF8), filePath: "test.cs").Project.Solution);

            var solution = localWorkspace.CurrentSolution;
            var project = solution.Projects.Single();
            var document = project.Documents.Single();

            var mockDiagnosticService = new Mock<IDiagnosticAnalyzerService>(MockBehavior.Strict);
            mockDiagnosticService.Setup(s => s.Reanalyze(It.IsAny<Workspace>(), It.IsAny<IEnumerable<ProjectId>>(), It.IsAny<IEnumerable<DocumentId>>(), It.IsAny<bool>()));

            void VerifyReanalyzeInvocation(ImmutableArray<DocumentId> documentIds)
               => mockDiagnosticService.Invocations.VerifyAndClear((nameof(IDiagnosticAnalyzerService.Reanalyze), new object?[] { localWorkspace, null, documentIds, false }));

            var diagnosticUpdateSource = new EditAndContinueDiagnosticUpdateSource();
            var emitDiagnosticsUpdated = new List<DiagnosticsUpdatedArgs>();
            var emitDiagnosticsClearedCount = 0;
            diagnosticUpdateSource.DiagnosticsUpdated += (object sender, DiagnosticsUpdatedArgs args) => emitDiagnosticsUpdated.Add(args);
            diagnosticUpdateSource.DiagnosticsCleared += (object sender, EventArgs args) => emitDiagnosticsClearedCount++;

            var span1 = new LinePositionSpan(new LinePosition(1, 2), new LinePosition(1, 5));
            var moduleId1 = new Guid("{44444444-1111-1111-1111-111111111111}");
            var methodId1 = new ManagedMethodId(moduleId1, token: 0x06000003, version: 2);
            var instructionId1 = new ManagedInstructionId(methodId1, ilOffset: 10);

            var as1 = new ManagedActiveStatementDebugInfo(
                instructionId1,
                documentName: "test.cs",
                span1.ToSourceSpan(),
                flags: ActiveStatementFlags.IsLeafFrame | ActiveStatementFlags.PartiallyExecuted);

            var methodId2 = new ManagedModuleMethodId(token: 0x06000002, version: 1);

            var exceptionRegionUpdate1 = new ManagedExceptionRegionUpdate(
                methodId2,
                delta: 1,
                newSpan: new SourceSpan(1, 2, 1, 5));

            var document1 = localWorkspace.CurrentSolution.Projects.Single().Documents.Single();

            var activeSpans1 = ImmutableArray.Create(TextSpan.FromBounds(1, 2), TextSpan.FromBounds(3, 4));

            var solutionActiveStatementSpanProvider = new SolutionActiveStatementSpanProvider((documentId, cancellationToken) =>
            {
                Assert.Equal(document1.Id, documentId);
                return new(activeSpans1);
            });

            var documentActiveStatementSpanProvider = new DocumentActiveStatementSpanProvider(cancellationToken => new(activeSpans1));

            var proxy = new RemoteEditAndContinueServiceProxy(localWorkspace);

            // StartDebuggingSession

            var called = false;
            mockEncService.StartDebuggingSessionImpl = solution =>
            {
                Assert.Equal("proj", solution.Projects.Single().Name);
                called = true;
            };

            await proxy.StartDebuggingSessionAsync(localWorkspace.CurrentSolution, CancellationToken.None).ConfigureAwait(false);
            Assert.True(called);

            // StartEditSession

            IManagedEditAndContinueDebuggerService? remoteDebuggeeModuleMetadataProvider = null;
            mockEncService.StartEditSessionImpl = (IManagedEditAndContinueDebuggerService debuggerService, out ImmutableArray<DocumentId> documentsToReanalyze) =>
            {
                remoteDebuggeeModuleMetadataProvider = debuggerService;
                documentsToReanalyze = ImmutableArray<DocumentId>.Empty;
            };

            await proxy.StartEditSessionAsync(
                mockDiagnosticService.Object,
                debuggerService: new MockManagedEditAndContinueDebuggerService()
                {
                    IsEditAndContinueAvailable = _ => new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.NotAllowedForModule, "can't do enc"),
                    GetActiveStatementsImpl = () => ImmutableArray.Create(as1)
                },
                CancellationToken.None).ConfigureAwait(false);

            VerifyReanalyzeInvocation(ImmutableArray<DocumentId>.Empty);

            var activeStatement = (await remoteDebuggeeModuleMetadataProvider!.GetActiveStatementsAsync(CancellationToken.None).ConfigureAwait(false)).Single();
            Assert.Equal(as1.ActiveInstruction, activeStatement.ActiveInstruction);
            Assert.Equal(as1.SourceSpan, activeStatement.SourceSpan);
            Assert.Equal(as1.Flags, activeStatement.Flags);

            var availability = await remoteDebuggeeModuleMetadataProvider!.GetAvailabilityAsync(moduleId1, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(new ManagedEditAndContinueAvailability(ManagedEditAndContinueAvailabilityStatus.NotAllowedForModule, "can't do enc"), availability);

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

            mockEncService.HasChangesImpl = (solution, activeStatementSpanProvider, sourceFilePath) =>
            {
                Assert.Equal("proj", solution.Projects.Single().Name);
                Assert.Equal("test.cs", sourceFilePath);
                AssertEx.Equal(activeSpans1, activeStatementSpanProvider(document1.Id, CancellationToken.None).Result);
                return true;
            };

            Assert.True(await proxy.HasChangesAsync(localWorkspace.CurrentSolution, solutionActiveStatementSpanProvider, "test.cs", CancellationToken.None).ConfigureAwait(false));

            // EmitSolutionUpdate

            var diagnosticDescriptor1 = EditAndContinueDiagnosticDescriptors.GetDescriptor(EditAndContinueErrorCode.ErrorReadingFile);

            mockEncService.EmitSolutionUpdateImpl = (solution, activeStatementSpanProvider) =>
            {
                var project = solution.Projects.Single();
                Assert.Equal("proj", project.Name);
                AssertEx.Equal(activeSpans1, activeStatementSpanProvider(document1.Id, CancellationToken.None).Result);

                var deltas = ImmutableArray.Create(new ManagedModuleUpdate(
                    module: moduleId1,
                    ilDelta: ImmutableArray.Create<byte>(1, 2),
                    metadataDelta: ImmutableArray.Create<byte>(3, 4),
                    pdbDelta: ImmutableArray.Create<byte>(5, 6),
                    updatedMethods: ImmutableArray.Create(0x06000001),
                    sequencePoints: ImmutableArray.Create(new SequencePointUpdates("file.cs", ImmutableArray.Create(new SourceLineUpdate(1, 2)))),
                    activeStatements: ImmutableArray.Create(new ManagedActiveStatementUpdate(instructionId1.Method.Method, instructionId1.ILOffset, span1.ToSourceSpan())),
                    exceptionRegions: ImmutableArray.Create(exceptionRegionUpdate1)));

                var syntaxTree = project.Documents.Single().GetSyntaxTreeSynchronously(CancellationToken.None)!;

                var documentDiagnostic = DiagnosticData.Create(Diagnostic.Create(diagnosticDescriptor1, Location.Create(syntaxTree, TextSpan.FromBounds(1, 2)), new[] { "doc", "some error" }), document);
                var projectDiagnostic = DiagnosticData.Create(Diagnostic.Create(diagnosticDescriptor1, Location.None, new[] { "proj", "some error" }), project);
                var solutionDiagnostic = DiagnosticData.Create(Diagnostic.Create(diagnosticDescriptor1, Location.None, new[] { "sol", "some error" }), solution.Options);

                return (new(ManagedModuleUpdateStatus.Ready, deltas), ImmutableArray.Create(documentDiagnostic, projectDiagnostic, solutionDiagnostic));
            };

            var updates = await proxy.EmitSolutionUpdateAsync(localWorkspace.CurrentSolution, solutionActiveStatementSpanProvider, diagnosticUpdateSource, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(ManagedModuleUpdateStatus.Ready, updates.Status);

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
                var d = update.GetPushDiagnostics(localWorkspace, InternalDiagnosticsOptions.NormalDiagnosticMode).Single();
                return $"[{d.ProjectId}] {d.Severity} {d.Id}:" +
                       (d.DataLocation != null ? $" {d.DataLocation.OriginalFilePath}({d.DataLocation.OriginalStartLine}, {d.DataLocation.OriginalStartColumn}, {d.DataLocation.OriginalEndLine}, {d.DataLocation.OriginalEndColumn}):" : "") +
                       $" {d.Message}";
            }));

            var delta = updates.Updates.Single();
            Assert.Equal(moduleId1, delta.Module);
            AssertEx.Equal(new byte[] { 1, 2 }, delta.ILDelta);
            AssertEx.Equal(new byte[] { 3, 4 }, delta.MetadataDelta);
            AssertEx.Equal(new byte[] { 5, 6 }, delta.PdbDelta);
            AssertEx.Equal(new[] { 0x06000001 }, delta.UpdatedMethods);

            var lineEdit = delta.SequencePoints.Single();
            Assert.Equal("file.cs", lineEdit.FileName);
            AssertEx.Equal(new[] { new SourceLineUpdate(1, 2) }, lineEdit.LineUpdates);
            Assert.Equal(exceptionRegionUpdate1, delta.ExceptionRegions.Single());

            var activeStatements = delta.ActiveStatements.Single();
            Assert.Equal(instructionId1.Method.Method, activeStatements.Method);
            Assert.Equal(instructionId1.ILOffset, activeStatements.ILOffset);
            Assert.Equal(span1, activeStatements.NewSpan.ToLinePositionSpan());

            // CommitSolutionUpdate

            called = false;
            mockEncService.CommitSolutionUpdateImpl = () => called = true;
            await proxy.CommitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.True(called);

            // DiscardSolutionUpdate

            called = false;
            mockEncService.DiscardSolutionUpdateImpl = () => called = true;
            await proxy.DiscardSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.True(called);

            // GetCurrentActiveStatementPosition

            mockEncService.GetCurrentActiveStatementPositionImpl = (solution, activeStatementSpanProvider, instructionId) =>
            {
                Assert.Equal("proj", solution.Projects.Single().Name);
                Assert.Equal(instructionId1, instructionId);
                AssertEx.Equal(activeSpans1, activeStatementSpanProvider(document1.Id, CancellationToken.None).Result);
                return new LinePositionSpan(new LinePosition(1, 2), new LinePosition(1, 5));
            };

            Assert.Equal(span1, await proxy.GetCurrentActiveStatementPositionAsync(
                localWorkspace.CurrentSolution,
                solutionActiveStatementSpanProvider,
                instructionId1,
                CancellationToken.None).ConfigureAwait(false));

            // IsActiveStatementInExceptionRegion

            mockEncService.IsActiveStatementInExceptionRegionImpl = (solution, instructionId) =>
            {
                Assert.Equal(instructionId1, instructionId);
                return true;
            };

            Assert.True(await proxy.IsActiveStatementInExceptionRegionAsync(localWorkspace.CurrentSolution, instructionId1, CancellationToken.None).ConfigureAwait(false));

            // GetBaseActiveStatementSpans

            mockEncService.GetBaseActiveStatementSpansImpl = (solution, documentIds) =>
            {
                AssertEx.Equal(new[] { document1.Id }, documentIds);
                return ImmutableArray.Create(ImmutableArray.Create((span1, ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.PartiallyExecuted)));
            };

            var baseActiveSpans = await proxy.GetBaseActiveStatementSpansAsync(localWorkspace.CurrentSolution, ImmutableArray.Create(document1.Id), CancellationToken.None).ConfigureAwait(false);
            Assert.Equal((span1, ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.PartiallyExecuted), baseActiveSpans.Single().Single());

            // GetDocumentActiveStatementSpans

            mockEncService.GetAdjustedActiveStatementSpansImpl = (document, activeStatementSpanProvider) =>
            {
                Assert.Equal("test.cs", document.Name);
                AssertEx.Equal(activeSpans1, activeStatementSpanProvider(CancellationToken.None).Result);
                return ImmutableArray.Create((span1, ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.PartiallyExecuted));
            };

            var documentActiveSpans = await proxy.GetAdjustedActiveStatementSpansAsync(document1, documentActiveStatementSpanProvider, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal((span1, ActiveStatementFlags.IsNonLeafFrame | ActiveStatementFlags.PartiallyExecuted), documentActiveSpans.Single());

            // GetDocumentActiveStatementSpans (default array)

            mockEncService.GetAdjustedActiveStatementSpansImpl = (document, _) => default;

            documentActiveSpans = await proxy.GetAdjustedActiveStatementSpansAsync(document1, documentActiveStatementSpanProvider, CancellationToken.None).ConfigureAwait(false);
            Assert.True(documentActiveSpans.IsDefault);

            // OnSourceFileUpdatedAsync

            called = false;
            mockEncService.OnSourceFileUpdatedImpl = updatedDocument =>
            {
                Assert.Equal(document.Id, updatedDocument.Id);
                called = true;
            };

            await proxy.OnSourceFileUpdatedAsync(document, CancellationToken.None).ConfigureAwait(false);
            Assert.True(called);
        }
    }
}
