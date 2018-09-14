// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Operations
{
    /// <summary>
    /// Represents an invocation of a method.
    /// <para>
    /// Current usage:
    ///  (1) C# method invocation expression.
    ///  (2) C# collection element initializer.
    ///      For example, in the following collection initializer: <code>new C() { 1, 2, 3 }</code>, we will have
    ///      3 <see cref="IInvocationOperation"/> nodes, each of which will be a call to the corresponding Add method
    ///      with either 1, 2, 3 as the argument.
    ///  (3) VB method invocation expression.
    ///  (4) VB collection element initializer.
    ///      Similar to the C# example, <code>New C() From {1, 2, 3}</code> will have 3 <see cref="IInvocationOperation"/>
    ///      nodes with 1, 2, and 3 as their arguments, respectively.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInvocationOperation : IOperation
    {
        /// <summary>
        /// Method to be invoked.
        /// </summary>
        IMethodSymbol TargetMethod { get; }
        /// <summary>
        /// 'This' or 'Me' instance to be supplied to the method, or null if the method is static.
        /// </summary>
        IOperation Instance { get; }
        /// <summary>
        /// True if the invocation uses a virtual mechanism, and false otherwise.
        /// </summary>
        bool IsVirtual { get; }
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source.
        /// </remarks>
        ImmutableArray<IArgumentOperation> Arguments { get; }
    }
}

