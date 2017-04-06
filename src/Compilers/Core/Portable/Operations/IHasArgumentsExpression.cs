// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IHasArgumentsExpression : IOperation
    {
        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in evaluation order.
        /// </summary>
        /// <remarks>
        /// If the invocation is in its expanded form, then params/ParamArray arguments would be collected into arrays. 
        /// Default values are supplied for optional arguments missing in source. Because they are always constant, their
        /// evaluation will not impact the evaluation order of the remaining arguments, therefore are appended at the end 
        /// of the returned argument list.
        /// </remarks>
        ImmutableArray<IArgument> ArgumentsInEvaluationOrder { get; }
    }
}

