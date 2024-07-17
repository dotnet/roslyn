// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

internal static class SpecializedTasks
{
    public static readonly Task<bool> True = Task.FromResult(true);
    public static readonly Task<bool> False = Task.FromResult(false);

    // This is being consumed through InternalsVisibleTo by Source-Based test discovery
    [Obsolete("Use Task.CompletedTask instead which is available in the framework.")]
    public static readonly Task EmptyTask = Task.CompletedTask;

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    public static Task<T?> AsNullable<T>(this Task<T> task) where T : class
        => task!;

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    public static Task<T?> Default<T>()
        => EmptyTasks<T>.Default;

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    public static Task<T?> Null<T>() where T : class
        => Default<T>();

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    public static Task<IReadOnlyList<T>> EmptyReadOnlyList<T>()
        => EmptyTasks<T>.EmptyReadOnlyList;

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    public static Task<IList<T>> EmptyList<T>()
        => EmptyTasks<T>.EmptyList;

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    public static Task<ImmutableArray<T>> EmptyImmutableArray<T>()
        => EmptyTasks<T>.EmptyImmutableArray;

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
    public static Task<IEnumerable<T>> EmptyEnumerable<T>()
        => EmptyTasks<T>.EmptyEnumerable;

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Naming is modeled after Task.WhenAll.")]
    public static ValueTask<T[]> WhenAll<T>(IEnumerable<ValueTask<T>> tasks)
    {
        var taskArray = tasks.AsArray();
        if (taskArray.Length == 0)
            return ValueTaskFactory.FromResult(Array.Empty<T>());

        var allCompletedSuccessfully = true;
        for (var i = 0; i < taskArray.Length; i++)
        {
            if (!taskArray[i].IsCompletedSuccessfully)
            {
                allCompletedSuccessfully = false;
                break;
            }
        }

        if (allCompletedSuccessfully)
        {
            var result = new T[taskArray.Length];
            for (var i = 0; i < taskArray.Length; i++)
            {
                result[i] = taskArray[i].Result;
            }

            return ValueTaskFactory.FromResult(result);
        }
        else
        {
            return new ValueTask<T[]>(Task.WhenAll(taskArray.Select(task => task.AsTask())));
        }
    }

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Naming is modeled after Task.WhenAll.")]
    public static async ValueTask<ImmutableArray<TResult>> WhenAll<TResult>(this IReadOnlyCollection<Task<TResult>> tasks)
    {
        // Explicit cast to IEnumerable<Task> so we call the overload that doesn't allocate an array as the result.
        await Task.WhenAll((IEnumerable<Task>)tasks).ConfigureAwait(false);
        var result = new FixedSizeArrayBuilder<TResult>(tasks.Count);
        foreach (var task in tasks)
            result.Add(await task.ConfigureAwait(false));

        return result.MoveToImmutable();
    }

    /// <summary>
    /// This helper method provides semantics equivalent to the following, but avoids throwing an intermediate
    /// <see cref="OperationCanceledException"/> in the case where the asynchronous operation is cancelled.
    ///
    /// <code><![CDATA[
    /// public ValueTask<TResult> MethodAsync(TArg arg, CancellationToken cancellationToken)
    /// {
    ///   var intermediate = await func(arg, cancellationToken).ConfigureAwait(false);
    ///   return transform(intermediate);
    /// }
    /// ]]></code>
    /// </summary>
    /// <remarks>
    /// This helper method is only intended for use in cases where profiling reveals substantial overhead related to
    /// cancellation processing.
    /// </remarks>
    /// <typeparam name="TArg">The type of a state variable to pass to <paramref name="func"/> and <paramref name="transform"/>.</typeparam>
    /// <typeparam name="TIntermediate">The type of intermediate result produced by <paramref name="func"/>.</typeparam>
    /// <typeparam name="TResult">The type of result produced by <paramref name="transform"/>.</typeparam>
    /// <param name="func">The intermediate asynchronous operation.</param>
    /// <param name="transform">The synchronous transformation to apply to the result of <paramref name="func"/>.</param>
    /// <param name="arg">The state to pass to <paramref name="func"/> and <paramref name="transform"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> that the operation will observe.</param>
    /// <returns></returns>
    public static ValueTask<TResult> TransformWithoutIntermediateCancellationExceptionAsync<TArg, TIntermediate, TResult>(
        TArg arg,
        Func<TArg, CancellationToken, ValueTask<TIntermediate>> func,
        Func<TIntermediate, TArg, TResult> transform,
        CancellationToken cancellationToken)
    {
        if (func is null)
            throw new ArgumentNullException(nameof(func));
        if (transform is null)
            throw new ArgumentNullException(nameof(transform));

        var intermediateResult = func(arg, cancellationToken);
        if (intermediateResult.IsCompletedSuccessfully)
        {
            // Synchronous fast path if 'func' completes synchronously
            var result = intermediateResult.Result;
            if (cancellationToken.IsCancellationRequested)
                return new ValueTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));

            return new ValueTask<TResult>(transform(result, arg));
        }
        else if (intermediateResult.IsCanceled && cancellationToken.IsCancellationRequested)
        {
            // Synchronous fast path if 'func' cancels synchronously
            return new ValueTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));
        }
        else
        {
            // Asynchronous fallback path
            return UnwrapAndTransformAsync(intermediateResult, transform, arg, cancellationToken);
        }

        static ValueTask<TResult> UnwrapAndTransformAsync(ValueTask<TIntermediate> intermediateResult, Func<TIntermediate, TArg, TResult> transform, TArg arg, CancellationToken cancellationToken)
        {
            // Apply the transformation function once a result is available. The behavior depends on the final
            // status of 'intermediateResult' and the 'cancellationToken'.
            //
            // | 'intermediateResult'       | 'cancellationToken' | Behavior                                 |
            // | -------------------------- | ------------------- | ---------------------------------------- |
            // | Ran to completion          | Not cancelled       | Apply transform                          |
            // | Ran to completion          | Cancelled           | Cancel result without applying transform |
            // | Cancelled (matching token) | Cancelled           | Cancel result without applying transform |
            // | Cancelled (mismatch token) | Not cancelled       | Cancel result without applying transform |
            // | Cancelled (mismatch token) | Cancelled           | Cancel result without applying transform |
            // | Direct fault¹              | Not cancelled       | Directly fault (exception is not caught) |
            // | Direct fault¹              | Cancelled           | Directly fault (exception is not caught) |
            // | Indirect fault             | Not cancelled       | Fault result without applying transform  |
            // | Indirect fault             | Cancelled           | Cancel result without applying transform |
            //
            // ¹ Direct faults are exceptions thrown from 'func' prior to returning a ValueTask<TIntermediate>
            //   instances. Indirect faults are exceptions captured by return an instance of
            //   ValueTask<TIntermediate> which (immediately or eventually) transitions to the faulted state. The
            //   direct fault behavior is currently handled without calling UnwrapAndTransformAsync.
            return new ValueTask<TResult>(intermediateResult.AsTask().ContinueWith(
                task => transform(task.GetAwaiter().GetResult(), arg),
                cancellationToken,
                TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default));
        }
    }

    private static class EmptyTasks<T>
    {
        public static readonly Task<T?> Default = Task.FromResult<T?>(default);
        public static readonly Task<IEnumerable<T>> EmptyEnumerable = Task.FromResult<IEnumerable<T>>(SpecializedCollections.EmptyEnumerable<T>());
        public static readonly Task<ImmutableArray<T>> EmptyImmutableArray = Task.FromResult(ImmutableArray<T>.Empty);
        public static readonly Task<IList<T>> EmptyList = Task.FromResult(SpecializedCollections.EmptyList<T>());
        public static readonly Task<IReadOnlyList<T>> EmptyReadOnlyList = Task.FromResult(SpecializedCollections.EmptyReadOnlyList<T>());
    }
}
