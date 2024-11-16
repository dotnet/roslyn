// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.PooledObjects;

internal sealed partial class SpannableArrayBuilder<T>
{
    /// <summary>
    /// Private helper class for use only by <see cref="PooledObjectsMarshal"/>.
    /// </summary>
    internal static class PrivateMarshal
    {
        /// <inheritdoc cref="PooledObjectsMarshal.AsArray{T}(SpannableArrayBuilder{T})"/>
        public static T[] AsArray(SpannableArrayBuilder<T> builder)
            => builder._items;
    }
}
