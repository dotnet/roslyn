// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal static class ControlFlowRegionExtensions
    {
        public static bool ContainsRegionOrSelf(this ControlFlowRegion controlFlowRegion, ControlFlowRegion nestedRegion)
            => controlFlowRegion.FirstBlockOrdinal <= nestedRegion.FirstBlockOrdinal &&
            controlFlowRegion.LastBlockOrdinal >= nestedRegion.LastBlockOrdinal;

        public static IEnumerable<IOperation> DescendantOperations(this ControlFlowRegion controlFlowRegion, ControlFlowGraph cfg)
        {
            for (var i = controlFlowRegion.FirstBlockOrdinal; i <= controlFlowRegion.LastBlockOrdinal; i++)
            {
                var block = cfg.Blocks[i];
                foreach (var operation in block.DescendantOperations())
                {
                    yield return operation;
                }
            }
        }
    }
}
