// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class TrackingInvocationSet : CacheBasedEquatable<TrackingInvocationSet>
    {
        public ImmutableHashSet<IOperation> Operations { get; }
        public InvocationCount TotalCount { get; }

        public static readonly TrackingInvocationSet Empty = new(ImmutableHashSet<IOperation>.Empty, InvocationCount.Zero);

        public TrackingInvocationSet(ImmutableHashSet<IOperation> operations, InvocationCount totalCount)
        {
            Operations = operations;
            TotalCount = totalCount;
        }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(TotalCount.GetHashCode());
            hashCode.Add(HashUtilities.Combine(Operations));
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<TrackingInvocationSet> obj)
        {
            var other = (TrackingInvocationSet)obj;
            return other.TotalCount.GetHashCode() == TotalCount.GetHashCode()
                && HashUtilities.Combine(other.Operations) == HashUtilities.Combine(Operations);
        }
    }
}
