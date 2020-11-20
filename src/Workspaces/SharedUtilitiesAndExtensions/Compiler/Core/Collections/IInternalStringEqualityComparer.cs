// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    /// <summary>
    /// Represents an <see cref="IEqualityComparer{String}"/> that's meant for internal
    /// use only and isn't intended to be serialized or returned back to the user.
    /// Use the <see cref="GetUnderlyingEqualityComparer"/> method to get the object
    /// that should actually be returned to the caller.
    /// </summary>
    internal interface IInternalStringEqualityComparer : IEqualityComparer<string?>
    {
        IEqualityComparer<string?> GetUnderlyingEqualityComparer();
    }

    internal static class InternalStringEqualityComparer
    {
        /// <summary>
        /// Unwraps the internal equality comparer, if proxied.
        /// Otherwise returns the equality comparer itself or its default equivalent.
        /// </summary>
        internal static IEqualityComparer<string?> GetUnderlyingEqualityComparer(IEqualityComparer<string?>? outerComparer)
        {
            if (outerComparer is null)
            {
                return EqualityComparer<string?>.Default;
            }
            else if (outerComparer is IInternalStringEqualityComparer internalComparer)
            {
                return internalComparer.GetUnderlyingEqualityComparer();
            }
            else
            {
                return outerComparer;
            }
        }
    }
}
