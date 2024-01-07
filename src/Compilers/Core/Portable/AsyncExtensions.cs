// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

// PROTOTYPE this should come from the BCL
public static class TaskAwaiterExceptionExtensions
{
    // !!!WARNING!!!: These depend on the exact layout of awaiters having the stored task
    // as the first field of the struct.  That is the case in both .NET Framework and .NET Core.

    public static void GetResult(this TaskAwaiter awaiter, out Exception? exception)
    {
        Task? t = Unsafe.As<TaskAwaiter, Task?>(ref awaiter)!;
        exception = null;
        if (t is not null)
        {
            if (t.IsFaulted)
            {
                exception = t.Exception!.InnerException;
            }
            else if (t.IsCanceled)
            {
                exception = new TaskCanceledException(t);
            }
        }
    }

    public static TResult GetResult<TResult>(this TaskAwaiter<TResult> awaiter, out Exception? exception)
    {
        Task<TResult>? t = Unsafe.As<TaskAwaiter<TResult>, Task<TResult>?>(ref awaiter)!;
        exception = null;

        if (t is not null)
        {
            if (!t.IsFaulted && !t.IsCanceled)
            {
                return t.Result;
            }

            exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        }

        return default!;
    }

    public static void GetResult(this ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter, out Exception? exception)
    {
        Task? t = Unsafe.As<ConfiguredTaskAwaitable.ConfiguredTaskAwaiter, Task?>(ref awaiter)!;
        exception = null;
        if (t is not null)
        {
            if (t.IsFaulted)
            {
                exception = t.Exception!.InnerException;
            }
            else if (t.IsCanceled)
            {
                exception = new TaskCanceledException(t);
            }
        }
    }

    public static TResult GetResult<TResult>(this ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter, out Exception? exception)
    {
        Task<TResult>? t = Unsafe.As<ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter, Task<TResult>?>(ref awaiter)!;
        exception = null;

        if (t is not null)
        {
            if (!t.IsFaulted && !t.IsCanceled)
            {
                return t.Result;
            }

            exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        }

        return default!;
    }
}

public static class ValueTaskAwaiterExceptionExtensions
{
    // !!!WARNING!!!: These depend on the exact layout of awaiters having the stored task
    // as the first field of the struct.  That _should_ be the case in both .NET Framework
    // and .NET Core, but it's technically not guaranteed, as the structs have AutoLayout
    // and the runtime _could_ reorder the fields, though it doesn't have a good reason to.

    public static void GetResult(this ValueTaskAwaiter awaiter, out Exception? exception)
    {
        exception = null;

        if (Unsafe.As<ValueTaskAwaiter, object?>(ref awaiter) is not Task t)
        {
            awaiter.GetResult();
            return;
        }

        if (t.IsFaulted | t.IsCanceled)
        {
            exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        }
    }

    public static TResult GetResult<TResult>(this ValueTaskAwaiter<TResult> awaiter, out Exception? exception)
    {
        exception = null;

        if (Unsafe.As<ValueTaskAwaiter<TResult>, object?>(ref awaiter) is not Task<TResult> t)
        {
            return awaiter.GetResult();
        }

        if (!(t.IsFaulted | t.IsCanceled))
        {
            return t.Result;
        }

        exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        return default!;
    }

    public static void GetResult(this ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter, out Exception? exception)
    {
        exception = null;

        if (Unsafe.As<ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter, object?>(ref awaiter) is not Task t)
        {
            awaiter.GetResult();
            return;
        }

        if (t.IsFaulted | t.IsCanceled)
        {
            exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        }
    }

    public static TResult GetResult<TResult>(this ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter awaiter, out Exception? exception)
    {
        exception = null;

        if (Unsafe.As<ConfiguredValueTaskAwaitable<TResult>.ConfiguredValueTaskAwaiter, object?>(ref awaiter) is not Task<TResult> t)
        {
            return awaiter.GetResult();
        }

        if (!(t.IsFaulted | t.IsCanceled))
        {
            return t.Result;
        }

        exception = t.Exception?.InnerException ?? new TaskCanceledException(t);
        return default!;
    }
}
