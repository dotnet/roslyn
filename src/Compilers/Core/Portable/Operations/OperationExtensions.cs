// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Operations
{
    public static partial class OperationExtensions
    {
        /// <summary>
        /// Helper function to simplify the access to the function pointer signature of an FunctionPointerInvocationOperation
        /// </summary>
        public static IMethodSymbol GetFunctionPointerSignature(this IFunctionPointerInvocationOperation functionPointer)
        {
            return ((IFunctionPointerTypeSymbol)functionPointer.Target.Type!).Signature;
        }

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
            var model = operation.SemanticModel;

            // An IOperation tree for a simple program includes statements from all compilation units involved,
            // but each model is tied to a single syntax tree.
            if (model is null || model.SyntaxTree != operation.Syntax.SyntaxTree)
            {
                model = compilation.GetSemanticModel(operation.Syntax.SyntaxTree);
            }

            if (model.IsSpeculativeSemanticModel)
            {
                // GetDiagnostics not supported for speculative semantic model.
                // https://github.com/dotnet/roslyn/issues/28075
                return false;
            }

            return model.GetDiagnostics(operation.Syntax.Span, cancellationToken).Any(static d => d.DefaultSeverity == DiagnosticSeverity.Error);
        }

        /// <summary>
        /// Returns all the descendant operations of the given <paramref name="operation"/> in evaluation order.
        /// </summary>
        /// <param name="operation">Operation whose descendants are to be fetched.</param>
        public static IEnumerable<IOperation> Descendants(this IOperation? operation)
        {
            return Descendants(operation, includeSelf: false);
        }

        /// <summary>
        /// Returns all the descendant operations of the given <paramref name="operation"/> including the given <paramref name="operation"/> in evaluation order.
        /// </summary>
        /// <param name="operation">Operation whose descendants are to be fetched.</param>
        public static IEnumerable<IOperation> DescendantsAndSelf(this IOperation? operation)
        {
            return Descendants(operation, includeSelf: true);
        }

        private static IEnumerable<IOperation> Descendants(IOperation? operation, bool includeSelf)
        {
            if (operation == null)
            {
                yield break;
            }

            if (includeSelf)
            {
                yield return operation;
            }

            var stack = ArrayBuilder<IOperation.OperationList.Enumerator>.GetInstance();
            stack.Push(operation.ChildOperations.GetEnumerator());

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
                    stack.Push(current.ChildOperations.GetEnumerator());
                }
            }

            stack.Free();
        }

        /// <summary>
        /// Gets all the declared local variables in the given <paramref name="declarationGroup"/>.
        /// </summary>
        /// <param name="declarationGroup">Variable declaration group</param>
        public static ImmutableArray<ILocalSymbol> GetDeclaredVariables(this IVariableDeclarationGroupOperation declarationGroup)
        {
            if (declarationGroup == null)
            {
                throw new ArgumentNullException(nameof(declarationGroup));
            }

            var arrayBuilder = ArrayBuilder<ILocalSymbol>.GetInstance();
            foreach (IVariableDeclarationOperation group in declarationGroup.Declarations)
            {
                group.GetDeclaredVariables(arrayBuilder);
            }

            return arrayBuilder.ToImmutableAndFree();
        }

        /// <summary>
        /// Gets all the declared local variables in the given <paramref name="declaration"/>.
        /// </summary>
        /// <param name="declaration">Variable declaration</param>
        public static ImmutableArray<ILocalSymbol> GetDeclaredVariables(this IVariableDeclarationOperation declaration)
        {
            if (declaration == null)
            {
                throw new ArgumentNullException(nameof(declaration));
            }

            var arrayBuilder = ArrayBuilder<ILocalSymbol>.GetInstance();
            declaration.GetDeclaredVariables(arrayBuilder);
            return arrayBuilder.ToImmutableAndFree();
        }

        private static void GetDeclaredVariables(this IVariableDeclarationOperation declaration, ArrayBuilder<ILocalSymbol> arrayBuilder)
        {
            foreach (var decl in declaration.Declarators)
            {
                arrayBuilder.Add(decl.Symbol);
            }
        }

        /// <summary>
        /// Gets the variable initializer for the given <paramref name="declarationOperation"/>, checking to see if there is a parent initializer
        /// if the single variable initializer is null.
        /// </summary>
        /// <param name="declarationOperation">Single variable declaration to retrieve initializer for.</param>
        public static IVariableInitializerOperation? GetVariableInitializer(this IVariableDeclaratorOperation declarationOperation)
        {
            if (declarationOperation == null)
            {
                throw new ArgumentNullException(nameof(declarationOperation));
            }

            return declarationOperation.Initializer ?? (declarationOperation.Parent as IVariableDeclarationOperation)?.Initializer;
        }

        /// <summary>
        /// Get an optional argument name for a named argument to the given <paramref name="dynamicOperation"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="dynamicOperation">Dynamic or late bound operation.</param>
        /// <param name="index">Argument index.</param>
        public static string? GetArgumentName(this IDynamicInvocationOperation dynamicOperation, int index)
        {
            if (dynamicOperation == null)
            {
                throw new ArgumentNullException(nameof(dynamicOperation));
            }

            return GetArgumentName((HasDynamicArgumentsExpression)dynamicOperation, index);
        }

        /// <summary>
        /// Get an optional argument name for a named argument to the given <paramref name="dynamicOperation"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="dynamicOperation">Dynamic or late bound operation.</param>
        /// <param name="index">Argument index.</param>
        public static string? GetArgumentName(this IDynamicIndexerAccessOperation dynamicOperation, int index)
        {
            if (dynamicOperation == null)
            {
                throw new ArgumentNullException(nameof(dynamicOperation));
            }

            return GetArgumentName((HasDynamicArgumentsExpression)dynamicOperation, index);
        }

        /// <summary>
        /// Get an optional argument name for a named argument to the given <paramref name="dynamicOperation"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="dynamicOperation">Dynamic or late bound operation.</param>
        /// <param name="index">Argument index.</param>
        public static string? GetArgumentName(this IDynamicObjectCreationOperation dynamicOperation, int index)
        {
            if (dynamicOperation == null)
            {
                throw new ArgumentNullException(nameof(dynamicOperation));
            }

            return GetArgumentName((HasDynamicArgumentsExpression)dynamicOperation, index);
        }

        /// <summary>
        /// Get an optional argument name for a named argument to the given <paramref name="dynamicOperation"/> at the given <paramref name="index"/>.
        /// </summary>
        /// <param name="dynamicOperation">Dynamic or late bound operation.</param>
        /// <param name="index">Argument index.</param>
        internal static string? GetArgumentName(this HasDynamicArgumentsExpression dynamicOperation, int index)
        {
            if (dynamicOperation.Arguments.IsDefaultOrEmpty)
            {
                throw new InvalidOperationException();
            }

            if (index < 0 || index >= dynamicOperation.Arguments.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var argumentNames = dynamicOperation.ArgumentNames;
            return argumentNames.IsDefaultOrEmpty ? null : argumentNames[index];
        }

        /// <summary>
        /// Get an optional argument <see cref="RefKind"/> for an argument at the given <paramref name="index"/> to the given <paramref name="dynamicOperation"/>.
        /// Returns a non-null argument <see cref="RefKind"/> for C#.
        /// Always returns null for VB as <see cref="RefKind"/> cannot be specified for an argument in VB.
        /// </summary>
        /// <param name="dynamicOperation">Dynamic or late bound operation.</param>
        /// <param name="index">Argument index.</param>
        public static RefKind? GetArgumentRefKind(this IDynamicInvocationOperation dynamicOperation, int index)
        {
            if (dynamicOperation == null)
            {
                throw new ArgumentNullException(nameof(dynamicOperation));
            }

            return GetArgumentRefKind((HasDynamicArgumentsExpression)dynamicOperation, index);
        }

        /// <summary>
        /// Get an optional argument <see cref="RefKind"/> for an argument at the given <paramref name="index"/> to the given <paramref name="dynamicOperation"/>.
        /// Returns a non-null argument <see cref="RefKind"/> for C#.
        /// Always returns null for VB as <see cref="RefKind"/> cannot be specified for an argument in VB.
        /// </summary>
        /// <param name="dynamicOperation">Dynamic or late bound operation.</param>
        /// <param name="index">Argument index.</param>
        public static RefKind? GetArgumentRefKind(this IDynamicIndexerAccessOperation dynamicOperation, int index)
        {
            if (dynamicOperation == null)
            {
                throw new ArgumentNullException(nameof(dynamicOperation));
            }

            return GetArgumentRefKind((HasDynamicArgumentsExpression)dynamicOperation, index);
        }

        /// <summary>
        /// Get an optional argument <see cref="RefKind"/> for an argument at the given <paramref name="index"/> to the given <paramref name="dynamicOperation"/>.
        /// Returns a non-null argument <see cref="RefKind"/> for C#.
        /// Always returns null for VB as <see cref="RefKind"/> cannot be specified for an argument in VB.
        /// </summary>
        /// <param name="dynamicOperation">Dynamic or late bound operation.</param>
        /// <param name="index">Argument index.</param>
        public static RefKind? GetArgumentRefKind(this IDynamicObjectCreationOperation dynamicOperation, int index)
        {
            if (dynamicOperation == null)
            {
                throw new ArgumentNullException(nameof(dynamicOperation));
            }

            return GetArgumentRefKind((HasDynamicArgumentsExpression)dynamicOperation, index);
        }

        internal static RefKind? GetArgumentRefKind(this HasDynamicArgumentsExpression dynamicOperation, int index)
        {
            if (dynamicOperation.Arguments.IsDefaultOrEmpty)
            {
                throw new InvalidOperationException();
            }

            if (index < 0 || index >= dynamicOperation.Arguments.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var argumentRefKinds = dynamicOperation.ArgumentRefKinds;
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

        /// <summary>
        /// Gets the root operation for the <see cref="IOperation"/> tree containing the given <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation">Operation whose root is requested.</param>
        internal static IOperation GetRootOperation(this IOperation operation)
        {
            Debug.Assert(operation != null);

            while (operation.Parent != null)
            {
                operation = operation.Parent;
            }

            return operation;
        }

        /// <summary>
        /// Gets either a loop or a switch operation that corresponds to the given branch operation.
        /// </summary>
        /// <param name="operation">The branch operation for which a corresponding operation is looked up</param>
        /// <returns>The corresponding operation or <c>null</c> in case not found (e.g. no loop or switch syntax, or the branch is not a break or continue)</returns>
        /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null</exception>
        /// <exception cref="InvalidOperationException">The operation is a part of Control Flow Graph</exception>
        public static IOperation? GetCorrespondingOperation(this IBranchOperation operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            if (operation.SemanticModel == null)
            {
                throw new InvalidOperationException(CodeAnalysisResources.OperationMustNotBeControlFlowGraphPart);
            }

            if (operation.BranchKind != BranchKind.Break && operation.BranchKind != BranchKind.Continue)
            {
                return null;
            }

            if (operation.Target == null)
            {
                return null;
            }

            for (IOperation current = operation; current.Parent != null; current = current.Parent)
            {
                switch (current)
                {
                    case ILoopOperation correspondingLoop when operation.Target.Equals(correspondingLoop.ExitLabel) ||
                                                               operation.Target.Equals(correspondingLoop.ContinueLabel):
                        return correspondingLoop;
                    case ISwitchOperation correspondingSwitch when operation.Target.Equals(correspondingSwitch.ExitLabel):
                        return correspondingSwitch;
                }
            }

            return null;
        }

        internal static ConstantValue? GetConstantValue(this IOperation operation)
        {
            return ((Operation)operation).OperationConstantValue;
        }
    }
}
