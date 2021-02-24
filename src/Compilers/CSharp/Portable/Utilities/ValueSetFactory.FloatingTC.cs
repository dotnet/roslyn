// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A type class providing primitive operations needed to support a value set for a floating-point type.
        /// </summary>
        private interface FloatingTC<T> : INumericTC<T>
        {
            /// <summary>
            /// A "not a number" value for the floating-point type <typeparamref name="T"/>.
            /// All NaN values are treated as equivalent.
            /// </summary>
            T NaN { get; }
        }
    }
}
