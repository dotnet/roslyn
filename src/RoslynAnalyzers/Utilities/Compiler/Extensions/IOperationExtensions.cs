// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Analyzer.Utilities.Extensions
{
    internal static partial class IOperationExtensions
    {
        /// <summary>
        /// Gets the receiver type for an invocation expression (i.e. type of 'A' in invocation 'A.B()')
        /// If the invocation actually involves a conversion from A to some other type, say 'C', on which B is invoked,
        /// then this method returns type A if <paramref name="beforeConversion"/> is true, and C if false.
        /// </summary>
        public static ITypeSymbol? GetReceiverType(this IInvocationOperation invocation, Compilation compilation, bool beforeConversion, CancellationToken cancellationToken)
        {
            if (invocation.Instance != null)
            {
                return beforeConversion ?
                    GetReceiverType(invocation.Instance.Syntax, compilation, cancellationToken) :
                    invocation.Instance.Type;
            }
            else if (invocation.TargetMethod.IsExtensionMethod && !invocation.TargetMethod.Parameters.IsEmpty)
            {
                var firstArg = invocation.Arguments.FirstOrDefault();
                if (firstArg != null)
                {
                    return beforeConversion ?
                        GetReceiverType(firstArg.Value.Syntax, compilation, cancellationToken) :
                        firstArg.Value.Type;
                }
                else if (invocation.TargetMethod.Parameters[0].IsParams)
                {
                    return invocation.TargetMethod.Parameters[0].Type;
                }
            }

            return null;
        }

        private static ITypeSymbol? GetReceiverType(SyntaxNode receiverSyntax, Compilation compilation, CancellationToken cancellationToken)
        {
            var model = compilation.GetSemanticModel(receiverSyntax.SyntaxTree);
            var typeInfo = model.GetTypeInfo(receiverSyntax, cancellationToken);
            return typeInfo.Type;
        }

        public static bool HasNullConstantValue(this IOperation operation)
        {
            return operation.ConstantValue.HasValue && operation.ConstantValue.Value == null;
        }

        public static bool TryGetBoolConstantValue(this IOperation operation, out bool constantValue)
        {
            if (operation.ConstantValue.HasValue && operation.ConstantValue.Value is bool value)
            {
                constantValue = value;
                return true;
            }

            constantValue = false;
            return false;
        }

        /// <summary>
        /// Gets explicit descendants or self of the given <paramref name="operation"/> that have no explicit ancestor in
        /// the operation tree rooted at <paramref name="operation"/>.
        /// </summary>
        /// <param name="operation">Operation</param>
        public static ImmutableArray<IOperation> GetTopmostExplicitDescendants(this IOperation operation)
        {
            if (!operation.IsImplicit)
            {
                return ImmutableArray.Create(operation);
            }

            var builder = ImmutableArray.CreateBuilder<IOperation>();
            var operationsToProcess = new Queue<IOperation>();
            operationsToProcess.Enqueue(operation);

            while (operationsToProcess.Count > 0)
            {
                operation = operationsToProcess.Dequeue();
                if (!operation.IsImplicit)
                {
                    builder.Add(operation);
                }
                else
                {
#pragma warning disable CS0618 // 'IOperation.Children' is obsolete: 'This API has performance penalties, please use ChildOperations instead.'
                    foreach (var child in operation.Children)
#pragma warning restore CS0618 // 'IOperation.Children' is obsolete: 'This API has performance penalties, please use ChildOperations instead.'
                    {
                        operationsToProcess.Enqueue(child);
                    }
                }
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// True if this operation has no IOperation API support, i.e. <see cref="OperationKind.None"/> and
        /// is the root operation, i.e. <see cref="Operation.Parent"/> is null.
        /// For example, this returns true for attribute operations.
        /// </summary>
        public static bool IsOperationNoneRoot(this IOperation operation)
        {
            return operation is { Kind: OperationKind.None, Parent: null };
        }

        /// <summary>
        /// Gets the first ancestor of this operation with:
        ///  1. Specified OperationKind
        ///  2. If <paramref name="predicate"/> is non-null, it succeeds for the ancestor.
        /// Returns null if there is no such ancestor.
        /// </summary>
        public static TOperation? GetAncestor<TOperation>(this IOperation root, OperationKind ancestorKind, Func<TOperation, bool>? predicate = null)
            where TOperation : class, IOperation
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var ancestor = root;
            do
            {
                ancestor = ancestor.Parent;
            } while (ancestor != null && ancestor.Kind != ancestorKind);

            if (ancestor != null)
            {
                if (predicate != null && !predicate((TOperation)ancestor))
                {
                    return GetAncestor(ancestor, ancestorKind, predicate);
                }

                return (TOperation)ancestor;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the first ancestor of this operation with:
        ///  1. Any OperationKind from the specified <paramref name="ancestorKinds"/>.
        ///  2. If <paramref name="predicate"/> is non-null, it succeeds for the ancestor.
        /// Returns null if there is no such ancestor.
        /// </summary>
        public static IOperation? GetAncestor(this IOperation root, ImmutableArray<OperationKind> ancestorKinds, Func<IOperation, bool>? predicate = null)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var ancestor = root;
            do
            {
                ancestor = ancestor.Parent;
            } while (ancestor != null && !ancestorKinds.Contains(ancestor.Kind));

            if (ancestor != null)
            {
                if (predicate != null && !predicate(ancestor))
                {
                    return GetAncestor(ancestor, ancestorKinds, predicate);
                }

                return ancestor;
            }
            else
            {
                return null;
            }
        }

        public static IConditionalAccessOperation? GetConditionalAccess(this IConditionalAccessInstanceOperation operation)
        {
            return operation.GetAncestor(OperationKind.ConditionalAccess, (IConditionalAccessOperation c) => c.Operation.Syntax == operation.Syntax);
        }

        /// <summary>
        /// Gets the operation for the object being created that is being referenced by <paramref name="operation"/>.
        /// If the operation is referencing an implicit or an explicit this/base/Me/MyBase/MyClass instance, then we return "null".
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="isInsideAnonymousObjectInitializer">Flag to indicate if the operation is a descendant of an <see cref="IAnonymousObjectCreationOperation"/>.</param>
        /// <remarks>
        /// PERF: Note that the parameter <paramref name="isInsideAnonymousObjectInitializer"/> is to improve performance by avoiding walking the entire IOperation parent for non-initializer cases.
        /// </remarks>
        public static IOperation? GetInstance(this IInstanceReferenceOperation operation, bool isInsideAnonymousObjectInitializer)
        {
            Debug.Assert(isInsideAnonymousObjectInitializer ==
                (operation.GetAncestor<IAnonymousObjectCreationOperation>(OperationKind.AnonymousObjectCreation) != null));

            if (isInsideAnonymousObjectInitializer)
            {
                for (IOperation? current = operation; current != null && current.Kind != OperationKind.Block; current = current.Parent)
                {
                    switch (current.Kind)
                    {
                        // VB object initializer allows accessing the members of the object being created with "." operator.
                        // The syntax of such an IInstanceReferenceOperation points to the object being created.
                        // Check for such an IAnonymousObjectCreationOperation with matching syntax.
                        // For example, instance reference for members ".Field1" and ".Field2" in "New C() With { .Field1 = 0, .Field2 = .Field1 }".
                        case OperationKind.AnonymousObjectCreation:
                            if (current.Syntax == operation.Syntax)
                            {
                                return current;
                            }

                            break;
                    }
                }
            }

            // For all other cases, IInstanceReferenceOperation refers to the implicit or explicit this/base/Me/MyBase/MyClass reference.
            // We return null for such cases.
            return null;
        }

        /// <summary>
        /// Indicates if the given <paramref name="binaryOperation"/> is a predicate operation used in a condition.
        /// </summary>
        /// <param name="binaryOperation"></param>
        /// <returns></returns>
        public static bool IsComparisonOperator(this IBinaryOperation binaryOperation)
            => binaryOperation.OperatorKind switch
            {
                BinaryOperatorKind.Equals
                or BinaryOperatorKind.NotEquals
                or BinaryOperatorKind.ObjectValueEquals
                or BinaryOperatorKind.ObjectValueNotEquals
                or BinaryOperatorKind.LessThan
                or BinaryOperatorKind.LessThanOrEqual
                or BinaryOperatorKind.GreaterThan
                or BinaryOperatorKind.GreaterThanOrEqual => true,
                _ => false,
            };

        public static IOperation GetRoot(this IOperation operation)
        {
            while (operation.Parent != null)
            {
                operation = operation.Parent;
            }

            return operation;
        }

        /// <summary>
        /// PERF: Cache from operation roots to their corresponding <see cref="ControlFlowGraph"/> to enable interprocedural flow analysis
        /// across analyzers and analyzer callbacks to re-use the control flow graph.
        /// </summary>
        /// <remarks>Also see <see cref="IMethodSymbolExtensions.s_methodToTopmostOperationBlockCache"/></remarks>
        private static readonly BoundedCache<Compilation, ConcurrentDictionary<IOperation, ControlFlowGraph?>> s_operationToCfgCache
            = new();

        public static bool TryGetEnclosingControlFlowGraph(this IOperation operation, [NotNullWhen(returnValue: true)] out ControlFlowGraph? cfg)
        {
            operation = operation.GetRoot();
            RoslynDebug.Assert(operation.SemanticModel is not null);
            var operationToCfgMap = s_operationToCfgCache.GetOrCreateValue(operation.SemanticModel.Compilation);
            cfg = operationToCfgMap.GetOrAdd(operation, CreateControlFlowGraph);
            return cfg != null;
        }

        public static ControlFlowGraph? GetEnclosingControlFlowGraph(this IBlockOperation blockOperation)
        {
            var success = blockOperation.TryGetEnclosingControlFlowGraph(out var cfg);
            Debug.Assert(success);
            Debug.Assert(cfg != null);
            return cfg;
        }

        private static ControlFlowGraph? CreateControlFlowGraph(IOperation operation)
        {
            switch (operation)
            {
                case IBlockOperation blockOperation:
                    return ControlFlowGraph.Create(blockOperation);

                case IMethodBodyOperation methodBodyOperation:
                    return ControlFlowGraph.Create(methodBodyOperation);

                case IConstructorBodyOperation constructorBodyOperation:
                    return ControlFlowGraph.Create(constructorBodyOperation);

                case IFieldInitializerOperation fieldInitializerOperation:
                    return ControlFlowGraph.Create(fieldInitializerOperation);

                case IPropertyInitializerOperation propertyInitializerOperation:
                    return ControlFlowGraph.Create(propertyInitializerOperation);

                case IParameterInitializerOperation:
                    // We do not support flow analysis for parameter initializers
                    return null;

                default:
                    // Attribute blocks have OperationKind.None (prior to IAttributeOperation support) or
                    // OperationKind.Attribute, but we do not support flow analysis for attributes.
                    // Gracefully return null for this case and fire an assert for any other OperationKind.
                    Debug.Assert(operation.Kind is OperationKind.None or OperationKind.Attribute, $"Unexpected root operation kind: {operation.Kind}");
                    return null;
            }
        }

        /// <summary>
        /// Gets the symbols captured from the enclosing function(s) by the given lambda or local function.
        /// </summary>
        /// <param name="operation">Operation representing the lambda or local function.</param>
        /// <param name="lambdaOrLocalFunction">Method symbol for the lambda or local function.</param>
        public static PooledDisposer<PooledHashSet<ISymbol>> GetCaptures(
            this IOperation operation, IMethodSymbol lambdaOrLocalFunction, out PooledHashSet<ISymbol> builder)
        {
            Debug.Assert(operation is IAnonymousFunctionOperation anonymousFunction && anonymousFunction.Symbol.OriginalDefinition.ReturnTypeAndParametersAreSame(lambdaOrLocalFunction.OriginalDefinition) ||
                         operation is ILocalFunctionOperation localFunction && localFunction.Symbol.OriginalDefinition.Equals(lambdaOrLocalFunction.OriginalDefinition));

            lambdaOrLocalFunction = lambdaOrLocalFunction.OriginalDefinition;

            var builderDisposer = PooledHashSet<ISymbol>.GetInstance(out builder);
            using var _ = PooledHashSet<IMethodSymbol>.GetInstance(out var nestedLambdasAndLocalFunctions);
            nestedLambdasAndLocalFunctions.Add(lambdaOrLocalFunction);

            foreach (var child in operation.Descendants())
            {
                switch (child.Kind)
                {
                    case OperationKind.LocalReference:
                        ProcessLocalOrParameter(((ILocalReferenceOperation)child).Local, builder);
                        break;

                    case OperationKind.ParameterReference:
                        ProcessLocalOrParameter(((IParameterReferenceOperation)child).Parameter, builder);
                        break;

                    case OperationKind.InstanceReference:
                        builder.Add(lambdaOrLocalFunction.ContainingType);
                        break;

                    case OperationKind.AnonymousFunction:
                        nestedLambdasAndLocalFunctions.Add(((IAnonymousFunctionOperation)child).Symbol);
                        break;

                    case OperationKind.LocalFunction:
                        nestedLambdasAndLocalFunctions.Add(((ILocalFunctionOperation)child).Symbol);
                        break;
                }
            }

            return builderDisposer;

            // Local functions.
            void ProcessLocalOrParameter(ISymbol symbol, PooledHashSet<ISymbol> builder)
            {
                if (symbol.ContainingSymbol?.Kind == SymbolKind.Method &&
                    !nestedLambdasAndLocalFunctions.Contains(symbol.ContainingSymbol.OriginalDefinition))
                {
                    builder.Add(symbol);
                }
            }
        }

        private static readonly ImmutableArray<OperationKind> s_LambdaAndLocalFunctionKinds =
            ImmutableArray.Create(OperationKind.AnonymousFunction, OperationKind.LocalFunction);

        public static bool IsWithinLambdaOrLocalFunction(this IOperation operation, [NotNullWhen(true)] out IOperation? containingLambdaOrLocalFunctionOperation)
        {
            containingLambdaOrLocalFunctionOperation = operation.GetAncestor(s_LambdaAndLocalFunctionKinds);
            return containingLambdaOrLocalFunctionOperation != null;
        }

        public static ITypeSymbol? GetPatternType(this IPatternOperation pattern)
        {
            return pattern switch
            {
                IDeclarationPatternOperation declarationPattern => declarationPattern.MatchedType,
                IRecursivePatternOperation recursivePattern => recursivePattern.MatchedType,
                IDiscardPatternOperation discardPattern => discardPattern.InputType,
                IConstantPatternOperation constantPattern => constantPattern.Value.Type,

                _ => null,
            };
        }

        /// <summary>
        /// If the given <paramref name="tupleOperation"/> is a nested tuple,
        /// gets the parenting tuple operation and the tuple element of that parenting tuple
        /// which contains the given tupleOperation as a descendant operation.
        /// </summary>
        public static bool TryGetParentTupleOperation(this ITupleOperation tupleOperation,
            [NotNullWhen(returnValue: true)] out ITupleOperation? parentTupleOperation,
            [NotNullWhen(returnValue: true)] out IOperation? elementOfParentTupleContainingTuple)
        {
            parentTupleOperation = null;
            elementOfParentTupleContainingTuple = null;

            IOperation previousOperation = tupleOperation;
            var currentOperation = tupleOperation.Parent;
            while (currentOperation != null)
            {
                switch (currentOperation.Kind)
                {
                    case OperationKind.Parenthesized:
                    case OperationKind.Conversion:
                    case OperationKind.DeclarationExpression:
                        previousOperation = currentOperation;
                        currentOperation = currentOperation.Parent;
                        continue;

                    case OperationKind.Tuple:
                        parentTupleOperation = (ITupleOperation)currentOperation;
                        elementOfParentTupleContainingTuple = previousOperation;
                        return true;

                    default:
                        return false;
                }
            }

            return false;
        }

        public static bool IsExtensionMethodAndHasNoInstance(this IInvocationOperation invocationOperation)
        {
            // This method exists to abstract away the language specific differences between IInvocationOperation implementations
            // See https://github.com/dotnet/roslyn/issues/23625 for more details
            return invocationOperation.TargetMethod.IsExtensionMethod && (invocationOperation.Language != LanguageNames.VisualBasic || invocationOperation.Instance == null);
        }

        public static IOperation? GetInstance(this IInvocationOperation invocationOperation)
            => invocationOperation.IsExtensionMethodAndHasNoInstance() ? invocationOperation.Arguments[0].Value : invocationOperation.Instance;

        public static IOperation? GetThrownException(this IThrowOperation operation)
        {
            var thrownObject = operation.Exception;

            // Starting C# 8.0, C# compiler wraps the thrown operation within an implicit conversion to System.Exception type.
            // We also want to walk down explicit conversions such as "throw (Exception)new ArgumentNullException())".
            if (thrownObject is IConversionOperation conversion &&
                conversion.Conversion.Exists)
            {
                thrownObject = conversion.Operand;
            }

            return thrownObject;
        }

        public static ITypeSymbol? GetThrownExceptionType(this IThrowOperation operation)
            => operation.GetThrownException()?.Type;

        public static bool IsSetMethodInvocation(this IPropertyReferenceOperation operation)
        {
            if (operation.Property.SetMethod is null)
            {
                // This is either invalid code, or an assignment through a ref-returning getter
                return false;
            }

            IOperation potentialLeftSide = operation;
            while (potentialLeftSide.Parent is IParenthesizedOperation or ITupleOperation)
            {
                potentialLeftSide = potentialLeftSide.Parent;
            }

            return potentialLeftSide.Parent switch
            {
                IAssignmentOperation { Target: var target } when target == potentialLeftSide => true,
                _ => false,
            };
        }

        public static bool TryGetArgumentForParameterAtIndex(
            this ImmutableArray<IArgumentOperation> arguments,
            int parameterIndex,
            [NotNullWhen(true)] out IArgumentOperation? result)
        {
            Debug.Assert(parameterIndex >= 0);
            Debug.Assert(parameterIndex < arguments.Length);

            foreach (var argument in arguments)
            {
                if (argument.Parameter?.Ordinal == parameterIndex)
                {
                    result = argument;
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static IArgumentOperation GetArgumentForParameterAtIndex(
            this ImmutableArray<IArgumentOperation> arguments,
            int parameterIndex)
        {
            if (TryGetArgumentForParameterAtIndex(arguments, parameterIndex, out var result))
            {
                return result;
            }

            throw new InvalidOperationException();
        }
    }
}

