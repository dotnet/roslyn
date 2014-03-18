// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
            public static readonly Task<IEnumerable<T>> EmptyEnumerable = Task.FromResult<IEnumerable<T>>(SpecializedCollections.EmptyEnumerable<T>());
        }

        private static class FromResultCache<T> where T : class
        {
            private static readonly ConditionalWeakTable<T, Task<T>> fromResultCache = new ConditionalWeakTable<T, Task<T>>();
            private static readonly ConditionalWeakTable<T, Task<T>>.CreateValueCallback taskCreationCallback = Task.FromResult<T>;

            public static Task<T> FromResult(T t)
            {
                return fromResultCache.GetValue(t, taskCreationCallback);
            }
        }
    }
}
