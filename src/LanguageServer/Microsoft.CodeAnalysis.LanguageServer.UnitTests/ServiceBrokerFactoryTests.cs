// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.ExternalAccess.TestDiscovery.Contracts;
using Microsoft.CodeAnalysis.ExternalAccess.TestDiscovery.Internal;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.BrokeredServiceBridgeManifest;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.Definitions;
using Microsoft.CodeAnalysis.Remote.ProjectSystem;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Utilities.ServiceBroker;
using Nerdbank.Streams;
using Xunit.Abstractions;
using DebuggerContracts = Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

/// <summary>
/// These tests verify specifics around LSP service MEF lifetimes and availability, so utilize the real
/// MEF composition via <see cref="AbstractLanguageServerMefHost"/> 
/// </summary>
public sealed class ServiceBrokerFactoryTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerMefHost(testOutputHelper)
{
    private const string ServiceBrokerConnectMethodName = "serviceBroker/connect";
    private const string ServiceBrokerChannelName = "serviceBroker";
    private const int MacOSUnixDomainSocketMaxPathLength = 104;
    private const string HelixUnixPipePathPrefix = "/tmp/helix/working/94B7081D/t/CoreFxPipe_";
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task ServiceBrokerFactoryIsManagedPerServerAsync()
    {
        var server1 = await CreateLanguageServerAsync();
        var server2 = await CreateLanguageServerAsync();
        var server1Disposed = false;

        await using var brokeredServiceClient1 = new TestBrokeredServiceClient();
        await using var brokeredServiceClient2 = new TestBrokeredServiceClient();

        try
        {
            var serviceBrokerFactory1 = server1.GetRequiredLspService<ServiceBrokerFactory>();
            var serviceBrokerFactory2 = server2.GetRequiredLspService<ServiceBrokerFactory>();

            Assert.NotSame(serviceBrokerFactory1, serviceBrokerFactory2);

            await brokeredServiceClient1.ConnectAsync(server1);
            await brokeredServiceClient2.ConnectAsync(server2);

            Assert.Same(serviceBrokerFactory1, server1.GetRequiredLspService<ServiceBrokerFactory>());
            Assert.Same(serviceBrokerFactory2, server2.GetRequiredLspService<ServiceBrokerFactory>());

            var workspaceProjectFactory1 = await GetRequiredServiceAsync<IWorkspaceProjectFactoryService>(brokeredServiceClient1.ServiceBroker, WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor, CancellationToken.None);
            var workspaceProjectFactory2 = await GetRequiredServiceAsync<IWorkspaceProjectFactoryService>(brokeredServiceClient2.ServiceBroker, WorkspaceProjectFactoryServiceDescriptor.ServiceDescriptor, CancellationToken.None);

            Assert.NotNull(await workspaceProjectFactory1.GetSupportedBuildSystemPropertiesAsync(CancellationToken.None));
            Assert.NotNull(await workspaceProjectFactory2.GetSupportedBuildSystemPropertiesAsync(CancellationToken.None));
            Assert.NotSame(workspaceProjectFactory1, workspaceProjectFactory2);

            await server1.DisposeAsync();
            server1Disposed = true;
            // Now that the server is disposed, the client connection should close.
            await brokeredServiceClient1.Connection;

            Assert.False(brokeredServiceClient2.Connection.IsCompleted);
            Assert.NotNull(await workspaceProjectFactory2.GetSupportedBuildSystemPropertiesAsync(CancellationToken.None));
        }
        finally
        {
            if (!server1Disposed)
            {
                await server1.DisposeAsync();
            }

            await server2.DisposeAsync();
        }
    }

    [Fact]
    public async Task ManagedHotReloadLanguageServiceIsAvailableAsync()
    {
        await using var server = await CreateLanguageServerAsync();
        await using var brokeredServiceClient = new TestBrokeredServiceClient();

        await brokeredServiceClient.ConnectAsync(server);

        var serverServices = await GetAvailableServerServicesAsync(brokeredServiceClient.ServiceBroker, CancellationToken.None);
        Assert.Contains(ManagedHotReloadLanguageServiceDescriptor.Descriptor.Moniker, serverServices);

        var languageService = await GetRequiredServiceAsync<DebuggerContracts.IManagedHotReloadLanguageService3>(
            brokeredServiceClient.ServiceBroker,
            ManagedHotReloadLanguageServiceDescriptor.Descriptor,
            CancellationToken.None);
        Assert.NotNull(languageService);
    }

    [Fact]
    public void TestBrokeredServicePipeNameFitsWithinMacOSSocketPathLimit()
    {
        var pipeName = NamedPipeTestUtilities.CreateShortPipeName("rsb-");

        Assert.InRange((HelixUnixPipePathPrefix + pipeName).Length, 1, MacOSUnixDomainSocketMaxPathLength);
    }

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
        var container = new TestBrokeredServiceContainer(new TraceSource(nameof(TestBrokeredServiceContainer)));
        var contributor = new TestDiscoveryServiceContributor(() => null);

        // Should be a no-op (and must not throw) when there is no implementation to proffer.
        contributor.Proffer(container);
    }

    [Fact]
    public async Task ProfferedService_IsAcquirable_AndInitializeIsInvoked()
    {
        var service = new FakeTestDiscoveryLanguageService();
        var contributor = new TestDiscoveryServiceContributor(() => service);

        var container = new TestBrokeredServiceContainer(new TraceSource(nameof(TestBrokeredServiceContainer)));

        // Mirror the production ordering performed by BrokeredServiceContainer.CreateAsync /
        // ServiceBrokerFactory.CreateAsync: register the advertised monikers, then proffer.
        container.RegisterServices(contributor.ServicesToRegister);
        contributor.Proffer(container);

        // The proffered moniker is advertised and serviceable: acquiring a proxy via the implementation's
        // descriptor runs the proffer factory callback, which invokes InitializeAsync before handing back
        // the proffered instance. Invoking an RPC method then round-trips to that instance.
        var proxy = await GetRequiredServiceAsync<IFakeTestDiscoveryRpc>(
            container.GetFullAccessServiceBroker(), service.Descriptor, CancellationToken.None);

        await service.InitializeCalled.Task.WaitAsync(CancellationToken.None);
        Assert.Equal(1, service.InitializeCallCount);

        Assert.Equal("pong", await proxy.PingAsync(CancellationToken.None));
    }

    private static async Task<IReadOnlyCollection<ServiceMoniker>> GetAvailableServerServicesAsync(IServiceBroker serviceBroker, CancellationToken cancellationToken)
    {
        var manifest = await GetRequiredServiceAsync<IBrokeredServiceBridgeManifest>(serviceBroker, BrokeredServiceBridgeManifest.ServiceDescriptor, cancellationToken);
        return await manifest.GetAvailableServicesAsync(cancellationToken);
    }

#pragma warning disable ISB001 // Dispose of proxies - the caller owns the returned proxy.
    private static async Task<T> GetRequiredServiceAsync<T>(IServiceBroker serviceBroker, ServiceRpcDescriptor serviceDescriptor, CancellationToken cancellationToken) where T : class
    {
        return await serviceBroker.GetProxyAsync<T>(
            serviceDescriptor,
            cancellationToken: cancellationToken) ?? throw new InvalidOperationException($"Unable to get {typeof(T).Name}.");
    }
#pragma warning restore ISB001

    private sealed class ServiceBrokerConnectParams
    {
        [JsonPropertyName("pipeName")]
        public required string PipeName { get; init; }
    }

    private sealed class TestBrokeredServiceClient : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly TestBrokeredServiceContainer _container = new(new TraceSource(nameof(TestBrokeredServiceClient)));
        private readonly NamedPipeServerStream _pipeStream;
        private readonly string _pipeName;
        private Task? _connectionTask;

        public TestBrokeredServiceClient()
        {
            _pipeName = NamedPipeTestUtilities.CreateShortPipeName("rsb-");
            _pipeStream = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                maxNumberOfServerInstances: 1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);

            ProfferClientServices();
        }

        public IServiceBroker ServiceBroker
            => _container.GetFullAccessServiceBroker();

        public Task Connection
            => _connectionTask ?? throw new InvalidOperationException($"{nameof(ConnectAsync)} has not been called.");

        public async Task ConnectAsync(TestLspServer server)
        {
            Contract.ThrowIfFalse(_connectionTask == null, $"{nameof(ConnectAsync)} should only be called once.");

            var serverServicesAvailable = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _connectionTask = RunConnectionAsync(serverServicesAvailable);

            await server.ExecuteNotificationAsync(ServiceBrokerConnectMethodName, new ServiceBrokerConnectParams { PipeName = _pipeName });

            var completedTask = await Task.WhenAny(serverServicesAvailable.Task, _connectionTask);
            if (completedTask == _connectionTask)
            {
                await _connectionTask;
            }

            await serverServicesAvailable.Task;
        }

        public async ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            _pipeStream.Dispose();

            if (_connectionTask != null)
            {
                try
                {
                    await _connectionTask.WaitAsync(s_timeout);
                }
                catch (OperationCanceledException)
                {
                }
                catch (IOException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }

            _cancellationTokenSource.Dispose();
        }

        private void ProfferClientServices()
        {
            _container.RegisterServices(new Dictionary<ServiceMoniker, ServiceRegistration>
            {
                { Descriptors.RemoteProjectInitializationStatusService.Moniker, new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false) },
            });

            _container.Proffer(
                Descriptors.RemoteProjectInitializationStatusService,
                (moniker, options, serviceBroker, cancellationToken) => new ValueTask<object?>(new TestProjectInitializationStatusService()));
        }

        /// <summary>
        /// Sets up the bridge to the server, registers remotes services, and returns a task that completes when the connection ends.
        /// </summary>
        /// <param name="serverServicesAvailable">A source that is completed when the server's services are available.</param>
        private async Task RunConnectionAsync(TaskCompletionSource serverServicesAvailable)
        {
            await _pipeStream.WaitForConnectionAsync(_cancellationTokenSource.Token);

            using var multiplexingStream = await MultiplexingStream.CreateAsync(_pipeStream, _cancellationTokenSource.Token);
            await Task.WhenAll(
                ConsumeServicesFromServerAsync(multiplexingStream, serverServicesAvailable),
                ProfferServicesToServerAsync(multiplexingStream));
        }

        private async Task ConsumeServicesFromServerAsync(MultiplexingStream multiplexingStream, TaskCompletionSource serverServicesAvailable)
        {
            using var channel = await multiplexingStream.AcceptChannelAsync(ServiceBrokerChannelName, _cancellationTokenSource.Token);
            var remoteClient = FrameworkServices.RemoteServiceBroker.ConstructRpc<IRemoteServiceBroker>(channel);

            // Register the bridge service first so we can query it for available services on the server.
            _container.RegisterServices(CreateServiceRegistrations([BrokeredServiceBridgeManifest.ServiceDescriptor.Moniker]));
            using var manifestBroker = _container.ProfferRemoteBroker(
                remoteClient,
                multiplexingStream,
                ServiceSource.OtherProcessOnSameMachine,
                [BrokeredServiceBridgeManifest.ServiceDescriptor.Moniker]);

            var serverServiceMonikers = await GetAvailableServerServicesAsync(ServiceBroker, _cancellationTokenSource.Token);
            // Register the services the server offers to our container so we can query them.
            _container.RegisterServices(CreateServiceRegistrations(serverServiceMonikers));

            using (_container.ProfferRemoteBroker(
                remoteClient,
                multiplexingStream,
                ServiceSource.OtherProcessOnSameMachine,
                [.. serverServiceMonikers]))
            {
                // Now that the bridge is established and we've registered the server's services, signal that we're ready.
                serverServicesAvailable.SetResult();
                await channel.Completion.WaitAsync(_cancellationTokenSource.Token);
            }

            static Dictionary<ServiceMoniker, ServiceRegistration> CreateServiceRegistrations(IEnumerable<ServiceMoniker> serviceMonikers)
                => serviceMonikers.ToDictionary(
                    static serviceMoniker => serviceMoniker,
                    static _ => new ServiceRegistration(ServiceAudience.Local, null, allowGuestClients: false));
        }

        private async Task ProfferServicesToServerAsync(MultiplexingStream multiplexingStream)
        {
            using var channel = await multiplexingStream.OfferChannelAsync(ServiceBrokerChannelName, _cancellationTokenSource.Token);
            var serviceBroker = _container.GetLimitedAccessServiceBroker(
                ServiceAudience.Local,
                ImmutableDictionary<string, string>.Empty,
                ClientCredentialsPolicy.RequestOverridesDefault);

            using IpcRelayServiceBroker relayServiceBroker = new(serviceBroker);
            FrameworkServices.RemoteServiceBroker.ConstructRpc(relayServiceBroker, channel);
            await relayServiceBroker.Completion.WaitAsync(_cancellationTokenSource.Token);
        }
    }

    private sealed class TestBrokeredServiceContainer : GlobalBrokeredServiceContainer
    {
        public TestBrokeredServiceContainer(TraceSource traceSource)
            : base(ImmutableDictionary<ServiceMoniker, ServiceRegistration>.Empty, isClientOfExclusiveServer: false, joinableTaskFactory: null, traceSource)
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

        internal new void UnregisterServices(IEnumerable<ServiceMoniker> services)
            => base.UnregisterServices(services);
    }

    private sealed class TestProjectInitializationStatusService : IProjectInitializationStatusService
    {
        public ValueTask<IDisposable> SubscribeInitializationCompletionAsync(IObserver<ProjectInitializationCompletionState> observer, CancellationToken cancellationToken)
            => ValueTask.FromResult<IDisposable>(new EmptyDisposable());
    }

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

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
