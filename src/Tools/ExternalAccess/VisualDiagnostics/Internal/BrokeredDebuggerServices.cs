// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Contracts;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Nerdbank.Streams;

namespace Microsoft.CodeAnalysis.ExternalAccess.VisualDiagnostics.Internal
{
    [Export(typeof(IBrokeredDebuggerServices))]
    internal sealed class BrokeredDebuggerServices : IBrokeredDebuggerServices, IDisposable
    {
        // HotReloadSessionNotificationService
        private static readonly ServiceRpcDescriptor HotReloadSessionNotificationServiceDescriptor = CreateDescriptor(
            new(HotReloadSessionNotificationServiceInfo.Moniker, new Version(HotReloadSessionNotificationServiceInfo.Version)),
        clientInterface: null);

        // ManagedHotReloadService
        private static readonly ServiceRpcDescriptor ManagedHotReloadAgentManagerServiceDescriptor = CreateDescriptor(
            new(ManagedHotReloadAgentManagerServiceInfo.Moniker, new Version(ManagedHotReloadAgentManagerServiceInfo.Version)),
            clientInterface: null);

        private const string ManagedHotReloadServiceInfo_Moniker = "Microsoft.VisualStudio.Debugger.ManagedHotReloadService";
        private const string ManagedHotReloadServiceInfo_Version = "0.1";
        private static readonly ServiceRpcDescriptor ManagedHotReloadServiceDescriptor = CreateDescriptor(
            new(ManagedHotReloadServiceInfo_Moniker, new Version(ManagedHotReloadServiceInfo_Version)),
            clientInterface: null);

        private CancellationToken _serviceBrokerToken = new CancellationToken();

        private readonly IServiceBroker _serviceBroker;
        private IHotReloadSessionNotificationService? _hotReloadSessionNotificationService;
        private IManagedHotReloadAgentManagerService? _ManagedHotReloadAgentManagerService;
        private IManagedHotReloadService? _managedHotReloadService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BrokeredDebuggerServices(
        [Import(typeof(SVsFullAccessServiceBroker))]
        IServiceBroker serviceBroker)
        {
            _serviceBroker = serviceBroker;
        }

        public Task<IServiceBroker> ServiceBrokerAsync()
        {
            return Task.FromResult<IServiceBroker>(_serviceBroker);
        }

        public async Task<IHotReloadSessionNotificationService?> HotReloadSessionNotificationServiceAsync()
        {
            if (_hotReloadSessionNotificationService == null)
            {
                _hotReloadSessionNotificationService = await _serviceBroker.GetProxyAsync<IHotReloadSessionNotificationService>(HotReloadSessionNotificationServiceDescriptor, _serviceBrokerToken).ConfigureAwait(false);
            }

            return _hotReloadSessionNotificationService;
        }

        public async Task<IManagedHotReloadAgentManagerService?> ManagedHotReloadAgentManagerServiceAsync()
        {
            if (_ManagedHotReloadAgentManagerService == null)
            {
                _ManagedHotReloadAgentManagerService = await _serviceBroker.GetProxyAsync<IManagedHotReloadAgentManagerService>(ManagedHotReloadAgentManagerServiceDescriptor, _serviceBrokerToken).ConfigureAwait(false);
            }

            return _ManagedHotReloadAgentManagerService;
        }

        public async Task<IManagedHotReloadService?> ManagedHotReloadServiceAsync()
        {
            if (_managedHotReloadService == null)
            {
                _managedHotReloadService = await _serviceBroker.GetProxyAsync<IManagedHotReloadService>(ManagedHotReloadServiceDescriptor, _serviceBrokerToken).ConfigureAwait(false);
            }

            return _managedHotReloadService;
        }

        public void Dispose()
        {
            (_hotReloadSessionNotificationService as IDisposable)?.Dispose();
            (_ManagedHotReloadAgentManagerService as IDisposable)?.Dispose();
            (_managedHotReloadService as IDisposable)?.Dispose();
        }

        private static ServiceJsonRpcDescriptor CreateDescriptor(ServiceMoniker moniker, Type? clientInterface) => new ServiceJsonRpcDescriptor(
            moniker,
            clientInterface,
            ServiceJsonRpcDescriptor.Formatters.MessagePack,
            ServiceJsonRpcDescriptor.MessageDelimiters.BigEndianInt32LengthHeader,
            new MultiplexingStream.Options { ProtocolMajorVersion = 3 })
            .WithExceptionStrategy(StreamJsonRpc.ExceptionProcessing.ISerializable);
    }
}
