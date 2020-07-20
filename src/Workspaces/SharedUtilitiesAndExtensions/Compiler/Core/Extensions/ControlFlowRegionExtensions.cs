// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal static partial class ControlFlowRegionExtensions
    {
        internal static bool ContainsBlock(this ControlFlowRegion region, int destinationOrdinal)
            => region.FirstBlockOrdinal <= destinationOrdinal && region.LastBlockOrdinal >= destinationOrdinal;
    }
}
