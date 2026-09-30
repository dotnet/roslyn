// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Test.Utilities;

/// <summary>
/// The synchronization context <see cref="UseExportProviderAttribute"/> installs for the duration of a test.
/// </summary>
/// <remarks>
/// <para>Without an ambient synchronization context, an <c>await</c> in a test body resumes inline on whichever
/// thread completed the awaited task. When that thread is a Roslyn worker draining an asynchronous operation
/// tracked by <see cref="Shared.TestHooks.IAsynchronousOperationListener"/>, the remainder of the test — and the
/// test framework's per-test cleanup — runs on top of that operation's stack frames. Cleanup then blocks waiting
/// for the very operation it is nested inside, deadlocking the test process.</para>
///
/// <para>Posting continuations that capture this context elsewhere keeps them off the worker completing the
/// awaited operation. When an outer context exists (for example the dispatcher context of a WPF test),
/// continuations are forwarded to it so its thread affinity is preserved; otherwise they are queued to the
/// thread pool. Awaits using <c>ConfigureAwait(false)</c> do not capture this context.</para>
/// </remarks>
internal sealed class TestSynchronizationContext : SynchronizationContext
{
    private readonly SynchronizationContext? _innerContext;

    public TestSynchronizationContext(SynchronizationContext? innerContext)
        => _innerContext = innerContext;

    /// <summary>
    /// The context that was current when this instance was installed, or <see langword="null"/> if there was none.
    /// </summary>
    public SynchronizationContext? InnerContext => _innerContext;

    public override void Post(SendOrPostCallback d, object? state)
    {
        if (_innerContext is not null)
        {
#pragma warning disable VSTHRD001 // Forward to the captured context to preserve its scheduling behavior.
            _innerContext.Post(d, state);
#pragma warning restore VSTHRD001 // Forward to the captured context to preserve its scheduling behavior.
            return;
        }

        ThreadPool.QueueUserWorkItem(
            static s =>
            {
                var (context, callback, callbackState) = ((TestSynchronizationContext, SendOrPostCallback, object?))s!;
                context.Send(callback, callbackState);
            },
            (this, d, state));
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (_innerContext is not null)
        {
#pragma warning disable VSTHRD001 // Forward to the captured context to preserve its scheduling behavior.
            _innerContext.Send(d, state);
#pragma warning restore VSTHRD001 // Forward to the captured context to preserve its scheduling behavior.
            return;
        }

        var previousContext = Current;
        SetSynchronizationContext(this);
        try
        {
            d(state);
        }
        finally
        {
            SetSynchronizationContext(previousContext);
        }
    }

    public override SynchronizationContext CreateCopy()
        => this;
}
