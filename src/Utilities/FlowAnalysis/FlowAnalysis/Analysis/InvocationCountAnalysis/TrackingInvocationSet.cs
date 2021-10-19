// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class TrackingInvocationSet
    {
        public ImmutableHashSet<IOperation> Operations { get; }
        public InvocationCount TotalCount { get; }

        public static readonly TrackingInvocationSet Empty = new(ImmutableHashSet<IOperation>.Empty, InvocationCount.Zero);

        public TrackingInvocationSet(ImmutableHashSet<IOperation> operations, InvocationCount totalCount)
        {
            Operations = operations;
            TotalCount = totalCount;
        }
    }
}
