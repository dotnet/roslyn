// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T> - CacheBasedEquatable handles equality

using Analyzer.Utilities.PooledObjects;

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

#pragma warning disable CA1307 // Specify StringComparison - string.GetHashCode(StringComparison) not available in all projects that reference this shared project
            protected override void ComputeHashCodeParts(ArrayBuilder<int> builder)
            {
                builder.Add(AnalysisEntity.GetHashCode());
                builder.Add(nameof(AnalysisEntityBasedIndex).GetHashCode());
            }
#pragma warning restore CA1307 // Specify StringComparison
        }
    }
}
