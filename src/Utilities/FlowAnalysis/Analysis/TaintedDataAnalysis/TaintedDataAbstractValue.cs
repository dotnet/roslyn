// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using System.Collections.Immutable;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

    /// <summary>
    /// Abstract tainted data value shared by a set of one of more <see cref="AnalysisEntity"/> instances tracked by <see cref="TaintedDataAnalysis"/>.
    /// </summary>
    internal class TaintedDataAbstractValue : CacheBasedEquatable<TaintedDataAbstractValue>
    {
        public static readonly TaintedDataAbstractValue Unknown = new TaintedDataAbstractValue(TaintedDataAbstractValueKind.Unknown);
        public static readonly TaintedDataAbstractValue Tainted = new TaintedDataAbstractValue(TaintedDataAbstractValueKind.Tainted);
        public static readonly TaintedDataAbstractValue NotTainted = new TaintedDataAbstractValue(TaintedDataAbstractValueKind.NotTainted);

        public TaintedDataAbstractValue(ImmutableHashSet<AnalysisEntity> analysisEntities, TaintedDataAbstractValueKind kind)
        {
            this.AnalysisEntities = analysisEntities;
            this.Kind = kind;
        }

        public TaintedDataAbstractValue(TaintedDataAbstractValueKind kind)
            : this(null, kind)
        {
        }

        /// <summary>
        /// The set of <see cref="AnalysisEntity"/> that this abstract value is representing.
        /// </summary>
        public ImmutableHashSet<AnalysisEntity> AnalysisEntities { get; }

        /// <summary>
        /// The abstract value that this is representing.
        /// </summary>
        public TaintedDataAbstractValueKind Kind { get; }

        protected override int ComputeHashCode()
        {
            int hashCode = HashUtilities.Combine(Kind.GetHashCode(), this.AnalysisEntities.Count.GetHashCode());
            foreach (AnalysisEntity entity in this.AnalysisEntities)
            {
                hashCode = HashUtilities.Combine(entity.GetHashCode(), hashCode);
            }

            return hashCode;
        }
    }
}
