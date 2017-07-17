// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Semantics
{
    public static class OperationExtensions
    {
        /// <summary>
        /// This will check whether context around the operation has any error such as syntax or semantic error
        /// </summary>
        public static bool HasErrors(this IOperation operation, Compilation compilation, CancellationToken cancellationToken = default(CancellationToken))
        {
            // once we made sure every operation has Syntax, we will remove this condition
            if (operation.Syntax == null)
            {
                return true;
            }

            // if wrong compilation is given, GetSemanticModel will throw due to tree not belong to the given compilation.
            var model = compilation.GetSemanticModel(operation.Syntax.SyntaxTree);
            return model.GetDiagnostics(operation.Syntax.Span, cancellationToken).Any(d => d.DefaultSeverity == DiagnosticSeverity.Error);
        }

        public static IEnumerable<IOperation> Descendants(this IOperation operation)
        {
            if (operation == null)
            {
                return SpecializedCollections.EmptyEnumerable<IOperation>();
            }
            var list = new List<IOperation>();
            var collector = new OperationCollector(list);
            collector.Visit(operation);
            list.RemoveAt(0);
            return list;
        }

        public static IEnumerable<IOperation> DescendantsAndSelf(this IOperation operation)
        {
            if (operation == null)
            {
                return SpecializedCollections.EmptyEnumerable<IOperation>();
            }
            var list = new List<IOperation>();
            var collector = new OperationCollector(list);
            collector.Visit(operation);
            return list;
        }

        public static IOperation GetRootOperation(this ISymbol symbol, CancellationToken cancellationToken = default(CancellationToken))
        {
            var symbolWithOperation = symbol as ISymbolWithOperation;
            if (symbolWithOperation != null)
            {
                return symbolWithOperation.GetRootOperation(cancellationToken);
            }
            else
            {
                return null;
            }
        }

        public static ImmutableArray<ILocalSymbol> GetDeclaredVariables(this IVariableDeclarationStatement declarationStatement)
        {
            var arrayBuilder = ArrayBuilder<ILocalSymbol>.GetInstance();
            foreach (IVariableDeclaration group in declarationStatement.Declarations)
            {
                foreach (ILocalSymbol symbol in group.Variables)
                {
                    arrayBuilder.Add(symbol);
                }
            }

            return arrayBuilder.ToImmutableAndFree();
        }

        private static IOperation WalkDownOperationToFindParent(
            HashSet<IOperation> operationAlreadyProcessed, IOperation operation, TextSpan span, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            void EnqueueChildOperations(Queue<(IOperation, IOperation)> queue, IOperation parent)
            {
                foreach (var o in parent.Children)
                {
                    queue.Enqueue((parent, o));
                }
            }

            // do we have a pool for queue?
            var parentChildQueue = new Queue<(IOperation, IOperation)>();
            EnqueueChildOperations(parentChildQueue, operation);

            // walk down the child operation to find parent operation
            while (true)
            {
                var (parent, child) = parentChildQueue.Dequeue();
                if (parent == null)
                {
                    // no more item
                    break;
                }

                if (!operationAlreadyProcessed.Add(child))
                {
                    // don't process IOperation we already processed otherwise,
                    // we can walk down same tree multiple times
                    continue;
                }

                if (child == operation)
                {
                    // parent found
                    return parent;
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

        internal static IOperation FindParentOperation(this SemanticModel semanticModel, IOperation operation, CancellationToken cancellationToken)
        {
            Debug.Assert(operation != null);

            // do we have a pool for hashset?
            var operationAlreadyProcessed = new HashSet<IOperation>();

            var targetNode = operation.Syntax;
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
                    var childOperation = semanticModel.GetOperationInternal(childNode, cancellationToken);
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
                    if (childOperation == operation)
                    {
                        // found parent, go up the spine until we found non-null parent Operation
                        return currentCandidate.AncestorsAndSelf().Select(n => semanticModel.GetOperationInternal(n)).WhereNotNull().FirstOrDefault();
                    }

                    // walk down child operation tree to see whether sub tree contains the given operation
                    var parent = WalkDownOperationToFindParent(operationAlreadyProcessed, childOperation, targetNode.FullSpan, cancellationToken);
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

        private sealed class OperationCollector : OperationWalker
        {
            private readonly List<IOperation> _list;

            public OperationCollector(List<IOperation> list)
            {
                _list = list;
            }

            public override void Visit(IOperation operation)
            {
                if (operation != null)
                {
                    _list.Add(operation);
                }
                base.Visit(operation);
            }
        }
    }

    internal interface ISymbolWithOperation
    {
        IOperation GetRootOperation(CancellationToken cancellationToken);
    }
}
