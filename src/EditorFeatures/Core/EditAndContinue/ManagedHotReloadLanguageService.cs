// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Wrapper of <see cref="ManagedHotReloadLanguageServiceImpl"/> implementing closed-source debugger contract interfaces.
/// Created via <see cref="ManagedHotReloadLanguageServiceFactory"/> and manually proffered as a brokered service.
/// </summary>
internal sealed class ManagedHotReloadLanguageService(Func<IServiceBroker, ManagedHotReloadLanguageServiceImpl> implFactory) : IManagedHotReloadUpdatesProvider, IDisposable
{
    private ManagedHotReloadLanguageServiceImpl? _impl;
    private IDisposable? _eventSubscription;
    private IDisposable? _providerRegistration;

    internal async ValueTask InitializeAsync(IServiceBroker serviceBroker, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(_impl is null);

        _impl = implFactory(serviceBroker);

#pragma warning disable ISB001 // Dispose of proxies
        var hotReloadEventSubscriber = await serviceBroker.GetProxyAsync<IHotReloadEventSubscriber>(
            IHotReloadEventSubscriber.ServiceDescriptor,
            new() { ClientRpcTarget = _impl },
            cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

        Assumes.Present(hotReloadEventSubscriber);
        using var _1 = hotReloadEventSubscriber as IDisposable;

        _eventSubscription = await hotReloadEventSubscriber.SubscribeAsync(cancellationToken).ConfigureAwait(false);

#pragma warning disable ISB001 // Dispose of proxies
        var registrationService = await serviceBroker.GetProxyAsync<IManagedHotReloadUpdatesProviderRegistration>(IManagedHotReloadUpdatesProviderRegistration.ServiceDescriptor, cancellationToken).ConfigureAwait(false);
#pragma warning restore ISB001 // Dispose of proxies

        Assumes.Present(registrationService);
        using var _2 = registrationService as IDisposable;

        _providerRegistration = await registrationService.RegisterAsync(ManagedHotReloadUpdatesProviderDescriptor.Moniker, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _eventSubscription?.Dispose();
        _providerRegistration?.Dispose();
    }

    // internal for testing:
    internal ManagedHotReloadLanguageServiceImpl GetImplementation()
    {
        Contract.ThrowIfNull(_impl);
        return _impl;
    }

    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(ImmutableArray<RunningProjectInfo> runningProjects, CancellationToken cancellationToken)
        => (await GetImplementation().GetUpdatesAsync(runningProjects.SelectAsArray(rp => rp.ToContract()), cancellationToken).ConfigureAwait(false)).FromContract();

    public ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
        => GetImplementation().CommitUpdatesAsync(cancellationToken);

    public ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
        => GetImplementation().DiscardUpdatesAsync(cancellationToken);

    public ValueTask<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
        => GetImplementation().HasChangesAsync(sourceFilePath, cancellationToken);

}
