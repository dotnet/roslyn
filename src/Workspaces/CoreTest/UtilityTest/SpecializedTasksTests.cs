// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit;

#pragma warning disable IDE0039 // Use local function

namespace Microsoft.CodeAnalysis.UnitTests
{
    [SuppressMessage("Usage", "VSTHRD104:Offer async methods", Justification = "This class tests specific behavior of tasks.")]
    public class SpecializedTasksTests
    {
        private record StateType;
        private record IntermediateType;
        private record ResultType;

        [Fact]
        public void WhenAll_Null()
        {
#pragma warning disable CA2012 // Use ValueTasks correctly (the instance is never created)
            Assert.Throws<ArgumentNullException>(() => SpecializedTasks.WhenAll<int>((IEnumerable<ValueTask<int>>)null!));
#pragma warning restore CA2012 // Use ValueTasks correctly
        }

        [Fact]
        public void WhenAll_Empty()
        {
            var whenAll = SpecializedTasks.WhenAll(SpecializedCollections.EmptyEnumerable<ValueTask<int>>());
            Debug.Assert(whenAll.IsCompleted);
            Assert.True(whenAll.IsCompletedSuccessfully);
            Assert.Same(Array.Empty<int>(), whenAll.Result);
        }

        [Fact]
        public void WhenAll_AllCompletedSuccessfully()
        {
            var whenAll = SpecializedTasks.WhenAll([new ValueTask<int>(0), new ValueTask<int>(1)]);
            Debug.Assert(whenAll.IsCompleted);
            Assert.True(whenAll.IsCompletedSuccessfully);
            Assert.Equal((int[])[0, 1], whenAll.Result);
        }

        [Fact]
        public async Task WhenAll_CompletedButCanceled()
        {
            var whenAll = SpecializedTasks.WhenAll([new ValueTask<int>(Task.FromCanceled<int>(new CancellationToken(true)))]);
            Assert.True(whenAll.IsCompleted);
            Assert.False(whenAll.IsCompletedSuccessfully);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await whenAll);
        }

        [Fact]
        public void WhenAll_NotYetCompleted()
        {
            var completionSource = new TaskCompletionSource<int>();
            var whenAll = SpecializedTasks.WhenAll([new ValueTask<int>(completionSource.Task)]);
            Assert.False(whenAll.IsCompleted);
            completionSource.SetResult(0);
            Assert.True(whenAll.IsCompleted);
            Debug.Assert(whenAll.IsCompleted);
            Assert.Equal((int[])[0], whenAll.Result);
        }

        [Fact]
        public void Transform_ArgumentValidation()
        {
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => new(new IntermediateType());
            Func<IntermediateType, StateType, ResultType> transform = (_, _) => new();
            var arg = new StateType();
            var cancellationToken = new CancellationToken(canceled: false);

#pragma warning disable CA2012 // Use ValueTasks correctly (the instance is never created)
            Assert.Throws<ArgumentNullException>("func", () => SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(null!, transform, arg, cancellationToken));
            Assert.Throws<ArgumentNullException>("transform", () => SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync<StateType, IntermediateType, ResultType>(func, null!, arg, cancellationToken));
#pragma warning restore CA2012 // Use ValueTasks correctly
        }

        [Fact]
        public void Transform_SyncCompletedFunction_CompletedTransform()
        {
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => new(new IntermediateType());
            Func<IntermediateType, StateType, ResultType> transform = (_, _) => new();
            var arg = new StateType();
            var cancellationToken = new CancellationToken(canceled: false);

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.True(task.IsCompletedSuccessfully);
            Assert.NotNull(task.Result);
        }

        [Fact]
        public void Transform_SyncCompletedFunction_CancellationRequested_IgnoresTransform()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var executedTransform = false;

            var cancellationToken = cts.Token;
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => new(new IntermediateType());
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.True(task.IsCanceled);
            var exception = Assert.Throws<TaskCanceledException>(() => task.Result);
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public async Task Transform_AsyncCompletedFunction_CompletedTransform()
        {
            var gate = new ManualResetEventSlim();
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = async (_, _) =>
            {
                await Task.Yield();
                gate.Wait(CancellationToken.None);
                return new IntermediateType();
            };
            Func<IntermediateType, StateType, ResultType> transform = (_, _) => new();
            var arg = new StateType();
            var cancellationToken = new CancellationToken(canceled: false);

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.False(task.IsCompleted);

            gate.Set();
            Assert.NotNull(await task);
        }

        [Fact]
        public async Task Transform_AsyncCompletedFunction_CancellationRequested_IgnoresTransform()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var executedTransform = false;

            var cancellationToken = cts.Token;
            var gate = new ManualResetEventSlim();
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = async (_, _) =>
            {
                await Task.Yield();
                gate.Wait(CancellationToken.None);
                return new IntermediateType();
            };
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.False(task.IsCompleted);

            gate.Set();
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public void Transform_SyncCanceledFunction_IgnoresTransform()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var executedTransform = false;

            var cancellationToken = cts.Token;
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => new(Task.FromCanceled<IntermediateType>(cancellationToken));
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.True(task.IsCanceled);
            var exception = Assert.Throws<TaskCanceledException>(() => task.Result);
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public async Task Transform_AsyncCanceledFunction_IgnoresTransform()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var executedTransform = false;

            var cancellationToken = cts.Token;
            var gate = new ManualResetEventSlim();
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = async (_, _) =>
            {
                await Task.Yield();
                gate.Wait(CancellationToken.None);
                cts.Token.ThrowIfCancellationRequested();
                throw ExceptionUtilities.Unreachable();
            };
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.False(task.IsCompleted);

            gate.Set();
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public void Transform_SyncCanceledFunction_NotRequested_IgnoresTransform()
        {
            using var unexpectedCts = new CancellationTokenSource();
            unexpectedCts.Cancel();

            var executedTransform = false;

            var cancellationToken = new CancellationToken(canceled: false);
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => new(Task.FromCanceled<IntermediateType>(unexpectedCts.Token));
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.True(task.IsCanceled);
            var exception = Assert.Throws<TaskCanceledException>(() => task.Result);

            // ⚠ Due to the way cancellation is handled in ContinueWith, the resulting exception fails to preserve the
            // cancellation token applied when the intermediate task was cancelled.
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public async Task Transform_AsyncCanceledFunction_NotRequested_IgnoresTransform()
        {
            using var unexpectedCts = new CancellationTokenSource();
            unexpectedCts.Cancel();

            var executedTransform = false;

            var cancellationToken = new CancellationToken(canceled: false);
            var gate = new ManualResetEventSlim();
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = async (_, _) =>
            {
                await Task.Yield();
                gate.Wait(CancellationToken.None);
                unexpectedCts.Token.ThrowIfCancellationRequested();
                throw ExceptionUtilities.Unreachable();
            };
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.False(task.IsCompleted);

            gate.Set();
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
            Assert.True(task.IsCanceled);

            // ⚠ Due to the way cancellation is handled in ContinueWith, the resulting exception fails to preserve the
            // cancellation token applied when the intermediate task was cancelled.
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public void Transform_SyncCanceledFunction_MismatchToken_IgnoresTransform()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var unexpectedCts = new CancellationTokenSource();
            unexpectedCts.Cancel();

            var executedTransform = false;

            var cancellationToken = cts.Token;
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => new(Task.FromCanceled<IntermediateType>(unexpectedCts.Token));
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.True(task.IsCanceled);
            var exception = Assert.Throws<TaskCanceledException>(() => task.Result);
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public async Task Transform_AsyncCanceledFunction_MismatchToken_IgnoresTransform()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var unexpectedCts = new CancellationTokenSource();
            unexpectedCts.Cancel();

            var executedTransform = false;

            var cancellationToken = cts.Token;
            var gate = new ManualResetEventSlim();
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = async (_, _) =>
            {
                await Task.Yield();
                gate.Wait(CancellationToken.None);
                unexpectedCts.Token.ThrowIfCancellationRequested();
                throw ExceptionUtilities.Unreachable();
            };
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.False(task.IsCompleted);

            gate.Set();
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
            Assert.True(task.IsCanceled);
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public void Transform_SyncDirectFaultedFunction_IgnoresTransform()
        {
            var executedTransform = false;

            var fault = ExceptionUtilities.Unreachable();
            var cancellationToken = new CancellationToken(canceled: false);
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => throw fault;
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

#pragma warning disable CA2012 // Use ValueTasks correctly (the instance is never created)
            var exception = Assert.Throws<InvalidOperationException>(() => SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken));
#pragma warning restore CA2012 // Use ValueTasks correctly
            Assert.Same(fault, exception);
            Assert.False(executedTransform);
        }

        [Fact]
        public void Transform_SyncFaultedFunction_IgnoresTransform()
        {
            var executedTransform = false;

            var fault = ExceptionUtilities.Unreachable();
            var cancellationToken = new CancellationToken(canceled: false);
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => new(Task.FromException<IntermediateType>(fault));
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.True(task.IsFaulted);
            var exception = Assert.Throws<InvalidOperationException>(() => task.Result);
            Assert.Same(fault, exception);
            Assert.False(executedTransform);
        }

        [Fact]
        public async Task Transform_AsyncFaultedFunction_IgnoresTransform()
        {
            var executedTransform = false;

            var fault = ExceptionUtilities.Unreachable();
            var cancellationToken = new CancellationToken(canceled: false);
            var gate = new ManualResetEventSlim();
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = async (_, _) =>
            {
                await Task.Yield();
                gate.Wait(CancellationToken.None);
                throw fault;
            };
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.False(task.IsCompleted);

            gate.Set();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => task.AsTask());
            Assert.Same(fault, exception);
            Assert.False(executedTransform);
        }

        [Fact]
        public void Transform_SyncDirectFaultedFunction_CancellationRequested_IgnoresTransform()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var executedTransform = false;

            var fault = ExceptionUtilities.Unreachable();
            var cancellationToken = cts.Token;
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => throw fault;
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

#pragma warning disable CA2012 // Use ValueTasks correctly (the instance is never created)
            var exception = Assert.Throws<InvalidOperationException>(() => SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken));
#pragma warning restore CA2012 // Use ValueTasks correctly
            Assert.Same(fault, exception);
            Assert.False(executedTransform);
        }

        [Fact]
        public void Transform_SyncFaultedFunction_CancellationRequested_IgnoresTransform()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var executedTransform = false;

            var fault = ExceptionUtilities.Unreachable();
            var cancellationToken = cts.Token;
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => new(Task.FromException<IntermediateType>(fault));
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.True(task.IsCanceled);
            var exception = Assert.Throws<TaskCanceledException>(() => task.Result);
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public async Task Transform_AsyncFaultedFunction_CancellationRequested_IgnoresTransform()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var executedTransform = false;

            var fault = ExceptionUtilities.Unreachable();
            var cancellationToken = cts.Token;
            var gate = new ManualResetEventSlim();
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = async (_, _) =>
            {
                await Task.Yield();
                gate.Wait(CancellationToken.None);
                throw fault;
            };
            Func<IntermediateType, StateType, ResultType> transform = (_, _) =>
            {
                executedTransform = true;
                return new ResultType();
            };
            var arg = new StateType();

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.False(task.IsCompleted);

            gate.Set();
            var exception = await Assert.ThrowsAsync<TaskCanceledException>(() => task.AsTask());
            Assert.True(task.IsCanceled);
            Assert.Equal(cancellationToken, exception.CancellationToken);
            Assert.False(executedTransform);
        }

        [Fact]
        public void Transform_SyncCompletedFunction_FaultedTransform()
        {
            var fault = ExceptionUtilities.Unreachable();
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = (_, _) => new(new IntermediateType());
            Func<IntermediateType, StateType, ResultType> transform = (_, _) => throw fault;
            var arg = new StateType();
            var cancellationToken = new CancellationToken(canceled: false);

#pragma warning disable CA2012 // Use ValueTasks correctly (the instance is never created)
            var exception = Assert.Throws<InvalidOperationException>(() => SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken));
#pragma warning restore CA2012 // Use ValueTasks correctly
            Assert.Same(fault, exception);
        }

        [Fact]
        public async Task Transform_AsyncCompletedFunction_FaultedTransform()
        {
            var fault = ExceptionUtilities.Unreachable();
            var gate = new ManualResetEventSlim();
            Func<StateType, CancellationToken, ValueTask<IntermediateType>> func = async (_, _) =>
            {
                await Task.Yield();
                gate.Wait(CancellationToken.None);
                return new IntermediateType();
            };
            Func<IntermediateType, StateType, ResultType> transform = (_, _) => throw fault;
            var arg = new StateType();
            var cancellationToken = new CancellationToken(canceled: false);

            var task = SpecializedTasks.TransformWithoutIntermediateCancellationExceptionAsync(func, transform, arg, cancellationToken).Preserve();
            Assert.False(task.IsCompleted);

            gate.Set();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => task.AsTask());
            Assert.Same(fault, exception);
        }
    }
}
