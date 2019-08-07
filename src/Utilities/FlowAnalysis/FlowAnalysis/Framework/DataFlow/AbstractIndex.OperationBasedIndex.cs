// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T> - CacheBasedEquatable handles equality

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    public abstract partial class AbstractIndex
    {
        private sealed class OperationBasedIndex : AbstractIndex
        {
            public OperationBasedIndex(IOperation operation)
            {
                Debug.Assert(operation != null);
                Operation = operation;
            }

            public IOperation Operation { get; }

#pragma warning disable CA1307 // Specify StringComparison - string.GetHashCode(StringComparison) not available in all projects that reference this shared project
            protected override void ComputeHashCodeParts(Action<int> addPart)
            {
                addPart(Operation.GetHashCode());
                addPart(nameof(OperationBasedIndex).GetHashCode());
            }
#pragma warning restore CA1307 // Specify StringComparison
        }
    }
}
