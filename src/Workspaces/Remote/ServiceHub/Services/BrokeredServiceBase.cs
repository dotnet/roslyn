// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Nerdbank.Streams;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Base type for Roslyn brokered services hosted in ServiceHub.
    /// </summary>
    internal abstract class BrokeredServiceBase : IDisposable
    {
        internal interface IFactory
        {
            public object Create(IDuplexPipe pipe, IServiceProvider hostProvidedServices, ServiceActivationOptions serviceActivationOptions, IServiceBroker serviceBroker);
            public Type ServiceType { get; }
        }

        internal abstract class FactoryBase<TService> : IServiceHubServiceFactory, IFactory
            where TService : class
        {
            protected abstract TService CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker);

            protected virtual TService CreateService(
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

                return Task.FromResult((object)Create(
                    stream.UsePipe(),
                    hostProvidedServices,
                    serviceActivationOptions,
                    serviceBroker));
            }

            object IFactory.Create(IDuplexPipe pipe, IServiceProvider hostProvidedServices, ServiceActivationOptions serviceActivationOptions, IServiceBroker serviceBroker)
                => Create(pipe, hostProvidedServices, serviceActivationOptions, serviceBroker);

            Type IFactory.ServiceType => typeof(TService);

            internal TService Create(
               IDuplexPipe pipe,
               IServiceProvider hostProvidedServices,
               ServiceActivationOptions serviceActivationOptions,
               IServiceBroker serviceBroker)
            {
                var descriptor = ServiceDescriptors.GetServiceDescriptor(typeof(TService), isRemoteHost64Bit: IntPtr.Size == 8);
                var serverConnection = descriptor.ConstructRpcConnection(pipe);

                var service = CreateService(hostProvidedServices, serviceBroker, descriptor, serverConnection, serviceActivationOptions.ClientRpcTarget);

                serverConnection.AddLocalRpcTarget(service);
                serverConnection.StartListening();

                return service;
            }
        }

        internal abstract class FactoryBase<TService, TCallback> : FactoryBase<TService>
            where TService : class
            where TCallback : class
        {
            protected abstract TService CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, RemoteCallback<TCallback> callback);

            protected sealed override TService CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker)
                => throw ExceptionUtilities.Unreachable;

            protected sealed override TService CreateService(
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

        protected async ValueTask<T> RunServiceAsync<T>(Func<CancellationToken, ValueTask<T>> implementation, CancellationToken cancellationToken)
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

        protected async ValueTask RunServiceAsync(Func<CancellationToken, ValueTask> implementation, CancellationToken cancellationToken)
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
