// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a C# or VB method invocation.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IInvocationExpression : IHasArgumentsExpression
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
        /// Arguments of the invocation, excluding the instance argument. Arguments are in the order specified in source,
        /// and params/ParamArray arguments have been collected into arrays. Arguments are not present
        /// unless supplied in source.
        /// </summary>
        ImmutableArray<IArgument> ArgumentsInSourceOrder { get; }
    }
}

