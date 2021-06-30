// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace Microsoft.CodeAnalysis.Collections.Internal
{
    internal static class ICollectionCalls
    {
        public static bool IsSynchronized<TCollection>(ref TCollection collection)
            where TCollection : ICollection
            => collection.IsSynchronized;

        public static void CopyTo<TCollection>(ref TCollection collection, Array array, int index)
            where TCollection : ICollection
        {
            collection.CopyTo(array, index);
        }
    }
}
