// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;
using Microsoft.VisualStudio.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Wrapper of <see cref="ManagedHotReloadLanguageServiceImpl"/> implementing closed-source debugger contract interfaces.
/// Created via <see cref="ManagedHotReloadLanguageServiceFactory"/> and manually proffered as a brokered service.
/// </summary>
internal sealed class ManagedHotReloadLanguageService(ManagedHotReloadLanguageServiceImpl impl, IDisposable eventListener, IDisposable providerRegistration) : IManagedHotReloadUpdatesProvider, IDisposable
{
    public void Dispose()
    {
        eventListener.Dispose();
        providerRegistration.Dispose();
    }

    // internal for testing:
    internal ManagedHotReloadLanguageServiceImpl Impl
        => impl;

    public async ValueTask<ManagedHotReloadUpdates> GetUpdatesAsync(ImmutableArray<RunningProjectInfo> runningProjects, CancellationToken cancellationToken)
        => (await impl.GetUpdatesAsync(runningProjects.SelectAsArray(rp => rp.ToContract()), cancellationToken).ConfigureAwait(false)).FromContract();

    public ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
        => impl.CommitUpdatesAsync(cancellationToken);

    public ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
        => impl.DiscardUpdatesAsync(cancellationToken);

    public ValueTask<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
        => impl.HasChangesAsync(sourceFilePath, cancellationToken);
}
