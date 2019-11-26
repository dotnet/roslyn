// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if HAS_IOPERATION

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.Extensions
{
    internal static partial class IOperationExtensions
    {
        /// <summary>
        /// Gets the receiver type for an invocation expression (i.e. type of 'A' in invocation 'A.B()')
        /// If the invocation actually involves a conversion from A to some other type, say 'C', on which B is invoked,
        /// then this method returns type A if <paramref name="beforeConversion"/> is true, and C if false.
        /// </summary>
        public static INamedTypeSymbol? GetReceiverType(this IInvocationOperation invocation, Compilation compilation, bool beforeConversion, CancellationToken cancellationToken)
        {
            if (invocation.Instance != null)
            {
                return beforeConversion ?
                    GetReceiverType(invocation.Instance.Syntax, compilation, cancellationToken) :
                    invocation.Instance.Type as INamedTypeSymbol;
            }
            else if (invocation.TargetMethod.IsExtensionMethod && invocation.TargetMethod.Parameters.Length > 0)
            {
                var firstArg = invocation.Arguments.FirstOrDefault();
                if (firstArg != null)
                {
                    return beforeConversion ?
                        GetReceiverType(firstArg.Value.Syntax, compilation, cancellationToken) :
                        firstArg.Type as INamedTypeSymbol;
                }
                else if (invocation.TargetMethod.Parameters[0].IsParams)
                {
                    return invocation.TargetMethod.Parameters[0].Type as INamedTypeSymbol;
                }
            }

            return null;
        }

        private static INamedTypeSymbol? GetReceiverType(SyntaxNode receiverSyntax, Compilation compilation, CancellationToken cancellationToken)
        {
            var model = compilation.GetSemanticModel(receiverSyntax.SyntaxTree);
            var typeInfo = model.GetTypeInfo(receiverSyntax, cancellationToken);
            return typeInfo.Type as INamedTypeSymbol;
        }

        public static bool HasConstantValue(this IOperation operation, string comparand, StringComparison comparison)
        {
            var constantValue = operation.ConstantValue;
            if (!constantValue.HasValue)
            {
                return false;
            }

            if (operation.Type == null || operation.Type.SpecialType != SpecialType.System_String)
            {
                return false;
            }

            return string.Equals((string)constantValue.Value, comparand, comparison);
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

        public static bool HasConstantValue(this IOperation operation, long comparand)
        {
            return operation.HasConstantValue(unchecked((ulong)(comparand)));
        }

        public static bool HasConstantValue(this IOperation operation, ulong comparand)
        {
            var constantValue = operation.ConstantValue;
            if (!constantValue.HasValue)
            {
                return false;
            }

            if (operation.Type == null || operation.Type.IsErrorType())
            {
                return false;
            }

            if (operation.Type.IsPrimitiveType())
            {
                return HasConstantValue(constantValue, operation.Type, comparand);
            }

            if (operation.Type.TypeKind == TypeKind.Enum)
            {
                var enumUnderlyingType = ((INamedTypeSymbol)operation.Type).EnumUnderlyingType;
                return enumUnderlyingType != null &&
                    enumUnderlyingType.IsPrimitiveType() &&
                    HasConstantValue(constantValue, enumUnderlyingType, comparand);
            }

            return false;
        }

        private static bool HasConstantValue(Optional<object> constantValue, ITypeSymbol constantValueType, ulong comparand)
        {
            if (constantValueType.SpecialType == SpecialType.System_Double || constantValueType.SpecialType == SpecialType.System_Single)
            {
                return (double)constantValue.Value == comparand;
            }

            return DiagnosticHelpers.TryConvertToUInt64(constantValue.Value, constantValueType.SpecialType, out ulong convertedValue) && convertedValue == comparand;
        }

        public static ITypeSymbol? GetElementType(this IArrayCreationOperation? arrayCreation)
        {
            return (arrayCreation?.Type as IArrayTypeSymbol)?.ElementType;
        }

        /// <summary>
        /// Filters out operations that are implicit and have no explicit descendant with a constant value or a non-null type.
        /// </summary>
        public static ImmutableArray<IOperation> WithoutFullyImplicitOperations(this ImmutableArray<IOperation> operations)
        {
            ImmutableArray<IOperation>.Builder? builder = null;
            for (int i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];

                // Check if all descendants are either implicit or are explicit with no constant value or type, indicating it is not user written code.
                if (operation.DescendantsAndSelf().All(o => o.IsImplicit || (!o.ConstantValue.HasValue && o.Type == null)))
                {
                    if (builder == null)
                    {
                        builder = ImmutableArray.CreateBuilder<IOperation>();
                        builder.AddRange(operations, i);
                    }
                }
                else if (builder != null)
                {
                    builder.Add(operation);
                }
            }

            return builder != null ? builder.ToImmutable() : operations;
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
                    foreach (var child in operation.Children)
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
            return operation.Kind == OperationKind.None && operation.Parent == null;
        }

        /// <summary>
        /// Returns the topmost <see cref="IBlockOperation"/> containing the given <paramref name="operation"/>.
        /// </summary>
        public static IBlockOperation? GetTopmostParentBlock(this IOperation? operation)
        {
            IOperation? currentOperation = operation;
            IBlockOperation? topmostBlockOperation = null;
            while (currentOperation != null)
            {
                if (currentOperation is IBlockOperation blockOperation)
                {
                    topmostBlockOperation = blockOperation;
                }

                currentOperation = currentOperation.Parent;
            }

            return topmostBlockOperation;
        }

        /// <summary>
        /// Gets the first ancestor of this operation with:
        ///  1. Specified OperationKind
        ///  2. If <paramref name="predicateOpt"/> is non-null, it succeeds for the ancestor.
        /// Returns null if there is no such ancestor.
        /// </summary>
        public static TOperation? GetAncestor<TOperation>(this IOperation root, OperationKind ancestorKind, Func<TOperation, bool>? predicateOpt = null)
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
                if (predicateOpt != null && !predicateOpt((TOperation)ancestor))
                {
                    return GetAncestor(ancestor, ancestorKind, predicateOpt);
                }
                return (TOperation)ancestor;
            }
            else
            {
                return default;
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
                for (IOperation current = operation; current != null && current.Kind != OperationKind.Block; current = current.Parent)
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
        /// Workaround for https://github.com/dotnet/roslyn/issues/22736 (IPropertyReferenceExpressions in IAnonymousObjectCreationExpression are missing a receiver).
        /// Gets the instance for the anonymous object being created that is being referenced by <paramref name="operation"/>.
        /// Otherwise, returns null
        /// </summary>
        public static IAnonymousObjectCreationOperation? GetAnonymousObjectCreation(this IPropertyReferenceOperation operation)
        {
            if (operation.Instance == null &&
                operation.Property.ContainingType.IsAnonymousType)
            {
                var declarationSyntax = operation.Property.ContainingType.DeclaringSyntaxReferences[0].GetSyntax();
                return operation.GetAncestor(OperationKind.AnonymousObjectCreation, (IAnonymousObjectCreationOperation a) => a.Syntax == declarationSyntax);
            }

            return null;
        }

        public static bool IsInsideAnonymousFunction(this IOperation operation)
            => operation.GetAncestor<IAnonymousFunctionOperation>(OperationKind.AnonymousFunction) != null;

        public static bool HasAnyOperationDescendant(this ImmutableArray<IOperation> operationBlocks, Func<IOperation, bool> predicate)
        {
            foreach (var operationBlock in operationBlocks)
            {
                if (operationBlock.HasAnyOperationDescendant(predicate))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAnyOperationDescendant(this IOperation operationBlock, Func<IOperation, bool> predicate)
        {
            return operationBlock.HasAnyOperationDescendant(predicate, out _);
        }

        public static bool HasAnyOperationDescendant(this IOperation operationBlock, Func<IOperation, bool> predicate, [NotNullWhen(returnValue: true)] out IOperation? foundOperation)
        {
            foreach (var descendant in operationBlock.DescendantsAndSelf())
            {
                if (predicate(descendant))
                {
                    foundOperation = descendant;
                    return true;
                }
            }

            foundOperation = null;
            return false;
        }

        public static bool HasAnyOperationDescendant(this ImmutableArray<IOperation> operationBlocks, OperationKind kind)
        {
            return operationBlocks.HasAnyOperationDescendant(predicate: operation => operation.Kind == kind);
        }

        /// <summary>
        /// Indicates if the given <paramref name="binaryOperation"/> is a predicate operation used in a condition.
        /// </summary>
        /// <param name="binaryOperation"></param>
        /// <returns></returns>
        public static bool IsComparisonOperator(this IBinaryOperation binaryOperation)
        {
            switch (binaryOperation.OperatorKind)
            {
                case BinaryOperatorKind.Equals:
                case BinaryOperatorKind.NotEquals:
                case BinaryOperatorKind.ObjectValueEquals:
                case BinaryOperatorKind.ObjectValueNotEquals:
                case BinaryOperatorKind.LessThan:
                case BinaryOperatorKind.LessThanOrEqual:
                case BinaryOperatorKind.GreaterThan:
                case BinaryOperatorKind.GreaterThanOrEqual:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsLambdaOrLocalFunctionOrDelegateInvocation(this IInvocationOperation operation)
            => operation.TargetMethod.IsLambdaOrLocalFunctionOrDelegate();

        public static bool IsLambdaOrLocalFunctionOrDelegateReference(this IMethodReferenceOperation operation)
            => operation.Method.IsLambdaOrLocalFunctionOrDelegate();

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
            = new BoundedCache<Compilation, ConcurrentDictionary<IOperation, ControlFlowGraph?>>();

        public static bool TryGetEnclosingControlFlowGraph(this IOperation operation, [NotNullWhen(returnValue: true)] out ControlFlowGraph? cfg)
        {
            operation = operation.GetRoot();
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

                case IParameterInitializerOperation parameterInitializerOperation:
                    return ControlFlowGraph.Create(parameterInitializerOperation);

                default:
                    // Attribute blocks have OperationKind.None, but ControlFlowGraph.Create does not
                    // have an overload for such operation roots.
                    // Gracefully return null for this case and fire an assert for any other OperationKind.
                    Debug.Assert(operation.Kind == OperationKind.None, $"Unexpected root operation kind: {operation.Kind.ToString()}");
                    return null;
            }
        }

        /// <summary>
        /// Gets the symbols captured from the enclosing function(s) by the given lambda or local function.
        /// </summary>
        /// <param name="operation">Operation representing the lambda or local function.</param>
        /// <param name="lambdaOrLocalFunction">Method symbol for the lambda or local function.</param>
        public static PooledHashSet<ISymbol> GetCaptures(this IOperation operation, IMethodSymbol lambdaOrLocalFunction)
        {
            Debug.Assert(operation is IAnonymousFunctionOperation anonymousFunction && anonymousFunction.Symbol.OriginalDefinition.ReturnTypeAndParametersAreSame(lambdaOrLocalFunction.OriginalDefinition) ||
                         operation is ILocalFunctionOperation localFunction && localFunction.Symbol.OriginalDefinition.Equals(lambdaOrLocalFunction.OriginalDefinition));

            lambdaOrLocalFunction = lambdaOrLocalFunction.OriginalDefinition;

            var builder = PooledHashSet<ISymbol>.GetInstance();
            var nestedLambdasAndLocalFunctions = PooledHashSet<IMethodSymbol>.GetInstance();
            nestedLambdasAndLocalFunctions.Add(lambdaOrLocalFunction);

            try
            {
                foreach (var child in operation.Descendants())
                {
                    switch (child.Kind)
                    {
                        case OperationKind.LocalReference:
                            ProcessLocalOrParameter(((ILocalReferenceOperation)child).Local);
                            break;

                        case OperationKind.ParameterReference:
                            ProcessLocalOrParameter(((IParameterReferenceOperation)child).Parameter);
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

                return builder;
            }
            finally
            {
                nestedLambdasAndLocalFunctions.Free();
            }

            // Local functions.
            void ProcessLocalOrParameter(ISymbol symbol)
            {
                if (symbol.ContainingSymbol?.Kind == SymbolKind.Method &&
                    !nestedLambdasAndLocalFunctions.Contains(symbol.ContainingSymbol.OriginalDefinition))
                {
                    builder.Add(symbol);
                }
            }
        }

        public static bool IsWithinLambdaOrLocalFunction(this IOperation operation)
            => operation.GetAncestor<IAnonymousFunctionOperation>(OperationKind.AnonymousFunction) != null ||
               operation.GetAncestor<ILocalFunctionOperation>(OperationKind.LocalFunction) != null;

        public static ITypeSymbol? GetPatternType(this IPatternOperation pattern)
        {
            return pattern switch
            {
                IDeclarationPatternOperation declarationPattern => declarationPattern.DeclaredSymbol switch
                {
                    ILocalSymbol local => local.Type,

                    IDiscardSymbol discard => discard.Type,

                    _ => null,
                },

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

        public static SyntaxNode GetInstance(this IInvocationOperation invocationOperation)
        {
            return invocationOperation.IsExtensionMethodAndHasNoInstance() ? invocationOperation.Arguments[0].Value.Syntax : invocationOperation.Instance.Syntax;
        }

        public static ISymbol? GetReferencedMemberOrLocalOrParameter(this IOperation operation)
        {
            return operation switch
            {
                IMemberReferenceOperation memberReference => memberReference.Member,

                IParameterReferenceOperation parameterReference => parameterReference.Parameter,

                ILocalReferenceOperation localReference => localReference.Local,

                IParenthesizedOperation parenthesized => parenthesized.Operand.GetReferencedMemberOrLocalOrParameter(),

                IConversionOperation conversion => conversion.Operand.GetReferencedMemberOrLocalOrParameter(),

                _ => null,
            };
        }

        /// <summary>
        /// Walks down consequtive parenthesized operations until an operand is reached that isn't a parenthesized operation.
        /// </summary>
        /// <param name="operation">The starting operation.</param>
        /// <returns>The inner non parenthesized operation or the starting operation if it wasn't a parenthesized operation.</returns>
        public static IOperation WalkDownParenthesis(this IOperation operation)
        {
            while (operation is IParenthesizedOperation parenthesizedOperation)
            {
                operation = parenthesizedOperation.Operand;
            }

            return operation;
        }

        /// <summary>
        /// Walks up consequtive parenthesized operations until a parent is reached that isn't a parenthesized operation.
        /// </summary>
        /// <param name="operation">The starting operation.</param>
        /// <returns>The outer non parenthesized operation or the starting operation if it wasn't a parenthesized operation.</returns>
        public static IOperation WalkUpParenthesis(this IOperation operation)
        {
            while (operation is IParenthesizedOperation parenthesizedOperation)
            {
                operation = parenthesizedOperation.Parent;
            }

            return operation;
        }

        /// <summary>
        /// Walks down consequtive conversion operations until an operand is reached that isn't a conversion operation.
        /// </summary>
        /// <param name="operation">The starting operation.</param>
        /// <returns>The inner non conversion operation or the starting operation if it wasn't a conversion operation.</returns>
        public static IOperation WalkDownConversion(this IOperation operation)
        {
            while (operation is IConversionOperation conversionOperation)
            {
                operation = conversionOperation.Operand;
            }

            return operation;
        }

        /// <summary>
        /// Walks up consequtive conversion operations until a parent is reached that isn't a conversion operation.
        /// </summary>
        /// <param name="operation">The starting operation.</param>
        /// <returns>The outer non conversion operation or the starting operation if it wasn't a conversion operation.</returns>
        public static IOperation WalkUpConversion(this IOperation operation)
        {
            while (operation is IConversionOperation conversionOperation)
            {
                operation = conversionOperation.Parent;
            }

            return operation;
        }

        public static ITypeSymbol? GetThrownExceptionType(this IThrowOperation operation)
        {
            var thrownObject = operation.Exception;

            // Starting C# 8.0, C# compiler wraps the thrown operation within an implicit conversion to System.Exception type.
            if (thrownObject is IConversionOperation conversion &&
                conversion.IsImplicit)
            {
                thrownObject = conversion.Operand;
            }

            return thrownObject?.Type;
        }
    }
}

#endif
