// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A type class for values that can be directly compared using equality.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private interface EqualableValueTC<T>
        {
            /// <summary>
            /// Get the constant value of type <typeparamref name="T"/> from a <see cref="ConstantValue"/>.
            /// </summary>
            T FromConstantValue(ConstantValue constantValue);
        }
    }
}
