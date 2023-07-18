// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.InlineHints
{
    /// <summary>
    /// Used as a tiebreaker to position coincident type and parameter hints.
    /// </summary>
    internal static class InlineHintsConstants
    {
        /// <summary>
        /// Parameter hints will always appear first.
        /// </summary>
        public static readonly double ParameterRanking;

        /// <summary>
        /// Type hints will always appear second.
        /// </summary>
        public static readonly double TypeRanking;

        static InlineHintsConstants()
        {
            ParameterRanking = 0.0;
            TypeRanking = 1.0;
        }
    }
}
