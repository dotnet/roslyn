// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public abstract partial class AbstractIndex
    {
        private sealed class AnalysisEntityBasedIndex : AbstractIndex
        {
            public AnalysisEntityBasedIndex(AnalysisEntity analysisEntity)
            {
                AnalysisEntity = analysisEntity;
            }

            public AnalysisEntity AnalysisEntity { get; }

            protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
            {
                hashCode.Add(AnalysisEntity.GetHashCode());
                hashCode.Add(nameof(AnalysisEntityBasedIndex).GetHashCode());
            }

            protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<AbstractIndex> obj)
            {
                var other = (AnalysisEntityBasedIndex)obj;
                return AnalysisEntity.GetHashCode() == other.AnalysisEntity.GetHashCode();
            }
        }
    }
}
