// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal static class SegmentedArray
    {
        /// <seealso cref="Array.Clear(Array, int, int)"/>
        internal static void Clear<T>(SegmentedArray<T> buckets, int index, int length)
        {
            throw new NotImplementedException();
        }

        /// <seealso cref="Array.Copy(Array, Array, int)"/>
        internal static void Copy<T>(SegmentedArray<T> sourceArray, SegmentedArray<T> destinationArray, int length)
        {
            throw new NotImplementedException();
        }
    }
}
