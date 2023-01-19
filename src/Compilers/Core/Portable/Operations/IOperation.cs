// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Operations;

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
    public partial interface IOperation
    {
        /// <summary>
        /// IOperation that has this operation as a child. Null for the root.
        /// </summary>
        IOperation? Parent { get; }

        /// <summary>
        /// Identifies the kind of the operation.
        /// </summary>
        OperationKind Kind { get; }

        /// <summary>
        /// Syntax that was analyzed to produce the operation.
        /// </summary>
        SyntaxNode Syntax { get; }

        /// <summary>
        /// Result type of the operation, or null if the operation does not produce a result.
        /// </summary>
        ITypeSymbol? Type { get; }

        /// <summary>
        /// If the operation is an expression that evaluates to a constant value, <see cref="Optional{Object}.HasValue"/> is true and <see cref="Optional{Object}.Value"/> is the value of the expression. Otherwise, <see cref="Optional{Object}.HasValue"/> is false.
        /// </summary>
        Optional<object?> ConstantValue { get; }

        /// <summary>
        /// An array of child operations for this operation. Deprecated: please use <see cref="ChildOperations"/>.
        /// </summary>
        [Obsolete($"This API has performance penalties, please use {nameof(ChildOperations)} instead.", error: false)]
        IEnumerable<IOperation> Children { get; }

        /// <summary>
        /// An enumerable of child operations for this operation.
        /// </summary>
        OperationList ChildOperations { get; }

        /// <summary>
        /// The source language of the IOperation. Possible values are <see cref="LanguageNames.CSharp"/> and <see cref="LanguageNames.VisualBasic"/>.
        /// </summary>
        string Language { get; }

        void Accept(OperationVisitor visitor);

        TResult? Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

        /// <summary>
        /// Set to True if compiler generated /implicitly computed by compiler code
        /// </summary>
        bool IsImplicit { get; }

        /// <summary>
        /// Optional semantic model that was used to generate this operation.
        /// Non-null for operations generated from source with <see cref="SemanticModel.GetOperation(SyntaxNode, System.Threading.CancellationToken)"/> API
        /// and operation callbacks made to analyzers.
        /// Null for operations inside a <see cref="FlowAnalysis.ControlFlowGraph"/>.
        /// </summary>
        SemanticModel? SemanticModel { get; }
    }
}
