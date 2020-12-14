// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Build enforcement recommendation for a code style analyzer.
    /// </summary>
    internal enum EnforceOnBuild
    {
        Never,
        WhenExplicitlyEnabled,
        Recommended,
        HighlyRecommended,
    }

    internal static class EnforceOnBuildExtensions
    {
        public static string ToCustomTag(this EnforceOnBuild enforceOnBuild)
            => $"{nameof(EnforceOnBuild)}_{enforceOnBuild}";
    }
}
