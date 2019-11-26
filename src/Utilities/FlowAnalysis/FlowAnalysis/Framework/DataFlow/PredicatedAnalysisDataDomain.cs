// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// An abstract domain implementation for analyses that store dictionary typed data along with predicated data.
    /// </summary>
    public class PredicatedAnalysisDataDomain<TAnalysisData, TValue> : AbstractAnalysisDomain<TAnalysisData>
        where TAnalysisData : AnalysisEntityBasedPredicateAnalysisData<TValue>
    {
        public PredicatedAnalysisDataDomain(MapAbstractDomain<AnalysisEntity, TValue> coreDataAnalysisDomain)
        {
            CoreDataAnalysisDomain = coreDataAnalysisDomain;
        }

        protected MapAbstractDomain<AnalysisEntity, TValue> CoreDataAnalysisDomain { get; }

        public override TAnalysisData Clone(TAnalysisData value) => (TAnalysisData)value.Clone();

        public override int Compare(TAnalysisData oldValue, TAnalysisData newValue) => oldValue.Compare(newValue, CoreDataAnalysisDomain);

        public override bool Equals(TAnalysisData value1, TAnalysisData value2) => value1.Equals(value2);

        public override TAnalysisData Merge(TAnalysisData value1, TAnalysisData value2)
        {
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