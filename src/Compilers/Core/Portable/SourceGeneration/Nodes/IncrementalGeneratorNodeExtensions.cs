// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace Microsoft.CodeAnalysis
{
    internal static class IncrementalGeneratorNodeExtensions
    {
        public static NodeStateTable<TOutput> CreateCachedTableWithUpdatedSteps<TInput, TOutput>(this NodeStateTable<TOutput> previousTable, DriverStateTable.Builder graphState, NodeStateTable<TInput> inputTable, string? stepName)
        {
            Debug.Assert(inputTable.HasTrackedSteps && inputTable.IsCached);
            NodeStateTable<TOutput>.Builder builder = graphState.CreateTableBuilder(previousTable, stepName);
            foreach (var entry in inputTable)
            {
                var inputs = ImmutableArray.Create((entry.Step!, entry.OutputIndex));
                bool usedCachedEntry = builder.TryUseCachedEntries(TimeSpan.Zero, inputs);
                Debug.Assert(usedCachedEntry);
            }
            return builder.ToImmutableAndFree();
        }
    }
}
