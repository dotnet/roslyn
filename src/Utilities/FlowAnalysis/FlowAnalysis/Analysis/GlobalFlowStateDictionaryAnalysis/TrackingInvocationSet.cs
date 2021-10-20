// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class TrackingInvocationSet : CacheBasedEquatable<TrackingInvocationSet>
    {
        /// <summary>
        /// All the operations that might contribute to the <see cref="EnumerationCount"/>
        /// </summary>
        /// <remarks>
        /// In some cases, EnumerationCount would be Zero, but this property is not empty.
        /// e.g.
        /// if (b)
        /// {
        ///     Bar.First();
        /// }
        /// else if (a)
        /// {
        /// }
        /// Here the block after the two if-branches would have a Zero EnumerationCount (because 'Bar' is not enumerated on all paths),
        /// with Bar.First() recorded in this property
        /// </remarks>
        public ImmutableHashSet<IOperation> Operations { get; }

        /// <summary>
        /// The total number of enumeration.
        /// </summary>
        public InvocationCount EnumerationCount { get; }

        public static readonly TrackingInvocationSet Empty = new(ImmutableHashSet<IOperation>.Empty, InvocationCount.Zero);

        public TrackingInvocationSet(ImmutableHashSet<IOperation> operations, InvocationCount enumerationCount)
        {
            Operations = operations;
            EnumerationCount = enumerationCount;
        }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(EnumerationCount.GetHashCode());
            hashCode.Add(HashUtilities.Combine(Operations));
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<TrackingInvocationSet> obj)
        {
            var other = (TrackingInvocationSet)obj;
            return other.EnumerationCount.GetHashCode() == EnumerationCount.GetHashCode()
                && HashUtilities.Combine(other.Operations) == HashUtilities.Combine(Operations);
        }
    }
}
