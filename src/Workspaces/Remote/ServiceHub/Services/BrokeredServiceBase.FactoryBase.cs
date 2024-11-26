// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Remote.Host;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Nerdbank.Streams;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal abstract partial class BrokeredServiceBase
{
    private static int s_cultureInitialized;

    internal interface IFactory
    {
        object Create(IDuplexPipe pipe, IServiceProvider hostProvidedServices, ServiceActivationOptions serviceActivationOptions, IServiceBroker serviceBroker);
        Type ServiceType { get; }
    }

    internal abstract class FactoryBase<TService> : IServiceHubServiceFactory, IFactory
        where TService : class
    {
        static FactoryBase()
        {
            Debug.Assert(typeof(TService).IsInterface);
        }

        protected abstract TService CreateService(in ServiceConstructionArguments arguments);

        protected virtual TService CreateService(
            in ServiceConstructionArguments arguments,
            ServiceRpcDescriptor descriptor,
            ServiceRpcDescriptor.RpcConnection serverConnection,
            object? clientRpcTarget)
            => CreateService(arguments);

        public Task<object> CreateAsync(
           Stream stream,
           IServiceProvider hostProvidedServices,
           ServiceActivationOptions serviceActivationOptions,
           IServiceBroker serviceBroker,
           AuthorizationServiceClient? authorizationServiceClient)
        {
            // Exception from IServiceHubServiceFactory.CreateAsync are not propagated to the client.
            // Intercept the exception here and report it to the log.
            // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1410923
            try
            {
                // Dispose the AuthorizationServiceClient since we won't be using it
                authorizationServiceClient?.Dispose();

                // First request determines the default culture:
                // TODO: Flow culture explicitly where needed https://github.com/dotnet/roslyn/issues/43670
                if (Interlocked.CompareExchange(ref s_cultureInitialized, 1, 0) == 0)
                {
                    CultureInfo.DefaultThreadCurrentUICulture = serviceActivationOptions.ClientUICulture;
                    CultureInfo.DefaultThreadCurrentCulture = serviceActivationOptions.ClientCulture;
                }

                return Task.FromResult((object)Create(
                    stream.UsePipe(),
                    hostProvidedServices,
                    serviceActivationOptions,
                    serviceBroker));
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
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
            // Register this service broker globally (if it's the first we encounter) so it can be used by other
            // global services that need it.
            GlobalServiceBroker.RegisterServiceBroker(serviceBroker);

            var descriptor = ServiceDescriptors.Instance.GetServiceDescriptorForServiceFactory(typeof(TService));
            var serviceHubTraceSource = (TraceSource?)hostProvidedServices.GetService(typeof(TraceSource));
            var serverConnection = descriptor.WithTraceSource(serviceHubTraceSource).ConstructRpcConnection(pipe);

            var args = new ServiceConstructionArguments(hostProvidedServices, serviceBroker);
            var service = CreateService(args, descriptor, serverConnection, serviceActivationOptions.ClientRpcTarget);

            serverConnection.AddLocalRpcTarget(service);
            serverConnection.StartListening();

            return service;
        }
    }

    internal abstract class FactoryBase<TService, TCallback> : FactoryBase<TService>
        where TService : class
        where TCallback : class
    {
        static FactoryBase()
        {
            Debug.Assert(typeof(TCallback).IsInterface);
        }

        protected abstract TService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<TCallback> callback);

        protected sealed override TService CreateService(in ServiceConstructionArguments arguments)
            => throw ExceptionUtilities.Unreachable();

        protected sealed override TService CreateService(
            in ServiceConstructionArguments arguments,
            ServiceRpcDescriptor descriptor,
            ServiceRpcDescriptor.RpcConnection serverConnection,
            object? clientRpcTarget)
        {
            Contract.ThrowIfNull(descriptor.ClientInterface);
            var callback = (TCallback)(clientRpcTarget ?? serverConnection.ConstructRpcClient(descriptor.ClientInterface));
            return CreateService(arguments, new RemoteCallback<TCallback>(callback));
        }
    }
}
