// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class RelationshipSet : IInvocationSet
    {
        public ImmutableHashSet<IInvocationSet> InvocationSets { get; }

        public InvocationSetKind Kind { get; }

        public RelationshipSet(ImmutableHashSet<IInvocationSet> invocationSets, InvocationSetKind kind)
        {
            InvocationSets = invocationSets;
            Kind = kind;
        }
    }
}
