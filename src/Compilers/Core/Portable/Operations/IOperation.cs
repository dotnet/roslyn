﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements and expressions.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    [InternalImplementationOnly]
    public interface IOperation
    {
        /// <summary>
        /// Identifies the kind of the operation.
        /// </summary>
        OperationKind Kind { get; }

        /// <summary>
        ///  Indicates whether the operation is invalid, either semantically or syntactically.
        /// </summary>
        bool IsInvalid { get; }

        /// <summary>
        /// Syntax that was analyzed to produce the operation.
        /// </summary>
        SyntaxNode Syntax { get; }

        /// <summary>
        /// Result type of the operation, or null if the operation does not produce a result.
        /// </summary>
        ITypeSymbol Type { get; }

        /// <summary>
        /// If the operation is an expression that evaluates to a constant value, <see cref="Optional{Object}.HasValue"/> is true and <see cref="Optional{Object}.Value"/> is the value of the expression. Otherwise, <see cref="Optional{Object}.HasValue"/> is false.
        /// </summary>
        Optional<object> ConstantValue { get; }

        void Accept(OperationVisitor visitor);

        TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }
}
