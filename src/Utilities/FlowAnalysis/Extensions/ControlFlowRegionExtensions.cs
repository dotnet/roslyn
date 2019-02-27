// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal static class ControlFlowRegionExtensions
    {
        public static bool ContainsRegionOrSelf(this ControlFlowRegion controlFlowRegion, ControlFlowRegion nestedRegion)
            => controlFlowRegion.FirstBlockOrdinal <= nestedRegion.FirstBlockOrdinal &&
            controlFlowRegion.LastBlockOrdinal >= nestedRegion.LastBlockOrdinal;
    }
}
