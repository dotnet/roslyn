// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

/// <summary>
/// Implements <see cref="IThreadingContext"/>, which provides an implementation of
/// <see cref="VisualStudio.Threading.JoinableTaskFactory"/> to Roslyn code.
/// </summary>
/// <remarks>
/// <para>The <see cref="VisualStudio.Threading.JoinableTaskFactory"/> is constructed from the
/// <see cref="VisualStudio.Threading.JoinableTaskContext"/> provided by the MEF container, if available. If no
/// <see cref="VisualStudio.Threading.JoinableTaskContext"/> is available, a new instance is constructed using the
/// synchronization context of the current thread as the main thread.</para>
/// </remarks>
[Export(typeof(IThreadingContext))]
[Shared]
internal sealed class ThreadingContext : IThreadingContext, IDisposable
{
    private readonly CancellationTokenSource _disposalTokenSource = new();

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ThreadingContext(JoinableTaskContext joinableTaskContext)
    {
        HasMainThread = joinableTaskContext.MainThread.IsAlive;
        JoinableTaskContext = joinableTaskContext;
        JoinableTaskFactory = joinableTaskContext.Factory;
        ShutdownBlockingTasks = new JoinableTaskCollection(JoinableTaskContext);
        ShutdownBlockingTaskFactory = JoinableTaskContext.CreateFactory(ShutdownBlockingTasks);
    }

    /// <inheritdoc/>
    public bool HasMainThread
    {
        get;
    }

    /// <inheritdoc/>
    public JoinableTaskContext JoinableTaskContext
    {
        get;
    }

    /// <inheritdoc/>
    public JoinableTaskFactory JoinableTaskFactory
    {
        get;
    }

    public JoinableTaskCollection ShutdownBlockingTasks { get; }

    private JoinableTaskFactory ShutdownBlockingTaskFactory { get; }

    public CancellationToken DisposalToken => _disposalTokenSource.Token;

    public JoinableTask RunWithShutdownBlockAsync(Func<CancellationToken, Task> func)
    {
        return ShutdownBlockingTaskFactory.RunAsync(() =>
        {
            DisposalToken.ThrowIfCancellationRequested();
            return func(DisposalToken);
        });
    }

    public void Dispose()
    {
        // https://github.com/Microsoft/vs-threading/blob/main/doc/cookbook_vs.md#how-to-write-a-fire-and-forget-method-responsibly
        _disposalTokenSource.Cancel();

        try
        {
            // Block Dispose until all async work has completed.
            JoinableTaskContext.Factory.Run(ShutdownBlockingTasks.JoinTillEmptyAsync);
        }
        catch (OperationCanceledException)
        {
            // this exception is expected because we signaled the cancellation token
        }
        catch (AggregateException ex)
        {
            // ignore AggregateException containing only OperationCanceledException
            ex.Handle(inner => inner is OperationCanceledException);
        }
    }
}
