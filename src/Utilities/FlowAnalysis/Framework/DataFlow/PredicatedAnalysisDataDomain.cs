// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// An abstract domain implementation for analyses that store dictionary typed data along with predicated data.
    /// </summary>
    internal class PredicatedAnalysisDataDomain<TAnalysisData, TValue> : AbstractAnalysisDomain<TAnalysisData>
        where TAnalysisData: AnalysisEntityBasedPredicateAnalysisData<TValue>, new()
    {
        public PredicatedAnalysisDataDomain(MapAbstractDomain<AnalysisEntity, TValue> coreDataAnalysisDomain)
        {
            CoreDataAnalysisDomain = coreDataAnalysisDomain;
        }

        protected MapAbstractDomain<AnalysisEntity, TValue> CoreDataAnalysisDomain { get; }

        public override TAnalysisData Bottom => new TAnalysisData();

        public override TAnalysisData Clone(TAnalysisData value) => (TAnalysisData)value.Clone();

        public override int Compare(TAnalysisData oldValue, TAnalysisData newValue) => oldValue.Compare(newValue, CoreDataAnalysisDomain);

        public override TAnalysisData Merge(TAnalysisData value1, TAnalysisData value2)
        {
            Debug.Assert(value1 != null);
            Debug.Assert(value2 != null);

            AnalysisEntityBasedPredicateAnalysisData<TValue> result;
            if (ReferenceEquals(value1, value2))
            {
                result = value1.Clone();
            }
            else if (!value1.IsReachableBlockData && value2.IsReachableBlockData)
            {
                result = value2.Clone();
            }
            else if (!value2.IsReachableBlockData && value1.IsReachableBlockData)
            {
                result = value1.Clone();
            }
            else
            {
                result = value1.WithMergedData(value2, CoreDataAnalysisDomain);
            }

            return (TAnalysisData)result;
        }
    }
}