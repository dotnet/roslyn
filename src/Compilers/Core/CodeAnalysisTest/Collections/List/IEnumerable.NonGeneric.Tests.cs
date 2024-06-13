// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/Common/tests/System/Collections/IEnumerable.NonGeneric.Tests.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the nongeneric
    /// IEnumerable interface
    /// </summary>
    public abstract partial class IEnumerable_NonGeneric_Tests : TestBase
    {
        #region IEnumerable Helper Methods

        /// <summary>
        /// Creates an instance of an IEnumerable that can be used for testing.
        /// </summary>
        /// <param name="count">The number of unique items that the returned IEnumerable contains.</param>
        /// <returns>An instance of an IEnumerable that can be used for testing.</returns>
        protected abstract IEnumerable NonGenericIEnumerableFactory(int count);

        /// <summary>
        /// Modifies the given IEnumerable such that any enumerators for that IEnumerable will be
        /// invalidated.
        /// </summary>
        /// <param name="enumerable">An IEnumerable to modify</param>
        /// <returns>true if the enumerable was successfully modified. Else false.</returns>
        protected delegate bool ModifyEnumerable(IEnumerable enumerable);

        /// <summary>
        /// To be implemented in the concrete collections test classes. Returns a set of ModifyEnumerable delegates
        /// that modify the enumerable passed to them.
        /// </summary>
        protected abstract IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations);

        protected virtual ModifyOperation ModifyEnumeratorThrows => ModifyOperation.Add | ModifyOperation.Insert | ModifyOperation.Overwrite | ModifyOperation.Remove | ModifyOperation.Clear;

        protected virtual ModifyOperation ModifyEnumeratorAllowed => ModifyOperation.None;

        /// <summary>
        /// The Reset method is provided for COM interoperability. It does not necessarily need to be
        /// implemented; instead, the implementer can simply throw a NotSupportedException.
        ///
        /// If Reset is not implemented, this property must return False. The default value is true.
        /// </summary>
        protected virtual bool ResetImplemented => true;

        /// <summary>
        /// When calling Current of the enumerator before the first MoveNext, after the end of the collection,
        /// or after modification of the enumeration, the resulting behavior is undefined. Tests are included
        /// to cover two behavioral scenarios:
        ///   - Throwing an InvalidOperationException
        ///   - Returning an undefined value.
        ///
        /// If this property is set to true, the tests ensure that the exception is thrown. The default value is
        /// false.
        /// </summary>
        protected virtual bool Enumerator_Current_UndefinedOperation_Throws => false;

        /// <summary>
        /// Whether the collection can be serialized.
        /// </summary>
        protected virtual bool SupportsSerialization => true;

        /// <summary>
        /// Specifies whether this IEnumerable follows some sort of ordering pattern.
        /// </summary>
        protected virtual EnumerableOrder Order => EnumerableOrder.Sequential;

        /// <summary>
        /// An enum to allow specification of the order of the Enumerable. Used in validation for enumerables.
        /// </summary>
        protected enum EnumerableOrder
        {
            Unspecified,
            Sequential
        }

        #endregion

        #region GetEnumerator()

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_GetEnumerator_NoExceptionsWhileGetting(int count)
        {
            IEnumerable enumerable = NonGenericIEnumerableFactory(count);
            Assert.NotNull(enumerable.GetEnumerator());
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_GetEnumerator_ReturnsUniqueEnumerator(int count)
        {
            //Tests that the enumerators returned by GetEnumerator operate independently of one another
            IEnumerable enumerable = NonGenericIEnumerableFactory(count);
            int iterations = 0;
            foreach (object item in enumerable)
                foreach (object item2 in enumerable)
                    foreach (object item3 in enumerable)
                        iterations++;
            Assert.Equal(count * count * count, iterations);
        }

        #endregion

        #region Enumerator.MoveNext

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_MoveNext_FromStartToFinish(int count)
        {
            int iterations = 0;
            IEnumerator enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
            while (enumerator.MoveNext())
                iterations++;
            Assert.Equal(count, iterations);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_MoveNext_AfterEndOfCollection(int count)
        {
            IEnumerator enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
            for (int i = 0; i < count; i++)
                enumerator.MoveNext();
            Assert.False(enumerator.MoveNext());
            Assert.False(enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_MoveNext_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable enumerable = NonGenericIEnumerableFactory(count);
                IEnumerator enumerator = enumerable.GetEnumerator();
                if (ModifyEnumerable(enumerable))
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_MoveNext_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable enumerable = NonGenericIEnumerableFactory(count);
                IEnumerator enumerator = enumerable.GetEnumerator();
                for (int i = 0; i < count / 2; i++)
                    enumerator.MoveNext();
                if (ModifyEnumerable(enumerable))
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_MoveNext_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable enumerable = NonGenericIEnumerableFactory(count);
                IEnumerator enumerator = enumerable.GetEnumerator();
                while (enumerator.MoveNext()) ;
                if (ModifyEnumerable(enumerable))
                    Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            });
        }

        #endregion

        #region Enumerator.Current

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_Current_FromStartToFinish(int count)
        {
            IEnumerator enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
            object current;
            while (enumerator.MoveNext())
                current = enumerator.Current;
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_Current_ReturnsSameValueOnRepeatedCalls(int count)
        {
            IEnumerator enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
            while (enumerator.MoveNext())
            {
                object current = enumerator.Current;
                Assert.Equal(current, enumerator.Current);
                Assert.Equal(current, enumerator.Current);
                Assert.Equal(current, enumerator.Current);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_Current_ReturnsSameObjectsOnDifferentEnumerators(int count)
        {
            // Ensures that the elements returned from enumeration are exactly the same collection of
            // elements returned from a previous enumeration
            IEnumerable enumerable = NonGenericIEnumerableFactory(count);
            HashSet<object> firstValues = new HashSet<object>(count);
            HashSet<object> secondValues = new HashSet<object>(count);
            foreach (object item in enumerable)
                Assert.True(firstValues.Add(item));
            foreach (object item in enumerable)
                Assert.True(secondValues.Add(item));
            Assert.Equal(firstValues.Count, secondValues.Count);
            foreach (object item in firstValues)
                Assert.True(secondValues.Contains(item));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void Enumerator_Current_BeforeFirstMoveNext_UndefinedBehavior(int count)
        {
            object current;
            IEnumerable enumerable = NonGenericIEnumerableFactory(count);
            IEnumerator enumerator = enumerable.GetEnumerator();
            if (Enumerator_Current_UndefinedOperation_Throws)
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            else
                current = enumerator.Current;
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void Enumerator_Current_AfterEndOfEnumerable_UndefinedBehavior(int count)
        {
            object current;
            IEnumerable enumerable = NonGenericIEnumerableFactory(count);
            IEnumerator enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext()) ;
            if (Enumerator_Current_UndefinedOperation_Throws)
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            else
                current = enumerator.Current;
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void Enumerator_Current_ModifiedDuringEnumeration_UndefinedBehavior(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                object current;
                IEnumerable enumerable = NonGenericIEnumerableFactory(count);
                IEnumerator enumerator = enumerable.GetEnumerator();
                if (ModifyEnumerable(enumerable))
                {
                    if (Enumerator_Current_UndefinedOperation_Throws)
                        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                    else
                        current = enumerator.Current;
                }
            });
        }

        #endregion

        #region Enumerator.Reset

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_Reset_BeforeIteration_Support(int count)
        {
            IEnumerator enumerator = NonGenericIEnumerableFactory(count).GetEnumerator();
            if (ResetImplemented)
                enumerator.Reset();
            else
                Assert.Throws<NotSupportedException>(() => enumerator.Reset());
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_Reset_ModifiedBeforeEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable enumerable = NonGenericIEnumerableFactory(count);
                IEnumerator enumerator = enumerable.GetEnumerator();
                if (ModifyEnumerable(enumerable))
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_Reset_ModifiedDuringEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable enumerable = NonGenericIEnumerableFactory(count);
                IEnumerator enumerator = enumerable.GetEnumerator();
                for (int i = 0; i < count / 2; i++)
                    enumerator.MoveNext();
                if (ModifyEnumerable(enumerable))
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IEnumerable_NonGeneric_Enumerator_Reset_ModifiedAfterEnumeration_ThrowsInvalidOperationException(int count)
        {
            Assert.All(GetModifyEnumerables(ModifyEnumeratorThrows), ModifyEnumerable =>
            {
                IEnumerable enumerable = NonGenericIEnumerableFactory(count);
                IEnumerator enumerator = enumerable.GetEnumerator();
                while (enumerator.MoveNext()) ;
                if (ModifyEnumerable(enumerable))
                    Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
            });
        }

        #endregion
    }
}
