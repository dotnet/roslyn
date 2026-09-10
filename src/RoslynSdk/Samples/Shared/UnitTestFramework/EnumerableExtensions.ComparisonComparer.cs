// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Roslyn.UnitTestFramework
{
    internal static partial class EnumerableExtensions
    {
        private class ComparisonComparer<T> : Comparer<T>
        {
            private readonly Comparison<T> _compare;

            public ComparisonComparer(Comparison<T> compare)
            {
                _compare = compare;
            }

            public override int Compare(T x, T y)
            {
                return _compare(x, y);
            }
        }
    }
}
