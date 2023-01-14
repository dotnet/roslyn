// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing
{
    internal static class SectionMatchExtensions
    {
        public static bool IsWorseMatchThan(this SectionMatch actualMatchKind, SectionMatch expectedMatchKind)
        {
            var lowestMatch = (int)expectedMatchKind;
            var actualMatch = (int)actualMatchKind;
            return actualMatch > lowestMatch;
        }

        public static bool IsWorseOrEqualMatchThan(this SectionMatch actualMatchKind, SectionMatch expectedMatchKind)
        {
            var lowestMatch = (int)expectedMatchKind;
            var actualMatch = (int)actualMatchKind;
            return actualMatch >= lowestMatch;
        }

        public static bool IsBetterMatchThan(this SectionMatch actualMatchKind, SectionMatch expectedMatchKind)
        {
            var lowestMatch = (int)expectedMatchKind;
            var actualMatch = (int)actualMatchKind;
            return actualMatch < lowestMatch;
        }

        public static bool IsBetterOrEqualMatchThan(this SectionMatch actualMatchKind, SectionMatch expectedMatchKind)
        {
            var lowestMatch = (int)expectedMatchKind;
            var actualMatch = (int)actualMatchKind;
            return actualMatch <= lowestMatch;
        }
    }
}
