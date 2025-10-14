// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

#nullable enable

#if NET

#pragma warning disable RS0016 // Add public types and members to the declared API (this is a supporting forwarder for an internal polyfill API)
[assembly: TypeForwardedTo(typeof(System.Collections.Generic.ReferenceEqualityComparer))]
#pragma warning restore RS0016 // Add public types and members to the declared API

#else

namespace System.Collections.Generic;

internal sealed class ReferenceEqualityComparer : IEqualityComparer<object?>, IEqualityComparer
{
    private ReferenceEqualityComparer() { }

    /// <summary>
    /// Gets the singleton <see cref="ReferenceEqualityComparer"/> instance.
    /// </summary>
    public static ReferenceEqualityComparer Instance { get; } = new ReferenceEqualityComparer();

    /// <summary>
    /// Determines whether two object references refer to the same object instance.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>
    /// <see langword="true"/> if both <paramref name="x"/> and <paramref name="y"/> refer to the same object instance
    /// or if both are <see langword="null"/>; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This API is a wrapper around <see cref="object.ReferenceEquals(object?, object?)"/>.
    /// It is not necessarily equivalent to calling <see cref="object.Equals(object?, object?)"/>.
    /// </remarks>
    public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

#pragma warning disable CA1200 // Avoid using cref tags with a prefix - this works around cref not correctly resolving RuntimeHelpers as SCI also has an internal version without GetHashCode.
    /// <summary>
    /// Returns a hash code for the specified object. The returned hash code is based on the object
    /// identity, not on the contents of the object.
    /// </summary>
    /// <param name="obj">The object for which to retrieve the hash code.</param>
    /// <returns>A hash code for the identity of <paramref name="obj"/>.</returns>
    /// <remarks>
    /// This API is a wrapper around <see cref="M:System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(object)"/>.
    /// It is not necessarily equivalent to calling <see cref="object.GetHashCode()"/>.
    /// </remarks>
#pragma warning restore CA1200
    public int GetHashCode(object? obj)
    {
        // Depending on target framework, RuntimeHelpers.GetHashCode might not be annotated
        // with the proper nullability attribute. We'll suppress any warning that might
        // result.
        return RuntimeHelpers.GetHashCode(obj!);
    }
}

#endif
