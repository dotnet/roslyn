// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for <see cref="IOperation"/> nodes that are still not fully designed and hence need a backdoor to stitch the child IOperation nodes to the entire IOperation tree.
    /// </summary>
    /// <remarks>
    /// NOTE: This type is a temporary workaround and should be deleted once we ship IOperation APIs.
    /// </remarks>
    [InternalImplementationOnly]
    internal interface IOperationWithChildren: IOperation
    {
        /// <summary>
        /// An array of child operations for this operation.
        /// </summary>
        /// <remarks>Note that any of the child operation nodes may be null.</remarks>
        ImmutableArray<IOperation> Children { get; }
    }
}
