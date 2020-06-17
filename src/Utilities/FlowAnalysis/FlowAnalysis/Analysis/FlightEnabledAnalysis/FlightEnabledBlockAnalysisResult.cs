// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    using FlightEnabledAnalysisData = DictionaryAnalysisData<AnalysisEntity, FlightEnabledAbstractValue>;

    /// <summary>
    /// Result from execution of <see cref="FlightEnabledAnalysis"/> on a basic block.
    /// It store FlightEnabled value at the start and end of the basic block.
    /// </summary>
    internal sealed class FlightEnabledBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        internal FlightEnabledBlockAnalysisResult(BasicBlock basicBlock, FlightEnabledAnalysisData blockAnalysisData)
            : base(basicBlock)
        {
            Data = blockAnalysisData?.ToImmutableDictionary() ?? ImmutableDictionary<AnalysisEntity, FlightEnabledAbstractValue>.Empty;
        }

        public ImmutableDictionary<AnalysisEntity, FlightEnabledAbstractValue> Data { get; }
    }
}
