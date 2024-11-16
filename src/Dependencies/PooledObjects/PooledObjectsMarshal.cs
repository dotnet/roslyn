// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.PooledObjects;

/// <summary>
/// An unsafe class that provides a set of methods to access the underlying data representations of pooled
/// collections.
/// </summary>
internal static class PooledObjectsMarshal
{
    /// <summary>
    /// Gets the backing storage array for a <see cref="SpannableArrayBuilder{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements stored in the array.</typeparam>
    /// <param name="builder">The array builder.</param>
    /// <returns>The backing storage array for the array builder. The returned array will have a length
    /// of <paramref name="builder"/>.Capacity, of which only the first <paramref name="builder"/>.Count
    /// entries are valid. Note that Adding items or capacity to <paramref name="builder"/> may invalidate 
    /// the association between it and the returned array.</returns>
    /// <remarks>Use of this method is fraught with danger if done incorrectly. Adding items or capacity 
    /// to <paramref name="builder"/> may invalidate the association between it and the returned array.
    /// Freeing <paramref name="builder"/> may add the returned array back to the pool, allowing access
    /// to this array to other <see cref="SpannableArrayBuilder{T}.GetInstance"/> callers.</remarks>
    public static T[] AsArray<T>(SpannableArrayBuilder<T> builder)
        => SpannableArrayBuilder<T>.PrivateMarshal.AsArray(builder);
}
