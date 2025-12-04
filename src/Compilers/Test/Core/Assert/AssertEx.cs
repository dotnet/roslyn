// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Assert style type to deal with the lack of features in xUnit's Assert type
    /// </summary>
    public static class AssertEx
    {
        private static readonly IChunker s_lineChunker = new LineChunker();
        private static readonly IChunker s_lineEndingsPreservingChunker = new LineEndingsPreservingChunker();
        private static readonly InlineDiffBuilder s_diffBuilder = new InlineDiffBuilder(new Differ());

        #region AssertEqualityComparer<T>

        private class AssertEqualityComparer<T> : IEqualityComparer<T>
        {
            public static readonly IEqualityComparer<T> Instance = new AssertEqualityComparer<T>();

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
                return Instance.Equals(left, right);
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

                if (x is IEquatable<T> equatable)
                {
                    return equatable.Equals(y);
                }

                if (x is IComparable<T> comparableT)
                {
                    return comparableT.CompareTo(y) == 0;
                }

                if (x is IComparable comparable)
                {
                    return comparable.CompareTo(y) == 0;
                }

                if (x is IEnumerable enumerableX && y is IEnumerable enumerableY)
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
                Fail("expected was null, but actual wasn't" + Environment.NewLine + message);
            }
            else if (actual == null)
            {
                Fail("actual was null, but expected wasn't" + Environment.NewLine + message);
            }
            else if (!(comparer ?? AssertEqualityComparer<T>.Instance).Equals(expected, actual))
            {
                string expectedAndActual;
                if (expected is IEnumerable expectedEnumerable && actual is IEnumerable actualEnumerable)
                {
                    expectedAndActual = GetAssertMessage(expectedEnumerable.OfType<object>(), actualEnumerable.OfType<object>(), comparer: null);
                }
                else
                {
                    expectedAndActual = $"""
                        Expected:
                        {expected}
                        Actual:
                        {actual}
                        """;
                }

                Fail(message + Environment.NewLine + expectedAndActual);
            }
        }

        public static void Equal<T>(ReadOnlySpan<T> expected, T[] actual) =>
            Equal<T>(expected.ToArray(), actual);

        public static void Equal<T>(ImmutableArray<T> expected, IEnumerable<T> actual)
            => Equal(expected, actual, comparer: null, message: null);

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

        public static void Equal<T>(IEnumerable<T> expected, ImmutableArray<T> actual)
            => Equal(expected, actual, comparer: null, message: null, itemInspector: null);

        public static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = null)
        {
            if (expected == null || actual == null)
            {
                Assert.True(expected is null == actual is null, message);
            }
            else
            {
                Equal(expected, actual, comparer, message, itemSeparator);
            }
        }

        public static void Equal<T>(ImmutableArray<T> expected, ImmutableArray<T> actual)
            => Equal(expected, actual, comparer: null, message: null, itemInspector: null);

        public static void Equal<T>(ImmutableArray<T> expected, ImmutableArray<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = null)
        {
            Equal(expected, (IEnumerable<T>)actual, comparer, message, itemSeparator);
        }

        public static void Equal(string expected, string actual)
        {
            if (string.Equals(expected, actual, StringComparison.Ordinal))
            {
                return;
            }

            var message = new StringBuilder();
            message.AppendLine();
            message.AppendLine("Expected:");
            message.AppendLine(expected);
            message.AppendLine("Actual:");
            message.AppendLine(actual);

            Assert.True(false, message.ToString());
        }

        public static void Equal<T>(
            IEnumerable<T> expected,
            IEnumerable<T> actual,
            IEqualityComparer<T> comparer = null,
            string message = null,
            string itemSeparator = null,
            Func<T, string> itemInspector = null,
            string expectedValueSourcePath = null,
            int expectedValueSourceLine = 0)
        {
            if (expected == null)
            {
                Assert.Null(actual);
            }
            else
            {
                Assert.NotNull(actual);
            }

            if (SequenceEqual(expected, actual, comparer))
            {
                return;
            }

            Assert.True(false, GetAssertMessage(expected, actual, comparer, message, itemInspector, itemSeparator, expectedValueSourcePath, expectedValueSourceLine));
        }

        public static void Equal<T>(
            ReadOnlySpan<T> expected,
            ReadOnlySpan<T> actual,
            IEqualityComparer<T> comparer = null,
            string message = null,
            string itemSeparator = null,
            Func<T, string> itemInspector = null,
            string expectedValueSourcePath = null,
            int expectedValueSourceLine = 0)
        {
            if (SequenceEqual(expected, actual, comparer))
                return;

            Assert.True(false, GetAssertMessage(expected, actual, comparer, message, itemInspector, itemSeparator, expectedValueSourcePath, expectedValueSourceLine));
        }

        /// <summary>
        /// Asserts that two strings are equal, and prints a diff between the two if they are not.
        /// </summary>
        /// <param name="expected">The expected string. This is presented as the "baseline/before" side in the diff.</param>
        /// <param name="actual">The actual string. This is presented as the changed or "after" side in the diff.</param>
        /// <param name="message">The message to precede the diff, if the values are not equal.</param>
        public static void EqualOrDiff(string expected, string actual, string message = null)
        {
            if (expected == actual)
            {
                return;
            }

            var diff = s_diffBuilder.BuildDiffModel(expected, actual, ignoreWhitespace: false, ignoreCase: false, s_lineChunker);
            var messageBuilder = new StringBuilder();
            messageBuilder.AppendLine(
                string.IsNullOrEmpty(message)
                    ? "Actual and expected values differ. Expected shown in baseline of diff:"
                    : message);

            if (!diff.Lines.Any(line => line.Type == ChangeType.Inserted || line.Type == ChangeType.Deleted))
            {
                // We have a failure only caused by line ending differences; recalculate with line endings visible
                diff = s_diffBuilder.BuildDiffModel(expected, actual, ignoreWhitespace: false, ignoreCase: false, s_lineEndingsPreservingChunker);
            }

            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        messageBuilder.Append('+');
                        break;
                    case ChangeType.Deleted:
                        messageBuilder.Append('-');
                        break;
                    default:
                        messageBuilder.Append(' ');
                        break;
                }

                messageBuilder.AppendLine(line.Text.Replace("\r", "<CR>").Replace("\n", "<LF>"));
            }

            Assert.True(false, messageBuilder.ToString());
        }

        public static void NotEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null)
        {
            if (ReferenceEquals(expected, actual))
            {
                Fail("expected and actual references are identical\r\n" + message);
            }

            if (expected == null || actual == null)
            {
                return;
            }
            else if (SequenceEqual(expected, actual, comparer))
            {
                Fail("expected and actual sequences match\r\n" + message);
            }
        }

        private static bool SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null)
        {
            if (ReferenceEquals(expected, actual))
            {
                return true;
            }

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

        private static bool SequenceEqual<T>(ReadOnlySpan<T> expected, ReadOnlySpan<T> actual, IEqualityComparer<T> comparer = null)
        {
            if (expected.Length != actual.Length)
                return false;

            for (int i = 0; i < expected.Length; i++)
            {
                if (!(comparer is not null ? comparer.Equals(expected[i], actual[i]) : AssertEqualityComparer<T>.Equals(expected[i], actual[i])))
                {
                    return false;
                }
            }

            return true;
        }

        public static void SetEqual(IEnumerable<string> expected, IEnumerable<string> actual, IEqualityComparer<string> comparer = null, string message = null, string itemSeparator = "\r\n", Func<string, string> itemInspector = null)
        {
            var indexes = new Dictionary<string, int>(comparer);
            int counter = 0;
            foreach (var expectedItem in expected)
            {
                if (!indexes.ContainsKey(expectedItem))
                {
                    indexes.Add(expectedItem, counter++);
                }
            }

            SetEqual<string>(expected, actual.OrderBy(e => getIndex(e)), comparer, message, itemSeparator, itemInspector);

            int getIndex(string item)
            {
                // exact match to expected items
                if (indexes.TryGetValue(item, out var index))
                {
                    return index;
                }

                // closest match to expected items
                int closestDistance = int.MaxValue;
                string closestItem = null;
                foreach (var expectedItem in indexes.Keys)
                {
                    var distance = levenshtein(item, expectedItem);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestItem = expectedItem;
                    }
                }

                if (closestItem != null)
                {
                    _ = indexes.TryGetValue(closestItem, out index);
                    return index;
                }

                return -1;
            }

            // Adapted from Toub's https://blogs.msdn.microsoft.com/toub/2006/05/05/generic-levenshtein-edit-distance-with-c/
            int levenshtein(string first, string second)
            {
                // Get the length of both.  If either is 0, return
                // the length of the other, since that number of insertions
                // would be required.
                int n = first.Length, m = second.Length;
                if (n == 0)
                    return m;
                if (m == 0)
                    return n;

                // Rather than maintain an entire matrix (which would require O(n*m) space),
                // just store the current row and the next row, each of which has a length m+1,
                // so just O(m) space. Initialize the current row.
                int curRow = 0, nextRow = 1;
                int[][] rows = new int[][] { new int[m + 1], new int[m + 1] };
                for (int j = 0; j <= m; ++j)
                    rows[curRow][j] = j;

                // For each virtual row (since we only have physical storage for two)
                for (int i = 1; i <= n; ++i)
                {
                    // Fill in the values in the row
                    rows[nextRow][0] = i;
                    for (int j = 1; j <= m; ++j)
                    {
                        int dist1 = rows[curRow][j] + 1;
                        int dist2 = rows[nextRow][j - 1] + 1;
                        int dist3 = rows[curRow][j - 1] + (first[i - 1].Equals(second[j - 1]) ? 0 : 1);
                        rows[nextRow][j] = Math.Min(dist1, Math.Min(dist2, dist3));
                    }

                    // Swap the current and next rows
                    if (curRow == 0)
                    {
                        curRow = 1;
                        nextRow = 0;
                    }
                    else
                    {
                        curRow = 0;
                        nextRow = 1;
                    }
                }

                // Return the computed edit distance
                return rows[curRow][m];
            }
        }

        public static void SetEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, IEqualityComparer<T> comparer = null, string message = null, string itemSeparator = "\r\n", Func<T, string> itemInspector = null)
        {
            var expectedSet = new HashSet<T>(expected, comparer);
            var result = expected.Count() == actual.Count() && expectedSet.SetEquals(actual);
            if (!result)
            {
                Assert.True(result, GetAssertMessage(
                    ToString(expected, itemSeparator, itemInspector),
                    ToString(actual, itemSeparator, itemInspector),
                    prefix: message));
            }
        }

        public static void SetEqual<T>(T[] expected, T[] actual)
            => SetEqual((IEnumerable<T>)actual, expected);

        public static void SetEqual<T>(IEnumerable<T> actual, params T[] expected)
        {
            var expectedSet = new HashSet<T>(expected);
            if (!expectedSet.SetEquals(actual))
            {
                var message = GetAssertMessage(ToString(expected, ",\r\n", itemInspector: withQuotes), ToString(actual, ",\r\n", itemInspector: withQuotes));
                Assert.True(false, message);
            }

            string withQuotes(T t) => $"\"{Convert.ToString(t)}\"";
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
            throw new Xunit.Sdk.XunitException(message);
        }

        public static void Fail(string format, params object[] args)
        {
            throw new Xunit.Sdk.XunitException(string.Format(format, args));
        }

        public static void NotNull<T>([NotNull] T @object, string message = null)
        {
            Assert.False(AssertEqualityComparer<T>.IsNull(@object), message);
        }

        // compares against a baseline
        public static void AssertEqualToleratingWhitespaceDifferences(
            string expected,
            string actual,
            string message = null,
            bool escapeQuotes = false,
            [CallerFilePath] string expectedValueSourcePath = null,
            [CallerLineNumber] int expectedValueSourceLine = 0)
        {
            var normalizedExpected = NormalizeWhitespace(expected);
            var normalizedActual = NormalizeWhitespace(actual);

            if (normalizedExpected != normalizedActual)
            {
                Assert.True(false, GetAssertMessage(expected, actual, message, escapeQuotes, expectedValueSourcePath, expectedValueSourceLine));
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

        public static void AssertStartsWithToleratingWhitespaceDifferences(string expectedSubString, string actualString)
        {
            expectedSubString = NormalizeWhitespace(expectedSubString);
            actualString = NormalizeWhitespace(actualString);
            Assert.StartsWith(expectedSubString, actualString, StringComparison.Ordinal);
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

        public static string GetAssertMessage(string expected, string actual, string prefix = null, bool escapeQuotes = false, string expectedValueSourcePath = null, int expectedValueSourceLine = 0)
            => GetAssertMessage(DiffUtil.Lines(expected), DiffUtil.Lines(actual), prefix, escapeQuotes, expectedValueSourcePath, expectedValueSourceLine);

        public static string GetAssertMessage<T>(IEnumerable<T> expected, IEnumerable<T> actual, string prefix = null, bool escapeQuotes = false, string expectedValueSourcePath = null, int expectedValueSourceLine = 0)
        {
            Func<T, string> itemInspector = escapeQuotes ? new Func<T, string>(t => t.ToString().Replace("\"", "\"\"")) : null;
            return GetAssertMessage(expected, actual, prefix: prefix, itemInspector: itemInspector, itemSeparator: "\r\n", expectedValueSourcePath: expectedValueSourcePath, expectedValueSourceLine: expectedValueSourceLine);
        }

        private static readonly string s_diffToolPath = Environment.GetEnvironmentVariable("ROSLYN_DIFFTOOL");

        public static string GetAssertMessage<T>(
            IEnumerable<T> expected,
            IEnumerable<T> actual,
            IEqualityComparer<T> comparer = null,
            string prefix = null,
            Func<T, string> itemInspector = null,
            string itemSeparator = null,
            string expectedValueSourcePath = null,
            int expectedValueSourceLine = 0)
        {
            if (itemInspector == null)
            {
                if (typeof(T) == typeof(byte))
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
                if (typeof(T) == typeof(byte))
                {
                    itemSeparator = ", ";
                }
                else
                {
                    itemSeparator = "," + Environment.NewLine;
                }
            }

            var expectedString = string.Join(itemSeparator, expected.Take(10).Select(itemInspector));
            var actualString = string.Join(itemSeparator, actual.Select(itemInspector));
            var diffString = DiffUtil.DiffReport(expected, actual, itemSeparator, comparer, itemInspector);

            if (DifferOnlyInWhitespace(expectedString, actualString))
            {
                expectedString = VisualizeWhitespace(expectedString);
                actualString = VisualizeWhitespace(actualString);
                diffString = VisualizeWhitespace(diffString);
            }

            var message = new StringBuilder();

            if (!string.IsNullOrEmpty(prefix))
            {
                message.AppendLine(prefix);
                message.AppendLine();
            }

            message.AppendLine("Expected:");
            message.AppendLine(expectedString);
            if (expected.Count() is > 10 and var count)
            {
                message.AppendLine($"... truncated {count - 10} lines ...");
            }

            message.AppendLine("Actual:");
            message.AppendLine(actualString);
            message.AppendLine("Differences:");
            message.AppendLine(diffString);

            if (TryGenerateExpectedSourceFileAndGetDiffLink(actualString, expected.Count(), expectedValueSourcePath, expectedValueSourceLine, out var link))
            {
                message.AppendLine(link);
            }

            return message.ToString();
        }

        private static bool DifferOnlyInWhitespace(IEnumerable<char> expected, IEnumerable<char> actual)
            => expected.Where(c => !char.IsWhiteSpace(c)).SequenceEqual(actual.Where(c => !char.IsWhiteSpace(c)));

        private static string VisualizeWhitespace(string str)
        {
            var result = new StringBuilder(str.Length);

            var i = 0;
            while (i < str.Length)
            {
                var c = str[i++];
                if (c == '\r' && i < str.Length && str[i] == '\n')
                {
                    result.Append("␍␊\r\n");
                    i++;
                }
                else
                {
                    result.Append(c switch
                    {
                        ' ' => "·",
                        '\t' => "→",
                        '\r' => "␍\r",
                        '\n' => "␊\n",
                        _ => c,
                    });
                }
            }

            return result.ToString();
        }

        public static string GetAssertMessage<T>(
            ReadOnlySpan<T> expected,
            ReadOnlySpan<T> actual,
            IEqualityComparer<T> comparer = null,
            string prefix = null,
            Func<T, string> itemInspector = null,
            string itemSeparator = null,
            string expectedValueSourcePath = null,
            int expectedValueSourceLine = 0)
        {
            if (itemInspector == null)
            {
                if (typeof(T) == typeof(byte))
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
                if (typeof(T) == typeof(byte))
                {
                    itemSeparator = ", ";
                }
                else
                {
                    itemSeparator = "," + Environment.NewLine;
                }
            }

            const int maxDisplayedExpectedEntries = 10;
            var expectedString = join(itemSeparator, expected[..Math.Min(expected.Length, maxDisplayedExpectedEntries)], itemInspector);
            var actualString = join(itemSeparator, actual, itemInspector);

            var message = new StringBuilder();

            if (!string.IsNullOrEmpty(prefix))
            {
                message.AppendLine(prefix);
                message.AppendLine();
            }

            message.AppendLine("Expected:");
            message.AppendLine(expectedString);
            if (expected.Length > maxDisplayedExpectedEntries)
            {
                message.AppendLine("... truncated ...");
            }

            message.AppendLine("Actual:");
            message.AppendLine(actualString);
            message.AppendLine("Differences:");
            message.AppendLine(DiffUtil.DiffReport(expected.ToArray(), actual.ToArray(), itemSeparator, comparer, itemInspector));

            if (TryGenerateExpectedSourceFileAndGetDiffLink(actualString, expected.Length, expectedValueSourcePath, expectedValueSourceLine, out var link))
            {
                message.AppendLine(link);
            }

            return message.ToString();

            static string join(string itemSeparator, ReadOnlySpan<T> items, Func<T, string> itemInspector)
            {
                var result = new StringBuilder();
                var iter = items.GetEnumerator();

                if (iter.MoveNext())
                    result.Append(itemInspector(iter.Current));

                while (iter.MoveNext())
                    result.Append($"{itemSeparator}{itemInspector(iter.Current)}");

                return result.ToString();
            }
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

        private sealed class LineComparer : IEqualityComparer<string>
        {
            public static readonly LineComparer Instance = new LineComparer();

            public bool Equals(string left, string right) => left.Trim() == right.Trim();
            public int GetHashCode(string str) => str.Trim().GetHashCode();
        }

        private static IEnumerable<string> GetLines(string str) =>
                str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        public static void AssertLinesEqual(string expected, string actual)
        {
            AssertEx.Equal(
                GetLines(expected),
                GetLines(actual),
                comparer: LineComparer.Instance);
        }

        public static void AssertLinesEqual(string expected, string actual, string message, string expectedValueSourcePath, int expectedValueSourceLine, bool escapeQuotes)
        {
            AssertEx.Equal(
                GetLines(expected),
                GetLines(actual),
                comparer: LineComparer.Instance,
                message: message,
                itemInspector: escapeQuotes ? new Func<string, string>(line => line.Replace("\"", "\"\"")) : null,
                itemSeparator: Environment.NewLine,
                expectedValueSourcePath: expectedValueSourcePath,
                expectedValueSourceLine: expectedValueSourceLine);
        }

        public static void Equal(bool[,] expected, Func<int, int, bool> getResult, int size)
        {
            Equal<bool>(expected, getResult, (b1, b2) => b1 == b2, b => b ? "true" : "false", "{0,-6:G}", size);
        }

        public static void Equal<T>(T[,] expected, Func<int, int, T> getResult, Func<T, T, bool> valuesEqual, Func<T, string> printValue, string format, int size)
        {
            bool mismatch = false;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (!valuesEqual(expected[i, j], getResult(i, j)))
                    {
                        mismatch = true;
                    }
                }
            }

            if (mismatch)
            {
                var builder = new StringBuilder();
                builder.AppendLine("Actual result: ");
                for (int i = 0; i < size; i++)
                {
                    builder.Append("{ ");
                    for (int j = 0; j < size; j++)
                    {
                        string resultWithComma = printValue(getResult(i, j));
                        if (j < size - 1)
                        {
                            resultWithComma += ",";
                        }

                        builder.Append(string.Format(format, resultWithComma));
                        if (j < size - 1)
                        {
                            builder.Append(' ');
                        }
                    }
                    builder.AppendLine("},");
                }

                Assert.True(false, builder.ToString());
            }
        }

        /// <summary>
        /// Run multiple assertions at once and collect the result.
        /// This is useful when you want to verify multiple assertions but don't want to re-run to adjust every other case.
        /// </summary>
        public static void Multiple(params Action[] assertions)
        {
            Multiple(includeStackTrace: false, assertions);
        }

        /// <inheritdoc cref="Multiple(System.Action[])"/>
        public static void Multiple(bool includeStackTrace, params Action[] assertions)
        {
            List<(int, Exception)> exceptions = null;

            // Run assertions in reverse order so that line numbers don't change as we adjust the baseline.
            for (int index = assertions.Length - 1; index >= 0; --index)
            {
                try
                {
                    assertions[index]();
                }
                catch (Exception ex)
                {
                    (exceptions ??= new()).Add((index, ex));
                }
            }

            if (exceptions is null)
                return;

            var stringBuilder = new StringBuilder()
                .AppendLine($"{exceptions.Count} out of {assertions.Length} assertions failed.")
                .AppendLine();
            foreach (var (index, ex) in exceptions)
            {
                var stack = ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                stringBuilder
                    .AppendLine($"Assertion failed at index {index}:")
                    .AppendLine(stack[^2]) // Prints the failing line in the original test case.
                    .AppendLine(ex.Message);
                if (includeStackTrace)
                    stringBuilder.AppendLine(ex.StackTrace);
                stringBuilder
                    .AppendLine()
                    .AppendLine();
            }

            Fail(stringBuilder.ToString());
        }

#nullable enable
        public static void NotNull<T>([NotNull] T value)
        {
            Assert.NotNull(value);
            Debug.Assert(value is object);
        }

        public static void Contains<T>(IEnumerable<T> collection, Predicate<T> filter, Func<T, string>? itemInspector = null, string? itemSeparator = null)
        {
            foreach (var item in collection)
            {
                if (filter(item))
                {
                    return;
                }
            }

            Fail("Filter does not match any item in the collection: " + Environment.NewLine +
                ToString(collection, itemSeparator ?? Environment.NewLine, itemInspector));
        }

#nullable enable

        /// <summary>
        /// The xunit Assert.Equal method is not callable in Visual Basic  due to the presence of
        /// the unmanaged constraint. Need to indirect through C# here until we resolve this.
        ///
        /// https://github.com/dotnet/roslyn/issues/75063
        /// </summary>
        public static void Equal<T>(T[] expected, T[] actual) =>
            Assert.Equal<T>(expected, actual);

        /// <summary>
        /// The xunit Assert.Equal method is not callable in Visual Basic  due to the presence of
        /// the unmanaged constraint. Need to indirect through C# here until we resolve this.
        ///
        /// https://github.com/dotnet/roslyn/issues/75063
        /// </summary>
        public static void Equal<T>(T expected, T actual) =>
            Assert.Equal<T>(expected, actual);

        /// <summary>
        /// The xunit Assert.NotEqual method is not callable in Visual Basic  due to the presence of
        /// the unmanaged constraint. Need to indirect through C# here until we resolve this.
        ///
        /// https://github.com/dotnet/roslyn/issues/75063
        /// </summary>
        public static void NotEqual<T>(T expected, T actual) =>
            Assert.NotEqual<T>(expected, actual);

        /// <summary>
        /// This assert passes if the collection is not null and empty
        /// </summary>
        /// <remarks>
        /// The core <see cref="Xunit.Assert.Empty(IEnumerable)"/> is annotated to not accept null but many 
        /// of our call sites pass a potentially nullable value.
        /// </remarks>
        public static void AssertEmpty(IEnumerable? collection)
        {
            Assert.NotNull(collection);
            Assert.Empty(collection);
        }

        /// <summary>
        /// This assert passes if the collection is not null and has a single item.
        /// </summary>
        /// <remarks>
        /// The core <see cref="Xunit.Assert.Single{T}(IEnumerable{T})"/> is annotated to not accept null but many 
        /// of our call sites pass a potentially nullable value.
        /// </remarks>
        public static T Single<T>(IEnumerable<T>? collection)
        {
            Assert.NotNull(collection);
            return Assert.Single(collection);
        }

        /// <summary>
        /// Verify the collection is not null and all the items pass the action.
        /// </summary>
        /// <remarks>
        /// The core <see cref="Xunit.Assert.All{T}(IEnumerable{T}, Action{T})"/> is annotated to not accept null but many 
        /// of our call sites pass a potentially nullable value.
        /// </remarks>
        public static void All<T>(IEnumerable<T>? collection, Action<T> action)
        {
            Assert.NotNull(collection);
            Assert.All(collection, action);
        }

#nullable disable
    }
}
