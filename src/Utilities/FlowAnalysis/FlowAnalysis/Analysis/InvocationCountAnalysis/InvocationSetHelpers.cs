// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal static class InvocationSetHelpers
    {
        public static TrackingInvocationSet Merge(TrackingInvocationSet set1, TrackingInvocationSet set2)
        {
            using var builder = PooledHashSet<IOperation>.GetInstance();
            var totalCount = AddInvocationCount(set1.TotalCount, set2.TotalCount);
            foreach (var operation in set1.Operations)
            {
                builder.Add(operation);
            }

            foreach (var operation in set2.Operations)
            {
                builder.Add(operation);
            }

            return new TrackingInvocationSet(builder.ToImmutable(), totalCount);
        }

        public static TrackingInvocationSet Intersect(TrackingInvocationSet set1, TrackingInvocationSet set2)
        {
            using var builder = PooledHashSet<IOperation>.GetInstance();
            var totalCount = Min(set1.TotalCount, set2.TotalCount);
            foreach (var operation in set1.Operations)
            {
                builder.Add(operation);
            }

            foreach (var operation in set2.Operations)
            {
                builder.Add(operation);
            }

            return new TrackingInvocationSet(builder.ToImmutable(), totalCount);
        }

        public static InvocationCount Min(InvocationCount count1, InvocationCount count2)
        {
            // Unknown = -1, Zero = 0, One = 1, TwoOrMoreTime = 2
            var min = Math.Min((int)count1, (int)count2);
            return (InvocationCount)min;
        }

        public static InvocationCount AddInvocationCount(InvocationCount count1, InvocationCount count2)
            => (count1, count2) switch
            {
                (InvocationCount.None, _) => InvocationCount.None,
                (_, InvocationCount.None) => InvocationCount.None,
                (InvocationCount.Zero, _) => count2,
                (_, InvocationCount.Zero) => count1,
                (InvocationCount.One, InvocationCount.One) => InvocationCount.TwoOrMoreTime,
                (InvocationCount.TwoOrMoreTime, _) => InvocationCount.TwoOrMoreTime,
                (_, InvocationCount.TwoOrMoreTime) => InvocationCount.TwoOrMoreTime,
                (_, _) => InvocationCount.None,
            };
    }
}
