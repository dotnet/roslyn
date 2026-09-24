// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common.Logging;
using Microsoft.CodeAnalysis.Razor.Logging;

using Microsoft.VisualStudio.Threading;
using Xunit;
using Xunit.Abstractions;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.AspNetCore.Razor.Test.Common;

/// <summary>
///  Base class for all test classes that provides the following support:
///
///  <list type="bullet">
///   <item>A <see cref="Microsoft.VisualStudio.Threading.JoinableTaskFactory"/> that uses the xUnit
///   test thread as the main thread.</item>
///   <item>A <see cref="CancellationToken"/> that signals when the test has finished running
///   and xUnit disposes the test class.</item>
///   <item>An <see cref="ILoggerFactory"/> implementation that writes to an xUnit
///   <see cref="ITestOutputHelper"/>.</item>
///   <item>An easy way to register <see cref="IDisposable"/> objects that should be disposed
///   when the test completes.</item>
///   <item>An easy way to register <see cref="IAsyncDisposable"/> objects that should be disposed
///   when the test completes.</item>
///   <item>An implementation of <see cref="IAsyncLifetime"/> that test classes can override
///   to provide custom initialization and disposal for tests.</item>
///  </list>
/// </summary>
public abstract partial class ToolingTestBase : IAsyncLifetime
{
    static ToolingTestBase()
    {
#if NET472
        XunitDisposeHook.Initialize();
#endif
    }

    private readonly JoinableTaskCollection _joinableTaskCollection;
    private readonly CancellationTokenSource _disposalTokenSource;
    private List<IDisposable>? _disposables;
    private List<IAsyncDisposable>? _asyncDisposables;

    /// <summary>
    ///  A common context within which joinable tasks may be created and interact to avoid
    ///  deadlocks.
    /// </summary>
    protected JoinableTaskContext JoinableTaskContext { get; }

    /// <summary>
    ///  A factory for starting asynchronous tasks that can mitigate deadlocks when the
    ///  tasks require the Main thread of an application and the Main thread may itself
    ///  be blocking on the completion of a task.
    /// </summary>
    protected JoinableTaskFactory JoinableTaskFactory { get; }

    /// <summary>
    ///  A cancellation token that will signal when the currently running test completes.
    /// </summary>
    protected CancellationToken DisposalToken { get; }

    /// <summary>
    ///  An <see cref="ILoggerFactory"/> that creates <see cref="ILogger"/> instances that
    ///  write to xUnit's <see cref="ITestOutputHelper"/> for the currently running test.
    /// </summary>
    internal ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// An <see cref="ITestOutputHelper"/> that the currently running test can use to write
    /// though using <see cref="LoggerFactory"/> is probably preferred.
    /// </summary>
    internal ITestOutputHelper TestOutputHelper { get; }

    private ILogger? _logger;

    /// <summary>
    ///  An <see cref="ILogger"/> for the currently running test.
    /// </summary>
    private protected ILogger Logger => _logger ??= LoggerFactory.GetOrCreateLogger(GetType().Name);

    protected ToolingTestBase(ITestOutputHelper testOutput)
    {
        JoinableTaskContext = new();
        _joinableTaskCollection = JoinableTaskContext.CreateCollection();
        JoinableTaskFactory = JoinableTaskContext.CreateFactory(_joinableTaskCollection);

        _disposalTokenSource = new();
        DisposalToken = _disposalTokenSource.Token;

        TestOutputHelper = testOutput;
        LoggerFactory = new TestOutputLoggerFactory(testOutput);

        // Give this thread a name, so it's easier to find in the VS Threads window.
        Thread.CurrentThread.Name ??= "Main Thread";
    }

    Task IAsyncLifetime.InitializeAsync() => InitializeAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        // First, call the protected DisposeAsync() to let test classes to run custom logic.
        await DisposeAsync();

        // Next, dispose any IAsyncDisposables that were registered by the current test.
        if (_asyncDisposables is { } asyncDisposables)
        {
            foreach (var asyncDisposable in asyncDisposables)
            {
                await asyncDisposable.DisposeAsync();
            }
        }

        // Next, dispose any IDisposables that were registered by the current test.
        if (_disposables is { } disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        // Signal cancellation
        _disposalTokenSource.Cancel();
        try
        {
            // Wait for all joinable tasks to finish.
            await _joinableTaskCollection.JoinTillEmptyAsync();
        }
        catch (OperationCanceledException)
        {
            // This exception is expected because we signaled the cancellation token.
        }
        catch (AggregateException ex)
        {
            ex.Handle(x => x is OperationCanceledException);
        }
        finally
        {
            _disposalTokenSource.Dispose();
        }

        JoinableTaskContext.Dispose();
    }

    /// <summary>
    ///  Override to provide custom initialization logic for all tests in this test class.
    /// </summary>
    protected virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    ///  Override to provide custom initialization logic for all tests in this test class.
    /// </summary>
    protected virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    ///  Register an <see cref="IDisposable"/> instance to be disposed when the test completes.
    /// </summary>
    protected void AddDisposable(IDisposable disposable)
    {
        _disposables ??= [];
        _disposables.Add(disposable);
    }

    /// <summary>
    ///  Register an <see cref="IDisposable"/> instance to be disposed when the test completes.
    /// </summary>
    protected T AddDisposable<T>(T disposable)
        where T : IDisposable
    {
        AddDisposable((IDisposable)disposable);
        return disposable;
    }

    /// <summary>
    ///  Register a set of <see cref="IDisposable"/> instances to be disposed when the test completes.
    /// </summary>
    protected void AddDisposables(IEnumerable<IDisposable> disposables)
    {
        _disposables ??= [];
        _disposables.AddRange(disposables);
    }

    /// <summary>
    ///  Register a set of <see cref="IDisposable"/> instances to be disposed when the test completes.
    /// </summary>
    protected void AddDisposables(params IDisposable[] disposables)
        => AddDisposables((IEnumerable<IDisposable>)disposables);

    /// <summary>
    ///  Register an <see cref="IAsyncDisposable"/> instance to be disposed when the test completes.
    /// </summary>
    protected void AddDisposable(IAsyncDisposable disposable)
    {
        _asyncDisposables ??= [];
        _asyncDisposables.Add(disposable);
    }

    /// <summary>
    ///  Register a set of <see cref="IAsyncDisposable"/> instances to be disposed when the test completes.
    /// </summary>
    protected void AddDisposables(IEnumerable<IAsyncDisposable> disposables)
    {
        _asyncDisposables ??= [];
        _asyncDisposables.AddRange(disposables);
    }

    /// <summary>
    ///  Register a set of <see cref="IAsyncDisposable"/> instances to be disposed when the test completes.
    /// </summary>
    protected void AddDisposables(params IAsyncDisposable[] disposables)
        => AddDisposables((IEnumerable<IAsyncDisposable>)disposables);
}
