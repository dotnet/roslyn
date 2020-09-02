// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Nerdbank.Streams;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Base type with servicehub helper methods. this is not tied to how Roslyn OOP works. 
    /// 
    /// any type that derived from this type is supposed to be an entry point for servicehub services.
    /// name of the type should match one appears in GenerateServiceHubConfigurationFiles.targets 
    /// and signature of either its constructor or static CreateAsync must follow the convension
    /// ctor(Stream stream, IServiceProvider serviceProvider).
    /// 
    /// see servicehub detail from VSIDE onenote
    /// https://microsoft.sharepoint.com/teams/DD_VSIDE
    /// </summary>
    internal abstract class BrokeredServiceBase : IDisposable
    {
        internal abstract class FactoryBase : IServiceHubServiceFactory
        {
            protected abstract WellKnownServiceHubService ServiceId { get; }
            protected abstract object CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker);

            protected virtual object CreateService(
                IServiceProvider serviceProvider,
                IServiceBroker serviceBroker,
                ServiceRpcDescriptor descriptor,
                ServiceRpcDescriptor.RpcConnection serverConnection,
                object? clientRpcTarget)
                => CreateService(serviceProvider, serviceBroker);

            public Task<object> CreateAsync(
               Stream stream,
               IServiceProvider hostProvidedServices,
               ServiceActivationOptions serviceActivationOptions,
               IServiceBroker serviceBroker,
               AuthorizationServiceClient? authorizationServiceClient)
            {
                // Dispose the AuthorizationServiceClient since we won't be using it
                authorizationServiceClient?.Dispose();

                return Task.FromResult(Create(
                    stream.UsePipe(),
                    hostProvidedServices,
                    serviceActivationOptions,
                    serviceBroker));
            }

            internal object Create(
               IDuplexPipe pipe,
               IServiceProvider hostProvidedServices,
               ServiceActivationOptions serviceActivationOptions,
               IServiceBroker serviceBroker)
            {
                var descriptor = ServiceId.GetServiceDescriptor(isRemoteHost64Bit: IntPtr.Size == 8);
                var serverConnection = descriptor.ConstructRpcConnection(pipe);

                var service = CreateService(hostProvidedServices, serviceBroker, descriptor, serverConnection, serviceActivationOptions.ClientRpcTarget);

                serverConnection.AddLocalRpcTarget(service);
                serverConnection.StartListening();

                return service;
            }
        }

        internal abstract class FactoryBase<TCallback> : FactoryBase
            where TCallback : class
        {
            protected abstract object CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, RemoteCallback<TCallback> callback);

            protected sealed override object CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker)
                => throw ExceptionUtilities.Unreachable;

            protected sealed override object CreateService(
                IServiceProvider serviceProvider,
                IServiceBroker serviceBroker,
                ServiceRpcDescriptor descriptor,
                ServiceRpcDescriptor.RpcConnection serverConnection,
                object? clientRpcTarget)
            {
                Contract.ThrowIfNull(descriptor.ClientInterface);
                var callback = (TCallback)(clientRpcTarget ?? serverConnection.ConstructRpcClient(descriptor.ClientInterface));
                return CreateService(serviceProvider, serviceBroker, new RemoteCallback<TCallback>(callback));
            }
        }

        protected readonly RemoteWorkspaceManager WorkspaceManager;

        protected readonly SolutionAssetSource SolutionAssetSource;
        protected readonly CancellationTokenSource? ClientDisconnectedSource;
        protected readonly ServiceBrokerClient ServiceBrokerClient;

        // test data are only available when running tests:
        internal readonly RemoteHostTestData? TestData;

        static BrokeredServiceBase()
        {
            // Use a TraceListener hook to intercept assertion failures and report them through FatalError.
            WatsonTraceListener.Install();
        }

        protected BrokeredServiceBase(IServiceProvider serviceProvider, IServiceBroker serviceBroker, CancellationTokenSource? clientDisconnectedSource)
        {
            TestData = (RemoteHostTestData?)serviceProvider.GetService(typeof(RemoteHostTestData));
            WorkspaceManager = TestData?.WorkspaceManager ?? RemoteWorkspaceManager.Default;

#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
            ServiceBrokerClient = new ServiceBrokerClient(serviceBroker);
#pragma warning restore

            SolutionAssetSource = new SolutionAssetSource(ServiceBrokerClient);
            ClientDisconnectedSource = clientDisconnectedSource;
        }

        public void Dispose()
        {
            ServiceBrokerClient.Dispose();
        }

        public RemoteWorkspace GetWorkspace()
            => WorkspaceManager.GetWorkspace();

        protected Task<Solution> GetSolutionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            var workspace = GetWorkspace();
            var assetProvider = workspace.CreateAssetProvider(solutionInfo, WorkspaceManager.SolutionAssetCache, SolutionAssetSource);
            return workspace.GetSolutionAsync(assetProvider, solutionInfo.SolutionChecksum, solutionInfo.FromPrimaryBranch, solutionInfo.WorkspaceVersion, cancellationToken);
        }

        protected async ValueTask<T> RunServiceAsync<T>(Func<CancellationToken, Task<T>> implementation, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();
            using var _ = LinkToken(ref cancellationToken);

            try
            {
                return await implementation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected async ValueTask RunServiceAsync(Func<CancellationToken, Task> implementation, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();
            using var _ = LinkToken(ref cancellationToken);

            try
            {
                await implementation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportWithoutCrashUnlessCanceledAndPropagate(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private CancellationTokenSource? LinkToken(ref CancellationToken cancellationToken)
        {
            if (ClientDisconnectedSource is null)
            {
                return null;
            }

            var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ClientDisconnectedSource.Token);
            cancellationToken = source.Token;
            return source;
        }
    }
}
