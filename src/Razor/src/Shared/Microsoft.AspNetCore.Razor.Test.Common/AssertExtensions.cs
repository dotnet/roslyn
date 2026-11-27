// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace Xunit;

internal static class AssertExtensions
{
    extension(Assert)
    {
        public static void SameItems<T>(IEnumerable<T>? expected, IEnumerable<T>? actual)
        {
            if (expected is null && actual is null)
            {
                return;
            }

            if (expected is null || actual is null)
            {
                Assert.Fail($"Expected: {expected?.ToString() ?? "null"}, Actual: {actual?.ToString() ?? "null"}");
                return;
            }

            var expectedArray = expected.ToArray();
            var actualArray = actual.ToArray();

            if (expectedArray.Length != actualArray.Length)
            {
                Assert.Fail($"Expected collection length: {expectedArray.Length}, Actual collection length: {actualArray.Length}");
                return;
            }

            for (var i = 0; i < expectedArray.Length; i++)
            {
                if (!ReferenceEquals(expectedArray[i], actualArray[i]))
                {
                    Assert.Fail($"Expected and actual collections differ at index {i}. Expected: {expectedArray[i]}, Actual: {actualArray[i]}");
                    return;
                }
            }
        }
    }
}
