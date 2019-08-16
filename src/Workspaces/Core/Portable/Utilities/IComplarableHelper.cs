// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static class IComparableHelper
    {
        public static int CompareTo<T>(T first, T second, ImmutableArray<Func<T, IComparable>> comparingComponents)
        {
            foreach (var component in comparingComponents)
            {
                var comparison = component(first).CompareTo(component(second));
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }
    }
}
