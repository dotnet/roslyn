// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.2/src/libraries/System.Collections.Immutable/tests/ImmutableTestBase.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Collections
{
    public abstract partial class ImmutablesTestBase
    {
        /// <summary>
        /// Gets the number of operations to perform in randomized tests.
        /// </summary>
        protected static int RandomOperationsCount
        {
            get { return 100; }
        }

        internal static void AssertAreSame<T>(T expected, T actual)
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                Assert.Equal(expected, actual); //, message, formattingArgs);
            }
            else
            {
                Assert.Same((object?)expected, (object?)actual); //, message, formattingArgs);
            }
        }

        internal static void CollectionAssertAreEquivalent<T>(ICollection<T> expected, ICollection<T> actual)
        {
            Assert.Equal(expected.Count, actual.Count);
            foreach (var value in expected)
            {
                Assert.Contains(value, actual);
            }
        }

        protected static bool IsSame<T>(IImmutableList<T> first, IImmutableList<T> second)
        {
            if (first is ImmutableSegmentedList<T> firstSegmented
                && second is ImmutableSegmentedList<T> secondSegmented)
            {
                return firstSegmented == secondSegmented;
            }
            else if (first.GetType() != second.GetType())
            {
                // If the instances do not have the same type, they cannot be the same
                return false;
            }
            else if (first.GetType().IsValueType)
            {
                throw new NotSupportedException($"Unable to compare '{first.GetType()}' for identity.");
            }

            return first == second;
        }

        protected static bool IsSame<T>(IImmutableSet<T> first, IImmutableSet<T> second)
        {
            if (first is ImmutableSegmentedHashSet<T> firstSegmented
                && second is ImmutableSegmentedHashSet<T> secondSegmented)
            {
                return firstSegmented == secondSegmented;
            }
            else if (first.GetType() != second.GetType())
            {
                // If the instances do not have the same type, they cannot be the same
                return false;
            }
            else if (first.GetType().IsValueType)
            {
                throw new NotSupportedException($"Unable to compare '{first.GetType()}' for identity.");
            }

            return first == second;
        }

        protected static bool IsSame<TKey, TValue>(IImmutableDictionary<TKey, TValue> first, IImmutableDictionary<TKey, TValue> second)
            where TKey : notnull
        {
            if (first is ImmutableSegmentedDictionary<TKey, TValue> firstSegmented
                && second is ImmutableSegmentedDictionary<TKey, TValue> secondSegmented)
            {
                return firstSegmented == secondSegmented;
            }
            else if (first.GetType() != second.GetType())
            {
                // If the instances do not have the same type, they cannot be the same
                return false;
            }
            else if (first.GetType().IsValueType)
            {
                throw new NotSupportedException($"Unable to compare '{first.GetType()}' for identity.");
            }

            return first == second;
        }

        protected static string ToString(System.Collections.IEnumerable sequence)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            int count = 0;
            foreach (object item in sequence)
            {
                if (count > 0)
                {
                    sb.Append(',');
                }

                if (count == 10)
                {
                    sb.Append("...");
                    break;
                }

                sb.Append(item);
                count++;
            }

            sb.Append('}');
            return sb.ToString();
        }

        protected static object ToStringDeferred(System.Collections.IEnumerable sequence)
        {
            return new DeferredToString(() => ToString(sequence));
        }

        protected static void ManuallyEnumerateTest<T>(IList<T> expectedResults, IEnumerator<T> enumerator)
        {
            T[] manualArray = new T[expectedResults.Count];
            int i = 0;

            Assert.Equal(default(T), enumerator.Current);

            while (enumerator.MoveNext())
            {
                manualArray[i++] = enumerator.Current;
            }

            Assert.False(enumerator.MoveNext());
            Assert.Equal(default(T), enumerator.Current);
            Assert.False(enumerator.MoveNext());
            Assert.Equal(default(T), enumerator.Current);

            Assert.Equal(expectedResults.Count, i); //, "Enumeration did not produce enough elements.");
            Assert.Equal<T>(expectedResults, manualArray);
        }

        /// <summary>
        /// Generates an array of unique values.
        /// </summary>
        /// <param name="length">The desired length of the array.</param>
        /// <returns>An array of doubles.</returns>
        protected static double[] GenerateDummyFillData(int length = 1000)
        {
            Assert.InRange(length, 0, int.MaxValue);

            int seed = unchecked((int)DateTime.Now.Ticks);

            Debug.WriteLine("Random seed {0}", seed);

            var random = new Random(seed);
            var inputs = new double[length];
            var ensureUniqueness = new HashSet<double>();
            for (int i = 0; i < inputs.Length; i++)
            {
                double input;
                do
                {
                    input = random.NextDouble();
                }
                while (!ensureUniqueness.Add(input));
                inputs[i] = input;
            }

            Assert.NotNull(inputs);
            Assert.Equal(length, inputs.Length);

            return inputs;
        }

        private class DeferredToString
        {
            private readonly Func<string> _generator;

            internal DeferredToString(Func<string> generator)
            {
                Debug.Assert(generator != null);
                _generator = generator;
            }

            public override string ToString()
            {
                return _generator();
            }
        }
    }
}
