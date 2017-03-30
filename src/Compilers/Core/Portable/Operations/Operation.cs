// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements and expressions.
    /// </summary>
    internal abstract class Operation : IOperation
    {
        public Operation(OperationKind kind, bool isInvalid, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue)
        {
            Kind = kind;
            IsInvalid = isInvalid;
            Syntax = syntax;
            Type = type;
            ConstantValue = constantValue;
        }

        /// <summary>
        /// Identifies the kind of the operation.
        /// </summary>
        public OperationKind Kind { get; }

        /// <summary>
        ///  Indicates whether the operation is invalid, either semantically or syntactically.
        /// </summary>
        public bool IsInvalid { get; }

        /// <summary>
        /// Syntax that was analyzed to produce the operation.
        /// </summary>
        public SyntaxNode Syntax { get; }

        /// <summary>
        /// Result type of the operation, or null if the operation does not produce a result.
        /// </summary>
        public ITypeSymbol Type { get; }

        /// <summary>
        /// If the operation is an expression that evaluates to a constant value, <see cref="Optional{Object}.HasValue"/> is true and <see cref="Optional{Object}.Value"/> is the value of the expression. Otherwise, <see cref="Optional{Object}.HasValue"/> is false.
        /// </summary>
        public Optional<object> ConstantValue { get; }

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);
    }
}
