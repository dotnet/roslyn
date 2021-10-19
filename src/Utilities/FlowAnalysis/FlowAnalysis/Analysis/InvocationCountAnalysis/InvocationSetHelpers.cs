// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal static class InvocationSetHelpers
    {
        public static IInvocationSet Merge(IInvocationSet set1, IInvocationSet set2)
        {
            // If both of them are basic InvocationsSet then we can merge them directly
            if (set1 is TrackingInvocationSet trackingInvocationSet1 && set2 is TrackingInvocationSet trackingInvocationSet2)
            {
                using var builder = PooledObjects.PooledDictionary<IOperation, InvocationCount>.GetInstance();

                foreach (var kvp in trackingInvocationSet2.CountedInvocationOperations)
                {
                    builder[kvp.Key] = kvp.Value;
                }

                foreach (var kvp in trackingInvocationSet1.CountedInvocationOperations)
                {
                    var operation = kvp.Key;
                    var countTimesOfSet1 = kvp.Value;
                    if (trackingInvocationSet2.CountedInvocationOperations.TryGetValue(operation, out var countTimesOfSet2))
                    {
                        builder[operation] = AddInvocationCount(countTimesOfSet1, countTimesOfSet2);
                    }
                    else
                    {
                        builder[operation] = countTimesOfSet1;
                    }
                }

                return new TrackingInvocationSet(builder.ToImmutableDictionaryAndFree());
            }

            return new RelationshipSet(ImmutableHashSet.Create(set1, set2), InvocationSetKind.Union);
        }

        public static IInvocationSet Intersect(IInvocationSet set1, IInvocationSet set2)
        {
            // If both of them are basic InvocationsSet then we can intersect them directly
            if (set1 is TrackingInvocationSet trackingInvocationSet1 && set2 is TrackingInvocationSet trackingInvocationSet2)
            {
                using var builder = PooledObjects.PooledDictionary<IOperation, InvocationCount>.GetInstance();

                foreach (var kvp in trackingInvocationSet1.CountedInvocationOperations)
                {
                    var operation = kvp.Key;
                    var countTimesOfSet1 = kvp.Value;
                    if (trackingInvocationSet2.CountedInvocationOperations.TryGetValue(operation, out var countTimesOfSet2))
                    {
                        builder[operation] = Min(countTimesOfSet1, countTimesOfSet2);
                    }
                }

                return new TrackingInvocationSet(builder.ToImmutableDictionaryAndFree());
            }

            return new RelationshipSet(ImmutableHashSet.Create(set1, set2), InvocationSetKind.Intersect);
        }

        public static InvocationCount Min(InvocationCount count1, InvocationCount count2)
        {
            // Unknown = 0, One = 1, TwoOrMoreTime = 2
            var min = Math.Min((int)count1, (int)count2);
            return (InvocationCount)min;
        }

        public static InvocationCount AddInvocationCount(InvocationCount count1, InvocationCount count2)
            => (count1, count2) switch
            {
                (InvocationCount.None, _) => InvocationCount.None,
                (_, InvocationCount.None) => InvocationCount.None,
                (InvocationCount.One, InvocationCount.One) => InvocationCount.TwoOrMoreTime,
                (InvocationCount.One, InvocationCount.TwoOrMoreTime) => InvocationCount.TwoOrMoreTime,
                (InvocationCount.TwoOrMoreTime, InvocationCount.One) => InvocationCount.TwoOrMoreTime,
                (InvocationCount.TwoOrMoreTime, InvocationCount.TwoOrMoreTime) => InvocationCount.TwoOrMoreTime,
                (_, _) => InvocationCount.None,
            };
    }
}
