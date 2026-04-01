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
    ServiceRpcDescriptor IExportedBrokeredService.Descriptor
        => ManagedHotReloadLanguageServiceDescriptor.Descriptor;

    public Task InitializeAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public ValueTask StartSessionAsync(CancellationToken cancellationToken)
        => impl.StartSessionAsync(cancellationToken);

    public ValueTask EndSessionAsync(CancellationToken cancellationToken)
        => impl.EndSessionAsync(cancellationToken);

    public ValueTask EnterBreakStateAsync(CancellationToken cancellationToken)
        => impl.EnterBreakStateAsync(cancellationToken);

    public ValueTask ExitBreakStateAsync(CancellationToken cancellationToken)
        => impl.ExitBreakStateAsync(cancellationToken);

    public ValueTask OnCapabilitiesChangedAsync(CancellationToken cancellationToken)
        => impl.OnCapabilitiesChangedAsync(cancellationToken);

    [Obsolete]
    public ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(CancellationToken cancellationToken)
        => throw new NotImplementedException();

    [Obsolete]
    public ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(ImmutableArray<string> runningProjects, CancellationToken cancellationToken)
    {
        // StreamJsonRpc may use this overload when the method is invoked with empty parameters. Call the new implementation instead.
        if (!runningProjects.IsEmpty)
            throw new NotImplementedException();

        return GetUpdatesAsync(ImmutableArray<RunningProjectInfo>.Empty, cancellationToken);
    }

    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(ImmutableArray<RunningProjectInfo> runningProjects, CancellationToken cancellationToken)
        => (await impl.GetUpdatesAsync(runningProjects.SelectAsArray(static info => info.ToContract()), cancellationToken).ConfigureAwait(false)).FromContract();

    public ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
        => impl.CommitUpdatesAsync(cancellationToken);

    [Obsolete]
    public ValueTask UpdateBaselinesAsync(ImmutableArray<string> projectPaths, CancellationToken cancellationToken)
        => throw new NotImplementedException();

    public ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
        => impl.DiscardUpdatesAsync(cancellationToken);

    public ValueTask<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
        => impl.HasChangesAsync(sourceFilePath, cancellationToken);
}
