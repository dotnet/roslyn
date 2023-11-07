// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    public class TestUtilities
    {
        public static void ThrowIfExpectedItemNotFound<TCollection>(IEnumerable<TCollection> actual, IEnumerable<TCollection> expected)
            where TCollection : IEquatable<TCollection>
        {
            var shouldThrow = false;
            var sb = new StringBuilder();
            sb.AppendLine("The following expected item(s) not found:");

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
                sb.AppendLine("Actual items:");
                foreach (var item in actual)
                    sb.AppendLine(item.ToString());

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
    }
}
