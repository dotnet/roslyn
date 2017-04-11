// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Semantics
{
    public static class OperationExtensions
    {
        /// <summary>
        /// Find the argument supplied for a given parameter of the target method.
        /// </summary>
        /// <param name="hasArgumentsExpression">The IHasArgumentsExpression object to get matching argument object from.</param>
        /// <param name="parameter">Parameter of the target method.</param>
        /// <returns>Argument corresponding to the parameter.</returns>
        public static IArgument GetArgumentMatchingParameter(this IHasArgumentsExpression hasArgumentsExpression, IParameterSymbol parameter)
        {
            if (hasArgumentsExpression == null || parameter == null)
            {
                return null;
            }

            foreach (var argument in hasArgumentsExpression.ArgumentsInEvaluationOrder)
            {
                if (argument.Parameter == parameter)
                {
                    return argument;
                }
            }

            return null;
        }

        /// <summary>
        /// Arguments of the invocation, excluding the instance argument. Arguments are in parameter order,
        /// and params/ParamArray arguments have been collected into arrays. Default values are supplied for
        /// optional arguments missing in source.
        /// </summary>
        public static ImmutableArray<IArgument> ArgumentsInParameterOrder(this IHasArgumentsExpression hasArgumentsExpression)
        {
            ImmutableArray<IArgument> argumentsInEvaluationOrder = hasArgumentsExpression.ArgumentsInEvaluationOrder;

            // If all of the arguments were specified positionally the evaluation order is the same as the parameter order.
            if (ArgumentsAreInParameterOrder(argumentsInEvaluationOrder))
            {
                return argumentsInEvaluationOrder;
            }

            return argumentsInEvaluationOrder.Sort(
                (x, y) =>
                {
                    int x1 = x.Parameter?.Ordinal ?? -1;
                    int y1 = y.Parameter?.Ordinal ?? -1;

                    return x1 - y1;
                });

            bool ArgumentsAreInParameterOrder(ImmutableArray<IArgument> arguments)
            {
                for (int argumentIndex = 0; argumentIndex < arguments.Length; argumentIndex++)
                {
                    if (arguments[argumentIndex].Parameter?.Ordinal != argumentIndex)
                    {
                        return false;
                    }
                }  

                return true;
            }
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