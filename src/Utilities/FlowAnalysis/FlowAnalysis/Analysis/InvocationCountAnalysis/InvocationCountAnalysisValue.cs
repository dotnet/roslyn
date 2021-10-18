// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountAnalysisValue : CacheBasedEquatable<InvocationCountAnalysisValue>
    {
        public ImmutableDictionary<AnalysisEntity, IInvocationSet> TrackedEntities { get; }

        public InvocationCountAnalysisValueKind Kind { get; }

        public InvocationCountAnalysisValue(
            ImmutableDictionary<AnalysisEntity, IInvocationSet> trackedEntities,
            InvocationCountAnalysisValueKind kind)
        {
            TrackedEntities = trackedEntities;
            Kind = kind;
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<InvocationCountAnalysisValue> obj)
        {
            var other = (InvocationCountAnalysisValue)obj;
            return HashUtilities.Combine(TrackedEntities) == HashUtilities.Combine(other.TrackedEntities)
                && Kind.GetHashCode() == other.Kind.GetHashCode();
        }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(HashUtilities.Combine(TrackedEntities));
            hashCode.Add(Kind.GetHashCode());
        }
    }
}
