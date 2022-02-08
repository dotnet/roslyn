// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.UnitTests
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.RemoteHost)]
    public class RemoteHostClientServiceFactoryTests
    {
        private static readonly TestComposition s_composition = FeaturesTestCompositions.Features.WithTestHostParts(TestHost.OutOfProcess);

        private static AdhocWorkspace CreateWorkspace()
            => new(s_composition.GetHostServices());

        [Fact]
        public async Task UpdaterService()
        {
            using var workspace = CreateWorkspace();

            var exportProvider = (IMefHostExportProvider)workspace.Services.HostServices;
            var listenerProvider = exportProvider.GetExportedValue<AsynchronousOperationListenerProvider>();
            var globalOptions = exportProvider.GetExportedValue<IGlobalOptionService>();

            globalOptions.SetGlobalOption(new OptionKey(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS), 1);

            var checksumUpdater = new SolutionChecksumUpdater(workspace, globalOptions, listenerProvider, CancellationToken.None);
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

        [Fact]
        public async Task TestSessionWithNoSolution()
        {
            using var workspace = CreateWorkspace();

            var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();

            var mock = new MockLogService();
            var client = await service.TryGetRemoteHostClientAsync(CancellationToken.None);

            using var connection = client.CreateConnection<IRemoteSymbolSearchUpdateService>(callbackTarget: mock);
            Assert.True(await connection.TryInvokeAsync(
                (service, callbackId, cancellationToken) => service.UpdateContinuouslyAsync(callbackId, "emptySource", Path.GetTempPath(), cancellationToken),
                CancellationToken.None));
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

        private class MockLogService : ISymbolSearchLogService
        {
            public ValueTask LogExceptionAsync(string exception, string text, CancellationToken cancellationToken) => default;
            public ValueTask LogInfoAsync(string text, CancellationToken cancellationToken) => default;
        }
    }
}
