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
        public static NodeStateTable<TOutput> RecordStepsForCachedTable<TInput, TOutput>(this NodeStateTable<TOutput> previousTable, NodeStateTable<TInput> inputTable, string? stepName)
        {
            Debug.Assert(inputTable.HasTrackedSteps && inputTable.IsCached);
            NodeStateTable<TOutput>.Builder builder = previousTable.ToBuilder(stepTrackingEnabled: true);
            foreach (var entry in inputTable)
            {
                bool usedCachedEntry = builder.TryUseCachedEntries();
                Debug.Assert(usedCachedEntry);
                builder.RecordStepInfoForLastEntry(stepName, TimeSpan.Zero, ImmutableArray.Create((entry.Step!, entry.OutputIndex)), entry.State);
            }
            return builder.ToImmutableAndFree();
        }
    }
}
