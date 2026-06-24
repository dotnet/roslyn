// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// An interface representing a set of values of a specific type.  During construction of the state machine
    /// for pattern-matching, we track the set of values of each intermediate result that can reach each state.
    /// That permits us to determine when tests can be eliminated either because they are impossible (and can be
    /// replaced by an always-false test) or always true with the set of values that can reach that state (and
    /// can be replaced by an always-true test).
    /// </summary>
    internal interface IValueSet
    {
        /// <summary>
        /// Return the intersection of this value set with another. Both must have been created with the same <see cref="IConstantValueSetFactory{T}"/>.
        /// </summary>
        IValueSet Intersect(IValueSet other);

        /// <summary>
        /// Return this union of this value set with another. Both must have been created with the same <see cref="IConstantValueSetFactory{T}"/>.
        /// </summary>
        IValueSet Union(IValueSet other);

        /// <summary>
        /// Return the complement of this value set.
        /// </summary>
        IValueSet Complement();
    }

    internal interface IConstantValueSet : IValueSet
    {
        /// <summary>
        /// Test if the value set contains any values that satisfy the given relation with the given value.  Supported values for <paramref name="relation"/>
        /// are <see cref="BinaryOperatorKind.Equal"/> for all supported types, and for numeric types we also support
        /// <see cref="BinaryOperatorKind.LessThan"/>, <see cref="BinaryOperatorKind.LessThanOrEqual"/>, <see cref="BinaryOperatorKind.GreaterThan"/>, and
        /// <see cref="BinaryOperatorKind.GreaterThanOrEqual"/>.
        /// </summary>
        bool Any(BinaryOperatorKind relation, ConstantValue value);

        /// <summary>
        /// Test if all of the value in the set satisfy the given relation with the given value. Note that the empty set trivially satisfies this.
        /// Because of that all four combinations of results from <see cref="Any(BinaryOperatorKind, ConstantValue)"/> and <see cref="All(BinaryOperatorKind, ConstantValue)"/>
        /// are possible: both true when the set is nonempty and all values satisfy the relation; both false when the set is nonempty and none of
        /// the values satisfy the relation; all but not any when the set is empty; any but not all when the set is nonempty and some values satisfy
        /// the relation and some do not.
        /// </summary>
        bool All(BinaryOperatorKind relation, ConstantValue value);

        /// <summary>
        /// Does this value set contain no values?
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Produce a sample value contained in the set. Throws <see cref="ArgumentException"/> if the set is empty. If the set
        /// contains values but we cannot produce a particular value (e.g. for the set `nint > int.MaxValue`), returns null.
        /// </summary>
        ConstantValue? Sample { get; }
    }

    /// <summary>
    /// An interface representing a set of values of a specific type.  Like <see cref="IValueSet"/> but strongly typed to <typeparamref name="T"/>.
    /// </summary>
    internal interface IConstantValueSet<T> : IConstantValueSet
    {
        /// <summary>
        /// Return the intersection of this value set with another. Both must have been created with the same <see cref="IConstantValueSetFactory{T}"/>.
        /// </summary>
        IConstantValueSet<T> Intersect(IConstantValueSet<T> other);

        /// <summary>
        /// Return this union of this value set with another. Both must have been created with the same <see cref="IConstantValueSetFactory{T}"/>.
        /// </summary>
        IConstantValueSet<T> Union(IConstantValueSet<T> other);

        /// <summary>
        /// Return the complement of this value set.
        /// </summary>
        new IConstantValueSet<T> Complement();

        /// <summary>
        /// Test if the value set contains any values that satisfy the given relation with the given value.
        /// </summary>
        bool Any(BinaryOperatorKind relation, T value);

        /// <summary>
        /// Test if all of the value in the set satisfy the given relation with the given value. Note that the empty set trivially satisfies this.
        /// Because of that all four combinations of results from <see cref="Any(BinaryOperatorKind, T)"/> and <see cref="All(BinaryOperatorKind, T)"/>
        /// are possible: both true when the set is nonempty and all values satisfy the relation; both false when the set is nonempty and none of
        /// the values satisfy the relation; all but not any when the set is empty; any but not all when the set is nonempty and some values satisfy
        /// the relation and some do not.
        /// </summary>
        bool All(BinaryOperatorKind relation, T value);
    }
}
