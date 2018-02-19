// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public class TestUtilities
    {
        public static void CompareAndThrowIfNotEqual<TItem>(TItem expected, TItem actual) where TItem : IEquatable<TItem>
        {
            if (!expected.Equals(actual))
            {
                throw new Exception("Actual and expected items don't match:" + Environment.NewLine +
                    "Expected: " + expected.ToString() + Environment.NewLine +
                    "Actual: " + actual.ToString() + Environment.NewLine);
            }
        }

        public static void ThrowIfExpectedItemNotFound<TCollection>(IEnumerable<TCollection> actual, IEnumerable<TCollection> expected)
            where TCollection : IEquatable<TCollection>
        {
            var shouldThrow = false;
            var sb = new StringBuilder();
            sb.Append("The following expected item(s) not found:\r\n");

            foreach (var item in expected)
            {
                if (!actual.Contains(item))
                {
                    shouldThrow = true;
                    sb.AppendLine(item.ToString());
                }
            }

            if (shouldThrow)
            {
                throw new Exception(sb.ToString());
            }
        }

        public static void ThrowIfExpectedItemNotFound<TCollection>(IEnumerable<TCollection> actual,
            IEnumerable<TCollection> expected,
            IEqualityComparer<TCollection> comparer)
            where TCollection : IEquatable<TCollection>
        {
            var shouldThrow = false;
            var sb = new StringBuilder();
            sb.Append("The following expected item(s) not found:\r\n");

            foreach (var item in expected)
            {
                if (!actual.Contains(item, comparer))
                {
                    shouldThrow = true;
                    sb.AppendLine(item.ToString());
                }
            }

            if (shouldThrow)
            {
                throw new Exception(sb.ToString());
            }
        }

        public static void ThrowIfExpectedItemNotFoundInOrder<TCollection>(IEnumerable<TCollection> actual, IEnumerable<TCollection> expected)
            where TCollection : IEquatable<TCollection>
        {
            var shouldThrow = false;
            var sb = new StringBuilder();
            sb.Append("The following expected item(s) not found in sequence:\r\n");

            var remainingActualList = actual;

            foreach (var item in expected)
            {
                remainingActualList = remainingActualList.SkipWhile(a => !a.Equals(item));

                if (!remainingActualList.Any())
                {
                    shouldThrow = true;
                    sb.AppendLine(item.ToString());
                }

                remainingActualList = remainingActualList.Skip(1);
            }

            if (shouldThrow)
            {
                sb.AppendLine();
                sb.AppendLine("Actual items:");
                foreach (var item in actual)
                {
                    sb.AppendLine(item.ToString());
                }

                throw new Exception(sb.ToString());
            }
        }

        public static void ThrowIfUnExpectedItemFound<TCollection>(IEnumerable<TCollection> actual, IEnumerable<TCollection> unexpected)
        {
            var shouldThrow = false;
            var sb = new StringBuilder();
            sb.Append("The following UN-expected item(s) were encountered:\r\n");

            foreach (var item in unexpected)
            {
                if (actual.Contains(item))
                {
                    shouldThrow = true;
                    sb.AppendLine(item.ToString());
                }
            }

            if (shouldThrow)
            {
                throw new Exception(sb.ToString());
            }
        }

        public static void CompareAsSequenceAndThrowIfNotEqual<TListItem>(IEnumerable<TListItem> expectedList,
            IEnumerable<TListItem> actualList,
            IEqualityComparer<TListItem> comparer = null)
            where TListItem : IEquatable<TListItem>
        {
            if (!expectedList.SequenceEqual(actualList, comparer))
            {
                throw new Exception(string.Format("Expected list:\n{0}\nActual list:\n{1}", BuildString(expectedList), BuildString(actualList)));
            }
        }

        private static string BuildString<TElement>(IEnumerable<TElement> list)
            => string.Join(Environment.NewLine, list.Select(item => item.ToString()).ToArray());
    }
}
