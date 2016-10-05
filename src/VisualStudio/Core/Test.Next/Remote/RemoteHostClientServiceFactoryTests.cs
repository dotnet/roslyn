// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Microsoft.VisualStudio.Text.Editor;
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

            var checksumService = workspace.Services.GetService<ISolutionChecksumService>();
            var asset = checksumService.GetGlobalAsset(analyzerReference, CancellationToken.None);
            Assert.NotNull(asset);

            service.Disable();

            var noAsset = checksumService.GetGlobalAsset(analyzerReference, CancellationToken.None);
            Assert.Null(noAsset);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.RemoteHost)]
        public async Task UpdaterService()
        {
            var workspace = new AdhocWorkspace(TestHostServices.CreateHostServices());
            workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.SolutionChecksumMonitorBackOffTimeSpanInMS, 1);

            var analyzerReference = new AnalyzerFileReference(typeof(object).Assembly.Location, new NullAssemblyAnalyzerLoader());
            var service = CreateRemoteHostClientService(workspace, SpecializedCollections.SingletonEnumerable<AnalyzerReference>(analyzerReference));

            service.Enable();

            // make sure client is ready
            var client = await service.GetRemoteHostClientAsync(CancellationToken.None);

            // add solution
            workspace.AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default));

            // TODO: use waiter to make sure workspace events and updater is ready.
            //       this delay is temporary until I set all .Next unit test hardness to setup correctly
            await Task.Delay(TimeSpan.FromSeconds(1));

            var checksumService = workspace.Services.GetService<ISolutionChecksumService>();

            Checksum checksum;
            using (var scope = await checksumService.CreateChecksumAsync(workspace.CurrentSolution, CancellationToken.None))
            {
                // create solution checksum and hold onto the checksum and let it go
                checksum = scope.SolutionChecksum.Checksum;
            }

            // there should be one held in memory by solution checksum updator
            var solutionObject = checksumService.GetChecksumObject(checksum, CancellationToken.None);
            Assert.Equal(solutionObject.Checksum, checksum);

            service.Disable();
        }

        private RemoteHostClientServiceFactory.RemoteHostClientService CreateRemoteHostClientService(Workspace workspace = null, IEnumerable<AnalyzerReference> hostAnalyzerReferences = null)
        {
            workspace = workspace ?? new AdhocWorkspace(TestHostServices.CreateHostServices());
            workspace.Options = workspace.Options.WithChangedOption(RemoteHostOptions.RemoteHost, true)
                                                 .WithChangedOption(RemoteHostOptions.RemoteHostTest, true)
                                                 .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.CSharp, true)
                                                 .WithChangedOption(ServiceFeatureOnOffOptions.ClosedFileDiagnostic, LanguageNames.VisualBasic, true);

            var analyzerService = GetDiagnosticAnalyzerService(hostAnalyzerReferences ?? SpecializedCollections.EmptyEnumerable<AnalyzerReference>());

            var optionMock = new Mock<IEditorOptions>(MockBehavior.Strict);
            var optionFactoryMock = new Mock<IEditorOptionsFactoryService>(MockBehavior.Strict);
            optionFactoryMock.SetupGet(i => i.GlobalOptions).Returns(optionMock.Object);

            var factory = new RemoteHostClientServiceFactory(analyzerService, optionFactoryMock.Object);
            return factory.CreateService(workspace.Services) as RemoteHostClientServiceFactory.RemoteHostClientService;
        }

        private IDiagnosticAnalyzerService GetDiagnosticAnalyzerService(IEnumerable<AnalyzerReference> references)
        {
            var mock = new Mock<IDiagnosticAnalyzerService>(MockBehavior.Strict);
            mock.Setup(a => a.GetHostAnalyzerReferences()).Returns(references);
            return mock.Object;
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
    }
}