// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Contains information about an argument passed to interprocedural analysis.
    /// </summary>
    public sealed class ArgumentInfo<TAbstractAnalysisValue> : CacheBasedEquatable<ArgumentInfo<TAbstractAnalysisValue>>
    {
        public ArgumentInfo(
            IOperation operation,
            AnalysisEntity analysisEntityOpt,
            PointsToAbstractValue instanceLocation,
            TAbstractAnalysisValue value)
        {
            Operation = operation;
            AnalysisEntityOpt = analysisEntityOpt;
            InstanceLocation = instanceLocation;
            Value = value;
        }

        public IOperation Operation { get; }
        // Can be null for allocations.
        public AnalysisEntity AnalysisEntityOpt { get; }
        public PointsToAbstractValue InstanceLocation { get; }
        public TAbstractAnalysisValue Value { get; }

        protected override void ComputeHashCodeParts(ArrayBuilder<int> builder)
        {
            builder.Add(Operation.GetHashCode());
            builder.Add(AnalysisEntityOpt.GetHashCodeOrDefault());
            builder.Add(InstanceLocation.GetHashCode());
            builder.Add(Value.GetHashCode());
        }
    }
}
