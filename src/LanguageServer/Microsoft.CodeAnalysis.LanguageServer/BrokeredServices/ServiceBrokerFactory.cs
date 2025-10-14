// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using ExportProvider = Microsoft.VisualStudio.Composition.ExportProvider;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

/// <summary>
/// Exports an <see cref="IServiceBroker"/> for convenient and potentially cross-IDE importing by other features.
/// </summary>
/// <remarks>
/// Each import site gets its own <see cref="IServiceBroker"/> instance to match the behavior of calling <see cref="IBrokeredServiceContainer.GetFullAccessServiceBroker"/>
/// which returns a private instance for everyone.
/// This is observable to callers in a few ways, including that they only get the <see cref="IServiceBroker.AvailabilityChanged"/> events
/// based on their own service queries.
/// MEF will dispose of each instance as its lifetime comes to an end.
/// </remarks>
#pragma warning disable RS0030 // This is intentionally using System.ComponentModel.Composition for compatibility with MEF service broker.
[Export]
internal sealed class ServiceBrokerFactory
{
    private BrokeredServiceContainer? _container;
    private readonly ExportProvider _exportProvider;
    private readonly WrappedServiceBroker _wrappedServiceBroker;
    private Task _bridgeCompletionTask;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ImmutableArray<IOnServiceBrokerInitialized> _onServiceBrokerInitialized;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ServiceBrokerFactory([ImportMany] IEnumerable<IOnServiceBrokerInitialized> onServiceBrokerInitialized,
        ExportProvider exportProvider,
        WrappedServiceBroker wrappedServiceBroker)
    {
        _exportProvider = exportProvider;
        _bridgeCompletionTask = Task.CompletedTask;
        _onServiceBrokerInitialized = [.. onServiceBrokerInitialized];
        _wrappedServiceBroker = wrappedServiceBroker;
    }

    /// <summary>
    /// Returns a full-access service broker, but will return null if we haven't yet connected to the Dev Kit broker.
    /// </summary>
    public IServiceBroker? TryGetFullAccessServiceBroker() => _container?.GetFullAccessServiceBroker();

    public BrokeredServiceContainer GetRequiredServiceBrokerContainer()
    {
        Contract.ThrowIfNull(_container);
        return _container;
    }

    /// <summary>
    /// Creates a service broker instance without connecting via a pipe to another process.
    /// </summary>
    public async Task CreateAsync()
    {
        Contract.ThrowIfFalse(_container == null, "We should only create one container.");

        _container = await BrokeredServiceContainer.CreateAsync(_exportProvider, _cancellationTokenSource.Token);
        _wrappedServiceBroker.SetServiceBroker(_container.GetFullAccessServiceBroker());

        foreach (var onInitialized in _onServiceBrokerInitialized)
        {
            try
            {
                onInitialized.OnServiceBrokerInitialized(_container.GetFullAccessServiceBroker());
            }
            catch (Exception)
            {
            }
        }
    }

    public async Task CreateAndConnectAsync(string brokeredServicePipeName)
    {
        await CreateAsync();

        var bridgeProvider = _exportProvider.GetExportedValue<BrokeredServiceBridgeProvider>();
        _bridgeCompletionTask = bridgeProvider.SetupBrokeredServicesBridgeAsync(brokeredServicePipeName, _container!, _cancellationTokenSource.Token);
    }

    public Task ShutdownAndWaitForCompletionAsync()
    {
        _cancellationTokenSource.Cancel();

        // Return the task we created when we created the bridge; if we never started it in the first place, we'll just return the
        // completed task set in the constructor, so the waiter no-ops.
        return _bridgeCompletionTask;
    }
}
