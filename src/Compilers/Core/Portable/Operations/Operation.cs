// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements and expressions.
    /// </summary>
    internal abstract class Operation : IOperation
    {
        private readonly SemanticModel _semanticModel;

        // this will be lazily initialized. this will be initialized only once
        // but once initialized, will never change
        private IOperation _parentDoNotAccessDirectly;

        public Operation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue)
        {
            _semanticModel = semanticModel;

            Kind = kind;
            Syntax = syntax;
            Type = type;
            ConstantValue = constantValue;
        }

        /// <summary>
        /// IOperation that has this operation as a child
        /// </summary>
        public IOperation Parent
        {
            get
            {
                if (_parentDoNotAccessDirectly == null)
                {
                    Interlocked.CompareExchange(ref _parentDoNotAccessDirectly, _semanticModel.FindParentOperation(this), null);
                }

                return _parentDoNotAccessDirectly;
            }
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
        /// If the operation is an expression that evaluates to a constant value, <see cref="Optional{Object}.HasValue"/> is true and <see cref="Optional{Object}.Value"/> is the value of the expression. Otherwise, <see cref="Optional{Object}.HasValue"/> is false.
        /// </summary>
        public Optional<object> ConstantValue { get; }

        public abstract IEnumerable<IOperation> Children { get; }

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

        protected void SetParentOperation(IOperation operation)
        {
            var result = Interlocked.CompareExchange(ref _parentDoNotAccessDirectly, operation, null);
#if DEBUG
            if (result == null)
            {
                // tree must belong to same semantic model
                Debug.Assert(((Operation)operation)._semanticModel == _semanticModel);

                // confirm explicitly given parent is same as what we would have found.
                Debug.Assert(operation == _semanticModel.FindParentOperation(this));
            }
#endif
        }

        public static IOperation CreateOperationNone(SemanticModel semanticModel, SyntaxNode node, Optional<object> constantValue, Func<ImmutableArray<IOperation>> getChildren)
        {
            return new NoneOperation(semanticModel, node, constantValue, getChildren);
        }

        public static void SetParentOperation(IOperation current, IOperation parent)
        {
            ((Operation)current).SetParentOperation(parent);
        }

        private class NoneOperation : Operation
        {
            private readonly Func<ImmutableArray<IOperation>> _getChildren;

            public NoneOperation(SemanticModel semanticMode, SyntaxNode node, Optional<object> constantValue, Func<ImmutableArray<IOperation>> getChildren) :
                base(OperationKind.None, semanticMode, node, type: null, constantValue: constantValue)
            {
                _getChildren = getChildren;
            }

            public override void Accept(OperationVisitor visitor)
            {
                visitor.VisitNoneOperation(this);
            }

            public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitNoneOperation(this, argument);
            }

            public override IEnumerable<IOperation> Children => _getChildren().NullToEmpty();
        }
    }
}
