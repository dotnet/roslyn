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
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Moq;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.Remote;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
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

            var enabledClient = await service.GetRemoteHostClientAsync(CancellationToken.None);
            Assert.NotNull(enabledClient);

            service.Disable();

            var disabledClient = await service.GetRemoteHostClientAsync(CancellationToken.None);
            Assert.Null(disabledClient);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task GlobalAssets()
        {
            var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices());

            var analyzerReference = new AnalyzerFileReference(typeof(object).Assembly.Location, new NullAssemblyAnalyzerLoader());
            var service = CreateRemoteHostClientService(workspace, SpecializedCollections.SingletonEnumerable<AnalyzerReference>(analyzerReference));

            service.Enable();

            // make sure client is ready
            var client = await service.GetRemoteHostClientAsync(CancellationToken.None);

            var checksumService = workspace.Services.GetService<ISolutionSynchronizationService>();
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
            var client = await service.GetRemoteHostClientAsync(CancellationToken.None) as InProcRemoteHostClient;

            Assert.Equal(1, client.AssetStorage.GetGlobalAssetsOfType<AnalyzerReference>(CancellationToken.None).Count());

            service.Disable();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task UpdaterService()
        {
            var exportProvider = TestHostServices.CreateMinimalExportProvider();

            var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices(exportProvider));
            workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS, 1);

            var listener = new Listener();
            var analyzerReference = new AnalyzerFileReference(typeof(object).Assembly.Location, new NullAssemblyAnalyzerLoader());

            var service = CreateRemoteHostClientService(workspace, SpecializedCollections.SingletonEnumerable<AnalyzerReference>(analyzerReference), listener);

            service.Enable();

            // make sure client is ready
            var client = await service.GetRemoteHostClientAsync(CancellationToken.None);

            // add solution
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));

            var listeners = exportProvider.GetExports<IAsynchronousOperationListener, FeatureMetadata>();
            var workspaceListener = listeners.First(l => l.Metadata.FeatureName == FeatureAttribute.Workspace).Value as IAsynchronousOperationWaiter;

            // wait for listener
            await workspaceListener.CreateWaitTask();
            await listener.CreateWaitTask();

            // checksum should already exist
            SolutionStateChecksums checksums;
            Assert.True(workspace.CurrentSolution.State.TryGetStateChecksums(out checksums));

            service.Disable();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSessionWithNoSolution()
        {
            var service = CreateRemoteHostClientService();

            service.Enable();

            var mock = new MockLogService();
            var client = await service.GetRemoteHostClientAsync(CancellationToken.None);
            using (var session = await client.TryCreateServiceSessionAsync(WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine, mock, CancellationToken.None))
            {
                await session.InvokeAsync(nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync), "emptySource", Path.GetTempPath());
            }

            service.Disable();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSessionClosed()
        {
            // enable local remote host service
            var service = CreateRemoteHostClientService();
            service.Enable();

            var client = (InProcRemoteHostClient)(await service.GetRemoteHostClientAsync(CancellationToken.None));

            // register local service
            TestService testService = null;
            client.RegisterService("Test", (s, p) =>
            {
                testService = new TestService(s, p);
                return testService;
            });

            // create session that stay alive until client alive (ex, SymbolSearchUpdateEngine)
            var session = await client.TryCreateServiceSessionAsync("Test", CancellationToken.None);
            client.ConnectionChanged += (s, connected) =>
            {
                if (connected)
                {
                    return;
                }

                // let session go, when client goes away (ex, VS shutdown)
                session.Dispose();
            };

            // mimic unfortunate call that happens to be in the middle of communication.
            var task = Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await session.InvokeAsync("TestMethodAsync");
            });

            // make client to go away
            service.Disable();

            // set event so that remote Rpc thread is released. this shouldn't affect
            // host side's cancellation due to client closed
            testService.Event.Set();

            // verify session cancelled itself with cancellation token
            await task;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestRequestNewRemoteHost()
        {
            var service = CreateRemoteHostClientService();

            service.Enable();

            var completionTask = new TaskCompletionSource<bool>();

            var client1 = await service.GetRemoteHostClientAsync(CancellationToken.None);
            client1.ConnectionChanged += (s, connected) =>
            {
                // mark done
                completionTask.SetResult(connected);
            };

            await service.RequestNewRemoteHostAsync(CancellationToken.None);

            var result = await completionTask.Task;
            Assert.False(result);

            var client2 = await service.GetRemoteHostClientAsync(CancellationToken.None);

            Assert.NotEqual(client1, client2);

            service.Disable();
        }

        private RemoteHostClientServiceFactory.RemoteHostClientService CreateRemoteHostClientService(
            Workspace workspace = null,
            IEnumerable<AnalyzerReference> hostAnalyzerReferences = null,
            IAsynchronousOperationListener listener = null)
        {
            workspace = workspace ?? new AdhocWorkspace(TestHostServices.CreateHostServices());
            workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHostTest, true)
                                                 .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, true)
                                                 .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.VisualBasic, true);

            var analyzerService = GetDiagnosticAnalyzerService(hostAnalyzerReferences ?? SpecializedCollections.EmptyEnumerable<AnalyzerReference>());

            var listeners = AsynchronousOperationListener.CreateListeners(FeatureAttribute.RemoteHostClient, listener ?? new Listener());

            var factory = new RemoteHostClientServiceFactory(listeners, analyzerService);
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
            public TestService(Stream stream, IServiceProvider serviceProvider) :
                base(serviceProvider, stream)
            {
                Event = new ManualResetEvent(false);

                Rpc.StartListening();
            }

            public readonly ManualResetEvent Event;

            public Task TestMethodAsync()
            {
                Event.WaitOne();

                return SpecializedTasks.EmptyTask;
            }
        }

        private class Listener : AsynchronousOperationListener { }

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

        private class MockLogService : ISymbolSearchLogService
        {
            public Task LogExceptionAsync(string exception, string text) => SpecializedTasks.EmptyTask;
            public Task LogInfoAsync(string text) => SpecializedTasks.EmptyTask;
        }
    }
}