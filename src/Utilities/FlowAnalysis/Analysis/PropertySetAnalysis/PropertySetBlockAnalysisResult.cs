// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    using PropertySetAnalysisData = IDictionary<AbstractLocation, PropertySetAbstractValue>;

    /// <summary>
    /// Result from execution of <see cref="PropertySetAnalysis"/> on a basic block.
    /// It stores BinaryFormatter values for each <see cref="AbstractLocation"/> at the start and end of the basic block.
    /// </summary>
    internal class PropertySetBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public PropertySetBlockAnalysisResult(BasicBlock basicBlock, DataFlowAnalysisInfo<PropertySetAnalysisData> blockAnalysisData)
            : base(basicBlock)
        {
            InputData = blockAnalysisData.Input?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, PropertySetAbstractValue>.Empty;
            OutputData = blockAnalysisData.Output?.ToImmutableDictionary() ?? ImmutableDictionary<AbstractLocation, PropertySetAbstractValue>.Empty;
        }

        public ImmutableDictionary<AbstractLocation, PropertySetAbstractValue> InputData { get; }
        public ImmutableDictionary<AbstractLocation, PropertySetAbstractValue> OutputData { get; }
    }
}
