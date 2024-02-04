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
        private static readonly ServiceRpcDescriptor HotReloadSessionNotificationServiceDescriptor = CreateDescriptor(
            new(HotReloadSessionNotificationServiceInfo.Moniker, new Version(HotReloadSessionNotificationServiceInfo.Version)),
        clientInterface: null);

        private CancellationToken _serviceBrokerToken = new CancellationToken();
        private readonly IServiceBroker _serviceBroker;
        private IHotReloadSessionNotificationService? _hotReloadSessionNotificationService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public BrokeredDebuggerServices(
        [Import(typeof(SVsFullAccessServiceBroker))]
        IServiceBroker serviceBroker)
        {
            _serviceBroker = serviceBroker;
            _ = InitializeAsync();
        }

        public ValueTask<IHotReloadSessionNotificationService> HotReloadSessionNotificationService
        {
            get
            {
                if (_hotReloadSessionNotificationService == null)
                {
                    InitializeAsync().ConfigureAwait(true);
                }

                if (_hotReloadSessionNotificationService != null)
                {
                    return new ValueTask<IHotReloadSessionNotificationService>(_hotReloadSessionNotificationService);
                }

                return new ValueTask<IHotReloadSessionNotificationService>();
            }
        }

        private async Task InitializeAsync()
        {
            if (_serviceBroker == null)
            {
                return;
            }

            (_hotReloadSessionNotificationService as IDisposable)?.Dispose();

            _hotReloadSessionNotificationService = await _serviceBroker.GetProxyAsync<IHotReloadSessionNotificationService>(HotReloadSessionNotificationServiceDescriptor, _serviceBrokerToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            (_hotReloadSessionNotificationService as IDisposable)?.Dispose();
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
