// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Debugger.Contracts.HotReload;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Exposes <see cref="EditAndContinueLanguageService"/> as a brokered service.
/// TODO (https://github.com/dotnet/roslyn/issues/72713):
/// Once debugger is updated to use the brokered service, this class should be removed and <see cref="EditAndContinueLanguageService"/> should be exported directly.
/// </summary>
internal sealed partial class ManagedEditAndContinueLanguageServiceBridge(EditAndContinueLanguageService service) : IManagedHotReloadLanguageService
{
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
        => (await service.GetUpdatesAsync(cancellationToken).ConfigureAwait(false));

    public ValueTask CommitUpdatesAsync(CancellationToken cancellationToken)
        => service.CommitUpdatesAsync(cancellationToken);

    public ValueTask DiscardUpdatesAsync(CancellationToken cancellationToken)
        => service.DiscardUpdatesAsync(cancellationToken);

    public ValueTask<bool> HasChangesAsync(string? sourceFilePath, CancellationToken cancellationToken)
        => service.HasChangesAsync(sourceFilePath, cancellationToken);
}

