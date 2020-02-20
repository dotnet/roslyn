// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    static internal class ComparerWithState
    {
        public static int CompareTo<T, S>(T first, T second, S state, ImmutableArray<Func<T, S, IComparable?>> comparableMethods)
        {
            foreach (var comparableMethod in comparableMethods)
            {
                var comparableFirst = comparableMethod(first, state);
                var comparableSecond = comparableMethod(second, state);
                if (comparableFirst is null)
                {
                    if (comparableSecond is null)
                    {
                        continue;
                    }

                    return -1;
                }
                else if (comparableSecond is null)
                {
                    return 1;
                }

                var comparison = comparableFirst.CompareTo(comparableSecond);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        public static int CompareTo<T>(T first, T second, ImmutableArray<Func<T, IComparable?>> comparableMethods)
        {
            foreach (var comparableMethod in comparableMethods)
            {
                var comparableFirst = comparableMethod(first);
                var comparableSecond = comparableMethod(second);
                if (comparableFirst is null)
                {
                    if (comparableSecond is null)
                    {
                        continue;
                    }

                    return -1;
                }
                else if (comparableSecond is null)
                {
                    return 1;
                }

                var comparison = comparableFirst.CompareTo(comparableSecond);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }
    }
}
