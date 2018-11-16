// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Analyzer.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    internal abstract partial class AbstractIndex
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
            protected override int ComputeHashCode() => HashUtilities.Combine(Operation.GetHashCode(), nameof(OperationBasedIndex).GetHashCode());
#pragma warning restore CA1307 // Specify StringComparison
        }
    }
}
