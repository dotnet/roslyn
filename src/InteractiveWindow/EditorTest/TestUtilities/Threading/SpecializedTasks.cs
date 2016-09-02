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
        public static readonly Task<bool> True = Task.FromResult<bool>(true);
        public static readonly Task<bool> False = Task.FromResult<bool>(false);
        public static readonly Task EmptyTask = Empty<object>.Default;

        public static Task<T> Default<T>()
        {
            return Empty<T>.Default;
        }

        public static Task<T> DefaultOrResult<T>(T value)
        {
            if (EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                return Default<T>();
            }

            return Task.FromResult(value);
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

        private static class Empty<T>
        {
            public static readonly Task<T> Default = Task.FromResult<T>(default(T));
            public static readonly Task<IEnumerable<T>> EmptyEnumerable = Task.FromResult<IEnumerable<T>>(Array.Empty<T>());
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
