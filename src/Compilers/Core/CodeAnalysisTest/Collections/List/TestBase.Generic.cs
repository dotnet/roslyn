// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/Common/tests/System/Collections/TestBase.Generic.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    /// <summary>
    /// Provides a base set of generic operations that are used by all other generic testing interfaces.
    /// </summary>
    public abstract class TestBase<T> : TestBase
    {
        #region Helper Methods

        /// <summary>
        /// To be implemented in the concrete collections test classes. Creates an instance of T that
        /// is dependent only on the seed passed as input and will return the same value on repeated
        /// calls with the same seed.
        /// </summary>
        protected abstract T CreateT(int seed);

        /// <summary>
        /// The EqualityComparer that can be used in the overriding class when creating test enumerables
        /// or test collections. Default if not overridden is the default comparator.
        /// </summary>
        protected virtual IEqualityComparer<T> GetIEqualityComparer() => EqualityComparer<T>.Default;

        /// <summary>
        /// The Comparer that can be used in the overriding class when creating test enumerables
        /// or test collections. Default if not overridden is the default comparator.
        /// </summary>
        protected virtual IComparer<T> GetIComparer() => Comparer<T>.Default;

        /// <summary>
        /// MemberData to be passed to tests that take an IEnumerable{T}. This method returns every permutation of
        /// EnumerableType to test on (e.g. HashSet, Queue), and size of set to test with (e.g. 0, 1, etc.).
        /// </summary>
        public static IEnumerable<object[]> EnumerableTestData()
        {
            foreach (object[] collectionSizeArray in ValidCollectionSizes())
            {
                foreach (EnumerableType enumerableType in Enum.GetValues(typeof(EnumerableType)))
                {
                    int count = (int)collectionSizeArray[0];
                    yield return new object[] { enumerableType, count, 0, 0, 0 };                       // Empty Enumerable
                    yield return new object[] { enumerableType, count, count + 1, 0, 0 };               // Enumerable that is 1 larger

                    if (count >= 1)
                    {
                        yield return new object[] { enumerableType, count, count, 0, 0 };               // Enumerable of the same size
                        yield return new object[] { enumerableType, count, count - 1, 0, 0 };           // Enumerable that is 1 smaller
                        yield return new object[] { enumerableType, count, count, 1, 0 };               // Enumerable of the same size with 1 matching element
                        yield return new object[] { enumerableType, count, count + 1, 1, 0 };           // Enumerable that is 1 longer with 1 matching element
                        yield return new object[] { enumerableType, count, count, count, 0 };           // Enumerable with all elements matching
                        yield return new object[] { enumerableType, count, count + 1, count, 0 };       // Enumerable with all elements matching plus one extra
                    }

                    if (count >= 2)
                    {
                        yield return new object[] { enumerableType, count, count - 1, 1, 0 };           // Enumerable that is 1 smaller with 1 matching element
                        yield return new object[] { enumerableType, count, count + 2, 2, 0 };           // Enumerable that is 2 longer with 2 matching element
                        yield return new object[] { enumerableType, count, count - 1, count - 1, 0 };   // Enumerable with all elements matching minus one
                        yield return new object[] { enumerableType, count, count, 2, 0 };               // Enumerable of the same size with 2 matching element
                        if ((enumerableType == EnumerableType.List || enumerableType == EnumerableType.Queue))
                            yield return new object[] { enumerableType, count, count, 0, 1 };           // Enumerable with 1 element duplicated
                    }

                    if (count >= 3)
                    {
                        if ((enumerableType == EnumerableType.List || enumerableType == EnumerableType.Queue))
                            yield return new object[] { enumerableType, count, count, 0, 1 };           // Enumerable with all elements duplicated
                        yield return new object[] { enumerableType, count, count - 1, 2, 0 };           // Enumerable that is 1 smaller with 2 matching elements
                    }
                }
            }
        }

        /// <summary>
        /// Helper function to create an enumerable fulfilling the given specific parameters. The function will
        /// create an enumerable of the desired type using the Default constructor for that type and then add values
        /// to it until it is full. It will begin by adding the desired number of matching and duplicate elements,
        /// followed by random (deterministic) elements until the desired count is reached.
        /// </summary>
        protected IEnumerable<T> CreateEnumerable(EnumerableType type, IEnumerable<T>? enumerableToMatchTo, int count, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            Debug.Assert(count >= numberOfMatchingElements);
            Debug.Assert(count >= numberOfDuplicateElements);

            switch (type)
            {
                case EnumerableType.SegmentedHashSet:
                    Debug.Assert(numberOfDuplicateElements == 0, "Can not create a HashSet with duplicate elements - numberOfDuplicateElements must be zero");
                    return CreateSegmentedHashSet(enumerableToMatchTo, count, numberOfMatchingElements);
                case EnumerableType.List:
                    return CreateList(enumerableToMatchTo, count, numberOfMatchingElements, numberOfDuplicateElements);
                case EnumerableType.SortedSet:
                    Debug.Assert(numberOfDuplicateElements == 0, "Can not create a SortedSet with duplicate elements - numberOfDuplicateElements must be zero");
                    return CreateSortedSet(enumerableToMatchTo, count, numberOfMatchingElements);
                case EnumerableType.Queue:
                    return CreateQueue(enumerableToMatchTo, count, numberOfMatchingElements, numberOfDuplicateElements);
                case EnumerableType.Lazy:
                    return CreateLazyEnumerable(enumerableToMatchTo, count, numberOfMatchingElements, numberOfDuplicateElements);
                default:
                    Debug.Assert(false, "Check that the 'EnumerableType' Enum returns only types that are special-cased in the CreateEnumerable function within the Iset_Generic_Tests class");
                    return null;
            }
        }

        /// <summary>
        /// Helper function to create a Queue fulfilling the given specific parameters. The function will
        /// create an Queue and then add values
        /// to it until it is full. It will begin by adding the desired number of matching,
        /// followed by random (deterministic) elements until the desired count is reached.
        /// </summary>
        protected IEnumerable<T> CreateQueue(IEnumerable<T>? enumerableToMatchTo, int count, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            Queue<T> queue = new Queue<T>(count);
            int seed = 528;
            int duplicateAdded = 0;
            List<T>? match = null;

            // Enqueue Matching elements
            if (enumerableToMatchTo != null)
            {
                match = enumerableToMatchTo.ToList();
                for (int i = 0; i < numberOfMatchingElements; i++)
                {
                    queue.Enqueue(match[i]);
                    while (duplicateAdded++ < numberOfDuplicateElements)
                        queue.Enqueue(match[i]);
                }
            }

            // Enqueue elements to reach the desired count
            while (queue.Count < count)
            {
                T toEnqueue = CreateT(seed++);
                while (queue.Contains(toEnqueue) || (match != null && match.Contains(toEnqueue))) // Don't want any unexpectedly duplicate values
                    toEnqueue = CreateT(seed++);
                queue.Enqueue(toEnqueue);
                while (duplicateAdded++ < numberOfDuplicateElements)
                    queue.Enqueue(toEnqueue);
            }

            // Validate that the Enumerable fits the guidelines as expected
            Debug.Assert(queue.Count == count);
            if (match != null)
            {
                int actualMatchingCount = 0;
                foreach (T lookingFor in match)
                    actualMatchingCount += queue.Contains(lookingFor) ? 1 : 0;
                Assert.Equal(numberOfMatchingElements, actualMatchingCount);
            }

            return queue;
        }

        /// <summary>
        /// Helper function to create an List fulfilling the given specific parameters. The function will
        /// create an List and then add values
        /// to it until it is full. It will begin by adding the desired number of matching,
        /// followed by random (deterministic) elements until the desired count is reached.
        /// </summary>
        protected IEnumerable<T> CreateList(IEnumerable<T>? enumerableToMatchTo, int count, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            List<T> list = new List<T>(count);
            int seed = 528;
            int duplicateAdded = 0;
            List<T>? match = null;

            // Add Matching elements
            if (enumerableToMatchTo != null)
            {
                match = enumerableToMatchTo.ToList();
                for (int i = 0; i < numberOfMatchingElements; i++)
                {
                    list.Add(match[i]);
                    while (duplicateAdded++ < numberOfDuplicateElements)
                        list.Add(match[i]);
                }
            }

            // Add elements to reach the desired count
            while (list.Count < count)
            {
                T toAdd = CreateT(seed++);
                while (list.Contains(toAdd) || (match != null && match.Contains(toAdd))) // Don't want any unexpectedly duplicate values
                    toAdd = CreateT(seed++);
                list.Add(toAdd);
                while (duplicateAdded++ < numberOfDuplicateElements)
                    list.Add(toAdd);
            }

            // Validate that the Enumerable fits the guidelines as expected
            Debug.Assert(list.Count == count);
            if (match != null)
            {
                int actualMatchingCount = 0;
                foreach (T lookingFor in match)
                    actualMatchingCount += list.Contains(lookingFor) ? 1 : 0;
                Assert.Equal(numberOfMatchingElements, actualMatchingCount);
            }

            return list;
        }

        /// <summary>
        /// Helper function to create an HashSet fulfilling the given specific parameters. The function will
        /// create an HashSet using the Comparer constructor and then add values
        /// to it until it is full. It will begin by adding the desired number of matching,
        /// followed by random (deterministic) elements until the desired count is reached.
        /// </summary>
        protected IEnumerable<T> CreateSegmentedHashSet(IEnumerable<T>? enumerableToMatchTo, int count, int numberOfMatchingElements)
        {
            SegmentedHashSet<T> set = new SegmentedHashSet<T>(GetIEqualityComparer());
            int seed = 528;
            SegmentedList<T>? match = null;

            // Add Matching elements
            if (enumerableToMatchTo != null)
            {
                match = enumerableToMatchTo.ToSegmentedList();
                for (int i = 0; i < numberOfMatchingElements; i++)
                    set.Add(match[i]);
            }

            // Add elements to reach the desired count
            while (set.Count < count)
            {
                T toAdd = CreateT(seed++);
                while (set.Contains(toAdd) || (match != null && match.Contains(toAdd, GetIEqualityComparer()))) // Don't want any unexpectedly duplicate values
                    toAdd = CreateT(seed++);
                set.Add(toAdd);
            }

            // Validate that the Enumerable fits the guidelines as expected
            Debug.Assert(set.Count == count);
            if (match != null)
            {
                int actualMatchingCount = 0;
                foreach (T lookingFor in match)
                    actualMatchingCount += set.Contains(lookingFor) ? 1 : 0;
                Assert.Equal(numberOfMatchingElements, actualMatchingCount);
            }

            return set;
        }

        /// <summary>
        /// Helper function to create an SortedSet fulfilling the given specific parameters. The function will
        /// create an SortedSet using the Comparer constructor and then add values
        /// to it until it is full. It will begin by adding the desired number of matching,
        /// followed by random (deterministic) elements until the desired count is reached.
        /// </summary>
        protected IEnumerable<T> CreateSortedSet(IEnumerable<T>? enumerableToMatchTo, int count, int numberOfMatchingElements)
        {
            SortedSet<T> set = new SortedSet<T>(GetIComparer());
            int seed = 528;
            List<T>? match = null;

            // Add Matching elements
            if (enumerableToMatchTo != null)
            {
                match = enumerableToMatchTo.ToList();
                for (int i = 0; i < numberOfMatchingElements; i++)
                    set.Add(match[i]);
            }

            // Add elements to reach the desired count
            while (set.Count < count)
            {
                T toAdd = CreateT(seed++);
                while (set.Contains(toAdd) || (match != null && match.Contains(toAdd, GetIEqualityComparer()))) // Don't want any unexpectedly duplicate values
                    toAdd = CreateT(seed++);
                set.Add(toAdd);
            }

            // Validate that the Enumerable fits the guidelines as expected
            Debug.Assert(set.Count == count);
            if (match != null)
            {
                int actualMatchingCount = 0;
                foreach (T lookingFor in match)
                    actualMatchingCount += set.Contains(lookingFor) ? 1 : 0;
                Assert.Equal(numberOfMatchingElements, actualMatchingCount);
            }

            return set;
        }

        protected IEnumerable<T> CreateLazyEnumerable(IEnumerable<T>? enumerableToMatchTo, int count, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            IEnumerable<T> list = CreateList(enumerableToMatchTo, count, numberOfMatchingElements, numberOfDuplicateElements);
            return list.Select(item => item);
        }

        #endregion
    }
}
