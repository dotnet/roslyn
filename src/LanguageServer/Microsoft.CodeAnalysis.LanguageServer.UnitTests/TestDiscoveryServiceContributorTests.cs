// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.TestDiscovery.Contracts;
using Microsoft.CodeAnalysis.ExternalAccess.TestDiscovery.Internal;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// Verifies the registration / proffer / initialization behavior of
/// <see cref="TestDiscoveryServiceContributor"/> without the full LSP MEF host. The contributor takes a
/// resolver delegate specifically so this logic can be exercised against a real
/// <see cref="GlobalBrokeredServiceContainer"/> while substituting a fake
/// <see cref="ITestDiscoveryLanguageService"/> for the closed-source C# Dev Kit implementation.
/// </summary>
public sealed class TestDiscoveryServiceContributorTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public void ServicesToRegister_AdvertisesServiceMoniker_WhenImplementationPresent()
    {
        var service = new FakeTestDiscoveryLanguageService();
        var contributor = new TestDiscoveryServiceContributor(() => service);

        var servicesToRegister = contributor.ServicesToRegister;

        Assert.Single(servicesToRegister);
        Assert.True(servicesToRegister.ContainsKey(service.Descriptor.Moniker));
    }

    [Fact]
    public void ServicesToRegister_IsEmpty_WhenImplementationAbsent()
    {
        var contributor = new TestDiscoveryServiceContributor(() => null);

        Assert.Empty(contributor.ServicesToRegister);
    }

    [Fact]
    public void Proffer_DoesNothing_WhenImplementationAbsent()
    {
        var container = new TestBrokeredServiceContainer();
        var contributor = new TestDiscoveryServiceContributor(() => null);

        // Should be a no-op (and must not throw) when there is no implementation to proffer.
        contributor.Proffer(container);
    }

    [Fact]
    public async Task ProfferedService_IsAcquirable_AndInitializeIsInvoked()
    {
        var service = new FakeTestDiscoveryLanguageService();
        var contributor = new TestDiscoveryServiceContributor(() => service);

        var container = new TestBrokeredServiceContainer();

        // Mirror the production ordering performed by BrokeredServiceContainer.CreateAsync /
        // ServiceBrokerFactory.CreateAsync: register the advertised monikers, then proffer.
        container.RegisterServices(contributor.ServicesToRegister);
        contributor.Proffer(container);

        // The proffered moniker is advertised and serviceable: acquiring a proxy via the implementation's
        // descriptor runs the proffer factory callback, which invokes InitializeAsync before handing back
        // the proffered instance. Invoking an RPC method then round-trips to that instance.
        var proxy = await GetRequiredServiceAsync<IFakeTestDiscoveryRpc>(
            container.GetFullAccessServiceBroker(), service.Descriptor, CancellationToken.None);

        await service.InitializeCalled.Task.WaitAsync(s_timeout);
        Assert.Equal(1, service.InitializeCallCount);

        Assert.Equal("pong", await proxy.PingAsync(CancellationToken.None));
    }

#pragma warning disable ISB001 // Dispose of proxies - the caller owns the returned proxy.
    private static async Task<T> GetRequiredServiceAsync<T>(IServiceBroker serviceBroker, ServiceRpcDescriptor serviceDescriptor, CancellationToken cancellationToken) where T : class
    {
        return await serviceBroker.GetProxyAsync<T>(
            serviceDescriptor,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException($"Unable to get {typeof(T).Name}.");
    }
#pragma warning restore ISB001

    /// <summary>
    /// Fake RPC surface standing in for the rich, closed-source discovery contract defined in C# Dev Kit.
    /// Roslyn never references this interface; it is used here only to prove that the proffered service is
    /// reachable over the service broker.
    /// </summary>
    private interface IFakeTestDiscoveryRpc
    {
        Task<string> PingAsync(CancellationToken cancellationToken);
    }

    private sealed class FakeTestDiscoveryLanguageService : ITestDiscoveryLanguageService, IFakeTestDiscoveryRpc
    {
        public TaskCompletionSource InitializeCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int InitializeCallCount { get; private set; }

        public ServiceRpcDescriptor Descriptor { get; } = new ServiceJsonRpcDescriptor(
            new ServiceMoniker("Microsoft.VisualStudio.CSharpDevKit.SourceTestDiscoveryService", new Version(0, 1)),
            ServiceJsonRpcDescriptor.Formatters.MessagePack,
            ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader)
            .WithExceptionStrategy(StreamJsonRpc.ExceptionProcessing.ISerializable);

        public Task InitializeAsync(IServiceBroker serviceBroker, CancellationToken cancellationToken)
        {
            InitializeCallCount++;
            InitializeCalled.TrySetResult();
            return Task.CompletedTask;
        }

        public Task<string> PingAsync(CancellationToken cancellationToken)
            => Task.FromResult("pong");
    }

    private sealed class TestBrokeredServiceContainer : GlobalBrokeredServiceContainer
    {
        public TestBrokeredServiceContainer()
            : base(ImmutableDictionary<ServiceMoniker, ServiceRegistration>.Empty, isClientOfExclusiveServer: false, joinableTaskFactory: null, new TraceSource(nameof(TestBrokeredServiceContainer)))
        {
            ProfferIntrinsicService(
                FrameworkServices.Authorization,
                new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: true),
                (moniker, options, serviceBroker, cancellationToken) => new ValueTask<object?>(new TestAuthorizationService()));
        }

        public override IReadOnlyDictionary<string, string> LocalUserCredentials
            => ImmutableDictionary<string, string>.Empty;

        internal new void RegisterServices(IReadOnlyDictionary<ServiceMoniker, ServiceRegistration> services)
            => base.RegisterServices(services);
    }

    private sealed class TestAuthorizationService : IAuthorizationService
    {
        public event EventHandler? CredentialsChanged
        {
            add { }
            remove { }
        }

        public event EventHandler? AuthorizationChanged
        {
            add { }
            remove { }
        }

        public ValueTask<bool> CheckAuthorizationAsync(ProtectedOperation operation, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(true);

        public ValueTask<IReadOnlyDictionary<string, string>> GetCredentialsAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyDictionary<string, string>>(ImmutableDictionary<string, string>.Empty);
    }
}
