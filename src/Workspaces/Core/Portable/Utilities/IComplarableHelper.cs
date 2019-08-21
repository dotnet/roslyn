// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal static class IComparableHelper
    {
        public static int CompareTo<T>(T item1, T item2, Func<T, IEnumerable<IComparable>> comparingComponentsMethod)
        {
            var enumerator1 = comparingComponentsMethod(item1).GetEnumerator();
            var enumerator2 = comparingComponentsMethod(item2).GetEnumerator();

            // We cannot guarantee that both enumerators will complete with the same number of steps.
            // Iterate while the shorter is available. 
            // But we do not make a decision if the shorter enumerator win or lose the comparison. 
            // There is a tie in case if one of enumerators ended and all component comparisons tied.
            while (enumerator1.MoveNext() && enumerator2.MoveNext())
            {
                var component1 = enumerator1.Current;
                var component2 = enumerator2.Current;

                var comparison = component1.CompareTo(component2);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return 0;
        }
    }
}
