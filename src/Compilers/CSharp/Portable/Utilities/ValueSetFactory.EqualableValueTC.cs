﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A type class for values (of type <typeparamref name="T"/>) that can be directly compared for equality 
        /// using <see cref="System.Object.Equals(object?, object?)"/>.
        /// </summary>
        private interface EqualableValueTC<T>
        {
            /// <summary>
            /// Get the constant value of type <typeparamref name="T"/> from a <see cref="ConstantValue"/>. This method is shared among all
            /// typeclasses for value sets.
            /// </summary>
            T FromConstantValue(ConstantValue constantValue);
        }
    }
}
