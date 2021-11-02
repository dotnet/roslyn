// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.GlobalFlowStateDictionaryAnalysis
{
    internal class InvocationEntity : CacheBasedEquatable<InvocationEntity>
    {
        public ImmutableHashSet<AbstractLocation> Locations { get; }

        public InvocationEntity(ImmutableHashSet<AbstractLocation> locations)
        {
            Locations = locations;
        }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(HashUtilities.Combine(Locations));
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<InvocationEntity> obj)
        {
            return HashUtilities.Combine(((InvocationEntity)obj).Locations) == HashUtilities.Combine(Locations);
        }
    }
}