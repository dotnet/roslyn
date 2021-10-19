// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountAnalysisValue : CacheBasedEquatable<InvocationCountAnalysisValue>
    {
        public ImmutableDictionary<AnalysisEntity, IInvocationSet> TrackedEntities { get; }

        public InvocationCountAnalysisValueKind Kind { get; }

        public static readonly InvocationCountAnalysisValue Empty = new(ImmutableDictionary<AnalysisEntity, IInvocationSet>.Empty, InvocationCountAnalysisValueKind.Empty);
        public static readonly InvocationCountAnalysisValue Unknown = new(ImmutableDictionary<AnalysisEntity, IInvocationSet>.Empty, InvocationCountAnalysisValueKind.Unknown);

        public InvocationCountAnalysisValue(
            ImmutableDictionary<AnalysisEntity, IInvocationSet> trackedEntities,
            InvocationCountAnalysisValueKind kind)
        {
            TrackedEntities = trackedEntities;
            Kind = kind;
        }

        public static InvocationCountAnalysisValue MergeKnownValues(InvocationCountAnalysisValue value1, InvocationCountAnalysisValue value2)
        {
            RoslynDebug.Assert(value1.Kind == InvocationCountAnalysisValueKind.Known && value2.Kind == InvocationCountAnalysisValueKind.Known);

            if (value1.TrackedEntities.Count == 0)
            {
                return value2;
            }

            if (value2.TrackedEntities.Count == 0)
            {
                return value1;
            }

            using var builder = PooledObjects.PooledDictionary<AnalysisEntity, IInvocationSet>.GetInstance();
            foreach (var kvp in value2.TrackedEntities)
            {
                builder[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in value1.TrackedEntities)
            {
                var key = kvp.Key;
                var trackedEntities1 = kvp.Value;
                if (value2.TrackedEntities.TryGetValue(key, out var trackedEntities2))
                {
                    builder[key] = InvocationSetHelpers.Merge(trackedEntities1, trackedEntities2);
                }
                else
                {
                    builder[key] = kvp.Value;
                }
            }

            return new InvocationCountAnalysisValue(builder.ToImmutableDictionaryAndFree(), InvocationCountAnalysisValueKind.Known);
        }

        public static InvocationCountAnalysisValue IntersectKnownValues(InvocationCountAnalysisValue value1, InvocationCountAnalysisValue value2)
        {
            RoslynDebug.Assert(value1.Kind == InvocationCountAnalysisValueKind.Known && value2.Kind == InvocationCountAnalysisValueKind.Known);
            using var builder = PooledObjects.PooledDictionary<AnalysisEntity, IInvocationSet>.GetInstance();
            foreach (var kvp in value2.TrackedEntities)
            {
                builder[kvp.Key] = kvp.Value;
            }

            foreach (var kvp in value1.TrackedEntities)
            {
                var key = kvp.Key;
                var trackedEntities1 = kvp.Value;
                if (value2.TrackedEntities.TryGetValue(key, out var trackedEntities2))
                {
                    builder[key] = InvocationSetHelpers.Intersect(trackedEntities1, trackedEntities2);
                }
                else
                {
                    builder[key] = kvp.Value;
                }
            }

            return new InvocationCountAnalysisValue(builder.ToImmutableDictionaryAndFree(), InvocationCountAnalysisValueKind.Known);
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
