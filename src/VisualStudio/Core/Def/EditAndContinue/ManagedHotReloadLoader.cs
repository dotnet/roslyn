// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue;

/// <summary>
/// Used to delay loading managed Hot Reload MEF components until managed Hot Reload session has started.
/// Instantiated and <see cref="LoadAsync"/> invoked when the managed Hot Reload session starts.
/// The defining assembly of <see cref="IHotReloadComponentLoader"/> is not loaded until the managed Hot Reload session starts.
/// </summary>
[Export(typeof(IHotReloadComponentLoader)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ManagedHotReloadLoader(
    IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> containerService,
    IVsService<SComponentModel, IComponentModel> componentModelService) : IHotReloadComponentLoader, IDisposable
{
    private IDisposable? _sourceTextProvider;
    private IDisposable? _hotReloadService;

    public async ValueTask LoadAsync(CancellationToken cancellationToken)
    {
        var container = await containerService.GetValueAsync(cancellationToken).ConfigureAwait(false);
        var componentModel = await componentModelService.GetValueAsync(cancellationToken).ConfigureAwait(false);
        var serviceBroker = container.GetFullAccessServiceBroker();
        var hotReloadFactory = componentModel.GetService<ManagedHotReloadLanguageServiceFactory>();
        var solutionSnapshotProvider = componentModel.GetService<ISolutionSnapshotProvider>();
        var hostWorkspaceProvider = componentModel.GetService<IHostWorkspaceProvider>();

        var sourceTextProvider = new PdbMatchingSourceTextProvider(hostWorkspaceProvider.Workspace);
        _sourceTextProvider = sourceTextProvider;

        var hotReloadService = new ManagedHotReloadLanguageService(
            serviceBroker => hotReloadFactory.CreateImplementation(serviceBroker, solutionSnapshotProvider, hostWorkspaceProvider, sourceTextProvider));
        _hotReloadService = hotReloadService;

        await hotReloadService.InitializeAsync(serviceBroker, cancellationToken).ConfigureAwait(false);

        container.Proffer(ManagedHotReloadLanguageServiceFactory.ServiceDescriptor, async (_, _, _, _) => hotReloadService);
    }

    public void Dispose()
    {
        _sourceTextProvider?.Dispose();
        _hotReloadService?.Dispose();
    }
}
