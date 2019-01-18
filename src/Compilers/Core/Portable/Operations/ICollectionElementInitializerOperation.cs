// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Obsolete interface that used to represent a collection element initializer. It has been replaced by
    /// <see cref="IInvocationOperation"/> and <see cref="IDynamicInvocationOperation"/>, as appropriate.
    /// <para>
    /// Current usage:
    ///   None. This API has been obsoleted in favor of <see cref="IInvocationOperation"/> and <see cref="IDynamicInvocationOperation"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    [Obsolete(nameof(ICollectionElementInitializerOperation) + " has been replaced with " + nameof(IInvocationOperation) + " and " + nameof(IDynamicInvocationOperation), error: true)]
    public interface ICollectionElementInitializerOperation : IOperation
    {
        IMethodSymbol AddMethod { get; }
        ImmutableArray<IOperation> Arguments { get; }
        bool IsDynamic { get; }
    }
}
