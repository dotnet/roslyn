// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.Extensions;

namespace Analyzer.Utilities
{
    internal static class HashUtilities
    {
        internal static int GetHashCodeOrDefault(this object? objectOpt) => objectOpt?.GetHashCode() ?? 0;

        internal static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + newKey);
        }

        internal static int Combine<T>(ImmutableArray<T> array) => Combine(array, 0);
        internal static int Combine<T>(ImmutableArray<T> array, int currentKey) => Combine(array, array.Length, currentKey);

        public static int Combine<T>(params T[] sequence)
            => Combine(sequence, sequence.Length, currentKey: 0);

        public static int Combine<T>(IEnumerable<T> sequence, int length, int currentKey)
        {
            var hashCode = Combine(length, currentKey);
            foreach (var element in sequence)
            {
                hashCode = Combine(element.GetHashCodeOrDefault(), hashCode);
            }

            return hashCode;
        }

        internal static int Combine<T>(ImmutableStack<T> stack) => Combine(stack, 0);
        internal static int Combine<T>(ImmutableStack<T> stack, int currentKey)
        {
            var hashCode = currentKey;

            var stackSize = 0;
            foreach (var element in stack)
            {
                hashCode = Combine(element.GetHashCodeOrDefault(), hashCode);
                stackSize++;
            }

            return Combine(stackSize, hashCode);
        }

        internal static int Combine<T>(ImmutableHashSet<T> set) => Combine(set, 0);
        internal static int Combine<T>(ImmutableHashSet<T> set, int currentKey)
            => Combine(set.Select(element => element.GetHashCodeOrDefault()).Order(),
                       set.Count,
                       currentKey);

        internal static int Combine<TKey, TValue>(ImmutableDictionary<TKey, TValue> dictionary) => Combine(dictionary, 0);
        internal static int Combine<TKey, TValue>(ImmutableDictionary<TKey, TValue> dictionary, int currentKey)
            => Combine(dictionary.Select(kvp => Combine(kvp.Key.GetHashCodeOrDefault(), kvp.Value.GetHashCodeOrDefault())).Order(),
                       dictionary.Count,
                       currentKey);
    }
}