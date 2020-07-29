// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Execution;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.UnitTests;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Roslyn.VisualStudio.Next.UnitTests.Mocks;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote
{
    [UseExportProvider]
    public class RemoteHostClientServiceFactoryTests
    {
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/46255"), Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task UpdaterService()
        {
            var exportProvider = ExportProviderCache
                .GetOrCreateExportProviderFactory(ServiceTestExportProvider.CreateAssemblyCatalog()
                    .WithParts(typeof(InProcRemoteHostClientProvider.Factory), typeof(CSharpOptionsSerializationService)))
                .CreateExportProvider();

            using var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices(exportProvider));

            var options = workspace.CurrentSolution.Options
                .WithChangedOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS, 1)
                .WithChangedOption(RemoteTestHostOptions.RemoteHostTest, true);

            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(options));

            var listenerProvider = exportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();

            var checksumUpdater = new SolutionChecksumUpdater(workspace, listenerProvider, CancellationToken.None);
            var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();

            // make sure client is ready
            using var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            // add solution, change document
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));
            var project = workspace.AddProject("proj", LanguageNames.CSharp);
            var document = workspace.AddDocument(project.Id, "doc.cs", SourceText.From("code"));
            workspace.ApplyTextChanges(document.Id, new[] { new TextChange(new TextSpan(0, 1), "abc") }, CancellationToken.None);

            // wait for listener
            var workspaceListener = listenerProvider.GetWaiter(FeatureAttribute.Workspace);
            await workspaceListener.ExpeditedWaitAsync();

            var listener = listenerProvider.GetWaiter(FeatureAttribute.SolutionChecksumUpdater);
            await listener.ExpeditedWaitAsync();

            // checksum should already exist
            Assert.True(workspace.CurrentSolution.State.TryGetStateChecksums(out _));

            checksumUpdater.Shutdown();
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSessionWithNoSolution()
        {
            using var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices());

            var options = workspace.CurrentSolution.Options
                .WithChangedOption(RemoteTestHostOptions.RemoteHostTest, true);

            workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(options));

            var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();

            var mock = new MockLogAndProgressService();
            var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            using var connection = await client.CreateConnectionAsync(WellKnownServiceHubService.RemoteSymbolSearchUpdateEngine, callbackTarget: mock, CancellationToken.None);
            await connection.RunRemoteAsync(
                nameof(IRemoteSymbolSearchUpdateEngine.UpdateContinuouslyAsync),
                solution: null,
                new object[] { "emptySource", Path.GetTempPath() },
                CancellationToken.None);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task TestSessionClosed()
        {
            using var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices());

            var client = (InProcRemoteHostClient)await InProcRemoteHostClient.CreateAsync(workspace.Services, runCacheCleanup: false).ConfigureAwait(false);
            var serviceName = new RemoteServiceName("Test");

            // register local service
            TestService testService = null;
            client.RegisterService(serviceName, (s, p) =>
            {
                testService = new TestService(s, p);
                return testService;
            });

            // create session that stay alive until client alive (ex, SymbolSearchUpdateEngine)
            using var connection = await client.CreateConnectionAsync(serviceName, callbackTarget: null, CancellationToken.None);

            // mimic unfortunate call that happens to be in the middle of communication.
            var task = connection.RunRemoteAsync("TestMethodAsync", solution: null, arguments: null, CancellationToken.None);

            // make client to go away
            client.Dispose();

            // let the service to return
            testService.Event.Set();

            // make sure task finished gracefully
            await task;
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
