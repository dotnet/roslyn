// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Collections.Generic;

namespace Roslyn.UnitTestFramework
{
    internal static partial class EnumerableExtensions
    {
        private class ComparisonComparer<T> : Comparer<T>
        {
            private readonly Comparison<T> compare;

            public ComparisonComparer(Comparison<T> compare)
            {
                this.compare = compare;
            }

            public override int Compare(T x, T y)
            {
                return compare(x, y);
            }
        }
    }
}