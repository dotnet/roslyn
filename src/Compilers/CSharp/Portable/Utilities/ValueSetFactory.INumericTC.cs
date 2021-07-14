// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ValueSetFactory
    {
        /// <summary>
        /// A type class providing the primitive operations needed to support a value set.
        /// </summary>
        /// <typeparam name="T">the underlying primitive numeric type</typeparam>
        private interface INumericTC<T>
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
            /// Compute the value of the binary relational operator on the given operands.
            /// </summary>
            bool Related(BinaryOperatorKind relation, T left, T right);

            /// <summary>
            /// The smallest value of <typeparamref name="T"/>.
            /// </summary>
            T MinValue { get; }

            /// <summary>
            /// The largest value of <typeparamref name="T"/>.
            /// </summary>
            T MaxValue { get; }

            /// <summary>
            /// The successor (next larger) value to a given value. The result is not defined
            /// when <paramref name="value"/> is <see cref="MaxValue"/>.
            /// </summary>
            T Next(T value);

            /// <summary>
            /// The predecessor (previous larger) value to a given value. The result is not defined
            /// when <paramref name="value"/> is <see cref="MinValue"/>.
            /// </summary>
            T Prev(T value);

            /// <summary>
            /// Produce a randomly-selected value for testing purposes.
            /// </summary>
            T Random(Random random);

            /// <summary>
            /// Produce the zero value for the type.
            /// </summary>
            T Zero { get; }

            /// <summary>
            /// A formatter for values of type <typeparamref name="T"/>.  This is needed for testing because
            /// the default ToString output for float and double changed between desktop and .net core,
            /// and also because we want the string representation to be locale-independent.
            /// </summary>
            string ToString(T value);
        }
    }
}
