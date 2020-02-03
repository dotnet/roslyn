// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            /// <summary>
            /// Since the floating-point printing code has changed recently, we need to hard-code the way we form strings
            /// so that tests pass on all platforms.
            /// </summary>
            string ToString(T value);
        }
    }
}
