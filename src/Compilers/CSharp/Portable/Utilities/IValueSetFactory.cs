// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// A value set factory, which can be used to create a value set instance.  A given instance of <see cref="IValueSetFactory"/>
    /// supports only one type for the value sets it can produce.
    /// </summary>
    internal interface IValueSetFactory
    {
        /// <summary>
        /// Returns a value set that includes any values that satisfy the given relation when compared to the given value.
        /// </summary>
        IValueSet Related(BinaryOperatorKind relation, ConstantValue value);

        /// <summary>
        /// Returns true iff the values are related according to the given relation.
        /// </summary>
        bool Related(BinaryOperatorKind relation, ConstantValue left, ConstantValue right);

        /// <summary>
        /// Produce a random value set with the given expected size for testing.
        /// </summary>
        IValueSet Random(int expectedSize, Random random);

        /// <summary>
        /// Produce a random value for testing.
        /// </summary>
        ConstantValue RandomValue(Random random);

        /// <summary>
        /// The set containing all values of the type.
        /// </summary>
        IValueSet AllValues { get; }

        /// <summary>
        /// The empty set of values.
        /// </summary>
        IValueSet NoValues { get; }
    }

    /// <summary>
    /// A value set factory, which can be used to create a value set instance.  Like <see cref="ValueSetFactory"/> but strongly
    /// typed to <typeparamref name="T"/>.
    /// </summary>
    internal interface IValueSetFactory<T> : IValueSetFactory
    {
        /// <summary>
        /// Returns a value set that includes any values that satisfy the given relation when compared to the given value.
        /// </summary>
        IValueSet<T> Related(BinaryOperatorKind relation, T value);
    }
}
