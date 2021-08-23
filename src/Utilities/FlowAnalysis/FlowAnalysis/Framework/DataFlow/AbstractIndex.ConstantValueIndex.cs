// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public abstract partial class AbstractIndex
    {
        private sealed class ConstantValueIndex : AbstractIndex
        {
            public ConstantValueIndex(int index)
            {
                Index = index;
            }

            public int Index { get; }

            protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
            {
                hashCode.Add(Index.GetHashCode());
                hashCode.Add(nameof(ConstantValueIndex).GetHashCode());
            }

            protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<AbstractIndex> obj)
            {
                var other = (ConstantValueIndex)obj;
                return Index.GetHashCode() == other.Index.GetHashCode();
            }
        }
    }
}
