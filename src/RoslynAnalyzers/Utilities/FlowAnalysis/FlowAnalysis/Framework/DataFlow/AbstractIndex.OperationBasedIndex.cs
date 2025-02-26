// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public abstract partial class AbstractIndex
    {
        private sealed class OperationBasedIndex : AbstractIndex
        {
            public OperationBasedIndex(IOperation operation)
            {
                Operation = operation;
            }

            public IOperation Operation { get; }

            protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
            {
                hashCode.Add(Operation.GetHashCode());
                hashCode.Add(nameof(OperationBasedIndex).GetHashCode());
            }

            protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<AbstractIndex> obj)
            {
                var other = (OperationBasedIndex)obj;
                return Operation.GetHashCode() == other.Operation.GetHashCode();
            }
        }
    }
}
