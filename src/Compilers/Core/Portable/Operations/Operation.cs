﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements and expressions.
    /// </summary>
    internal abstract class Operation : IOperation
    {
        internal readonly SemanticModel SemanticModel;

        // this will be lazily initialized. this will be initialized only once
        // but once initialized, will never change
        private IOperation _parentDoNotAccessDirectly;

        public Operation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
        {
            SemanticModel = semanticModel;

            Kind = kind;
            Syntax = syntax;
            Type = type;
            ConstantValue = constantValue;
            IsImplicit = isImplicit;
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
                    SetParentOperation(SearchParentOperation());
                }

                return _parentDoNotAccessDirectly;
            }
        }

        /// <summary>
        /// Set to True if compiler generated /implicitly computed by compiler code
        /// </summary>
        public bool IsImplicit { get; }

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

        protected void SetParentOperation(IOperation parent)
        {
            var result = Interlocked.CompareExchange(ref _parentDoNotAccessDirectly, parent, null);

            // tree must belong to same semantic model if parent is given
            Debug.Assert(parent == null || ((Operation)parent).SemanticModel == SemanticModel);

            // make sure given parent and one we already have is same if we have one already
            Debug.Assert(result == null || result == parent);
        }

        /// <summary>
        /// Create <see cref="IOperation"/> of <see cref="OperationKind.None"/> with explicit children
        /// 
        /// Use this to create IOperation when we don't have proper specific IOperation yet for given language construct
        /// </summary>
        public static IOperation CreateOperationNone(SemanticModel semanticModel, SyntaxNode node, Optional<object> constantValue, Func<ImmutableArray<IOperation>> getChildren, bool isImplicit)
        {
            return new NoneOperation(semanticModel, node, constantValue, getChildren, isImplicit);
        }

        public static T SetParentOperation<T>(T operation, IOperation parent) where T : IOperation
        {
            // explicit cast is not allowed, so using "as" instead
            (operation as Operation)?.SetParentOperation(parent);
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

            // race is okay. penalty is going through a loop one more time or 
            // .Parent going through slower path of SearchParentOperation()
            // explicit cast is not allowed, so using "as" instead
            // invalid expression can have null element in the array
            if ((operations[0] as Operation)?._parentDoNotAccessDirectly != null)
            {
                // most likely already initialized. if not, due to a race or invalid expression,
                // operation.Parent will take slower path but still return correct Parent.
                return operations;
            }

            foreach (var operation in operations)
            {
                // go through slowest path
                SetParentOperation(operation, parent);
            }

            return operations;
        }

        public static T ResetParentOperation<T>(T operation) where T : IOperation
        {
            if (operation == null)
            {
                return operation;
            }

            Interlocked.Exchange(ref (operation as Operation)._parentDoNotAccessDirectly, null);
            return operation;
        }

        private class NoneOperation : Operation
        {
            private readonly Lazy<ImmutableArray<IOperation>> _lazyChildren;

            public NoneOperation(SemanticModel semanticModel, SyntaxNode node, Optional<object> constantValue, Func<ImmutableArray<IOperation>> getChildren, bool isImplicit) :
                base(OperationKind.None, semanticModel, node, type: null, constantValue: constantValue, isImplicit: isImplicit)
            {
                _lazyChildren = new Lazy<ImmutableArray<IOperation>>(getChildren);
            }

            public override void Accept(OperationVisitor visitor)
            {
                visitor.VisitNoneOperation(this);
            }

            public override TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument)
            {
                return visitor.VisitNoneOperation(this, argument);
            }

            public override IEnumerable<IOperation> Children
            {
                get
                {
                    foreach (var child in _lazyChildren.Value)
                    {
                        if (child == null)
                        {
                            continue;
                        }

                        yield return Operation.SetParentOperation(child, this);
                    }
                }
            }
        }

        private static readonly ObjectPool<Queue<IOperation>> s_queuePool =
            new ObjectPool<Queue<IOperation>>(() => new Queue<IOperation>(), 10);

        private IOperation WalkDownOperationToFindParent(HashSet<IOperation> operationAlreadyProcessed, IOperation root)
        {
            void EnqueueChildOperations(Queue<IOperation> queue, IOperation parent)
            {
                // for now, children can return null. once we fix the issue, children should never return null
                // https://github.com/dotnet/roslyn/issues/21196
                foreach (var o in parent.Children.WhereNotNull())
                {
                    queue.Enqueue(o);
                }
            }

            var operationQueue = s_queuePool.Allocate();

            try
            {
                EnqueueChildOperations(operationQueue, root);

                // walk down the tree to find parent operation
                // every operation returned by the queue should already have Parent operation set
                while (operationQueue.Count > 0)
                {
                    var operation = operationQueue.Dequeue();

                    if (!operationAlreadyProcessed.Add(operation))
                    {
                        // don't process IOperation we already processed otherwise,
                        // we can walk down same tree multiple times
                        continue;
                    }

                    if (operation == this)
                    {
                        // parent found
                        return operation.Parent;
                    }

                    // It can't filter visiting children by node span since IOperation
                    // might have children which belong to sibling but not direct spine
                    // of sub tree.

                    // queue children so that we can do breadth first search
                    EnqueueChildOperations(operationQueue, operation);
                }

                return null;
            }
            finally
            {
                operationQueue.Clear();
                s_queuePool.Free(operationQueue);
            }
        }

        // internal for testing
        internal IOperation SearchParentOperation()
        {
            var operationAlreadyProcessed = PooledHashSet<IOperation>.GetInstance();

            // start from current node since one node can have multiple operations mapped to
            var currentCandidate = Syntax;

            try
            {
                while (currentCandidate != null)
                {
                    if (!SemanticModel.Root.FullSpan.Contains(currentCandidate.FullSpan))
                    {
                        // reached top of parent chain
                        break;
                    }

                    // get operation
                    var tree = SemanticModel.GetOperationInternal(currentCandidate);
                    if (tree != null)
                    {
                        // walk down operation tree to see whether this tree contains parent of this operation
                        var parent = WalkDownOperationToFindParent(operationAlreadyProcessed, tree);
                        if (parent != null)
                        {
                            return parent;
                        }
                    }

                    // move up the tree
                    currentCandidate = currentCandidate.Parent;
                }

                // root node. there is no parent
                return null;
            }
            finally
            {
                // put the hashset back to the pool
                operationAlreadyProcessed.Free();
            }
        }
    }
}
