// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Semantics
{
    internal interface IOperationSearchable
    {
        IEnumerable<IOperation> Descendants();
        IEnumerable<IOperation> DescendantsAndSelf();
    }

    public static class OperationExtensions
    {
        public static IEnumerable<IOperation> Descendants(this IOperation operation)
        {
            var searchable = operation as IOperationSearchable;
            if (searchable != null)
            {
                return searchable.Descendants();
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<IOperation>();
            }
        }

        public static IEnumerable<IOperation> DescendantsAndSelf(this IOperation operation)
        {
            var searchable = operation as IOperationSearchable;
            if (searchable != null)
            {
                return searchable.DescendantsAndSelf();
            }
            else
            {
                return SpecializedCollections.EmptyEnumerable<IOperation>();
            }
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
    }

    internal interface ISymbolWithOperation
    {
        IOperation GetRootOperation(CancellationToken cancellationToken);
    }
}