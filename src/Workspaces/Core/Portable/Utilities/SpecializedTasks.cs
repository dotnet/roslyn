// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task<T> Default<T>()
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            return Empty<T>.Default;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task<T> DefaultOrResult<T>(T value)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            if (EqualityComparer<T>.Default.Equals(value, default))
            {
                return Default<T>();
            }

            return Task.FromResult(value);
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task<IReadOnlyList<T>> EmptyReadOnlyList<T>()
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            return Empty<T>.EmptyReadOnlyList;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task<IList<T>> EmptyList<T>()
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            return Empty<T>.EmptyList;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task<ImmutableArray<T>> EmptyImmutableArray<T>()
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            return Empty<T>.EmptyImmutableArray;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task<IEnumerable<T>> EmptyEnumerable<T>()
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            return Empty<T>.EmptyEnumerable;
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public static Task<T> FromResult<T>(T t) where T : class
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            return FromResultCache<T>.FromResult(t);
        }

        private static class Empty<T>
        {
            public static readonly Task<T> Default = Task.FromResult<T>(default);
            public static readonly Task<IEnumerable<T>> EmptyEnumerable = Task.FromResult<IEnumerable<T>>(SpecializedCollections.EmptyEnumerable<T>());
            public static readonly Task<ImmutableArray<T>> EmptyImmutableArray = Task.FromResult(ImmutableArray<T>.Empty);
            public static readonly Task<IList<T>> EmptyList = Task.FromResult(SpecializedCollections.EmptyList<T>());
            public static readonly Task<IReadOnlyList<T>> EmptyReadOnlyList = Task.FromResult(SpecializedCollections.EmptyReadOnlyList<T>());
        }

        private static class FromResultCache<T> where T : class
        {
            private static readonly ConditionalWeakTable<T, Task<T>> s_fromResultCache = new ConditionalWeakTable<T, Task<T>>();
            private static readonly ConditionalWeakTable<T, Task<T>>.CreateValueCallback s_taskCreationCallback = Task.FromResult<T>;

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
            public static Task<T> FromResult(T t)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
            {
                return s_fromResultCache.GetValue(t, s_taskCreationCallback);
            }
        }
    }
}
