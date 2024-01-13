// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Compares objects based upon their reference identity.
    /// </summary>
    internal class ReferenceEqualityComparer : IEqualityComparer<object?>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        private ReferenceEqualityComparer()
        {
        }

        bool IEqualityComparer<object?>.Equals(object? a, object? b)
        {
            return a == b;
        }

        int IEqualityComparer<object?>.GetHashCode(object? a)
        {
            return ReferenceEqualityComparer.GetHashCode(a);
        }

        public static int GetHashCode(object? a)
        {
            return RuntimeHelpers.GetHashCode(a);
        }
    }
}
