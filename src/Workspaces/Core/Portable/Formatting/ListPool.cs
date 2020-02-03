// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal static class ListPool<T>
    {
        public static List<T> Allocate()
        {
            return SharedPools.Default<List<T>>().AllocateAndClear();
        }

        public static void Free(List<T> list)
        {
            SharedPools.Default<List<T>>().ClearAndFree(list);
        }

        public static List<T> ReturnAndFree(List<T> list)
        {
            SharedPools.Default<List<T>>().ForgetTrackedObject(list);
            return list;
        }
    }
}
