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
        /// <summary>
        /// Indicates that the code style diagnostic is an IDE-only diagnostic that cannot be enforced on build.
        /// </summary>
        Never,

        /// <summary>
        /// Indicates that the code style diagnostic can be enforced on build when explicitly enabled in a configuration file,
        /// but is not part of the <see cref="Recommended"/> or <see cref="HighlyRecommended"/> group for build enforcement.
        /// <para>This is the suggested <b>P3</b> bucket of code style diagnostics to enforce on build.</para>
        /// </summary>
        WhenExplicitlyEnabled,

        /// <summary>
        /// Indicates that the code style diagnostic can be enforced on build and is part of the recommended group for build enforcement.
        /// <para>This is the suggested <b>P2</b> bucket of code style diagnostics to enforce on build.</para>
        /// </summary>
        Recommended,

        /// <summary>
        /// Indicates that the code style diagnostic can be enforced on build and is part of the highly recommended group for build enforcement.
        /// <para>This is the suggested <b>P1</b> bucket of code style diagnostics to enforce on build.</para>
        /// </summary>
        HighlyRecommended,
    }

    internal static class EnforceOnBuildExtensions
    {
        public static string ToCustomTag(this EnforceOnBuild enforceOnBuild)
            => $"{nameof(EnforceOnBuild)}_{enforceOnBuild}";
    }
}
