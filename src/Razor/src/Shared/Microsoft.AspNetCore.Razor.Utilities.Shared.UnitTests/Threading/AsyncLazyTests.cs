// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Threading;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.Threading;

// NOTE: This code is copied and modified from dotnet/roslyn:
// https://github.com/dotnet/roslyn/blob/1715a86114c4f8b6ea2d68db00dc2502da8237d6/src/Workspaces/CoreTest/UtilityTest/AsyncLazyTests.cs#L17

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method

public class AsyncLazyTests
{
    [Fact]
    public void GetValueAsyncReturnsCompletedTaskIfAsyncComputationCompletesImmediately()
    {
        // Note, this test may pass even if GetValueAsync posted a task to the thread pool, since the 
        // current thread may context switch out and allow the thread pool to complete the task before
        // we check the state.  However, a failure here definitely indicates a bug in AsyncLazy.
        var lazy = AsyncLazy.Create(static c => Task.FromResult(5));
        var t = lazy.GetValueAsync(CancellationToken.None);
        Assert.Equal(TaskStatus.RanToCompletion, t.Status);
        Assert.Equal(5, t.VerifyCompleted());
    }

    [Fact]
    public void GetValueAsyncThrowsCorrectExceptionDuringCancellation()
    {
        // NOTE: since GetValueAsync will inline the call to the async computation, the GetValueAsync call will throw
        // immediately instead of returning a task that transitions to the cancelled state
        // A call to GetValueAsync with a token that is cancelled should throw an OperationCancelledException, but it's
        // important to make sure the correct token is cancelled. It should be cancelled with the token passed
        // to GetValue, not the cancellation that was thrown by the computation function

        using var computeFunctionRunning = new ManualResetEvent(initialState: false);

        var lazy = AsyncLazy.Create<object, ManualResetEvent>(
            static (computeFunctionRunning, c) =>
            {
                computeFunctionRunning.Set();
                while (true)
                {
                    c.ThrowIfCancellationRequested();
                }
            },
            arg: computeFunctionRunning);

        using var cancellationTokenSource = new CancellationTokenSource();

        // Create a task that will cancel the request once it's started
        Task.Run(() =>
        {
            computeFunctionRunning.WaitOne();
            cancellationTokenSource.Cancel();
        });

        try
        {
            lazy.GetValueAsync(cancellationTokenSource.Token);
            Assert.Fail($"{nameof(AsyncLazy<>.GetValueAsync)} did not throw an exception.");
        }
        catch (OperationCanceledException oce)
        {
            Assert.Equal(cancellationTokenSource.Token, oce.CancellationToken);
        }
    }

    [Fact]
    public void GetValueAsyncThatIsCancelledReturnsTaskCancelledWithCorrectToken()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        var lazy = AsyncLazy.Create(static (cancellationTokenSource, c) => Task.Run((Func<object>)(() =>
        {
            cancellationTokenSource.Cancel();
            while (true)
            {
                c.ThrowIfCancellationRequested();
            }
        }), c), arg: cancellationTokenSource);

        var task = lazy.GetValueAsync(cancellationTokenSource.Token);

        // Now wait until the task completes
        try
        {
            task.Wait();
            Assert.Fail($"{nameof(AsyncLazy<>.GetValueAsync)} did not throw an exception.");
        }
        catch (AggregateException ex)
        {
            var operationCancelledException = (OperationCanceledException)ex.Flatten().InnerException!;
            Assert.Equal(cancellationTokenSource.Token, operationCancelledException.CancellationToken);
        }
    }

    [Fact]
    public async Task AwaitingProducesCorrectException()
    {
        var exception = new ArgumentException();

        var lazy = AsyncLazy.Create<object>(async c =>
        {
            await Task.Yield();
            throw exception;
        });

        var actual = await Assert.ThrowsAsync<ArgumentException>(async () => await lazy.GetValueAsync(CancellationToken.None));

        Assert.Same(exception, actual);
    }
}
