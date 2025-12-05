// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;

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
    /// <param name="sessionId">Id identifying this session.  Used with <see cref="WaitForSessionIdAsync"/> so that
    /// execution on the host side can proceed only once the proper snapshot is actually pinned on the OOP side.</param>
    ValueTask KeepAliveAsync(Checksum solutionChecksum, long sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Waits for the session identified by <paramref name="sessionId"/> to be fully hydrated and pinned in the OOP
    /// process.
    /// </summary>
    ValueTask WaitForSessionIdAsync(long sessionId, CancellationToken cancellationToken);
}

internal sealed class RemoteKeepAliveSession : IDisposable
{
    private static long s_sessionId = 1;

    /// <summary>
    /// A unique identifier for this session.  Used to allow us to kick off the work to sync a solution over, then
    /// wait for that work to complete, while the syncing task stays alive on the OOP side.
    /// </summary>
    private long SessionId { get; } = Interlocked.Increment(ref s_sessionId);

    /// <summary>
    /// Cancellation token used for keeping the keep-alive work alive.  We make a call over to oop, which syncs a
    /// solution/project-cone and then waits on this cancellation token.  So cancelling this is what actually allows the
    /// oop side to return and let go of the pinned solution/project-cone.
    /// </summary>
    private CancellationTokenSource KeepAliveTokenSource { get; } = new();

    private RemoteKeepAliveSession()
    {
    }

    /// <summary>
    /// Initializes a session, returning it once the session is fully established on the OOP side.
    /// </summary>
    private static async Task<RemoteKeepAliveSession> StartSessionAsync(
        SolutionCompilationState compilationState,
        ProjectId? projectId,
        RemoteHostClient? client,
        CancellationToken callerCancellationToken)
    {
        var session = new RemoteKeepAliveSession();

        // If we have no client, we just return a no-op session.  That's fine. This is the case when we're not running
        // anything in OOP, and so there's nothing to keep alive.  The caller will be holding onto the
        // solution/project-cone snapshot themselves, and so all the oop calls they make to it will just operate
        // directly on that shared instance.
        if (client is null)
            return session;

        // Now kick off the keep-alive work.  We don't wait on this as this will stick on the OOP side until the
        // cancellation token triggers.  Note: we pass the KeepAliveTokenSource.Token in here.  We want
        // disposing the returned RemoteKeepAliveSession to be the thing that cancels this work.
        _ = InvokeKeepAliveAsync(
            compilationState, projectId, client, session);

        // Now, actually make a call over to OOP to ensure the session is started.  This way the caller won't proceed
        // until the actual solution (or project-scope) is actually pinned in OOP.  Note: we pass the caller
        // cancellation token in here so that if the caller decides they don't need to proceed, we can bail quickly,
        // without waiting for the solution to sync and for us to wait on that completing.
        try
        {
            await client.TryInvokeAsync<IRemoteKeepAliveService>(
                compilationState,
                projectId,
                (service, _, cancellationToken) => service.WaitForSessionIdAsync(session.SessionId, cancellationToken),
                callerCancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // In the event of cancellation (or some other fault calling WaitForSessionIdAsync), we Dispose the
            // keep-alive session itself (which is critical for ensuring that we either stop syncing the
            // solution/project-cone over, or that we allow it to be released on the oop side), and bubble the exception
            // outwards to the caller to handle as they see fit.
            session.Dispose();
            throw;
        }

        // Succeeded in syncing the solution/project-cone over and waiting for the OOP side to pin it.  Return the
        // session to the caller so that it can let go of the pinned data on the OOP side once it no longer needs it.
        return session;

        static async Task InvokeKeepAliveAsync(
            SolutionCompilationState compilationState,
            ProjectId? projectId,
            RemoteHostClient client,
            RemoteKeepAliveSession session)
        {
            // Ensure we yield the current thread, allowing StartSessionAsync to then kick off the call to
            // WaitForSessionIdAsync
            await Task.Yield().ConfigureAwait(false);

            var sessionId = session.SessionId;
            await client.TryInvokeAsync<IRemoteKeepAliveService>(
               compilationState,
               projectId,
               (service, solutionInfo, cancellationToken) => service.KeepAliveAsync(solutionInfo, sessionId, cancellationToken),
               session.KeepAliveTokenSource.Token).ConfigureAwait(false);
        }
    }

    private RemoteKeepAliveSession(SolutionCompilationState compilationState, IAsynchronousOperationListener listener)
    {
        // This is the entry-point for KeepAliveSession when created synchronously.  In this case, we are documented as
        // just being a best-effort helper for keeping the solution/project-cone alive in OOP.  We do not guarantee that
        // when this returns that the session is fully established in OOP.  Instead, we just kick off the work to do so,
        // and return immediately.
        var token = listener.BeginAsyncOperation(nameof(RemoteKeepAliveSession));

        var task = CreateClientAndKeepAliveAsync();
        task.CompletesAsyncOperation(token);

        return;

        async Task CreateClientAndKeepAliveAsync()
        {
            var cancellationToken = this.KeepAliveTokenSource.Token;
            var client = await RemoteHostClient.TryGetClientAsync(compilationState.Services, cancellationToken).ConfigureAwait(false);
            if (client is null)
                return;

            // Now kick off the keep-alive work.  We don't wait on this as this will stick on the OOP side until the
            // cancellation token triggers.  Nor do we call WaitForSessionIdAsync either.  This is all just best-effort,
            // to be used by sync clients that can't await the full establishment of the session.
            var sessionId = this.SessionId;
            _ = client.TryInvokeAsync<IRemoteKeepAliveService>(
                compilationState,
                projectId: null,
                (service, solutionInfo, cancellationToken) => service.KeepAliveAsync(solutionInfo, sessionId, cancellationToken),
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
        this.KeepAliveTokenSource.Cancel();
        this.KeepAliveTokenSource.Dispose();
    }

    /// <summary>
    /// Creates a session between the host and OOP, effectively pinning this <paramref name="solution"/> until <see
    /// cref="IDisposable.Dispose"/> is called on it.  By pinning the solution we ensure that all calls to OOP for the
    /// same solution during the life of this session do not need to resync the solution.  Nor do they then need to
    /// rebuild any compilations they've already built due to the solution going away and then coming back.
    /// </summary>
    /// <remarks>
    /// The <paramref name="listener"/> is not strictly necessary for this type.  This class functions just as an
    /// optimization to hold onto data so it isn't resync'ed or recomputed.  However, we still want to let the system
    /// know when unobserved async work is kicked off in case we have any tooling that keep track of this for any reason
    /// (for example for tracking down problems in testing scenarios).
    /// </remarks>
    /// <remarks>
    /// This synchronous entry-point should be used only in contexts where using the async <see
    /// cref="CreateAsync(Solution, CancellationToken)"/> is not possible (for example, in a constructor). Unlike the
    /// async entry-points, this method does not guarantee that the session has been fully established on the OOP side
    /// when it returns.  Instead, it just kicks off the work in a best-effort fashion.  This does mean that it's
    /// possible for subsequent calls from the host to OOP to see different solutions on the OOP side (though this is
    /// unlikely).  For clients that require that all subsequent OOP calls see the same solution, the async entry-points
    /// must be used.
    /// </remarks>
    public static RemoteKeepAliveSession Create(Solution solution, IAsynchronousOperationListener listener)
        => new(solution.CompilationState, listener);

    /// <summary>
    /// Creates a session between the host and OOP, effectively pinning this <paramref name="solution"/> until <see
    /// cref="IDisposable.Dispose"/> is called on it.  By pinning the solution we ensure that all calls to OOP for
    /// the same solution during the life of this session do not need to resync the solution.  Nor do they then need
    /// to rebuild any compilations they've already built due to the solution going away and then coming back.
    /// </summary>
    /// <remarks>
    /// Subsequent calls to oop made while this session is alive must pass the same pinning data with the remote
    /// invocation calls.  In other words, if this session was created for a specific solution/project-cone, all
    /// subsequent calls must be for that same solution/project-cone.  If a session were created for a solution, but a
    /// later call was made for a project-cone (or vice versa), it would not see the same pinned instance of this
    /// session.
    /// </remarks>
    public static Task<RemoteKeepAliveSession> CreateAsync(Solution solution, CancellationToken cancellationToken)
        => CreateAsync(solution, projectId: null, cancellationToken);

    /// <inheritdoc cref="CreateAsync(Solution, CancellationToken)"/>
    public static Task<RemoteKeepAliveSession> CreateAsync(Solution solution, ProjectId? projectId, CancellationToken cancellationToken)
        => CreateAsync(solution.CompilationState, projectId, cancellationToken);

    /// <inheritdoc cref="CreateAsync(Solution, CancellationToken)"/>
    public static Task<RemoteKeepAliveSession> CreateAsync(SolutionCompilationState compilationState, CancellationToken cancellationToken)
        => CreateAsync(compilationState, projectId: null, cancellationToken);

    /// <inheritdoc cref="CreateAsync(Solution, CancellationToken)"/>
    public static async Task<RemoteKeepAliveSession> CreateAsync(
        SolutionCompilationState compilationState, ProjectId? projectId, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(
            compilationState.Services, cancellationToken).ConfigureAwait(false);

        return await StartSessionAsync(compilationState, projectId, client, cancellationToken).ConfigureAwait(false);
    }
}
