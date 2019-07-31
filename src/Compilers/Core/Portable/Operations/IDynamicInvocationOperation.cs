// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents a invocation that is dynamically bound.
    /// <para>
    /// Current usage:
    ///  (1) C# dynamic invocation expression.
    ///  (2) C# dynamic collection element initializer.
    ///      For example, in the following collection initializer: <code>new C() { do1, do2, do3 }</code> where
    ///      the doX objects are of type dynamic, we'll have 3 <see cref="IDynamicInvocationOperation" /> with do1, do2, and
    ///      do3 as their arguments.
    ///  (3) VB late bound invocation expression.
    ///  (4) VB dynamic collection element initializer.
    ///      Similar to the C# example, <code>New C() From {do1, do2, do3}</code> will generate 3 <see cref="IDynamicInvocationOperation" />
    ///      nodes with do1, do2, and do3 as their arguments, respectively.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IDynamicInvocationOperation : IOperation
    {
        /// <summary>
        /// Dynamically or late bound operation.
        /// </summary>
        IOperation Operation { get; }
        /// <summary>
        /// Dynamically bound arguments, excluding the instance argument.
        /// </summary>
        ImmutableArray<IOperation> Arguments { get; }
    }
}
