// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal static partial class ControlFlowRegionExtensions
    {
        internal static bool ContainsBlock(this ControlFlowRegion region, int destinationOrdinal)
        {
            return region.FirstBlockOrdinal <= destinationOrdinal && region.LastBlockOrdinal >= destinationOrdinal;
        }
    }
}
