// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Moq;
using Roslyn.Test.Utilities;
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
    }
}