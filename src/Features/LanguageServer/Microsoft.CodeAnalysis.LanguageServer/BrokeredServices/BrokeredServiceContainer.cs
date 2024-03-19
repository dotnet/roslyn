// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
internal class BrokeredServiceContainer : GlobalBrokeredServiceContainer
{
    public BrokeredServiceContainer(TraceSource traceSource)
        : base(ImmutableDictionary<ServiceMoniker, ServiceRegistration>.Empty, isClientOfExclusiveServer: false, joinableTaskFactory: null, traceSource)
    {
    }

    public override IReadOnlyDictionary<string, string> LocalUserCredentials
        => ImmutableDictionary<string, string>.Empty;

    /// <inheritdoc cref="GlobalBrokeredServiceContainer.RegisterServices(IReadOnlyDictionary{ServiceMoniker, ServiceRegistration})"/>
    internal new void RegisterServices(IReadOnlyDictionary<ServiceMoniker, ServiceRegistration> services)
        => base.RegisterServices(services);

    /// <inheritdoc cref="GlobalBrokeredServiceContainer.UnregisterServices(IEnumerable{ServiceMoniker})"/>
    internal new void UnregisterServices(IEnumerable<ServiceMoniker> services)
        => base.UnregisterServices(services);

    internal ImmutableDictionary<ServiceMoniker, ServiceRegistration> GetRegisteredServices()
        => RegisteredServices;

    internal static async Task<BrokeredServiceContainer> CreateAsync(ExportProvider exportProvider, CancellationToken cancellationToken)
    {
        var traceListener = exportProvider.GetExportedValue<BrokeredServiceTraceListener>();
        var container = new BrokeredServiceContainer(traceListener.Source);

        container.ProfferIntrinsicService(
            FrameworkServices.Authorization,
            new ServiceRegistration(VisualStudio.Shell.ServiceBroker.ServiceAudience.Local, null, allowGuestClients: true),
            (moniker, options, serviceBroker, cancellationToken) => new(new NoOpAuthorizationService()));

        var mefServiceBroker = exportProvider.GetExportedValue<MefServiceBrokerOfExportedServices>();
        mefServiceBroker.SetContainer(container);

        // Register local mef services.
        await mefServiceBroker.RegisterAndProfferServicesAsync(cancellationToken);

        // Register the desired remote services
        container.RegisterServices(Descriptors.RemoteServicesToRegister);

        return container;
    }

    private class NoOpAuthorizationService : IAuthorizationService
    {
        public event EventHandler? CredentialsChanged;

        public event EventHandler? AuthorizationChanged;

        public ValueTask<bool> CheckAuthorizationAsync(ProtectedOperation operation, CancellationToken cancellationToken = default)
        {
            return new(true);
        }

        public ValueTask<IReadOnlyDictionary<string, string>> GetCredentialsAsync(CancellationToken cancellationToken = default)
        {
            return new(ImmutableDictionary<string, string>.Empty);
        }

        protected virtual void OnCredentialsChanged(EventArgs args) => this.CredentialsChanged?.Invoke(this, args);

        protected virtual void OnAuthorizationChanged(EventArgs args) => this.AuthorizationChanged?.Invoke(this, args);
    }
}
