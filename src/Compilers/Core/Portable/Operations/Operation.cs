// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements and expressions.
    /// </summary>
    internal abstract class Operation : IOperation
    {
        public Operation(OperationKind kind, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue)
        {
            Kind = kind;
            Syntax = syntax;
            Type = type;
            ConstantValue = constantValue;
        }

        /// <summary>
        /// Identifies the kind of the operation.
        /// </summary>
        public OperationKind Kind { get; }

        /// <summary>
        /// Syntax that was analyzed to produce the operation.
        /// </summary>
        public SyntaxNode Syntax { get; }

        /// <summary>
        /// Result type of the operation, or null if the operation does not produce a result.
        /// </summary>
        public ITypeSymbol Type { get; }

        /// <summary>
        /// The source language of the IOperation. Possible values are <see cref="LanguageNames.CSharp"/> and <see cref="LanguageNames.VisualBasic"/>.
        /// </summary>

        public string Language
        {
            // It is an eventual goal to support analyzing IL. At that point, we'll need to detect a null
            // syntax and add a new field to LanguageNames for IL. Until then, though, we'll just assume that
            // syntax is not null and return its language.
            get => Syntax.Language;
        }

        /// <summary>
        /// If the operation is an expression that evaluates to a constant value, <see cref="Optional{Object}.HasValue"/> is true and <see cref="Optional{Object}.Value"/> is the value of the expression. Otherwise, <see cref="Optional{Object}.HasValue"/> is false.
        /// </summary>
        public Optional<object> ConstantValue { get; }

        public abstract IEnumerable<IOperation> Children { get; }

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

        public static IOperation CreateOperationNone(SyntaxNode node, Optional<object> constantValue, Func<ImmutableArray<IOperation>> getChildren)
        {
            return new NoneOperation(node, constantValue, getChildren);
        }

        private class NoneOperation : IOperation
        {
            private readonly Func<ImmutableArray<IOperation>> _getChildren;

            public NoneOperation(SyntaxNode node, Optional<object> constantValue, Func<ImmutableArray<IOperation>> getChildren)
            {
                Syntax = node;
                ConstantValue = constantValue;
                _getChildren = getChildren;
            }

            public OperationKind Kind => OperationKind.None;

            public SyntaxNode Syntax { get; }

            public ITypeSymbol Type => null;

            public Optional<object> ConstantValue { get; }

            public string Language => Syntax.Language;

            public void Accept(OperationVisitor visitor)
            {
                visitor.VisitNoneOperation(this);
            }

            public TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitNoneOperation(this, argument);
            }

            public IEnumerable<IOperation> Children => _getChildren().NullToEmpty();
        }
    }
}
