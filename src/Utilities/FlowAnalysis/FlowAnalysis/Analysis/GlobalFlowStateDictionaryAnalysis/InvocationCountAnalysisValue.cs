// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountAnalysisValue : CacheBasedEquatable<InvocationCountAnalysisValue>
    {
        /// <summary>
        /// The core analysis value of <see cref="InvocationCountAnalysis"/>.
        /// Key is the <see cref="AnalysisEntity"/> tracked my the analysis. Value is the state of the <see cref="AnalysisEntity"/>.
        /// </summary>
        public ImmutableDictionary<AnalysisEntity, TrackingInvocationSet> TrackedEntities { get; }

        public InvocationCountAnalysisValueKind Kind { get; }

        public static readonly InvocationCountAnalysisValue Empty = new(ImmutableDictionary<AnalysisEntity, TrackingInvocationSet>.Empty, InvocationCountAnalysisValueKind.Empty);

        public static readonly InvocationCountAnalysisValue Unknown = new(ImmutableDictionary<AnalysisEntity, TrackingInvocationSet>.Empty, InvocationCountAnalysisValueKind.Unknown);

        public InvocationCountAnalysisValue(
            ImmutableDictionary<AnalysisEntity, TrackingInvocationSet> trackedEntities,
            InvocationCountAnalysisValueKind kind)
        {
            TrackedEntities = trackedEntities;
            Kind = kind;
        }

        /// <summary>
        /// Merge <param name="value1"/> and <param name="value2"/>
        /// </summary>
        /// <remarks>
        /// e.g.
        /// {
        ///     "entity1" : ["1", "2", "3"]
        ///     "entity2" : "1"
        /// }
        /// {
        ///     "entity1" : ["2", "3", "4"]
        ///     "entity3" : "1"
        /// }
        /// After intersection, the result would be
        /// {
        ///     "entity1" : ["1", "2", "3", "4"]
        ///     "entity2" : "1"
        ///     "entity3" : "1"
        /// }
        /// </remarks>
        public static InvocationCountAnalysisValue Merge(InvocationCountAnalysisValue value1, InvocationCountAnalysisValue value2)
        {
            if (value1.TrackedEntities.Count == 0)
            {
                return value2;
            }

            if (value2.TrackedEntities.Count == 0)
            {
                return value1;
            }

            using var builder = PooledDictionary<AnalysisEntity, TrackingInvocationSet>.GetInstance();
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

            return new InvocationCountAnalysisValue(builder.ToImmutableDictionary(), InvocationCountAnalysisValueKind.Known);
        }

        /// <summary>
        /// Interset the state of the analysisEntity in <param name="value1"/> and <param name="value2"/>.
        /// </summary>
        /// <remarks>
        /// This will only intersect the value of the common analysisEntity of value1 and value2.
        /// If different analysisEntities are presented, they will be merged.
        /// e.g.
        /// {
        ///     "entity1" : [1, 2, 3]
        ///     "entity2" : "1"
        /// }
        /// {
        ///     "entity1" : [2, 3, 4]
        ///     "entity3" : "1"
        /// }
        /// After intersection, the result would be
        /// {
        ///     "entity1" : [2, 3]
        ///     "entity2" : "1"
        ///     "entity3" : "1"
        /// }
        /// </remarks>
        public static InvocationCountAnalysisValue Intersect(InvocationCountAnalysisValue value1, InvocationCountAnalysisValue value2)
        {
            var builder = ImmutableDictionary.CreateBuilder<AnalysisEntity, TrackingInvocationSet>();
            var intersectedKeys = new HashSet<AnalysisEntity>();

            foreach (var kvp in value1.TrackedEntities)
            {
                var key = kvp.Key;
                var invocationSet = kvp.Value;
                if (value2.TrackedEntities.TryGetValue(key, out var trackedEntities2))
                {
                    builder[key] = InvocationSetHelpers.Intersect(invocationSet, trackedEntities2);
                    intersectedKeys.Add(key);
                }
                else
                {
                    builder[key] = InvocationSetHelpers.Intersect(invocationSet, TrackingInvocationSet.Empty);
                }
            }

            foreach (var kvp in value2.TrackedEntities)
            {
                var key = kvp.Key;
                var invocationSet = kvp.Value;

                if (!intersectedKeys.Contains(kvp.Key))
                {
                    builder[key] = InvocationSetHelpers.Intersect(invocationSet, TrackingInvocationSet.Empty);
                }
            }

            return new InvocationCountAnalysisValue(builder.ToImmutable(), InvocationCountAnalysisValueKind.Known);
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
