// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing;

internal static class SectionMatchExtensions
{
    extension(SectionMatch actualMatchKind)
    {
        public bool IsWorseMatchThan(SectionMatch expectedMatchKind)
        {
            var lowestMatch = (int)expectedMatchKind;
            var actualMatch = (int)actualMatchKind;
            return actualMatch > lowestMatch;
        }

        public bool IsWorseOrEqualMatchThan(SectionMatch expectedMatchKind)
        {
            var lowestMatch = (int)expectedMatchKind;
            var actualMatch = (int)actualMatchKind;
            return actualMatch >= lowestMatch;
        }

        public bool IsBetterMatchThan(SectionMatch expectedMatchKind)
        {
            var lowestMatch = (int)expectedMatchKind;
            var actualMatch = (int)actualMatchKind;
            return actualMatch < lowestMatch;
        }

        public bool IsBetterOrEqualMatchThan(SectionMatch expectedMatchKind)
        {
            var lowestMatch = (int)expectedMatchKind;
            var actualMatch = (int)actualMatchKind;
            return actualMatch <= lowestMatch;
        }
    }
}
