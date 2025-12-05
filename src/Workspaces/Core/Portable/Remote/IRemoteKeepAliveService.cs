// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Remote;

internal interface IRemoteKeepAliveService
{
    /// <summary>
    /// Keeps the solution identified by <paramref name="solutionChecksum"/> alive in the OOP process until <paramref
    /// name="cancellationToken"/> is triggered. This enables long-running features (like inline rename or lightbulbs)
    /// to make multiple OOP calls against the same snapshot, ensuring that computed values (like <see
    /// cref="Compilation"/>s) remain cached rather than being rebuilt on each call.
    /// </summary>
    /// <param name="solutionChecksum">Checksum identifying the solution to pin.</param>
    /// <param name="sessionId">Unique identifier for this session. The host uses this with <see
    /// cref="WaitForSessionIdAsync"/> to block until the solution is actually pinned before proceeding with
    /// dependent work.</param>
    /// <param name="cancellationToken">Cancellation of this token releases the pinned solution.</param>
    ValueTask KeepAliveAsync(Checksum solutionChecksum, long sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Blocks until the session identified by <paramref name="sessionId"/> has fully synced and pinned its solution
    /// in the OOP process. This ensures the host doesn't proceed with OOP calls until the solution is guaranteed to
    /// be available.
    /// </summary>
    ValueTask WaitForSessionIdAsync(long sessionId, CancellationToken cancellationToken);
}

internal sealed class RemoteKeepAliveSession : IDisposable
{
    private static long s_sessionId = 1;

    /// <summary>
    /// Unique identifier for this session. Used to coordinate between <see cref="IRemoteKeepAliveService.KeepAliveAsync"/>
    /// (which syncs and pins the solution) and <see cref="IRemoteKeepAliveService.WaitForSessionIdAsync"/> (which blocks
    /// until pinning completes).
    /// </summary>
    private long SessionId { get; } = Interlocked.Increment(ref s_sessionId);

    /// <summary>
    /// Controls the lifetime of the OOP-side pinning. The <see cref="IRemoteKeepAliveService.KeepAliveAsync"/> call
    /// blocks on this token; canceling it allows that call to return, releasing the pinned solution.
    /// </summary>
    private CancellationTokenSource KeepAliveTokenSource { get; } = new();

    private RemoteKeepAliveSession()
    {
    }

    /// <summary>
    /// Creates and fully establishes a keep-alive session. Returns only after the solution is confirmed to be
    /// pinned on the OOP side.
    /// </summary>
    /// <remarks>
    /// <para>This method coordinates two concurrent OOP calls:</para>
    /// <list type="number">
    /// <item><see cref="IRemoteKeepAliveService.KeepAliveAsync"/>: Syncs the solution to OOP, then blocks until
    /// <see cref="KeepAliveTokenSource"/> is canceled. This call is fire-and-forget from the host's perspective.</item>
    /// <item><see cref="IRemoteKeepAliveService.WaitForSessionIdAsync"/>: Blocks until KeepAliveAsync has completed
    /// syncing. This call is awaited, ensuring the solution is pinned before returning to the caller.</item>
    /// </list>
    /// <para>The two calls share <see cref="SessionId"/> so the OOP side can correlate them.</para>
    /// </remarks>
    private static async Task<RemoteKeepAliveSession> StartSessionAsync(
        SolutionCompilationState compilationState,
        ProjectId? projectId,
        RemoteHostClient? client,
        CancellationToken callerCancellationToken)
    {
        var session = new RemoteKeepAliveSession();

        // When running in-process (no OOP client), return immediately. The caller holds the solution snapshot
        // directly, so no pinning is needed.
        if (client is null)
            return session;

        // Fire-and-forget: Start syncing and pinning the solution on the OOP side. This call will block on the OOP
        // side until KeepAliveTokenSource is canceled (i.e., when this session is Disposed).
        //
        // Important: We pass KeepAliveTokenSource.Token (not callerCancellationToken) because:
        // - The keep-alive must persist beyond this method, for the lifetime of the session
        // - Disposing the session is what should cancel this work, not the caller's token
        _ = InvokeKeepAliveAsync(compilationState, projectId, client, session);

        // Block until the OOP side confirms the solution is pinned. This uses callerCancellationToken so the caller
        // can abandon the wait if they no longer need the session.
        await WaitForSessionIdAsync(compilationState, projectId, client, session, callerCancellationToken).ConfigureAwait(false);

        return session;

        static async Task InvokeKeepAliveAsync(
            SolutionCompilationState compilationState,
            ProjectId? projectId,
            RemoteHostClient client,
            RemoteKeepAliveSession session)
        {
            try
            {
                // Yield to allow StartSessionAsync to proceed to WaitForSessionIdAsync concurrently.
                await Task.Yield().ConfigureAwait(false);

                var sessionId = session.SessionId;
                await client.TryInvokeAsync<IRemoteKeepAliveService>(
                   compilationState,
                   projectId,
                   (service, solutionInfo, cancellationToken) => service.KeepAliveAsync(solutionInfo, sessionId, cancellationToken),
                   session.KeepAliveTokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex))
            {
                // Non-cancellation exceptions indicate a catastrophic failure (e.g., broken OOP connection).
                // We must dispose the session to:
                // 1. Cancel KeepAliveTokenSource, which is linked into WaitForSessionIdAsync's token
                // 2. Unblock WaitForSessionIdAsync so it doesn't hang forever
                //
                // Cancellation exceptions are expected and normal - they occur when the session is properly
                // disposed, which cancels KeepAliveTokenSource and allows KeepAliveAsync to return.
                session.Dispose();

                // Don't rethrow: this is fire-and-forget. Errors were already reported via FatalError above.
            }
        }

        static async Task WaitForSessionIdAsync(
            SolutionCompilationState compilationState,
            ProjectId? projectId,
            RemoteHostClient client,
            RemoteKeepAliveSession session,
            CancellationToken callerCancellationToken)
        {
            try
            {
                // Link both cancellation sources so this call aborts if either:
                // - The caller cancels (they no longer need the session)
                // - InvokeKeepAliveAsync fails and disposes the session (which cancels KeepAliveTokenSource)
                //
                // Without the link to KeepAliveTokenSource, a failure in InvokeKeepAliveAsync would leave this
                // call hanging indefinitely since the OOP side would never signal completion.
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                    session.KeepAliveTokenSource.Token,
                    callerCancellationToken);

                await client.TryInvokeAsync<IRemoteKeepAliveService>(
                    compilationState,
                    projectId,
                    (service, _, cancellationToken) => service.WaitForSessionIdAsync(session.SessionId, cancellationToken),
                    linkedTokenSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (callerCancellationToken.IsCancellationRequested)
            {
                // The linked token was canceled, but the caller's token is the one that requested cancellation.
                // Rethrow with the caller's token to maintain proper cancellation semantics (the exception's
                // CancellationToken property should match what the caller passed in).
                session.Dispose();
                callerCancellationToken.ThrowIfCancellationRequested();
            }
            catch
            {
                // Any other failure (including cancellation from KeepAliveTokenSource due to InvokeKeepAliveAsync
                // failing) means we can't establish the session. Dispose to ensure cleanup, then propagate the
                // exception so the caller knows the session wasn't established.
                session.Dispose();
                throw;
            }
        }
    }

    /// <summary>
    /// Constructor for synchronous, best-effort session creation. Does not wait for the session to be established.
    /// </summary>
    private RemoteKeepAliveSession(SolutionCompilationState compilationState, IAsynchronousOperationListener listener)
    {
        // Unlike the async entry-point, this constructor returns immediately without waiting for the solution to
        // be pinned on the OOP side. This is acceptable for scenarios where:
        // - The caller cannot await (e.g., in a constructor)
        // - Best-effort pinning is sufficient (subsequent OOP calls will still work, just potentially slower)
        //
        // Track the async work so test infrastructure can detect outstanding operations.
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

            // Fire-and-forget: Start the keep-alive without waiting for confirmation. Unlike StartSessionAsync,
            // we don't call WaitForSessionIdAsync because this is a best-effort, non-blocking path.
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

        // Cancel rather than dispose the token source. CancellationTokenSource.Dispose() is only necessary when
        // not canceling (to clean up internal wait handles), but we always cancel. The finalizer's Contract.Fail
        // will catch any cases where Dispose is forgotten.
        this.KeepAliveTokenSource.Cancel();
    }

    /// <summary>
    /// Creates a best-effort keep-alive session synchronously. Returns immediately without waiting for the session
    /// to be established on the OOP side.
    /// </summary>
    /// <remarks>
    /// <para>Use this overload only when async code is not possible (e.g., in constructors). For guaranteed session
    /// establishment, use <see cref="CreateAsync(Solution, CancellationToken)"/> instead.</para>
    /// <para>Because this method doesn't wait for establishment, subsequent OOP calls may not benefit from the
    /// pinned solution if they race ahead of the keep-alive setup. In practice this is rare, but callers requiring
    /// guaranteed consistency must use the async overloads.</para>
    /// <para>The <paramref name="listener"/> is used to track the async keep-alive work for testing infrastructure.</para>
    /// </remarks>
    public static RemoteKeepAliveSession Create(Solution solution, IAsynchronousOperationListener listener)
        => new(solution.CompilationState, listener);

    /// <summary>
    /// Creates a keep-alive session, returning only after the session is fully established on the OOP side.
    /// </summary>
    /// <remarks>
    /// All subsequent OOP calls made while this session is alive will see the same pinned solution instance,
    /// provided they pass matching solution/project-cone data. Mismatched calls (e.g., session created for full
    /// solution but call made for project-cone) will not benefit from the pinning.
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
