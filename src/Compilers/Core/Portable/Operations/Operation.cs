// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Root type for representing the abstract semantics of C# and VB statements and expressions.
    /// </summary>
    internal abstract class Operation : IOperation
    {
        protected static readonly IOperation s_unset = new EmptyOperation(
            semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: true);
        protected static readonly IBlockOperation s_unsetBlock = new BlockOperation(
            operations: ImmutableArray<IOperation>.Empty, locals: default, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: true);
        protected static readonly IArrayInitializerOperation s_unsetArrayInitializer = new ArrayInitializerOperation(
            elementValues: ImmutableArray<IOperation>.Empty, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: true);
        protected static readonly IEventReferenceOperation s_unsetEventReference = new EventReferenceOperation(
            @event: null, instance: null, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: true);
        protected static readonly IObjectOrCollectionInitializerOperation s_unsetObjectOrCollectionInitializer = new ObjectOrCollectionInitializerOperation(
            initializers: ImmutableArray<IOperation>.Empty, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: true);
        protected static readonly IPatternOperation s_unsetPattern = new ConstantPatternOperation(
            value: null, inputType: null, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: true);
        protected static readonly IVariableDeclarationGroupOperation s_unsetVariableDeclarationGroup = new VariableDeclarationGroupOperation(
            declarations: ImmutableArray<IVariableDeclarationOperation>.Empty, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: true);
        protected static readonly IVariableInitializerOperation s_unsetVariableInitializer = new VariableInitializerOperation(
            locals: ImmutableArray<ILocalSymbol>.Empty, value: null, semanticModel: null, syntax: null, type: null, constantValue: default, isImplicit: false);
        private readonly SemanticModel _owningSemanticModelOpt;

        // this will be lazily initialized. this will be initialized only once
        // but once initialized, will never change
        private IOperation _parentDoNotAccessDirectly;

        protected Operation(OperationKind kind, SemanticModel semanticModel, SyntaxNode syntax, ITypeSymbol type, Optional<object> constantValue, bool isImplicit)
        {
            // Constant value cannot be "null" for non-nullable value type operations.
            Debug.Assert(type?.IsValueType != true || ITypeSymbolHelpers.IsNullableType(type) || !constantValue.HasValue || constantValue.Value != null);

#if DEBUG
            if (semanticModel != null)
            {
                Debug.Assert(semanticModel.ContainingModelOrSelf != null);
                if (semanticModel.IsSpeculativeSemanticModel)
                {
                    Debug.Assert(semanticModel.ContainingModelOrSelf == semanticModel);
                }
                else
                {
                    Debug.Assert(semanticModel.ContainingModelOrSelf != semanticModel);
                    Debug.Assert(semanticModel.ContainingModelOrSelf.ContainingModelOrSelf == semanticModel.ContainingModelOrSelf);
                }
            }
#endif
            _owningSemanticModelOpt = semanticModel;

            Kind = kind;
            Syntax = syntax;
            Type = type;
            ConstantValue = constantValue;
            IsImplicit = isImplicit;

            _parentDoNotAccessDirectly = s_unset;
        }

        /// <summary>
        /// IOperation that has this operation as a child
        /// </summary>
        public IOperation Parent
        {
            get
            {
                if (_parentDoNotAccessDirectly == s_unset)
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

        SemanticModel IOperation.SemanticModel => _owningSemanticModelOpt?.ContainingModelOrSelf;

        /// <summary>
        /// Gets the owning semantic model for this operation node.
        /// Note that this may be different than <see cref="IOperation.SemanticModel"/>, which
        /// is the semantic model on which <see cref="SemanticModel.GetOperation(SyntaxNode, CancellationToken)"/> was invoked
        /// to create this node.
        /// </summary>
        internal SemanticModel OwningSemanticModel => _owningSemanticModelOpt;

        public abstract void Accept(OperationVisitor visitor);

        public abstract TResult Accept<TArgument, TResult>(OperationVisitor<TArgument, TResult> visitor, TArgument argument);

        protected void SetParentOperation(IOperation parent)
        {
            var result = Interlocked.CompareExchange(ref _parentDoNotAccessDirectly, parent, s_unset);

            // tree must belong to same semantic model if parent is given
            Debug.Assert(parent == null || ((Operation)parent).OwningSemanticModel == OwningSemanticModel ||
                ((Operation)parent).OwningSemanticModel == null || OwningSemanticModel == null);

            // make sure given parent and one we already have is same if we have one already
            Debug.Assert(result == s_unset || result == parent);
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
            if ((operations[0] as Operation)?._parentDoNotAccessDirectly != s_unset)
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

        [Conditional("DEBUG")]
        internal static void VerifyParentOperation(IOperation parent, IOperation child)
        {
            if (child is object)
            {
                Debug.Assert((object)child.Parent == parent);
            }
        }

        [Conditional("DEBUG")]
        internal static void VerifyParentOperation<T>(IOperation parent, ImmutableArray<T> children) where T : IOperation
        {
            Debug.Assert(!children.IsDefault);
            foreach (var child in children)
            {
                VerifyParentOperation(parent, child);
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

            if (OwningSemanticModel.Root == Syntax)
            {
                // this is the root
                return null;
            }

            var currentCandidate = Syntax.Parent;

            try
            {
                while (currentCandidate != null)
                {
                    // get operation
                    var tree = OwningSemanticModel.GetOperation(currentCandidate);
                    if (tree != null)
                    {
                        // walk down operation tree to see whether this tree contains parent of this operation
                        var parent = WalkDownOperationToFindParent(operationAlreadyProcessed, tree);
                        if (parent != null)
                        {
                            return parent;
                        }
                    }

                    if (OwningSemanticModel.Root == currentCandidate)
                    {
                        // reached top of parent chain
                        break;
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
