// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.EditAndContinue.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
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
        private static readonly Lazy<IExportProviderFactory> s_remoteHostExportProviderFactory = new Lazy<IExportProviderFactory>(
            CreateRemoteHostExportProviderFactory,
            LazyThreadSafetyMode.ExecutionAndPublication);

        private static IExportProviderFactory CreateRemoteHostExportProviderFactory()
        {
            var configuration = CompositionConfiguration.Create(
                ExportProviderCache.GetOrCreateAssemblyCatalog(RoslynServices.RemoteHostAssemblies).
                    WithCompositionService().WithParts(
                        typeof(TestActiveStatementSpanTrackerFactory),
                        typeof(MockEncServiceFactory)));

            var runtimeComposition = RuntimeComposition.CreateRuntimeComposition(configuration);
            return runtimeComposition.CreateExportProviderFactory();
        }

        private static MefHostServices CreateRemoteHostServices()
            => new ExportProviderMefHostServices(s_remoteHostExportProviderFactory.Value.CreateExportProvider());

        private class ExportProviderMefHostServices : MefHostServices, IMefHostExportProvider
        {
            private readonly VisualStudioMefHostServices _vsHostServices;

            public ExportProviderMefHostServices(ExportProvider exportProvider)
                : base(new ContainerConfiguration().CreateContainer())
            {
                _vsHostServices = VisualStudioMefHostServices.Create(exportProvider);
            }

            protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
                => _vsHostServices.CreateWorkspaceServices(workspace);

            IEnumerable<Lazy<TExtension, TMetadata>> IMefHostExportProvider.GetExports<TExtension, TMetadata>()
                => _vsHostServices.GetExports<TExtension, TMetadata>();

            IEnumerable<Lazy<TExtension>> IMefHostExportProvider.GetExports<TExtension>()
                => _vsHostServices.GetExports<TExtension>();
        }

        [ExportWorkspaceServiceFactory(typeof(IEditAndContinueWorkspaceService), ServiceLayer.TestLayer), Shared]
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
            var exportProviderFactory = ExportProviderCache.GetOrCreateExportProviderFactory(
                TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                    typeof(RemotableDataServiceFactory),
                    typeof(InProcRemoteHostClientProvider.Factory)));

            RoslynServices.TestAccessor.HookHostServices(() => CreateRemoteHostServices());

            using var workspace = new TestWorkspace(exportProvider: exportProviderFactory.CreateExportProvider());

            var options = workspace.Services.GetRequiredService<IOptionService>();
            options.SetOptions(options.GetOptions().WithChangedOption(RemoteHostOptions.RemoteHostTest, true));

            workspace.ChangeSolution(workspace.CurrentSolution.
                AddProject("proj", "proj", LanguageNames.CSharp).
                AddMetadataReferences(TargetFrameworkUtil.GetReferences(TargetFramework.Mscorlib40)).
                AddDocument("test.cs", SourceText.From("class C { }", Encoding.UTF8), filePath: "test.cs").Project.Solution);

            var mockEncService = (MockEditAndContinueWorkspaceService)SolutionService.PrimaryWorkspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>();

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
            mockEncService.StartEditSessionImpl = (activeStatementProvider, debuggeeModuleMetadataProvider) =>
            {
                remoteActiveStatementProvider = activeStatementProvider;
                remoteDebuggeeModuleMetadataProvider = debuggeeModuleMetadataProvider;
            };

            var callback = new TestEditSessionCallback(as1);
            await proxy.StartEditSessionAsync(callback, CancellationToken.None).ConfigureAwait(false);

            var activeStatement = (await remoteActiveStatementProvider!(CancellationToken.None).ConfigureAwait(false)).Single();
            Assert.Equal(as1.InstructionId, activeStatement.InstructionId);
            Assert.Equal(as1.LinePositionSpan, activeStatement.LinePositionSpan);
            AssertEx.Equal(as1.ThreadIds, activeStatement.ThreadIds);
            Assert.Equal(as1.Flags, activeStatement.Flags);

            var availability = await remoteDebuggeeModuleMetadataProvider!.GetEncAvailabilityAsync(moduleId1, CancellationToken.None).ConfigureAwait(false);
            Assert.Equal<(int, string?)?>((1, "can't do enc"), availability);

            // EndEditSession

            await proxy.EndEditSessionAsync(CancellationToken.None).ConfigureAwait(false);

            // EndDebuggingSession

            await proxy.EndDebuggingSessionAsync(CancellationToken.None).ConfigureAwait(false);

            // HasChanges

            mockEncService.HasChangesAsyncImpl = (solution, sourceFilePath) =>
            {
                Assert.Equal("proj", solution.Projects.Single().Name);
                Assert.Equal("test.cs", sourceFilePath);
                return true;
            };

            Assert.True(await proxy.HasChangesAsync("test.cs", CancellationToken.None).ConfigureAwait(false));

            // EmitSolutionUpdate

            mockEncService.EmitSolutionUpdateAsyncImpl = solution =>
            {
                Assert.Equal("proj", solution.Projects.Single().Name);

                return (SolutionUpdateStatus.Ready, ImmutableArray.Create(new Deltas(
                    mvid: moduleId1,
                    il: ImmutableArray.Create<byte>(1, 2),
                    metadata: ImmutableArray.Create<byte>(3, 4),
                    pdb: ImmutableArray.Create<byte>(5, 6),
                    updatedMethods: ImmutableArray.Create(0x06000001),
                    lineEdits: ImmutableArray.Create(("file.cs", ImmutableArray.Create(new LineChange(1, 2)))),
                    nonRemappableRegions: ImmutableArray.Create((methodId1, region1)),
                    activeStatementsInUpdatedMethods: ImmutableArray.Create((threadId1, instructionId1, span1)))));
            };

            var (status, deltas) = await proxy.EmitSolutionUpdateAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.Equal(SolutionUpdateStatus.Ready, status);

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
        }
    }
}
