// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
                    Interlocked.CompareExchange(ref _parentDoNotAccessDirectly, SearchParentOperation(), null);
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
                Debug.Assert(operation == SearchParentOperation());
            }
#endif
        }

        public static IOperation CreateOperationNone(SemanticModel semanticModel, SyntaxNode node, Optional<object> constantValue, Func<ImmutableArray<IOperation>> getChildren)
        {
            return new NoneOperation(semanticModel, node, constantValue, getChildren);
        }

        public static T SetParentOperation<T>(T operation, IOperation parent) where T : IOperation
        {
            (operation as Operation).SetParentOperation(parent);
            return operation;
        }

        public static ImmutableArray<T> SetParentOperation<T>(ImmutableArray<T> operations, IOperation parent) where T : IOperation
        {
            // check quick bail out case first
            if (operations.Length == 0)
            {
                // no element
                return operations;
            }

            // race is okay. paneltiy is going through a loop one more time
            if ((operations[0] as Operation)._parentDoNotAccessDirectly != null)
            {
                // already initialized
                return operations;
            }

            for (var i = 0; i < operations.Length; i++)
            {
                // go through slowest path
                SetParentOperation(operations[i], parent);
            }

            return operations;
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

        private static IOperation WalkDownOperationToFindParent(
            HashSet<IOperation> operationAlreadyProcessed, IOperation operation, TextSpan span)
        {
            void EnqueueChildOperations(Queue<IOperation> queue, IOperation parent)
            {
                foreach (var o in parent.Children)
                {
                    queue.Enqueue(o);
                }
            }

            // do we have a pool for queue?
            var parentChildQueue = new Queue<IOperation>();
            EnqueueChildOperations(parentChildQueue, operation);

            // walk down the child operation to find parent operation
            // every child returned by the queue should already have Parent operation set
            IOperation child;
            while ((child = parentChildQueue.Dequeue()) != null)
            {
                if (!operationAlreadyProcessed.Add(child))
                {
                    // don't process IOperation we already processed otherwise,
                    // we can walk down same tree multiple times
                    continue;
                }

                if (child == operation)
                {
                    // parent found
                    return child.Parent;
                }

                if (!child.Syntax.FullSpan.IntersectsWith(span))
                {
                    // not related node, don't walk down
                    continue;
                }

                // queue children so that we can do breadth first search
                EnqueueChildOperations(parentChildQueue, child);
            }

            return null;
        }

        private IOperation SearchParentOperation()
        {
            // do we have a pool for hashset?
            var operationAlreadyProcessed = new HashSet<IOperation>();

            var targetNode = Syntax;
            var currentCandidate = targetNode.Parent;

            while (currentCandidate != null)
            {
                Debug.Assert(currentCandidate.FullSpan.IntersectsWith(targetNode.FullSpan));

                foreach (var childNode in currentCandidate.ChildNodes())
                {
                    if (!childNode.FullSpan.IntersectsWith(targetNode.FullSpan))
                    {
                        // skip unrelated node
                        continue;
                    }

                    // get child operation
                    var childOperation = _semanticModel.GetOperationInternal(childNode);
                    if (childOperation != null)
                    {
                        // there is no operation for this node
                        continue;
                    }

                    // record we have processed this node
                    if (!operationAlreadyProcessed.Add(childOperation))
                    {
                        // we already processed this tree. no need to dig down
                        continue;
                    }

                    // check easy case first
                    if (childOperation == this)
                    {
                        // found parent, go up the spine until we found non-null parent Operation
                        return currentCandidate.AncestorsAndSelf().Select(n => _semanticModel.GetOperationInternal(n)).WhereNotNull().FirstOrDefault();
                    }

                    // walk down child operation tree to see whether sub tree contains the given operation
                    var parent = WalkDownOperationToFindParent(operationAlreadyProcessed, childOperation, targetNode.FullSpan);
                    if (parent != null)
                    {
                        return parent;
                    }
                }

                currentCandidate = currentCandidate.Parent;
            }

            // root node. there is no parent
            return null;
        }
    }
}
