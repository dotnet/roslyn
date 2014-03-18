// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Assert style type to deal with the lack of features in xUnit's Assert type
    /// </summary>
    public static class AssertEx
    {
        #region AssertEqualityComparer<T>
        private class AssertEqualityComparer<T> : IEqualityComparer<T>
        {
            private readonly static IEqualityComparer<T> instance = new AssertEqualityComparer<T>();

            private static bool CanBeNull()
            {
                var type = typeof(T);
                return !type.IsValueType ||
                    (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
            }

            public static bool IsNull(T @object)
            {
                if (!CanBeNull())
                {
                    return false;
                }

                return object.Equals(@object, default(T));
            }

            public static bool Equals(T left, T right)
            {
                return instance.Equals(left, right);
            }

            bool IEqualityComparer<T>.Equals(T x, T y)
            {
                if (CanBeNull())
                {
                    if (object.Equals(x, default(T)))
                    {
                        return object.Equals(y, default(T));
                    }
                    if (object.Equals(y, default(T)))
                    {
                        return false;
                    }
                }

                if (x.GetType() != y.GetType())
                {
                    return false;
                }

                var equatable = x as IEquatable<T>;
                if (equatable != null)
                {
                    return equatable.Equals(y);
                }

                var comparableT = x as IComparable<T>;
                if (comparableT != null)
                {
                    return comparableT.CompareTo(y) == 0;
                }

                var comparable = x as IComparable;
                if (comparable != null)
                {
                    return comparable.CompareTo(y) == 0;
                }

                var enumerableX = x as IEnumerable;
                var enumerableY = y as IEnumerable;

                if (enumerableX != null && enumerableY != null)
                {
                    var enumeratorX = enumerableX.GetEnumerator();
                    var enumeratorY = enumerableY.GetEnumerator();

                    while (true)
                    {
                        bool hasNextX = enumeratorX.MoveNext();
                        bool hasNextY = enumeratorY.MoveNext();

                        if (!hasNextX || !hasNextY)
                        {
                            return (hasNextX == hasNextY);
                        }

                        if (!Equals(enumeratorX.Current, enumeratorY.Current))
                        {
                            return false;
                        }
                    }
                }

                return object.Equals(x, y);
            }

            int IEqualityComparer<T>.GetHashCode(T obj)
            {
                throw new NotImplementedException();
            }

        }
        #endregion

        public static void AreEqual<T>(T expected, T actual, string message = null, IEqualityComparer<T> comparer = null)
        {
            if (ReferenceEquals(expected, actual))
            {
                return;
            }

            if (expected == null)
            {
                Fail("expected was null, but actual wasn't\r\n" + message);
            }
            else if (actual == null)
            {
                Fail("actual was null, but expected wasn't\r\n" + message);
            }
            else
            {
                if (!(comparer != null ? 
                    comparer.Equals(expected, actual) :
                    AssertEqualityComparer<T>.Equals(expected, actual)))
                {
                    Fail("Expected and actual were different.\r\n" +
                         "Expected: " + expected + "\r\n" +
                         "Actual:   " + actual + "\r\n" +
                         message);
                }
            }
        }

        public static void Equal<T>(ImmutableArray<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null)
        {
            if (actual == null || expected.IsDefault)
            {
                Assert.True((actual == null) == expected.IsDefault, message);
            }
            else
            {
                Equal((IEnumerable<T>)expected, actual, comparer, message);
            }
        }

        public static void Equal<T>(IEnumerable<T> expected, ImmutableArray<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = ", ")
        {
            if (expected == null || actual.IsDefault)
            {
                Assert.True((expected == null) == actual.IsDefault, message);
            }
            else
            {
                Equal(expected, (IEnumerable<T>)actual, comparer, message, itemSeparator);
            }
        }

        public static void Equal<T>(ImmutableArray<T> expected, ImmutableArray<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = ", ")
        {
            Equal((IEnumerable<T>)expected, (IEnumerable<T>)actual, comparer, message, itemSeparator);
        }

        public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null, 
            string itemSeparator = ",\r\n", Func<T, string> itemInspector = null)
        {
            if (ReferenceEquals(expected, actual))
            {
                return;
            }

            if (expected == null)
            {
                Fail("expected was null, but actual wasn't\r\n" + message);
            }
            else if (actual == null)
            {
                Fail("actual was null, but expected wasn't\r\n" + message);
            }
            else
            {
                if (!SequenceEqual(expected, actual, comparer))
                {
                    string assertMessage = GetAssertMessage(expected, actual, comparer, itemInspector, itemSeparator);

                    if (message != null)
                    {
                        assertMessage = message + "\r\n" + assertMessage;
                    }

                    Assert.True(false, assertMessage);
                }
            }
        }

        private static bool SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null)
        {
            var enumerator1 = expected.GetEnumerator();
            var enumerator2 = actual.GetEnumerator();

            while (true)
            {
                var hasNext1 = enumerator1.MoveNext();
                var hasNext2 = enumerator2.MoveNext();

                if (hasNext1 != hasNext2)
                {
                    return false;
                }

                if (!hasNext1)
                {
                    break;
                }

                var value1 = enumerator1.Current;
                var value2 = enumerator2.Current;

                if (!(comparer != null ? comparer.Equals(value1, value2) : AssertEqualityComparer<T>.Equals(value1, value2)))
                {
                    return false;
                }
            }

            return true;
        }

        public static void SetEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = ", ")
        {
            var expectedSet = new HashSet<T>(expected, comparer);
            var result = expected.Count() == actual.Count() && expectedSet.SetEquals(actual);
            if (!result)
            {
                if (string.IsNullOrEmpty(message))
                {
                    message = GetAssertMessage(
                        ToString(expected, itemSeparator),
                        ToString(actual, itemSeparator));
                }

                Assert.True(result, message);
            }
        }

        public static void SetEqual<T>(IEnumerable<T> actual, params T[] expected)
        {
            var expectedSet = new HashSet<T>(expected);
            Assert.True(expectedSet.SetEquals(actual), string.Format("Expected: {0}\nActual: {1}", ToString(expected), ToString(actual)));
        }

        public static void None<T>(IEnumerable<T> actual, Func<T, bool> predicate)
        {
            var none = !actual.Any(predicate);
            if (!none)
            {
                Assert.True(none, string.Format(
                    "Unexpected item found among existing items: {0}\nExisting items: {1}",
                    ToString(actual.First(predicate)),
                    ToString(actual)));
            }
        }

        public static void Any<T>(IEnumerable<T> actual, Func<T, bool> predicate)
        {
            var any = actual.Any(predicate);
            Assert.True(any, string.Format("No expected item was found.\nExisting items: {0}", ToString(actual)));
        }

        public static void All<T>(IEnumerable<T> actual, Func<T, bool> predicate)
        {
            var all = actual.All(predicate);
            if (!all)
            {
                Assert.True(all, string.Format(
                    "Not all items satisfy condition:\n{0}",
                    ToString(actual.Where(i => !predicate(i)))));
            }
        }

        public static string ToString(object o)
        {
            return Convert.ToString(o);
        }

        public static string ToString<T>(IEnumerable<T> list, string separator = ", ", Func<T, string> itemInspector = null)
        {
            if (itemInspector == null)
            {
                itemInspector = i => Convert.ToString(i);
            }

            return string.Join(separator, list.Select(itemInspector));
        }

        public static void Fail(string message)
        {
            Assert.False(true, message);
        }

        public static void Fail(string format, params object[] args)
        {
            Assert.False(true, String.Format(format, args));
        }

        public static void Null<T>(T @object, string message = null)
        {
            Assert.True(AssertEqualityComparer<T>.IsNull(@object), message);
        }

        public static void NotNull<T>(T @object, string message = null)
        {
            Assert.False(AssertEqualityComparer<T>.IsNull(@object), message);
        }

        public static void ThrowsArgumentNull(string parameterName, Action del)
        {
            try
            {
                del();
            }
            catch (ArgumentNullException e)
            {
                Assert.Equal(parameterName, e.ParamName);
            }
        }

        public static void ThrowsArgumentException(string parameterName, Action del)
        {
            try
            {
                del();
            }
            catch (ArgumentException e)
            {
                Assert.Equal(parameterName, e.ParamName);
            }
        }

        public static void Throws<T>(Action del, bool allowDerived = false)
        {
            try
            {
                del();
            }
            catch (Exception ex)
            {
                var type = ex.GetType();
                if (type.Equals(typeof(T)))
                {
                    // We got exactly the type we wanted
                    return;
                }

                if (allowDerived && typeof(T).IsAssignableFrom(type))
                {
                    // We got a derived type
                    return;
                }

                // We got some other type. We know that type != typeof(T), and so we'll use Assert.Equal since Xunit
                // will give a nice Expected/Actual output for this
                Assert.Equal(typeof(T), type);
            }

            Assert.True(false, "No exception was thrown.");
        }

        public static void AssertEqualToleratingWhitespaceDifferences(string expected, string actual, bool escapeQuotes = false)
        {
            expected = Normalize(expected);
            actual = Normalize(actual);
            Assert.True(expected == actual, GetAssertMessage(expected, actual, escapeQuotes));
        }

        public static void AssertContainsToleratingWhitespaceDifferences(string expectedSubString, string actualString)
        {
            expectedSubString = Normalize(expectedSubString);
            actualString = Normalize(actualString);
            Assert.Contains(expectedSubString, actualString);
        }

        private static string Normalize(string input)
        {
            var output = new StringBuilder();
            var inputLines = input.Split('\n', '\r');
            foreach (var line in inputLines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length > 0)
                {
                    if (!(trimmedLine.StartsWith("{") || trimmedLine.StartsWith("}")))
                    {
                        output.Append("  ");
                    }

                    output.AppendLine(trimmedLine);
                }
            }

            return output.ToString();
        }

        public static string GetAssertMessage(string expected, string actual, bool escapeQuotes = false)
        {
            return GetAssertMessage(DiffUtil.Lines(expected), DiffUtil.Lines(actual), escapeQuotes);
        }

        public static string GetAssertMessage<T>(IEnumerable<T> expected, IEnumerable<T> actual, bool escapeQuotes)
        {
            return GetAssertMessage(expected, actual, toString: escapeQuotes ? new Func<T, string>(t => t.ToString().Replace("\"", "\"\"")) : null, separator: "\r\n");
        }

        public static string GetAssertMessage<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, Func<T, string> toString = null, string separator = ",\r\n")
        {
            if (toString == null)
            {
                toString = new Func<T, string>(obj => obj.ToString());
            }

            var message = new StringBuilder();
            message.AppendLine();
            message.AppendLine("Actual:");
            message.AppendLine(string.Join(separator, actual.Select(toString)));
            message.AppendLine("Differences:");
            message.AppendLine(DiffUtil.DiffReport(expected, actual, comparer, toString, separator));
            return message.ToString();
        }
    }
}
