// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.NavigateTo;

/// <summary>
/// Host interface abstracting over all the external functionality the <see cref="NavigateToSearcher"/> needs. This
/// provide an easy entry point for swapping out functionality of the host, including for testing purposes.
/// </summary>
internal interface INavigateToSearcherHost
{
    INavigateToSearchService? GetNavigateToSearchService(Project project);

    /// <summary>
    /// Returns the fully loaded state for both the project system and the remote host.
    /// </summary>
    ValueTask<bool> IsFullyLoadedAsync(CancellationToken cancellationToken);
}

internal interface IWorkspaceNavigateToSearcherHostService : IWorkspaceService
{
    ValueTask<bool> IsFullyLoadedAsync(CancellationToken cancellationToken);
}

internal sealed class DefaultNavigateToSearchHost(
    Solution solution,
    IAsynchronousOperationListener asyncListener,
    CancellationToken disposalToken) : INavigateToSearcherHost
{
    private readonly Solution _solution = solution;
    private readonly IAsynchronousOperationListener _asyncListener = asyncListener;
    private readonly CancellationToken _disposalToken = disposalToken;

    /// <summary>
    /// Single task used to both hydrate the remote host with the initial workspace solution,
    /// and track if that work completed.  Prior to it completing, we will try to get all
    /// navigate-to requests from our caches.  Once it is populated though, we can attempt to
    /// use the latest data instead.
    /// </summary>
    private static readonly object s_gate = new();
    private static Task? s_remoteHostHydrateTask = null;

    public INavigateToSearchService? GetNavigateToSearchService(Project project)
        => project.GetLanguageService<INavigateToSearchService>();

    public async ValueTask<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
    {
        var workspaceService = _solution.Workspace.Services.GetService<IWorkspaceNavigateToSearcherHostService>();
        if (workspaceService != null)
            return await workspaceService.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);

        var service = _solution.Services.GetRequiredService<IWorkspaceStatusService>();

        // We consider ourselves fully loaded when both the project system has completed loaded
        // us, and we've totally hydrated the oop side.  Until that happens, we'll attempt to
        // return cached data from languages that support that.
        var isProjectSystemFullyLoaded = await service.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
        if (!isProjectSystemFullyLoaded)
            return false;

        var isRemoteHostFullyLoaded = GetRemoteHostHydrateTask().IsCompleted;
        return isRemoteHostFullyLoaded;
    }

    /// <summary>
    /// If we're in a solution that is using OOP, this kicks off a task to get the oop side in
    /// sync with us.  Until that happens, we'll continue to use the cached results from prior
    /// sessions so that we can get results very quickly right after launch without forcing the
    /// user to wait for OOP to hydrate the entire solution over.  This strikes a good balance
    /// of speed and accuracy as most of the time cached results will be fast and good enough,
    /// and eventually (usually within dozens of seconds, even for large projects) we will
    /// switch over to full and accurate results which can also come back quickly.
    /// </summary>
    /// <remarks>
    /// If we do report cached data, we inform the user of this so they know the results may be
    /// incomplete or inaccurate and that they can try again later if necessary.
    /// </remarks>
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
    private Task GetRemoteHostHydrateTask()
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
    {
        lock (s_gate)
        {
            // Only need to do this once.
            if (s_remoteHostHydrateTask == null)
            {
                // If there are no projects in this solution that use OOP, then there's nothing we need to do.
                if (_solution.Projects.All(p => !RemoteSupportedLanguages.IsSupported(p.Language)))
                {
                    s_remoteHostHydrateTask = Task.CompletedTask;
                }
                else
                {
                    var asyncToken = _asyncListener.BeginAsyncOperation(nameof(GetRemoteHostHydrateTask));

                    s_remoteHostHydrateTask = Task.Run(async () =>
                    {
                        var client = await RemoteHostClient.TryGetClientAsync(_solution.Services, _disposalToken).ConfigureAwait(false);
                        if (client != null)
                        {
                            await client.TryInvokeAsync<IRemoteNavigateToSearchService>(
                                _solution,
                                (service, solutionInfo, cancellationToken) =>
                                service.HydrateAsync(solutionInfo, cancellationToken),
                                _disposalToken).ConfigureAwait(false);
                        }
                    }, _disposalToken);
                    s_remoteHostHydrateTask.CompletesAsyncOperation(asyncToken);
                }
            }

            return s_remoteHostHydrateTask;
        }
    }
}
