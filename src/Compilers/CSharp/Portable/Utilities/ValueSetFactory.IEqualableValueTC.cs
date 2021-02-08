// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A type class for values (of type <typeparamref name="T"/>) that can be directly compared for equality 
        /// using <see cref="System.Object.Equals(object?, object?)"/>.
        /// </summary>
        private interface IEquatableValueTC<T> where T : notnull
        {
            /// <summary>
            /// Get the constant value of type <typeparamref name="T"/> from a <see cref="ConstantValue"/>. This method is shared among all
            /// typeclasses for value sets.
            /// </summary>
            T FromConstantValue(ConstantValue constantValue);

            /// <summary>
            /// Translate a numeric value of type <typeparamref name="T"/> into a <see cref="ConstantValue"/>.
            /// </summary>
            ConstantValue ToConstantValue(T value);

            /// <summary>
            /// Generate <paramref name="count"/> random values of type <typeparamref name="T"/>.
            /// If the domain of <typeparamref name="T"/> is infinite (for example, a string type),
            /// the <paramref name="count"/> parameter is used to identify the size of a restricted
            /// domain.  If the domain is finite (for example the numeric types), then
            /// <paramref name="scope"/> is ignored.
            /// </summary>
            T[] RandomValues(int count, Random random, int scope = 0);
        }
    }
}
