// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    static internal class ComparerWithState
    {
        public static int CompareTo<T, S>(T first, T second, S state, ImmutableArray<Func<T, S, IComparable>> comparableMethods)
        {
            foreach (var comparableMethod in comparableMethods)
            {
                var comparison = comparableMethod(first, state).CompareTo(comparableMethod(second, state));
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }

        public static int CompareTo<T>(T first, T second, ImmutableArray<Func<T, IComparable>> comparableMethods)
        {
            foreach (var comparableMethod in comparableMethods)
            {
                var comparison = comparableMethod(first).CompareTo(comparableMethod(second));
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }
    }
}
