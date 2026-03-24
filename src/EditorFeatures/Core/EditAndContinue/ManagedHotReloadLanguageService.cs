// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using InternalContracts = Microsoft.CodeAnalysis.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Wrapper of <see cref="ManagedHotReloadLanguageServiceImpl"/> implementing closed-source debugger contract interfaces.
/// </summary>
[ExportBrokeredService(ManagedHotReloadLanguageServiceDescriptor.MonikerName, ManagedHotReloadLanguageServiceDescriptor.ServiceVersion, Audience = ServiceAudience.Local)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ManagedHotReloadLanguageService(ManagedHotReloadLanguageServiceImpl impl) : IManagedHotReloadLanguageService3, IExportedBrokeredService
{
    public ServiceRpcDescriptor? Descriptor
        => ManagedHotReloadLanguageServiceDescriptor.Descriptor;

    public Task InitializeAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public ValueTask StartSessionAsync(CancellationToken cancellationToken)
        => impl.StartSessionAsync(cancellationToken);

    public ValueTask EnterBreakStateAsync(CancellationToken cancellationToken)
        => impl.EnterBreakStateAsync(cancellationToken);

    public ValueTask ExitBreakStateAsync(CancellationToken cancellationToken)
        => impl.ExitBreakStateAsync(cancellationToken);

    public ValueTask OnCapabilitiesChangedAsync(CancellationToken cancellationToken)
        => impl.OnCapabilitiesChangedAsync(cancellationToken);

    public ValueTask<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
        => impl.HasChangesAsync(sourceFilePath, cancellationToken);

    [Obsolete]
    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(CancellationToken cancellationToken)
    {
        var updates = await impl.GetUpdatesAsync(ImmutableArray<InternalContracts.RunningProjectInfo>.Empty, cancellationToken).ConfigureAwait(false);
        return updates.FromContract();
    }

    [Obsolete]
    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(ImmutableArray<string> runningProjects, CancellationToken cancellationToken)
    {
        var updates = await impl.GetUpdatesAsync(runningProjects, cancellationToken).ConfigureAwait(false);
        return updates.FromContract();
    }

    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(ImmutableArray<RunningProjectInfo> runningProjects, CancellationToken cancellationToken)
    {
        var updates = await impl.GetUpdatesAsync(runningProjects.SelectAsArray(p => p.ToContract()), cancellationToken).ConfigureAwait(false);
        return updates.FromContract();
    }

    public ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
        => impl.CommitUpdatesAsync(cancellationToken);

    public ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
        => impl.DiscardUpdatesAsync(cancellationToken);

    [Obsolete]
    public ValueTask UpdateBaselinesAsync(ImmutableArray<string> projectPaths, CancellationToken cancellationToken)
        => impl.UpdateBaselinesAsync(projectPaths, cancellationToken);

    public ValueTask EndSessionAsync(CancellationToken cancellationToken)
        => impl.EndSessionAsync(cancellationToken);
}
