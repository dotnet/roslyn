// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A type class providing primitive operations needed to support a value set for a floating-point type.
        /// </summary>
        private interface FloatingTC<T> : NumericTC<T>
        {
            /// <summary>
            /// A "not a number" value for the floating-point type <typeparamref name="T"/>.
            /// All NaN values are treated as equivalent.
            /// </summary>
            T NaN { get; }

            /// <summary>
            /// The "negative infinity" value for the flowing-point type <typeparamref name="T"/>.
            /// </summary>
            T MinusInf { get; }

            /// <summary>
            /// The "positive infinity" value for the flowing-point type <typeparamref name="T"/>.
            /// </summary>
            T PlusInf { get; }
        }
    }
}
