// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Semantics
{
    public static partial class OperationExtensions
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
            return Descendants(operation, includeSelf: false);
        }

        public static IEnumerable<IOperation> DescendantsAndSelf(this IOperation operation)
        {
            return Descendants(operation, includeSelf: true);
        }

        private static IEnumerable<IOperation> Descendants(IOperation operation, bool includeSelf)
        {
            if (operation == null)
            {
                yield break;
            }

            if (includeSelf)
            {
                yield return operation;
            }

            var stack = ArrayBuilder<IEnumerator<IOperation>>.GetInstance();
            stack.Push(operation.Children.GetEnumerator());

            while (stack.Any())
            {
                var iterator = stack.Pop();

                if (!iterator.MoveNext())
                {
                    continue;
                }

                var current = iterator.Current;

                // push current iterator back in to the stack
                stack.Push(iterator);

                // push children iterator to the stack
                if (current != null)
                {
                    yield return current;
                    stack.Push(current.Children.GetEnumerator());
                }
            }

            stack.Free();
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
    }

    internal interface ISymbolWithOperation
    {
        IOperation GetRootOperation(CancellationToken cancellationToken);
    }
}
