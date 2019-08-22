// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static partial class IOperationExtensions
    {
        public static bool IsInsideCatchRegion(this IOperation operation, ControlFlowGraph cfg)
        {
            foreach (var block in cfg.Blocks)
            {
                var isCatchRegionBlock = false;
                var currentRegion = block.EnclosingRegion;
                while (currentRegion != null)
                {
                    switch (currentRegion.Kind)
                    {
                        case ControlFlowRegionKind.Catch:
                            isCatchRegionBlock = true;
                            break;
                    }

                    currentRegion = currentRegion.EnclosingRegion;
                }

                if (isCatchRegionBlock)
                {
                    foreach (var descendant in block.DescendantOperations())
                    {
                        if (operation == descendant)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
