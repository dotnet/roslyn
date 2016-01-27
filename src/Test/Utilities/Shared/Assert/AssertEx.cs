// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
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
            private static readonly IEqualityComparer<T> s_instance = new AssertEqualityComparer<T>();

            private static bool CanBeNull()
            {
                var type = typeof(T);
                return !type.GetTypeInfo().IsValueType ||
                    (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
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
                return s_instance.Equals(left, right);
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
                            return hasNextX == hasNextY;
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

        public static void Equal<T>(IEnumerable<T> expected, ImmutableArray<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = null)
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

        public static void Equal<T>(ImmutableArray<T> expected, ImmutableArray<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = null)
        {
            Equal(expected, (IEnumerable<T>)actual, comparer, message, itemSeparator);
        }

        public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null,
            string itemSeparator = null, Func<T, string> itemInspector = null)
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
            else if (!SequenceEqual(expected, actual, comparer))
            {
                string assertMessage = GetAssertMessage(expected, actual, comparer, itemInspector, itemSeparator);

                if (message != null)
                {
                    assertMessage = message + "\r\n" + assertMessage;
                }

                Assert.True(false, assertMessage);
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

        public static void SetEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = "\r\n")
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
            Assert.False(true, string.Format(format, args));
        }

        public static void NotNull<T>(T @object, string message = null)
        {
            Assert.False(AssertEqualityComparer<T>.IsNull(@object), message);
        }

        // compares against a baseline
        public static void AssertEqualToleratingWhitespaceDifferences(
            string expected,
            string actual,
            bool escapeQuotes = true,
            [CallerFilePath]string expectedValueSourcePath = null,
            [CallerLineNumber]int expectedValueSourceLine = 0)
        {
            var normalizedExpected = NormalizeWhitespace(expected);
            var normalizedActual = NormalizeWhitespace(actual);

            if (normalizedExpected != normalizedActual)
            {
                Assert.True(false, GetAssertMessage(expected, actual, escapeQuotes, expectedValueSourcePath, expectedValueSourceLine));
            }
        }

        // compares two results (no baseline)
        public static void AssertResultsEqual(string result1, string result2)
        {
            if (result1 != result2)
            {
                string message;

                if (DiffToolAvailable)
                {
                    string file1 = Path.GetTempFileName();
                    File.WriteAllText(file1, result1);

                    string file2 = Path.GetTempFileName();
                    File.WriteAllText(file2, result2);

                    message = MakeDiffToolLink(file1, file2);
                }
                else
                {
                    message = GetAssertMessage(result1, result2);
                }

                Assert.True(false, message);
            }
        }

        public static void AssertContainsToleratingWhitespaceDifferences(string expectedSubString, string actualString)
        {
            expectedSubString = NormalizeWhitespace(expectedSubString);
            actualString = NormalizeWhitespace(actualString);
            Assert.Contains(expectedSubString, actualString, StringComparison.Ordinal);
        }

        internal static string NormalizeWhitespace(string input)
        {
            var output = new StringBuilder();
            var inputLines = input.Split('\n', '\r');
            foreach (var line in inputLines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length > 0)
                {
                    if (!(trimmedLine[0] == '{' || trimmedLine[0] == '}'))
                    {
                        output.Append("  ");
                    }

                    output.AppendLine(trimmedLine);
                }
            }

            return output.ToString();
        }

        public static string GetAssertMessage(string expected, string actual, bool escapeQuotes = false, string expectedValueSourcePath = null, int expectedValueSourceLine = 0)
        {
            return GetAssertMessage(DiffUtil.Lines(expected), DiffUtil.Lines(actual), escapeQuotes, expectedValueSourcePath, expectedValueSourceLine);
        }

        public static string GetAssertMessage<T>(IEnumerable<T> expected, IEnumerable<T> actual, bool escapeQuotes, string expectedValueSourcePath = null, int expectedValueSourceLine = 0)
        {
            Func<T, string> itemInspector = escapeQuotes ? new Func<T, string>(t => t.ToString().Replace("\"", "\"\"")) : null;
            return GetAssertMessage(expected, actual, itemInspector: itemInspector, itemSeparator: "\r\n", expectedValueSourcePath: expectedValueSourcePath, expectedValueSourceLine: expectedValueSourceLine);
        }

        private static readonly string s_diffToolPath = Environment.GetEnvironmentVariable("ROSLYN_DIFFTOOL");

        public static string GetAssertMessage<T>(
            IEnumerable<T> expected,
            IEnumerable<T> actual,
            IEqualityComparer<T> comparer = null,
            Func<T, string> itemInspector = null,
            string itemSeparator = null,
            string expectedValueSourcePath = null,
            int expectedValueSourceLine = 0)
        {
            if (itemInspector == null)
            {
                if (expected is IEnumerable<byte>)
                {
                    itemInspector = b => $"0x{b:X2}";
                }
                else
                {
                    itemInspector = new Func<T, string>(obj => (obj != null) ? obj.ToString() : "<null>");
                }
            }

            if (itemSeparator == null)
            {
                if (expected is IEnumerable<byte>)
                {
                    itemSeparator = ", ";
                }
                else
                {
                    itemSeparator = ",\r\n";
                }
            }

            var actualString = string.Join(itemSeparator, actual.Select(itemInspector));

            var message = new StringBuilder();
            message.AppendLine();
            message.AppendLine("Actual:");
            message.AppendLine(actualString);
            message.AppendLine("Differences:");
            message.AppendLine(DiffUtil.DiffReport(expected, actual, comparer, itemInspector, itemSeparator));

            string link;
            if (TryGenerateExpectedSourceFileAndGetDiffLink(actualString, expected.Count(), expectedValueSourcePath, expectedValueSourceLine, out link))
            {
                message.AppendLine(link);
            }

            return message.ToString();
        }

        internal static bool TryGenerateExpectedSourceFileAndGetDiffLink(string actualString, int expectedLineCount, string expectedValueSourcePath, int expectedValueSourceLine, out string link)
        {
            // add a link to a .cmd file that opens a diff tool:
            if (DiffToolAvailable && expectedValueSourcePath != null && expectedValueSourceLine != 0)
            {
                var actualFile = Path.GetTempFileName();
                var testFileLines = File.ReadAllLines(expectedValueSourcePath);

                File.WriteAllLines(actualFile, testFileLines.Take(expectedValueSourceLine));
                File.AppendAllText(actualFile, actualString);
                File.AppendAllLines(actualFile, testFileLines.Skip(expectedValueSourceLine + expectedLineCount));

                link = MakeDiffToolLink(actualFile, expectedValueSourcePath);

                return true;
            }

            link = null;
            return false;
        }

        internal static bool DiffToolAvailable => !string.IsNullOrEmpty(s_diffToolPath);

        internal static string MakeDiffToolLink(string actualFilePath, string expectedFilePath)
        {
            var compareCmd = Path.GetTempFileName() + ".cmd";
            File.WriteAllText(compareCmd, string.Format("\"{0}\" \"{1}\" \"{2}\"", s_diffToolPath, actualFilePath, expectedFilePath));

            return "file://" + compareCmd;
        }

        public static void Empty<T>(IEnumerable<T> items, string message = "")
        {
            // realize the list in case it can't be traversed twice via .Count()/.Any() and .Select()
            var list = items.ToList();
            if (list.Count != 0)
            {
                Fail($"Expected 0 items but found {list.Count}: {message}\r\nItems:\r\n    {string.Join("\r\n    ", list)}");
            }
        }
    }
}
