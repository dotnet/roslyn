// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal static class SpecializedTasks
    {
        public static readonly Task<bool> True = Task.FromResult<bool>(true);
        public static readonly Task<bool> False = Task.FromResult<bool>(false);
        public static readonly Task EmptyTask = Empty<object>.Default;

        public static Task<T> Default<T>()
        {
            return Empty<T>.Default;
        }

        public static Task<ImmutableArray<T>> EmptyImmutableArray<T>()
        {
            return Empty<T>.EmptyImmutableArray;
        }

        public static Task<IEnumerable<T>> EmptyEnumerable<T>()
        {
            return Empty<T>.EmptyEnumerable;
        }

        public static Task<T> FromResult<T>(T t) where T : class
        {
            return FromResultCache<T>.FromResult(t);
        }

        /// <summary>
        /// Returns a Task that is already canceled.
        /// </summary>
        /// <typeparam name="T">The type of value that the Task might have returned had it not been canceled.</typeparam>
        /// <param name="cancellationToken">
        /// A cancellation token that should be blamed as cancelling the task.
        /// A <see cref="Task"/> instance does not offer an API by which to obtain the cancelling token.
        /// However when awaiting the <see cref="Task"/> it throws a <see cref="TaskCanceledException"/>
        /// which <em>does</em> expose <see cref="OperationCanceledException.CancellationToken"/>.
        /// This parameter allows you to control that value.
        /// That is, if we targeted .NET 4.6 which lets us do that without throwing a first chance exception to do it.
        /// But we leave this parameter here for the future so the callers don't have to be updated when we have the
        /// option to fulfill this contract.
        /// </param>
        /// <returns>A canceled task.</returns>
        public static Task<T> FromCancellationToken<T>(CancellationToken cancellationToken)
        {
            // In .NET 4.6 we can cancel a Task with a CancellationToken without actually throwing an exception.
            // But as we're not targeting that, we can either throw an exception (and catch it) or simply
            // take the accuracy loss and return a canceled Task that doesn't know which CancellationToken
            // completed it.
            return SingletonTask<T>.CanceledTask;
        }

        /// <summary>
        /// Wraps a Task{T} that has already been canceled.
        /// </summary>
        /// <typeparam name="T">The type of value that might have been returned by the task except for its cancellation.</typeparam>
        private static class SingletonTask<T>
        {
            /// <summary>
            /// A task that is already canceled.
            /// </summary>
            internal static readonly Task<T> CanceledTask = CreateCanceledTask();

            /// <summary>
            /// Creates a canceled task.
            /// </summary>
            private static Task<T> CreateCanceledTask()
            {
                var tcs = new TaskCompletionSource<T>();
                tcs.SetCanceled();
                return tcs.Task;
            }
        }

        private static class Empty<T>
        {
            public static readonly Task<T> Default = Task.FromResult<T>(default(T));
            public static readonly Task<IEnumerable<T>> EmptyEnumerable = Task.FromResult<IEnumerable<T>>(SpecializedCollections.EmptyEnumerable<T>());
            public static readonly Task<ImmutableArray<T>> EmptyImmutableArray = Task.FromResult(ImmutableArray<T>.Empty);
        }

        private static class FromResultCache<T> where T : class
        {
            private static readonly ConditionalWeakTable<T, Task<T>> s_fromResultCache = new ConditionalWeakTable<T, Task<T>>();
            private static readonly ConditionalWeakTable<T, Task<T>>.CreateValueCallback s_taskCreationCallback = Task.FromResult<T>;

            public static Task<T> FromResult(T t)
            {
                return s_fromResultCache.GetValue(t, s_taskCreationCallback);
            }
        }
    }
}
