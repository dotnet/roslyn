// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Factory that creates the hot reload brokered service stack. Non-brokered dependencies are resolved via MEF;
/// brokered-service-dependent components and the host's <see cref="IHostWorkspaceProvider"/> are passed
/// to <see cref="CreateAsync"/>.
/// </summary>
[Shared]
[Export(typeof(ManagedHotReloadLanguageServiceFactory))]
[Export(typeof(IEditAndContinueSolutionProvider))]
internal sealed class ManagedHotReloadLanguageServiceFactory : IEditAndContinueSolutionProvider
{
    private readonly EditAndContinueSessionState _sessionState;
    private readonly IActiveStatementTrackingController _activeStatementTrackingController;
    private readonly IDiagnosticsRefresher _diagnosticRefresher;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ManagedHotReloadLanguageServiceFactory(
        EditAndContinueSessionState sessionState,
        IActiveStatementTrackingController activeStatementTrackingController,
        IDiagnosticsRefresher diagnosticRefresher,
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _sessionState = sessionState;
        _activeStatementTrackingController = activeStatementTrackingController;
        _diagnosticRefresher = diagnosticRefresher;
        _listenerProvider = listenerProvider;
    }

    public static readonly ServiceRpcDescriptor ServiceDescriptor = IManagedHotReloadUpdatesProvider.CreateServiceDescriptor(ManagedHotReloadUpdatesProviderDescriptor.Moniker);

    public event Action<Solution>? SolutionCommitted;

    /// <summary>
    /// Creates the hot reload brokered service stack.
    /// </summary>
    /// <param name="serviceBroker">The service broker used to acquire debugger and logger services.</param>
    /// <param name="solutionSnapshotProvider">
    /// Host-specific solution snapshot provider. In VS this is EditorHostSolutionProvider (from MEF)
    /// </param>
    /// <param name="workspaceProvider">Host's workspace provider used by the implementation to enqueue source generator updates.</param>
    /// <param name="sourceTextProvider">
    /// Per-host source text provider, observing the host's <see cref="WorkspaceKind.Host"/> workspace. Owned by the caller
    /// (the per-server hot reload stack), which is responsible for disposing it when the host/server shuts down.
    /// </param>
    public async ValueTask<IManagedHotReloadUpdatesProvider> CreateAsync(
        IServiceBroker serviceBroker,
        ISolutionSnapshotProvider solutionSnapshotProvider,
        IHostWorkspaceProvider workspaceProvider,
        PdbMatchingSourceTextProvider sourceTextProvider,
        CancellationToken cancellationToken)
    {
        IDisposable? eventSubscription = null;
        IDisposable? providerRegistration = null;

        try
        {
            var hotReloadStateProxy = new ManagedHotReloadStateProxy(serviceBroker);
            var logReporter = new EditAndContinueLogReporter(serviceBroker, _listenerProvider);

            var impl = new ManagedHotReloadLanguageServiceImpl(
                _sessionState,
                workspaceProvider,
                hotReloadStateProxy,
                solutionSnapshotProvider,
                sourceTextProvider,
                _activeStatementTrackingController,
                logReporter,
                _diagnosticRefresher);

#pragma warning disable ISB001 // Dispose of proxies
            var hotReloadEventSubscriber = await serviceBroker.GetProxyAsync<IHotReloadEventSubscriber>(
                IHotReloadEventSubscriber.ServiceDescriptor,
                new() { ClientRpcTarget = impl },
                cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

            Assumes.Present(hotReloadEventSubscriber);
            using var _1 = hotReloadEventSubscriber as IDisposable;

            eventSubscription = await hotReloadEventSubscriber.SubscribeAsync(cancellationToken).ConfigureAwait(false);

#pragma warning disable ISB001 // Dispose of proxies
            var registrationService = await serviceBroker.GetProxyAsync<IManagedHotReloadUpdatesProviderRegistration>(IManagedHotReloadUpdatesProviderRegistration.ServiceDescriptor, cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

            Assumes.Present(registrationService);
            using var _2 = registrationService as IDisposable;

            providerRegistration = await registrationService.RegisterAsync(ManagedHotReloadUpdatesProviderDescriptor.Moniker, cancellationToken).ConfigureAwait(false);

            impl.SolutionCommitted += solution => SolutionCommitted?.Invoke(solution);

            var service = new ManagedHotReloadLanguageService(impl, eventSubscription, providerRegistration);

            // transfer ownership to the service instance
            eventSubscription = null;
            providerRegistration = null;

            return service;
        }
        finally
        {
            eventSubscription?.Dispose();
            providerRegistration?.Dispose();
        }
    }
}
