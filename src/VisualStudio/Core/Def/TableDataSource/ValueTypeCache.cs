// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal static class ValueTypeCache
    {
        /// <summary>
        /// Re-use already boxed object for value type.
        /// this cache never release cached object. must be used only with fixed set of value types. or
        /// something that grows very slowly like Guid for projects.
        /// </summary>
        public static object GetOrCreate<T>(T value) where T : struct
        {
            // let compiler creates a cache for each value type.
            return Cache<T>.Instance.GetOrCreate(value);
        }

        private class Cache<T> where T : struct
        {
            public static readonly Cache<T> Instance = new();

            private static readonly Func<T, object> s_boxer = v => v;

            // this will be never released, must be used only for fixed size set
            private readonly ConcurrentDictionary<T, object> _map =
                new(concurrencyLevel: 2, capacity: 5);

            public object GetOrCreate(T value)
                => _map.GetOrAdd(value, s_boxer);
        }
    }
}
