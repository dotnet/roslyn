// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents an argument in a method invocation.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IArgument : IOperation
    {
        /// <summary>
        /// Kind of argument.
        /// </summary>
        ArgumentKind ArgumentKind { get; }
        /// <summary>
        /// Parameter the argument matches.
        /// </summary>
        IParameterSymbol Parameter { get; }
        /// <summary>
        /// Value supplied for the argument.
        /// </summary>
        IOperation Value { get; }
        /// <summary>
        /// Conversion applied to the argument value passing it into the target method. Applicable only to VB Reference arguments.
        /// </summary>
        IOperation InConversion { get; }
        /// <summary>
        /// Conversion applied to the argument value after the invocation. Applicable only to VB Reference arguments.
        /// </summary>
        IOperation OutConversion { get; }
    }
}

