// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities.Extensions;

namespace Analyzer.Utilities
{
    internal static class HashUtilities
    {
        internal static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + newKey);
        }

        internal static int Combine<T>(ImmutableArray<T> array, int currentKey)
        {
            var hashCode = Combine(array.Length, currentKey);
            foreach (var element in array)
            {
                hashCode = Combine(element.GetHashCode(), hashCode);
            }

            return hashCode;
        }

        internal static int Combine<T>(ImmutableStack<T> stack, int currentKey)
        {
            var hashCode = currentKey;

            var stackSize = 0;
            foreach (var element in stack)
            {
                hashCode = Combine(element.GetHashCode(), hashCode);
                stackSize++;
            }

            return Combine(stackSize, hashCode);
        }

        internal static int Combine<T>(ImmutableHashSet<T> set, int currentKey)
        {
            var hashCode = Combine(set.Count, currentKey);
            var sortedHashCodes = set.Select(element => element.GetHashCode()).Order();
            foreach (var newKey in sortedHashCodes)
            {
                hashCode = Combine(newKey, hashCode);
            }

            return hashCode;
        }

        internal static int Combine<TKey, TValue>(ImmutableDictionary<TKey, TValue> dictionary, int currentKey)
        {
            var hashCode = Combine(dictionary.Count, currentKey);
            var sortedHashCodes = dictionary
                                  .Select(kvp => Combine(kvp.Key.GetHashCode(), kvp.Value.GetHashCode()))
                                  .Order();
            foreach (var newKey in sortedHashCodes)
            {
                hashCode = Combine(newKey, hashCode);
            }

            return hashCode;
        }
    }
}