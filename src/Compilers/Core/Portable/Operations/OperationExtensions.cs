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
        internal static bool HasErrors(this IOperation operation, Compilation compilation, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

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
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

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
            if (declarationStatement == null)
            {
                throw new ArgumentNullException(nameof(declarationStatement));
            }

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

        /// <summary>
        /// Get an optional argument name for a named argument to the given <paramref name="dynamicExpression"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="dynamicExpression">Dynamic or late bound expression.</param>
        /// <param name="index">Argument index.</param>
        public static string GetArgumentName(this IDynamicInvocationExpression dynamicExpression, int index)
        {
            if (dynamicExpression == null)
            {
                throw new ArgumentNullException(nameof(dynamicExpression));
            }

            return GetArgumentName((HasDynamicArgumentsExpression)dynamicExpression, index);
        }

        /// <summary>
        /// Get an optional argument name for a named argument to the given <paramref name="dynamicExpression"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="dynamicExpression">Dynamic or late bound expression.</param>
        /// <param name="index">Argument index.</param>
        public static string GetArgumentName(this IDynamicIndexerAccessExpression dynamicExpression, int index)
        {
            if (dynamicExpression == null)
            {
                throw new ArgumentNullException(nameof(dynamicExpression));
            }

            return GetArgumentName((HasDynamicArgumentsExpression)dynamicExpression, index);
        }

        /// <summary>
        /// Get an optional argument name for a named argument to the given <paramref name="dynamicExpression"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="dynamicExpression">Dynamic or late bound expression.</param>
        /// <param name="index">Argument index.</param>
        public static string GetArgumentName(this IDynamicObjectCreationExpression dynamicExpression, int index)
        {
            if (dynamicExpression == null)
            {
                throw new ArgumentNullException(nameof(dynamicExpression));
            }

            return GetArgumentName((HasDynamicArgumentsExpression)dynamicExpression, index);
        }

        /// <summary>
        /// Get an optional argument name for a named argument to the given <paramref name="dynamicExpression"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="dynamicExpression">Dynamic or late bound expression.</param>
        /// <param name="index">Argument index.</param>
        internal static string GetArgumentName(this HasDynamicArgumentsExpression dynamicExpression, int index)
        {
            if (dynamicExpression.Arguments.IsDefaultOrEmpty)
            {
                throw new InvalidOperationException();
            }

            if (index < 0 || index >= dynamicExpression.Arguments.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var argumentNames = dynamicExpression.ArgumentNames;
            return argumentNames.IsDefaultOrEmpty ? null : argumentNames[index];
        }

        /// <summary>
        /// Get an optional argument <see cref="RefKind"/> for an argument at the given <paramref name="index"/> to the given <paramref name="dynamicExpression"/>.
        /// Returns a non-null argument <see cref="RefKind"/> for C#.
        /// Always returns null for VB as <see cref="RefKind"/> cannot be specified for an the argument in VB.
        /// </summary>
        /// <param name="dynamicExpression">Dynamic or late bound expression.</param>
        /// <param name="index">Argument index.</param>
        public static RefKind? GetArgumentRefKind(this IDynamicInvocationExpression dynamicExpression, int index)
        {
            if (dynamicExpression == null)
            {
                throw new ArgumentNullException(nameof(dynamicExpression));
            }

            return GetArgumentRefKind((HasDynamicArgumentsExpression)dynamicExpression, index);
        }

        /// <summary>
        /// Get an optional argument <see cref="RefKind"/> for an argument at the given <paramref name="index"/> to the given <paramref name="dynamicExpression"/>.
        /// Returns a non-null argument <see cref="RefKind"/> for C#.
        /// Always returns null for VB as <see cref="RefKind"/> cannot be specified for an the argument in VB.
        /// </summary>
        /// <param name="dynamicExpression">Dynamic or late bound expression.</param>
        /// <param name="index">Argument index.</param>
        public static RefKind? GetArgumentRefKind(this IDynamicIndexerAccessExpression dynamicExpression, int index)
        {
            if (dynamicExpression == null)
            {
                throw new ArgumentNullException(nameof(dynamicExpression));
            }

            return GetArgumentRefKind((HasDynamicArgumentsExpression)dynamicExpression, index);
        }

        /// <summary>
        /// Get an optional argument <see cref="RefKind"/> for an argument at the given <paramref name="index"/> to the given <paramref name="dynamicExpression"/>.
        /// Returns a non-null argument <see cref="RefKind"/> for C#.
        /// Always returns null for VB as <see cref="RefKind"/> cannot be specified for an the argument in VB.
        /// </summary>
        /// <param name="dynamicExpression">Dynamic or late bound expression.</param>
        /// <param name="index">Argument index.</param>
        public static RefKind? GetArgumentRefKind(this IDynamicObjectCreationExpression dynamicExpression, int index)
        {
            if (dynamicExpression == null)
            {
                throw new ArgumentNullException(nameof(dynamicExpression));
            }

            return GetArgumentRefKind((HasDynamicArgumentsExpression)dynamicExpression, index);
        }

        internal static RefKind? GetArgumentRefKind(this HasDynamicArgumentsExpression dynamicExpression, int index)
        {
            if (dynamicExpression.Arguments.IsDefaultOrEmpty)
            {
                throw new InvalidOperationException();
            }

            if (index < 0 || index >= dynamicExpression.Arguments.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var argumentRefKinds = dynamicExpression.ArgumentRefKinds;
            if (argumentRefKinds.IsDefault)
            {
                // VB case, arguments cannot have RefKind.
                return null;
            }

            if (argumentRefKinds.IsEmpty)
            {
                // C# case where no explicit RefKind was specified for any argument, hence all arguments have RefKind.None.
                return RefKind.None;
            }

            return argumentRefKinds[index];
        }
    }

    internal interface ISymbolWithOperation
    {
        IOperation GetRootOperation(CancellationToken cancellationToken);
    }
}
