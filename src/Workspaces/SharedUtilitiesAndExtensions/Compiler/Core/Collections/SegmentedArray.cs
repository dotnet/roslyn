// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal static class SegmentedArray
    {
        /// <seealso cref="Array.Clear(Array, int, int)"/>
        internal static void Clear<T>(SegmentedArray<T> buckets, int index, int length)
        {
            if (index < 0 || length < 0 || (uint)(index + length) > (uint)buckets.Length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            // TODO: Improve this algorithm
            for (var i = 0; i < length; i++)
            {
                buckets[i + index] = default!;
            }
        }

        /// <seealso cref="Array.Copy(Array, Array, int)"/>
        internal static void Copy<T>(SegmentedArray<T> sourceArray, SegmentedArray<T> destinationArray, int length)
        {
            if (length == 0)
                return;

            if ((uint)length <= (uint)sourceArray.Length
                && (uint)length <= (uint)destinationArray.Length)
            {
                var sourcePages = (T[][])sourceArray.SyncRoot;
                var destinationPages = (T[][])destinationArray.SyncRoot;

                var remaining = length;
                for (var i = 0; i < sourcePages.Length; i++)
                {
                    var sourcePage = sourcePages[i];
                    var destinationPage = destinationPages[i];
                    if (remaining <= sourcePage.Length)
                    {
                        Array.Copy(sourcePage, destinationPage, remaining);
                        return;
                    }
                    else
                    {
                        Debug.Assert(sourcePage.Length == destinationPage.Length);
                        Array.Copy(sourcePage, destinationPage, sourcePage.Length);
                        remaining -= sourcePage.Length;
                    }
                }

                throw ExceptionUtilities.Unreachable;
            }

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (length > sourceArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));
            if (length > destinationArray.Length)
                throw new ArgumentException(SR.Arg_LongerThanDestArray, nameof(destinationArray));

            throw ExceptionUtilities.Unreachable;
        }
    }
}
