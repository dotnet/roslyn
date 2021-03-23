// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/ImmutableTestBase.nonnetstandard.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public abstract partial class ImmutablesTestBase
    {
        /// <summary>
        /// Tests the EqualsStructurally public method and the IStructuralEquatable.Equals method.
        /// </summary>
        /// <typeparam name="TCollection">The type of tested collection.</typeparam>
        /// <typeparam name="TElement">The type of element stored in the collection.</typeparam>
        /// <param name="objectUnderTest">An instance of the collection to test, which must have at least two elements.</param>
        /// <param name="additionalItem">A unique item that does not already exist in <paramref name="objectUnderTest" />.</param>
        /// <param name="equalsStructurally">A delegate that invokes the EqualsStructurally method.</param>
        protected static void StructuralEqualityHelper<TCollection, TElement>(TCollection objectUnderTest, TElement additionalItem, Func<TCollection, IEnumerable<TElement>?, bool> equalsStructurally)
            where TCollection : class, IEnumerable<TElement>
        {
            if (objectUnderTest is null)
                throw new ArgumentNullException(nameof(objectUnderTest));
            if (objectUnderTest.Count() < 2)
                throw new ArgumentException("Collection must contain at least two elements.", nameof(objectUnderTest));
            if (equalsStructurally is null)
                throw new ArgumentNullException(nameof(equalsStructurally));

            var structuralEquatableUnderTest = objectUnderTest as IStructuralEquatable;
            var enumerableUnderTest = (IEnumerable<TElement>)objectUnderTest;

            var equivalentSequence = objectUnderTest.ToList();
            var shorterSequence = equivalentSequence.Take(equivalentSequence.Count() - 1);
            var longerSequence = equivalentSequence.Concat(new[] { additionalItem });
            var differentSequence = shorterSequence.Concat(new[] { additionalItem });
            var nonUniqueSubsetSequenceOfSameLength = shorterSequence.Concat(shorterSequence.Take(1));

            var testValues = new IEnumerable<TElement>?[] {
                objectUnderTest,
                null,
                Enumerable.Empty<TElement>(),
                equivalentSequence,
                longerSequence,
                shorterSequence,
                nonUniqueSubsetSequenceOfSameLength,
            };

            foreach (var value in testValues)
            {
                bool expectedResult = value != null && Enumerable.SequenceEqual(objectUnderTest, value);

                if (structuralEquatableUnderTest != null)
                {
                    Assert.Equal(expectedResult, structuralEquatableUnderTest.Equals(value, null!));

                    if (value != null)
                    {
                        Assert.Equal(
                            expectedResult,
                            structuralEquatableUnderTest.Equals(new NonGenericEnumerableWrapper(value), null!));
                    }
                }

                Assert.Equal(expectedResult, equalsStructurally(objectUnderTest, value));
            }
        }

        private class NonGenericEnumerableWrapper : IEnumerable
        {
            private readonly IEnumerable _enumerable;

            internal NonGenericEnumerableWrapper(IEnumerable enumerable)
            {
                _enumerable = enumerable ?? throw new ArgumentNullException(nameof(enumerable));
            }

            public IEnumerator GetEnumerator()
            {
                return _enumerable.GetEnumerator();
            }
        }
    }
}
