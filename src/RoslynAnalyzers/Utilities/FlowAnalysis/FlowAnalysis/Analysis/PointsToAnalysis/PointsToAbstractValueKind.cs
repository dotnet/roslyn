// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Kind for the <see cref="PointsToAbstractValue"/>.
    /// </summary>
    public enum PointsToAbstractValueKind
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
        /// Points to unknown set of locations, which is known to be null.
        /// Note that this value kind is theoretically not needed, as the underlying
        /// value is null, but it has been added to ensure monotonicity of value merge.
        /// </summary>
        UnknownNull,

        /// <summary>
        /// Points to unknown set of locations, which is known to be non-null.
        /// </summary>
        UnknownNotNull,

        /// <summary>
        /// Points to unknown set of locations, which may or may not be null.
        /// </summary>
        Unknown,
    }
}
