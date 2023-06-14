// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public abstract partial class AbstractIndex
    {
        private sealed class OperationBasedIndex(IOperation operation) : AbstractIndex
        {
            public IOperation Operation { get; } = operation;

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
