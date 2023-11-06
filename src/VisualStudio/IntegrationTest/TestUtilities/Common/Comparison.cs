// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    internal static class Comparison
    {
        public static bool AreStringValuesEqual(string? str1, string? str2)
            => (str1 ?? "") == (str2 ?? "");
    }
}
