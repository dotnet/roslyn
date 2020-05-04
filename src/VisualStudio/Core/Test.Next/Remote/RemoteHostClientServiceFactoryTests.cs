// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Remote;
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
        public async Task UpdaterService()
        {
            var exportProvider = TestHostServices.CreateMinimalExportProvider();

            var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices(exportProvider));

            var options = workspace.CurrentSolution.Options.WithChangedOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS, 1);
            var analyzerReference = new AnalyzerFileReference(typeof(object).Assembly.Location, new NullAssemblyAnalyzerLoader());

            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(options).WithAnalyzerReferences(new[] { analyzerReference }));

            var listenerProvider = exportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();

            var service = CreateRemoteHostClientService(workspace, listenerProvider);

            service.Enable();

            // make sure client is ready
            _ = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            // add solution
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));

            // wait for listener
            var workspaceListener = listenerProvider.GetWaiter(FeatureAttribute.Workspace);
            await workspaceListener.ExpeditedWaitAsync();

            var listener = listenerProvider.GetWaiter(FeatureAttribute.RemoteHostClient);
            await listener.ExpeditedWaitAsync();

            // checksum should already exist
            Assert.True(workspace.CurrentSolution.State.TryGetStateChecksums(out _));

            service.Disable();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSessionWithNoSolution()
        {
            var service = CreateRemoteHostClientService();

            service.Enable();

            var mock = new MockLogAndProgressService();
            var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            using var session = await client.TryCreateKeepAliveSessionAsync(WellKnownServiceHubServices.RemoteSymbolSearchUpdateEngine, callbackTarget: mock, CancellationToken.None);
            await session.RunRemoteAsync(
                nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync),
                solution: null,
                new object[] { "emptySource", Path.GetTempPath() },
                CancellationToken.None);

            service.Disable();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSessionClosed()
        {
            // enable local remote host service
            var service = CreateRemoteHostClientService();
            service.Enable();

            var client = (InProcRemoteHostClient)await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            // register local service
            TestService testService = null;
            client.RegisterService("Test", (s, p) =>
            {
                testService = new TestService(s, p);
                return testService;
            });

            // create session that stay alive until client alive (ex, SymbolSearchUpdateEngine)
            using var session = await client.TryCreateKeepAliveSessionAsync("Test", callbackTarget: null, CancellationToken.None);

            // mimic unfortunate call that happens to be in the middle of communication.
            var task = session.RunRemoteAsync("TestMethodAsync", solution: null, arguments: null, CancellationToken.None);

            // make client to go away
            service.Disable();

            // let the service to return
            testService.Event.Set();

            // make sure task finished gracefully
            await task;
        }

        private RemoteHostClientServiceFactory.RemoteHostClientService CreateRemoteHostClientService(
            Workspace workspace = null,
            IAsynchronousOperationListenerProvider listenerProvider = null)
        {
            workspace ??= new AdhocWorkspace(TestHostServices.CreateHostServices());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(workspace.Options
                                                 .WithChangedOption(RemoteHostOptions.RemoteHostTest, true)
                                                 .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.FullSolution)
                                                 .WithChangedOption(SolutionCrawlerOptions.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, BackgroundAnalysisScope.FullSolution)));

            var threadingContext = ((IMefHostExportProvider)workspace.Services.HostServices).GetExports<IThreadingContext>().Single().Value;
            var factory = new RemoteHostClientServiceFactory(threadingContext, listenerProvider ?? AsynchronousOperationListenerProvider.NullProvider);
            return factory.CreateService(workspace.Services) as RemoteHostClientServiceFactory.RemoteHostClientService;
        }

        private class TestService : ServiceBase
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
