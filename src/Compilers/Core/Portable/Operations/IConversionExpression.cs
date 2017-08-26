// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConversionExpression : IHasOperatorMethodExpression
    {
        /// <summary>
        /// Value to be converted.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// Kind of conversion.
        /// </summary>
        ConversionKind ConversionKind { get; }
        /// <summary>
        /// True if and only if the conversion is indicated explicitly by a cast operation in the source code.
        /// </summary>
        bool IsExplicit { get; }
    }
}

