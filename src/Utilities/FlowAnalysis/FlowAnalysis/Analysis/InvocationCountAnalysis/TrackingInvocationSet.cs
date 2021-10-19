// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class TrackingInvocationSet : IInvocationSet
    {
        public TrackingInvocationSet(ImmutableDictionary<IOperation, InvocationCount> countedInvocationOperations)
        {
            CountedInvocationOperations = countedInvocationOperations;
        }

        public ImmutableDictionary<IOperation, InvocationCount> CountedInvocationOperations { get; }

        public InvocationSetKind Kind => InvocationSetKind.Invocations;
    }
}
