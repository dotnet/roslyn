// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    internal static class Comparison
    {
        public static bool AreStringValuesEqual(string str1, string str2)
            => string.IsNullOrEmpty(str1) == string.IsNullOrEmpty(str2)
            || str1 == str2;

        public static bool AreArraysEqual<T>(T[] array1, T[] array2) where T : IEquatable<T>
        {
            if (array1.Length != array2.Length)
            {
                return false;
            }

            for (var i = 0; i < array1.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(array1[i], array2[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
