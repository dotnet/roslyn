﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
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

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
        public static Task<T> FromResult<T>(T t) where T : class
            => FromResultCache<T>.FromResult(t);

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

        private static class EmptyTasks<T>
        {
            public static readonly Task<T?> Default = Task.FromResult<T?>(default);
            public static readonly Task<IEnumerable<T>> EmptyEnumerable = Task.FromResult<IEnumerable<T>>(SpecializedCollections.EmptyEnumerable<T>());
            public static readonly Task<ImmutableArray<T>> EmptyImmutableArray = Task.FromResult(ImmutableArray<T>.Empty);
            public static readonly Task<IList<T>> EmptyList = Task.FromResult(SpecializedCollections.EmptyList<T>());
            public static readonly Task<IReadOnlyList<T>> EmptyReadOnlyList = Task.FromResult(SpecializedCollections.EmptyReadOnlyList<T>());
        }

        private static class FromResultCache<T> where T : class
        {
            private static readonly ConditionalWeakTable<T, Task<T>> s_fromResultCache = new();
            private static readonly ConditionalWeakTable<T, Task<T>>.CreateValueCallback s_taskCreationCallback = Task.FromResult<T>;

            [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "This is a Task wrapper, not an asynchronous method.")]
            public static Task<T> FromResult(T t)
                => s_fromResultCache.GetValue(t, s_taskCreationCallback);
        }
    }
}
