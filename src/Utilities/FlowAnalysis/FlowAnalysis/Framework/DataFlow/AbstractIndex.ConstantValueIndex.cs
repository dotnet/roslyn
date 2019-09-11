// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

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

            protected override void ComputeHashCodeParts(Action<int> addPart)
            {
                addPart(Index.GetHashCode());
                addPart(nameof(ConstantValueIndex).GetHashCode());
            }
#pragma warning restore CA1307 // Specify StringComparison
        }
    }
}
