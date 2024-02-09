// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using InternalContracts = Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue;

[ExportBrokeredService(MonikerName, ServiceVersion, Audience = ServiceAudience.Local)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class ManagedHotReloadLanguageServiceBridge(InternalContracts.IManagedHotReloadLanguageService service) : IManagedHotReloadLanguageService, IExportedBrokeredService
{
    private const string ServiceName = "ManagedHotReloadLanguageService";
    private const string ServiceVersion = "0.1";
    private const string MonikerName = BrokeredServiceDescriptors.LanguageServerComponentNamespace + "." + BrokeredServiceDescriptors.LanguageServerComponentName + "." + ServiceName;

    public static readonly ServiceJsonRpcDescriptor ServiceDescriptor = BrokeredServiceDescriptors.CreateServerServiceDescriptor(ServiceName, new(ServiceVersion));

    static ManagedHotReloadLanguageServiceBridge()
        => Debug.Assert(ServiceDescriptor.Moniker.Name == MonikerName);

    ServiceRpcDescriptor IExportedBrokeredService.Descriptor
        => ServiceDescriptor;

    public Task InitializeAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public ValueTask StartSessionAsync(CancellationToken cancellationToken)
        => service.StartSessionAsync(cancellationToken);

    public ValueTask EndSessionAsync(CancellationToken cancellationToken)
        => service.EndSessionAsync(cancellationToken);

    public ValueTask EnterBreakStateAsync(CancellationToken cancellationToken)
        => service.EnterBreakStateAsync(cancellationToken);

    public ValueTask ExitBreakStateAsync(CancellationToken cancellationToken)
        => service.ExitBreakStateAsync(cancellationToken);

    public ValueTask OnCapabilitiesChangedAsync(CancellationToken cancellationToken)
        => service.OnCapabilitiesChangedAsync(cancellationToken);

    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(CancellationToken cancellationToken)
        => (await service.GetUpdatesAsync(cancellationToken).ConfigureAwait(false)).FromContract();

    public ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
        => service.CommitUpdatesAsync(cancellationToken);

    public ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
        => service.DiscardUpdatesAsync(cancellationToken);

    public ValueTask<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
        => service.HasChangesAsync(sourceFilePath, cancellationToken);
}
