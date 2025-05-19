// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
