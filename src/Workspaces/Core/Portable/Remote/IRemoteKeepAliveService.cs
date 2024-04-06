// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

internal interface IRemoteKeepAliveService
{
    /// <summary>
    /// Keeps alive this solution in the OOP process until the cancellation token is triggered.  Used so that long
    /// running features (like 'inline rename' or 'lightbulbs') we can call into oop several times, with the same
    /// snapshot, knowing that things will stay hydrated and alive on the OOP side.  Importantly, by keeping the
    /// same <see cref="Solution"/> snapshot alive on the OOP side, computed attached values (like <see
    /// cref="Compilation"/>s) will stay alive as well.
    /// </summary>
    ValueTask KeepAliveAsync(Checksum solutionChecksum, CancellationToken cancellationToken);
}

internal sealed class RemoteKeepAliveSession : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private RemoteKeepAliveSession(SolutionCompilationState compilationState, IAsynchronousOperationListener listener)
    {
        var cancellationToken = _cancellationTokenSource.Token;
        var token = listener.BeginAsyncOperation(nameof(RemoteKeepAliveSession));

        var task = CreateClientAndKeepAliveAsync();
        task.CompletesAsyncOperation(token);

        return;

        async Task CreateClientAndKeepAliveAsync()
        {
            var client = await RemoteHostClient.TryGetClientAsync(compilationState.Services, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return;

            // Now kick off the keep-alive work.  We don't wait on this as this will stick on the OOP side until
            // the cancellation token triggers.
            var unused = client.TryInvokeAsync<IRemoteKeepAliveService>(
                compilationState,
                (service, solutionInfo, cancellationToken) => service.KeepAliveAsync(solutionInfo, cancellationToken),
                cancellationToken).AsTask();
        }
    }

    ~RemoteKeepAliveSession()
    {
        if (Environment.HasShutdownStarted)
            return;

        Contract.Fail("Should have been disposed!");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Creates a session between the host and OOP, effectively pinning this <paramref name="solution"/> until <see
    /// cref="IDisposable.Dispose"/> is called on it.  By pinning the solution we ensure that all calls to OOP for
    /// the same solution during the life of this session do not need to resync the solution.  Nor do they then need
    /// to rebuild any compilations they've already built due to the solution going away and then coming back.
    /// </summary>
    /// <remarks>
    /// The <paramref name="listener"/> is not strictly necessary for this type.  This class functions just as an
    /// optimization to hold onto data so it isn't resync'ed or recomputed.  However, we still want to let the
    /// system know when unobserved async work is kicked off in case we have any tooling that keep track of this for
    /// any reason (for example for tracking down problems in testing scenarios).
    /// </remarks>
    public static RemoteKeepAliveSession Create(Solution solution, IAsynchronousOperationListener listener)
        => Create(solution.CompilationState, listener);

    /// <inheritdoc cref="Create(Solution, IAsynchronousOperationListener)"/>
    public static RemoteKeepAliveSession Create(
        SolutionCompilationState compilationState, IAsynchronousOperationListener listener)
    {
        return new RemoteKeepAliveSession(compilationState, listener);
    }
}
