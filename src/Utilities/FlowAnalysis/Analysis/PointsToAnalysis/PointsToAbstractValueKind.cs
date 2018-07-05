// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Kind for the <see cref="PointsToAbstractValue"/>.
    /// </summary>
    internal enum PointsToAbstractValueKind
    {
        /// <summary>
        /// Invalid value based on predicate analysis.
        /// </summary>
        Invalid,

        /// <summary>
        /// Undefined value.
        /// </summary>
        Undefined,

        /// <summary>
        /// Points to one or more known possible locations.
        /// </summary>
        KnownLocations,

        /// <summary>
        /// Points to one or more known possible l-values.
        /// Used for pointers, ref expressions and l-value flow captures.
        /// </summary>
        KnownLValueCaptures,

        /// <summary>
        /// Points to unknown set of locations.
        /// </summary>
        Unknown,
    }

    internal static class PointsToAbstractValueExtensions
    {
        public static bool IsInvalidOrUndefined(this PointsToAbstractValueKind kind)
        {
            switch (kind)
            {
                case PointsToAbstractValueKind.Invalid:
                case PointsToAbstractValueKind.Undefined:
                    return true;

                default:
                    return false;
            }
        }
    }
}
