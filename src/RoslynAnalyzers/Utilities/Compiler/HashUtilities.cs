// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

#if !NETCOREAPP
using Analyzer.Utilities.Extensions;
#endif

namespace Analyzer.Utilities
{
    internal static class HashUtilities
    {
        internal static int GetHashCodeOrDefault<T>(this T? obj)
            where T : class
            => obj?.GetHashCode() ?? 0;

        internal static int GetHashCodeOrDefault<T>(this T? obj)
            where T : struct
            => obj?.GetHashCode() ?? 0;

        internal static int Combine<T>(ImmutableArray<T> array)
        {
            var hashCode = new RoslynHashCode();
            Combine(array, ref hashCode);
            return hashCode.ToHashCode();
        }

        internal static void Combine<T>(ImmutableArray<T> array, ref RoslynHashCode hashCode)
        {
            foreach (var element in array)
            {
                hashCode.Add(element);
            }
        }

        internal static int Combine<T>(ImmutableStack<T> stack)
        {
            var hashCode = new RoslynHashCode();
            Combine(stack, ref hashCode);
            return hashCode.ToHashCode();
        }

        internal static void Combine<T>(ImmutableStack<T> stack, ref RoslynHashCode hashCode)
        {
            foreach (var element in stack)
            {
                hashCode.Add(element);
            }
        }

        internal static int Combine<T>(ImmutableHashSet<T> set)
        {
            var hashCode = new RoslynHashCode();
            Combine(set, ref hashCode);
            return hashCode.ToHashCode();
        }

        internal static void Combine<T>(ImmutableHashSet<T> set, ref RoslynHashCode hashCode)
        {
            foreach (var element in set)
            {
                hashCode.Add(element);
            }
        }

        internal static int Combine<TKey, TValue>(ImmutableDictionary<TKey, TValue> dictionary)
            where TKey : notnull
        {
            var hashCode = new RoslynHashCode();
            Combine(dictionary, ref hashCode);
            return hashCode.ToHashCode();
        }

        internal static void Combine<TKey, TValue>(ImmutableDictionary<TKey, TValue> dictionary, ref RoslynHashCode hashCode)
            where TKey : notnull
        {
            foreach (var (key, value) in dictionary)
            {
                hashCode.Add(key);
                hashCode.Add(value);
            }
        }
    }
}
