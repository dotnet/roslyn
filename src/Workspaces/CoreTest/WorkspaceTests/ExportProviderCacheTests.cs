// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
public sealed class ExportProviderCacheTests
{
    [Fact]
    public void CompositionReusesHostAndExports()
    {
        var composition = FeaturesTestCompositions.Features;
        var first = composition.GetHostServices(out var firstProvider);
        var second = composition.GetHostServices(out var secondProvider);

        Assert.Same(firstProvider, secondProvider);
        Assert.Same(first, second);
        AssertSameExports(first, second);
    }

    [Fact]
    public void WorkspacesReuseCompositionHost()
    {
        var host = FeaturesTestCompositions.Features.GetHostServices(out var provider);
        using var first = new TestWorkspace(provider);
        using var second = new TestWorkspace(FeaturesTestCompositions.Features);

        Assert.Same(provider, first.ExportProvider);
        Assert.Same(provider, second.ExportProvider);
        Assert.Same(host, first.Services.HostServices);
        Assert.Same(host, second.Services.HostServices);
        AssertSameExports(host, first.Services.HostServices);
    }

    [Fact]
    public void DefaultHostReusesTrackedExports()
    {
        var host = FeaturesTestCompositions.Features.GetHostServices();
        AssertSameExports(host, MefHostServices.DefaultHost);
    }

    [Fact]
    public async Task ConcurrentRequestsPublishOneHost()
    {
        var composition = FeaturesTestCompositions.Features;
        var provider = composition.ExportProviderFactory.CreateExportProvider();
        var hosts = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => composition.GetHostServices())));

        Assert.All(hosts, host =>
        {
            Assert.Same(hosts[0], host);
            AssertSameExports(hosts[0], host);
        });
        Assert.Same(provider, composition.ExportProviderFactory.CreateExportProvider());
    }

    [Fact]
    public void LocalAndRemoteHostsAreSeparate()
    {
        var local = FeaturesTestCompositions.Features.GetHostServices(out var localProvider);
        var remote = FeaturesTestCompositions.RemoteHost.GetHostServices(out var remoteProvider);

        Assert.NotSame(localProvider, remoteProvider);
        Assert.NotSame(local, remote);
        Assert.Same(local, FeaturesTestCompositions.Features.GetHostServices());
        Assert.Same(remote, FeaturesTestCompositions.RemoteHost.GetHostServices());
        AssertSameExports(remote, FeaturesTestCompositions.RemoteHost.GetHostServices());
    }

    [Fact]
    public void CustomCompositionPreservesAddedAndExcludedParts()
    {
        var composition = FeaturesTestCompositions.Features
            .AddParts(typeof(SharedExport))
            .AddExcludedPartTypes(typeof(Microsoft.CodeAnalysis.CSharp.CSharpCompilationFactoryService));
        var host = composition.GetHostServices(out var provider);

        Assert.Same(provider.GetExportedValue<SharedExport>(), ((IMefHostExportProvider)host).GetExports<SharedExport>().Single().Value);
        using var workspace = new AdhocWorkspace(host);
        Assert.Null(workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<ICompilationFactoryService>());
        Assert.Same(host, composition.GetHostServices());
    }

    [Fact]
    public void ConflictingCompositionsStillFail()
    {
        _ = FeaturesTestCompositions.Features.GetHostServices();
        var conflicting = FeaturesTestCompositions.Features.AddParts(typeof(SharedExport));

        var exception = Assert.Throws<InvalidOperationException>(() => conflicting.GetHostServices());
        Assert.Contains("Only one local ExportProvider", exception.Message);
    }

    [Fact]
    public void NonSharedExportsKeepExistingHostCacheSemantics()
    {
        var host = FeaturesTestCompositions.Features.AddParts(typeof(NonSharedExport)).GetHostServices(out var provider);
        var exports = ((IMefHostExportProvider)host).GetExports<NonSharedExport>();

        Assert.Same(exports, ((IMefHostExportProvider)host).GetExports<NonSharedExport>());
        Assert.Same(exports.Single().Value, exports.Single().Value);
        Assert.NotSame(exports.Single().Value, provider.GetExportedValue<NonSharedExport>());
        Assert.NotSame(provider.GetExportedValue<NonSharedExport>(), provider.GetExportedValue<NonSharedExport>());
    }

    [Fact]
    public void WorkspaceServicesAndDisposalRemainIndependent()
    {
        var composition = FeaturesTestCompositions.Features.AddParts(typeof(SharedExport), typeof(WorkspaceServiceFactory), typeof(OtherKindService));
        using var first = new TestWorkspace(composition);
        using var second = new TestWorkspace(composition);
        using var otherKind = new TestWorkspace(composition, workspaceKind: "Other");
        var shared = first.ExportProvider.GetExportedValue<SharedExport>();
        var firstService = Assert.IsType<WorkspaceService>(first.Services.GetRequiredService<ITestWorkspaceService>());
        var secondService = Assert.IsType<WorkspaceService>(second.Services.GetRequiredService<ITestWorkspaceService>());

        Assert.NotSame(first.Services, second.Services);
        Assert.NotSame(first.Services.GetLanguageServices(LanguageNames.CSharp), second.Services.GetLanguageServices(LanguageNames.CSharp));
        Assert.NotSame(firstService, secondService);
        Assert.Same(first, firstService.Workspace);
        Assert.Same(second, secondService.Workspace);
        Assert.IsType<OtherKindService>(otherKind.Services.GetRequiredService<ITestWorkspaceService>());

        first.Dispose();

        Assert.Equal(1, firstService.DisposeCount);
        Assert.Equal(0, secondService.DisposeCount);
        Assert.Equal(0, shared.DisposeCount);
        Assert.Same(secondService, second.Services.GetRequiredService<ITestWorkspaceService>());
        Assert.Same(shared, second.ExportProvider.GetExportedValue<SharedExport>());
    }

    [Fact]
    public void UntrackedProvidersKeepFreshHosts()
    {
        var composition = FeaturesTestCompositions.Features;
        var tracked = composition.GetHostServices(out var trackedProvider);
        var factory = RuntimeComposition.CreateRuntimeComposition(composition.GetCompositionConfiguration()).CreateExportProviderFactory(joinableTaskFactory: null);
        using var untrackedProvider = factory.CreateExportProvider();
        using var otherProvider = factory.CreateExportProvider();
        using var first = new TestWorkspace(untrackedProvider);
        using var second = new TestWorkspace(untrackedProvider);
        using var other = new TestWorkspace(otherProvider);

        Assert.NotSame(trackedProvider, untrackedProvider);
        Assert.NotSame(untrackedProvider, otherProvider);
        Assert.NotSame(tracked, first.Services.HostServices);
        Assert.NotSame(first.Services.HostServices, second.Services.HostServices);
        Assert.NotSame(first.Services.HostServices, other.Services.HostServices);
        Assert.NotSame(
            ((IMefHostExportProvider)first.Services.HostServices).GetExports<IWorkspaceService, WorkspaceServiceMetadata>(),
            ((IMefHostExportProvider)second.Services.HostServices).GetExports<IWorkspaceService, WorkspaceServiceMetadata>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScopeTeardownDisposesExportsAndResetsHost(bool remote)
    {
        var lifetime = new UseExportProviderAttribute();
        var composition = (remote ? FeaturesTestCompositions.RemoteHost : FeaturesTestCompositions.Features).AddParts(typeof(SharedExport));

        try
        {
            var first = composition.GetHostServices(out var firstProvider);
            var firstExport = firstProvider.GetExportedValue<SharedExport>();
            var firstExports = ((IMefHostExportProvider)first).GetExports<SharedExport>();
            lifetime.After(methodUnderTest: null);
            Assert.Equal(1, firstExport.DisposeCount);

            lifetime.Before(methodUnderTest: null);
            var second = composition.GetHostServices(out var secondProvider);
            Assert.NotSame(firstProvider, secondProvider);
            Assert.NotSame(first, second);
            Assert.NotSame(firstExports, ((IMefHostExportProvider)second).GetExports<SharedExport>());
            Assert.NotSame(firstExport, secondProvider.GetExportedValue<SharedExport>());
            Assert.Equal(0, secondProvider.GetExportedValue<SharedExport>().DisposeCount);
            Assert.Equal(1, firstExport.DisposeCount);
        }
        finally
        {
            lifetime.After(methodUnderTest: null);
            lifetime.Before(methodUnderTest: null);
        }
    }

    private static void AssertSameExports(HostServices first, HostServices second)
    {
        var firstExports = (IMefHostExportProvider)first;
        var secondExports = (IMefHostExportProvider)second;
        Assert.Same(firstExports.GetExports<IWorkspaceService>(), secondExports.GetExports<IWorkspaceService>());
        Assert.Same(
            firstExports.GetExports<IWorkspaceService, WorkspaceServiceMetadata>(),
            secondExports.GetExports<IWorkspaceService, WorkspaceServiceMetadata>());
    }

    [Export, Shared, PartNotDiscoverable]
    public sealed class SharedExport : IDisposable
    {
        public int DisposeCount { get; private set; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SharedExport()
        {
        }

        public void Dispose() => DisposeCount++;
    }

    public interface ITestWorkspaceService : IWorkspaceService;

#pragma warning disable RS0023 // Deliberately non-shared to verify the existing host-cache contract.
    [Export, PartNotDiscoverable]
#pragma warning restore RS0023
    public sealed class NonSharedExport
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NonSharedExport()
        {
        }
    }

    [ExportWorkspaceServiceFactory(typeof(ITestWorkspaceService)), Shared, PartNotDiscoverable]
    public sealed class WorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new WorkspaceService(workspaceServices.Workspace);
    }

    private sealed class WorkspaceService(Workspace workspace) : ITestWorkspaceService, IDisposable
    {
        public Workspace Workspace { get; } = workspace;
        public int DisposeCount { get; private set; }
        public void Dispose() => DisposeCount++;
    }

    [ExportWorkspaceService(typeof(ITestWorkspaceService), ["Other"]), Shared, PartNotDiscoverable]
    public sealed class OtherKindService : ITestWorkspaceService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OtherKindService()
        {
        }
    }
}
