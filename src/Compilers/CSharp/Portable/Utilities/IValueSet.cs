// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An interface representing a set of values of a specific type.
    /// </summary>
    internal interface IValueSet
    {
        /// <summary>
        /// Return the intersection of this value set with another. Both must have been created with the same <see cref="IValueSetFactory{T}"/>.
        /// </summary>
        IValueSet Intersect(IValueSet other);

        /// <summary>
        /// Return this union of this value set with another. Both must have been created with the same <see cref="IValueSetFactory{T}"/>.
        /// </summary>
        IValueSet Union(IValueSet other);

        /// <summary>
        /// Return the complement of this value set.
        /// </summary>
        IValueSet Complement();

        /// <summary>
        /// Test if the value set contains any values that satisfy the given relation with the given value.  Supported values for <paramref name="relation"/>
        /// Are <see cref="BinaryOperatorKind.Equal"/> for all supported types, and for numeric types (except decimal) we also support
        /// <see cref="BinaryOperatorKind.LessThan"/>, <see cref="BinaryOperatorKind.LessThanOrEqual"/>, <see cref="BinaryOperatorKind.GreaterThan"/>, and
        /// <see cref="BinaryOperatorKind.GreaterThanOrEqual"/>.
        /// </summary>
        bool Any(BinaryOperatorKind relation, ConstantValue value);

        /// <summary>
        /// Test if all of the value in the set satisfy the given relation with the given value. Note that the empty set trivially satisifies this.
        /// </summary>
        bool All(BinaryOperatorKind relation, ConstantValue value);

        /// <summary>
        /// Does this value set contain no values?
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Our value set factory.
        /// </summary>
        IValueSetFactory Factory { get; }
    }

    /// <summary>
    /// An interface representing a set of values of a specific type.  Like <see cref="IValueSet"/> but strongly typed to <typeparamref name="T"/>.
    /// </summary>
    internal interface IValueSet<T> : IValueSet
    {
        /// <summary>
        /// Return the intersection of this value set with another. Both must have been created with the same <see cref="IValueSetFactory{T}"/>.
        /// </summary>
        IValueSet<T> Intersect(IValueSet<T> other);

        /// <summary>
        /// Return this union of this value set with another. Both must have been created with the same <see cref="IValueSetFactory{T}"/>.
        /// </summary>
        IValueSet<T> Union(IValueSet<T> other);

        /// <summary>
        /// Return the complement of this value set.
        /// </summary>
        new IValueSet<T> Complement();

        /// <summary>
        /// Test if the value set contains any values that satisfy the given relation with the given value.
        /// </summary>
        bool Any(BinaryOperatorKind relation, T value);

        /// <summary>
        /// Test if all of the value in the set satisfy the given relation with the given value. Note that the empty set trivially satisifies this.
        /// </summary>
        bool All(BinaryOperatorKind relation, T value);

        /// <summary>
        /// Our value set factory for <typeparamref name="T"/>.
        /// </summary>
        new IValueSetFactory<T> Factory { get; }
    }
}
