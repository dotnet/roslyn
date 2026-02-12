// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis
{
    internal static class NodeExtensions
    {
        public static void LogTables<TSelf, TInput>(this IIncrementalGeneratorNode<TSelf> self, string? name, string? tableType, NodeStateTable<TSelf>? previousTable, NodeStateTable<TSelf> newTable, NodeStateTable<TInput> inputTable, TimeSpan elapsedTime)
            => LogTables<TSelf, TInput, TInput>(self, name, tableType, previousTable, newTable, inputTable, inputNode2: null, elapsedTime);

        public static void LogTables<TSelf, TInput1, TInput2>(this IIncrementalGeneratorNode<TSelf> self, string? name, string? tableType, NodeStateTable<TSelf>? previousTable, NodeStateTable<TSelf> newTable, NodeStateTable<TInput1> inputNode1, NodeStateTable<TInput2>? inputNode2, TimeSpan elapsedTime)
        {
            if (CodeAnalysisEventSource.Log.IsEnabled())
            {
                // don't log the new table if we skipped creating a new one
                var newTableOpt = newTable != previousTable ? newTable : null;

                CodeAnalysisEventSource.Log.NodeTransform(self.GetHashCode(),
                                                          name ?? "<anonymous>",
                                                          tableType ?? "<unknown>",
                                                          previousTable?.GetHashCode() ?? -1,
                                                          previousTable?.GetPackedStates() ?? "",
                                                          newTableOpt?.GetHashCode() ?? -1,
                                                          newTableOpt?.GetPackedStates() ?? "",
                                                          inputNode1.GetHashCode(),
                                                          inputNode2?.GetHashCode() ?? -1,
                                                          elapsedTime.Ticks);
            }
        }
    }
}
