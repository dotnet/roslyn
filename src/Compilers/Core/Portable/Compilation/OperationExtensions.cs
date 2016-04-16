// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Semantics
{
    public static class OperationExtensions
    {
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