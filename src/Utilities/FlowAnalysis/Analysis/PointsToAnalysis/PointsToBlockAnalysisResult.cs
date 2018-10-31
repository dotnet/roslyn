// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    /// <summary>
    /// Result from execution of <see cref="PointsToAnalysis"/> on a basic block.
    /// It stores the PointsTo value for each <see cref="AnalysisEntity"/> at the start and end of the basic block.
    /// </summary>
    internal class PointsToBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public PointsToBlockAnalysisResult(
            BasicBlock basicBlock,
            DataFlowAnalysisInfo<PointsToAnalysisData> blockAnalysisData,
            ImmutableDictionary<AnalysisEntity, PointsToAbstractValue> defaultPointsToValues)
            : base (basicBlock)
        {
            InputData = GetResult(blockAnalysisData.Input, defaultPointsToValues);
            OutputData = GetResult(blockAnalysisData.Output, defaultPointsToValues);
            IsReachable = blockAnalysisData.Input?.IsReachableBlockData ?? true;
        }

        private static ImmutableDictionary<AnalysisEntity, PointsToAbstractValue> GetResult(PointsToAnalysisData analysisData, ImmutableDictionary<AnalysisEntity, PointsToAbstractValue> defaultPointsToValues)
        {
            PointsToAnalysisData.AssertValidPointsToAnalysisData(defaultPointsToValues);

            if (analysisData == null || analysisData.CoreAnalysisData.Count == 0)
            {
                return defaultPointsToValues;
            }

            analysisData.AssertValidPointsToAnalysisData();

            var builder = ImmutableDictionary.CreateBuilder<AnalysisEntity, PointsToAbstractValue>();
            builder.AddRange(analysisData.CoreAnalysisData);
            foreach (var kvp in defaultPointsToValues)
            {
                AnalysisEntity entity = kvp.Key;
                if (!builder.ContainsKey(entity))
                {
                    PointsToAbstractValue pointsToAbstractValue = kvp.Value;
                    builder.Add(entity, pointsToAbstractValue);
                }
            }

            return builder.ToImmutable();
        }

        public ImmutableDictionary<AnalysisEntity, PointsToAbstractValue> InputData { get; }
        public ImmutableDictionary<AnalysisEntity, PointsToAbstractValue> OutputData { get; }
        public bool IsReachable { get; }
    }
}
