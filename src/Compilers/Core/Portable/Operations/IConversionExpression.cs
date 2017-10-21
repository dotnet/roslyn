// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Represents a conversion operation.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IConversionExpression : IOperation
    {
        /// <summary>
        /// Value to be converted.
        /// </summary>
        IOperation Operand { get; }
        /// <summary>
        /// Operator method used by the operation, null if the operation does not use an operator method.
        /// </summary>
        IMethodSymbol OperatorMethod { get; }

#pragma warning disable RS0010 // Avoid using cref tags with a prefix
        /// <summary>
        /// Gets the underlying common conversion information.
        /// </summary>
        /// <remarks>
        /// If you need conversion information that is language specific, use either
        /// <see cref="T:Microsoft.CodeAnalysis.CSharp.IConversionExpressionExtensions.GetConversion(IConversionExpression)"/> or
        /// <see cref="T:Microsoft.CodeAnalysis.VisualBasic.GetConversion(IConversionExpression)"/>.
        /// </remarks>
#pragma warning restore RS0010 // Avoid using cref tags with a prefix
        CommonConversion Conversion { get; }
        /// <summary>
        /// True if and only if the conversion is indicated explicity by a cast operation in the source code.
        /// </summary>
        bool IsExplicitInCode { get; }
        /// <summary>
        /// False if the conversion will fail with a <see cref="InvalidCastException"/> at runtime if the cast fails. This is true for C#'s
        /// <code>as</code> operator and for VB's <code>TryCast</code> operator.
        /// </summary>
        bool IsTryCast { get; }
        /// <summary>
        /// True if the conversion can fail at runtime with an overflow exception. This corresponds to C# checked and unchecked blocks.
        /// </summary>
        bool IsChecked { get; }
    }
}

