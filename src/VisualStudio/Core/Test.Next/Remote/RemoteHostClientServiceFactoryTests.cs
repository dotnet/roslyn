// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Moq;
using Roslyn.Test.Utilities.Remote;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    public class RemoteHostClientServiceFactoryTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public void Creation()
        {
            var service = CreateRemoteHostClientService();
            Assert.NotNull(service);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task Enable_Disable()
        {
            var service = CreateRemoteHostClientService();

            service.Enable();

            var enabledClient = await service.TryGetRemoteHostClientAsync(CancellationToken.None);
            Assert.NotNull(enabledClient);

            service.Disable();

            var disabledClient = await service.TryGetRemoteHostClientAsync(CancellationToken.None);
            Assert.Null(disabledClient);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task ClientId()
        {
            var service = CreateRemoteHostClientService();
            service.Enable();

            var client1 = await service.TryGetRemoteHostClientAsync(CancellationToken.None);
            var id1 = client1.ClientId;

            await service.RequestNewRemoteHostAsync(CancellationToken.None);

            var client2 = await service.TryGetRemoteHostClientAsync(CancellationToken.None);
            var id2 = client2.ClientId;

            Assert.NotEqual(id1, id2);

            service.Disable();
        }


        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task GlobalAssets()
        {
            var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices());

            var analyzerReference = new AnalyzerFileReference(typeof(object).Assembly.Location, new NullAssemblyAnalyzerLoader());
            var service = CreateRemoteHostClientService(workspace, SpecializedCollections.SingletonEnumerable<AnalyzerReference>(analyzerReference));

            service.Enable();

            // make sure client is ready
            var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            var checksumService = workspace.Services.GetService<IRemotableDataService>();
            var asset = checksumService.GetGlobalAsset(analyzerReference, CancellationToken.None);
            Assert.NotNull(asset);

            service.Disable();

            var noAsset = checksumService.GetGlobalAsset(analyzerReference, CancellationToken.None);
            Assert.Null(noAsset);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task SynchronizeGlobalAssets()
        {
            var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices());

            var analyzerReference = new AnalyzerFileReference(typeof(object).Assembly.Location, new NullAssemblyAnalyzerLoader());
            var service = CreateRemoteHostClientService(workspace, SpecializedCollections.SingletonEnumerable<AnalyzerReference>(analyzerReference));

            service.Enable();

            // make sure client is ready
            var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None) as InProcRemoteHostClient;

            Assert.Equal(1, client.AssetStorage.GetGlobalAssetsOfType<AnalyzerReference>(CancellationToken.None).Count());

            service.Disable();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task UpdaterService()
        {
            var exportProvider = TestHostServices.CreateMinimalExportProvider();

            var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices(exportProvider));
            workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS, 1);

            var listenerProvider = exportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            var analyzerReference = new AnalyzerFileReference(typeof(object).Assembly.Location, new NullAssemblyAnalyzerLoader());

            var service = CreateRemoteHostClientService(workspace, SpecializedCollections.SingletonEnumerable<AnalyzerReference>(analyzerReference), listenerProvider);

            service.Enable();

            // make sure client is ready
            var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            // add solution
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));

            // wait for listener
            var workspaceListener = listenerProvider.GetWaiter(FeatureAttribute.Workspace);
            await workspaceListener.CreateExpeditedWaitTask();

            var listener = listenerProvider.GetWaiter(FeatureAttribute.RemoteHostClient);
            await listener.CreateExpeditedWaitTask();

            // checksum should already exist
            Assert.True(workspace.CurrentSolution.State.TryGetStateChecksums(out var checksums));

            service.Disable();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSessionWithNoSolution()
        {
            var service = CreateRemoteHostClientService();

            service.Enable();

            var mock = new MockLogAndProgressService();
            var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            var session = await client.TryCreateKeepAliveSessionAsync(WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine, mock, CancellationToken.None);
            var result = await session.TryInvokeAsync(nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync), new object[] { "emptySource", Path.GetTempPath() }, CancellationToken.None);

            Assert.True(result);

            session.Shutdown();

            service.Disable();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSessionClosed()
        {
            // enable local remote host service
            var service = CreateRemoteHostClientService();
            service.Enable();

            var client = (InProcRemoteHostClient)(await service.TryGetRemoteHostClientAsync(CancellationToken.None));

            // register local service
            TestService testService = null;
            client.RegisterService("Test", (s, p) =>
            {
                testService = new TestService(s, p);
                return testService;
            });

            // create session that stay alive until client alive (ex, SymbolSearchUpdateEngine)
            var session = await client.TryCreateKeepAliveSessionAsync("Test", CancellationToken.None);

            // mimic unfortunate call that happens to be in the middle of communication.
            var task = session.TryInvokeAsync("TestMethodAsync", arguments: null, CancellationToken.None);

            // make client to go away
            service.Disable();

            // let the service to return
            testService.Event.Set();

            // make sure task finished gracefully
            await task;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRequestNewRemoteHost()
        {
            var service = CreateRemoteHostClientService();

            service.Enable();

            var completionTask = new TaskCompletionSource<bool>();

            var client1 = await service.TryGetRemoteHostClientAsync(CancellationToken.None);
            client1.StatusChanged += (s, connected) =>
            {
                // mark done
                completionTask.SetResult(connected);
            };

            await service.RequestNewRemoteHostAsync(CancellationToken.None);

            var result = await completionTask.Task;
            Assert.False(result);

            var client2 = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            Assert.NotEqual(client1, client2);

            service.Disable();
        }

        private RemoteHostClientServiceFactory.RemoteHostClientService CreateRemoteHostClientService(
            Workspace workspace = null,
            IEnumerable<AnalyzerReference> hostAnalyzerReferences = null,
            IAsynchronousOperationListenerProvider listenerProvider = null)
        {
            workspace = workspace ?? new AdhocWorkspace(TestHostServices.CreateHostServices());
            workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHostTest, true)
                                                 .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, BackgroundAnalysisScope.FullSolution);

            var analyzerService = GetDiagnosticAnalyzerService(hostAnalyzerReferences ?? SpecializedCollections.EmptyEnumerable<AnalyzerReference>());

            var threadingContext = ((IMefHostExportProvider)workspace.Services.HostServices).GetExports<IThreadingContext>().Single().Value;
            var factory = new RemoteHostClientServiceFactory(threadingContext, listenerProvider ?? AsynchronousOperationListenerProvider.NullProvider, analyzerService);
            return factory.CreateService(workspace.Services) as RemoteHostClientServiceFactory.RemoteHostClientService;
        }

        private IDiagnosticAnalyzerService GetDiagnosticAnalyzerService(IEnumerable<AnalyzerReference> references)
        {
            var mock = new Mock<IDiagnosticAnalyzerService>(MockBehavior.Strict);
            mock.Setup(a => a.GetHostAnalyzerReferences()).Returns(references);
            return mock.Object;
        }

        private class TestService : ServiceHubServiceBase
        {
            public TestService(Stream stream, IServiceProvider serviceProvider)
                : base(serviceProvider, stream)
            {
                Event = new ManualResetEvent(false);

                StartService();
            }

            public readonly ManualResetEvent Event;

            public Task TestMethodAsync()
            {
                Event.WaitOne();

                return Task.CompletedTask;
            }
        }

        private class NullAssemblyAnalyzerLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                // doesn't matter what it returns
                return typeof(object).Assembly;
            }
        }

        private class MockLogAndProgressService : ISymbolSearchLogService, ISymbolSearchProgressService
        {
            public Task LogExceptionAsync(string exception, string text) => Task.CompletedTask;
            public Task LogInfoAsync(string text) => Task.CompletedTask;

            public Task OnDownloadFullDatabaseStartedAsync(string title) => Task.CompletedTask;
            public Task OnDownloadFullDatabaseSucceededAsync() => Task.CompletedTask;
            public Task OnDownloadFullDatabaseCanceledAsync() => Task.CompletedTask;
            public Task OnDownloadFullDatabaseFailedAsync(string message) => Task.CompletedTask;
        }
    }
}
