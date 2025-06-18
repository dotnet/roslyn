// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if HAS_IOPERATION

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Analyzer.Utilities.Lightup;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

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

        public static bool HasConstantValue(this IOperation operation, long comparand)
        {
            return operation.HasConstantValue(unchecked((ulong)comparand));
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

        private static bool HasConstantValue(Optional<object?> constantValue, ITypeSymbol constantValueType, ulong comparand)
        {
            if (constantValueType.SpecialType is SpecialType.System_Double or SpecialType.System_Single)
            {
                return (double?)constantValue.Value == comparand;
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
                else
                {
                    builder?.Add(operation);
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
        /// Returns the first <see cref="IBlockOperation"/> in the parent chain of <paramref name="operation"/>.
        /// </summary>
        public static IBlockOperation? GetFirstParentBlock(this IOperation? operation)
        {
            IOperation? currentOperation = operation;
            while (currentOperation != null)
            {
                if (currentOperation is IBlockOperation blockOperation)
                {
                    return blockOperation;
                }

                currentOperation = currentOperation.Parent;
            }

            return null;
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

        /// <summary>
        /// Indicates if the given <paramref name="binaryOperation"/> is an addition or substaction operation.
        /// </summary>
        /// <param name="binaryOperation"></param>
        /// <returns>true if the operation is addition or substruction</returns>
        public static bool IsAdditionOrSubstractionOperation(this IBinaryOperation binaryOperation, out char binaryOperator)
        {
            binaryOperator = '\0';
            switch (binaryOperation.OperatorKind)
            {
                case BinaryOperatorKind.Add:
                    binaryOperator = '+'; return true;
                case BinaryOperatorKind.Subtract:
                    binaryOperator = '-'; return true;
            }

            return false;
        }

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
                    Debug.Assert(operation.Kind is OperationKind.None or OperationKindEx.Attribute, $"Unexpected root operation kind: {operation.Kind}");
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

        public static bool IsWithinExpressionTree(this IOperation operation, [NotNullWhen(true)] INamedTypeSymbol? linqExpressionTreeType)
            => linqExpressionTreeType != null
                && operation.GetAncestor(s_LambdaAndLocalFunctionKinds)?.Parent?.Type?.OriginalDefinition is { } lambdaType
                && linqExpressionTreeType.Equals(lambdaType);

        public static ITypeSymbol? GetPatternType(this IPatternOperation pattern)
        {
            return pattern switch
            {
#if CODEANALYSIS_V3_OR_BETTER
                IDeclarationPatternOperation declarationPattern => declarationPattern.MatchedType,
                IRecursivePatternOperation recursivePattern => recursivePattern.MatchedType,
                IDiscardPatternOperation discardPattern => discardPattern.InputType,
#else
                IDeclarationPatternOperation declarationPattern => declarationPattern.DeclaredSymbol switch
                {
                    ILocalSymbol local => local.Type,

                    IDiscardSymbol discard => discard.Type,

                    _ => null,
                },
#endif
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

        public static SyntaxNode? GetInstanceSyntax(this IInvocationOperation invocationOperation)
            => invocationOperation.GetInstance()?.Syntax;

        public static ITypeSymbol? GetInstanceType(this IOperation operation)
        {
            IOperation? instance = operation switch
            {
                IInvocationOperation invocation => invocation.GetInstance(),

                IPropertyReferenceOperation propertyReference => propertyReference.Instance,

                _ => throw new NotImplementedException()
            };

            return instance?.WalkDownConversion().Type;
        }

        public static ISymbol? GetReferencedMemberOrLocalOrParameter(this IOperation? operation)
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
        /// Walks down consecutive parenthesized operations until an operand is reached that isn't a parenthesized operation.
        /// </summary>
        /// <param name="operation">The starting operation.</param>
        /// <returns>The inner non parenthesized operation or the starting operation if it wasn't a parenthesized operation.</returns>
        public static IOperation WalkDownParentheses(this IOperation operation)
        {
            while (operation is IParenthesizedOperation parenthesizedOperation)
            {
                operation = parenthesizedOperation.Operand;
            }

            return operation;
        }

        [return: NotNullIfNotNull(nameof(operation))]
        public static IOperation? WalkUpParentheses(this IOperation? operation)
        {
            if (operation is null)
                return null;

            while (operation.Parent is IParenthesizedOperation parenthesizedOperation)
            {
                operation = parenthesizedOperation;
            }

            return operation;
        }

        /// <summary>
        /// Walks down consecutive conversion operations until an operand is reached that isn't a conversion operation.
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
        /// Walks down consecutive conversion operations that satisfy <paramref name="predicate"/> until an operand is reached that
        /// either isn't a conversion or doesn't satisfy <paramref name="predicate"/>.
        /// </summary>
        /// <param name="operation">The starting operation.</param>
        /// <param name="predicate">A predicate to filter conversion operations.</param>
        /// <returns>The first operation that either isn't a conversion or doesn't satisfy <paramref name="predicate"/>.</returns>
        public static IOperation WalkDownConversion(this IOperation operation, Func<IConversionOperation, bool> predicate)
        {
            while (operation is IConversionOperation conversionOperation && predicate(conversionOperation))
            {
                operation = conversionOperation.Operand;
            }

            return operation;
        }

        [return: NotNullIfNotNull(nameof(operation))]
        public static IOperation? WalkUpConversion(this IOperation? operation)
        {
            if (operation is null)
                return null;

            while (operation.Parent is IConversionOperation conversionOperation)
            {
                operation = conversionOperation;
            }

            return operation;
        }

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

        /// <summary>
        /// Determines if the one of the invocation's arguments' values is an argument of the specified type, and if so, find
        /// the first one.
        /// </summary>
        /// <param name="invocationOperation">Invocation operation whose arguments to look through.</param>
        /// <param name="firstFoundArgument">First found IArgumentOperation.Value of the specified type, order by the method's
        /// signature's parameters (as opposed to how arguments are specified when invoked).</param>
        /// <returns>True if one is found, false otherwise.</returns>
        /// <remarks>
        /// IInvocationOperation.Arguments are ordered by how they are specified, which may differ from the order in the method
        /// signature if the caller specifies arguments by name. This will find the first typeof operation ordered by the
        /// method signature's parameters.
        /// </remarks>
        public static bool HasArgument<TOperation>(
            this IInvocationOperation invocationOperation,
            [NotNullWhen(returnValue: true)] out TOperation? firstFoundArgument)
            where TOperation : class, IOperation
        {
            firstFoundArgument = null;
            int minOrdinal = int.MaxValue;
            foreach (IArgumentOperation argumentOperation in invocationOperation.Arguments)
            {
                if (argumentOperation.Parameter?.Ordinal < minOrdinal && argumentOperation.Value is TOperation to)
                {
                    minOrdinal = argumentOperation.Parameter.Ordinal;
                    firstFoundArgument = to;
                }
            }

            return firstFoundArgument != null;
        }

        public static bool HasAnyExplicitDescendant(this IOperation operation, Func<IOperation, bool>? descendIntoOperation = null)
        {
            using var _ = ArrayBuilder<IEnumerator<IOperation>>.GetInstance(out var stack);
#pragma warning disable CS0618 // 'IOperation.Children' is obsolete: 'This API has performance penalties, please use ChildOperations instead.'
            stack.Add(operation.Children.GetEnumerator());
#pragma warning restore CS0618 // 'IOperation.Children' is obsolete: 'This API has performance penalties, please use ChildOperations instead.'

            while (stack.Any())
            {
                var enumerator = stack.Last();
                stack.RemoveLast();
                if (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    stack.Add(enumerator);

                    if (current != null &&
                        (descendIntoOperation == null || descendIntoOperation(current)))
                    {
                        if (!current.IsImplicit &&
                            // This prevents non explicit operations like expression to be considered as ok
                            (current.ConstantValue.HasValue || current.Type != null))
                        {
                            return true;
                        }

#pragma warning disable CS0618 // 'IOperation.Children' is obsolete: 'This API has performance penalties, please use ChildOperations instead.'
                        stack.Add(current.Children.GetEnumerator());
#pragma warning restore CS0618 // 'IOperation.Children' is obsolete: 'This API has performance penalties, please use ChildOperations instead.'
                    }
                }
            }

            return false;
        }

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

        /// <summary>
        /// Useful when named arguments used for a method call and you need them in the original parameter order.
        /// </summary>
        /// <param name="arguments">Arguments of the method</param>
        /// <returns>Returns the arguments in parameter order</returns>
        public static ImmutableArray<IArgumentOperation> GetArgumentsInParameterOrder(
            this ImmutableArray<IArgumentOperation> arguments)
        {
            using var _ = ArrayBuilder<IArgumentOperation>.GetInstance(arguments.Length, null!, out var parameterOrderedArguments);

            foreach (var argument in arguments)
            {
                RoslynDebug.Assert(argument.Parameter is not null);
                Debug.Assert(parameterOrderedArguments[argument.Parameter.Ordinal] == null);
                parameterOrderedArguments[argument.Parameter.Ordinal] = argument;
            }

            return parameterOrderedArguments.ToImmutableArray();
        }

        // Copied from roslyn https://github.com/dotnet/roslyn/blob/main/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/OperationExtensions.cs#L25

#if CODEANALYSIS_V3_OR_BETTER
        /// <summary>
        /// Returns the <see cref="ValueUsageInfo"/> for the given operation.
        /// This extension can be removed once https://github.com/dotnet/roslyn/issues/25057 is implemented.
        /// </summary>
        public static ValueUsageInfo GetValueUsageInfo(this IOperation operation, ISymbol containingSymbol)
        {
            /*
            |    code                  | Read | Write | ReadableRef | WritableRef | NonReadWriteRef |
            | x.Prop = 1               |      |  ✔️   |             |             |                 |
            | x.Prop += 1              |  ✔️  |  ✔️   |             |             |                 |
            | x.Prop++                 |  ✔️  |  ✔️   |             |             |                 |
            | Foo(x.Prop)              |  ✔️  |       |             |             |                 |
            | Foo(x.Prop),             |      |       |     ✔️      |             |                 |
               where void Foo(in T v)
            | Foo(out x.Prop)          |      |       |             |     ✔️      |                 |
            | Foo(ref x.Prop)          |      |       |     ✔️      |     ✔️      |                 |
            | nameof(x)                |      |       |             |             |       ✔️        | ️
            | sizeof(x)                |      |       |             |             |       ✔️        | ️
            | typeof(x)                |      |       |             |             |       ✔️        | ️
            | out var x                |      |  ✔️   |             |             |                 | ️
            | case X x:                |      |  ✔️   |             |             |                 | ️
            | obj is X x               |      |  ✔️   |             |             |                 |
            | ref var x =              |      |       |     ✔️      |     ✔️      |                 |
            | ref readonly var x =     |      |       |     ✔️      |             |                 |

            */
            if (operation is ILocalReferenceOperation localReference &&
                localReference.IsDeclaration &&
                !localReference.IsImplicit) // Workaround for https://github.com/dotnet/roslyn/issues/30753
            {
                // Declaration expression is a definition (write) for the declared local.
                return ValueUsageInfo.Write;
            }
            else if (operation is IDeclarationPatternOperation)
            {
                switch (operation.Parent)
                {
                    case IPatternCaseClauseOperation:
                        // A declaration pattern within a pattern case clause is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      switch (obj)
                        //      {
                        //          case X x:
                        //
                        return ValueUsageInfo.Write;

                    case IRecursivePatternOperation:
                        // A declaration pattern within a recursive pattern is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      (obj) switch
                        //      {
                        //          (X x) => ...
                        //      };
                        //
                        return ValueUsageInfo.Write;

                    case ISwitchExpressionArmOperation:
                        // A declaration pattern within a switch expression arm is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      obj switch
                        //      {
                        //          X x => ...
                        //
                        return ValueUsageInfo.Write;

                    case IIsPatternOperation:
                        // A declaration pattern within an is pattern is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      if (obj is X x)
                        //
                        return ValueUsageInfo.Write;

                    case IPropertySubpatternOperation:
                        // A declaration pattern within a property sub-pattern is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj.Property' below:
                        //      if (obj is { Property : int x })
                        //
                        return ValueUsageInfo.Write;

                    default:
                        Debug.Fail("Unhandled declaration pattern context");

                        // Conservatively assume read/write.
                        return ValueUsageInfo.ReadWrite;
                }
            }

            if (operation.Parent is IAssignmentOperation assignmentOperation &&
                assignmentOperation.Target == operation)
            {
                return operation.Parent.IsAnyCompoundAssignment()
                    ? ValueUsageInfo.ReadWrite
                    : ValueUsageInfo.Write;
            }
            else if (operation.Parent is IIncrementOrDecrementOperation)
            {
                return ValueUsageInfo.ReadWrite;
            }
            else if (operation.Parent is IParenthesizedOperation parenthesizedOperation)
            {
                // Note: IParenthesizedOperation is specific to VB, where the parens cause a copy, so this cannot be classified as a write.
                Debug.Assert(parenthesizedOperation.Language == LanguageNames.VisualBasic);

                return parenthesizedOperation.GetValueUsageInfo(containingSymbol) &
                    ~(ValueUsageInfo.Write | ValueUsageInfo.Reference);
            }
            else if (operation.Parent is INameOfOperation or
                     ITypeOfOperation or
                     ISizeOfOperation)
            {
                return ValueUsageInfo.Name;
            }
            else if (operation.Parent is IArgumentOperation argumentOperation)
            {
                return argumentOperation.Parameter?.RefKind switch
                {
                    RefKind.RefReadOnly => ValueUsageInfo.ReadableReference,
                    RefKind.Out => ValueUsageInfo.WritableReference,
                    RefKind.Ref => ValueUsageInfo.ReadableWritableReference,
                    _ => ValueUsageInfo.Read,
                };
            }
            else if (operation.Parent is IReturnOperation returnOperation)
            {
                return returnOperation.GetRefKind(containingSymbol) switch
                {
                    RefKind.RefReadOnly => ValueUsageInfo.ReadableReference,
                    RefKind.Ref => ValueUsageInfo.ReadableWritableReference,
                    _ => ValueUsageInfo.Read,
                };
            }
            else if (operation.Parent is IConditionalOperation conditionalOperation)
            {
                if (operation == conditionalOperation.WhenTrue
                    || operation == conditionalOperation.WhenFalse)
                {
                    return GetValueUsageInfo(conditionalOperation, containingSymbol);
                }
                else
                {
                    return ValueUsageInfo.Read;
                }
            }
            else if (operation.Parent is IReDimClauseOperation reDimClauseOperation &&
                reDimClauseOperation.Operand == operation)
            {
                return reDimClauseOperation.Parent is IReDimOperation { Preserve: true }
                    ? ValueUsageInfo.ReadWrite
                    : ValueUsageInfo.Write;
            }
            else if (operation.Parent is IDeclarationExpressionOperation declarationExpression)
            {
                return declarationExpression.GetValueUsageInfo(containingSymbol);
            }
            else if (operation.IsInLeftOfDeconstructionAssignment(out _))
            {
                return ValueUsageInfo.Write;
            }
            else if (operation.Parent is IVariableInitializerOperation variableInitializerOperation &&
                variableInitializerOperation.Parent is IVariableDeclaratorOperation variableDeclaratorOperation)
            {
                switch (variableDeclaratorOperation.Symbol.RefKind)
                {
                    case RefKind.Ref:
                        return ValueUsageInfo.ReadableWritableReference;

                    case RefKind.RefReadOnly:
                        return ValueUsageInfo.ReadableReference;
                }
            }

            return ValueUsageInfo.Read;
        }

        public static bool IsInLeftOfDeconstructionAssignment([DisallowNull] this IOperation? operation, out IDeconstructionAssignmentOperation? deconstructionAssignment)
        {
            deconstructionAssignment = null;

            var previousOperation = operation;
            operation = operation.Parent;

            while (operation != null)
            {
                switch (operation.Kind)
                {
                    case OperationKind.DeconstructionAssignment:
                        deconstructionAssignment = (IDeconstructionAssignmentOperation)operation;
                        return deconstructionAssignment.Target == previousOperation;

                    case OperationKind.Tuple:
                    case OperationKind.Conversion:
                    case OperationKind.Parenthesized:
                        previousOperation = operation;
                        operation = operation.Parent;
                        continue;

                    default:
                        return false;
                }
            }

            return false;
        }

        public static RefKind GetRefKind(this IReturnOperation operation, ISymbol containingSymbol)
        {
            var containingMethod = TryGetContainingAnonymousFunctionOrLocalFunction(operation) ?? (containingSymbol as IMethodSymbol);
            return containingMethod?.RefKind ?? RefKind.None;
        }

        public static IMethodSymbol? TryGetContainingAnonymousFunctionOrLocalFunction(this IOperation? operation)
        {
            operation = operation?.Parent;
            while (operation != null)
            {
                switch (operation.Kind)
                {
                    case OperationKind.AnonymousFunction:
                        return ((IAnonymousFunctionOperation)operation).Symbol;

                    case OperationKind.LocalFunction:
                        return ((ILocalFunctionOperation)operation).Symbol;
                }

                operation = operation.Parent;
            }

            return null;
        }

        /// <summary>
        /// Returns true if the given operation is a regular compound assignment,
        /// i.e. <see cref="ICompoundAssignmentOperation"/> such as <code>a += b</code>,
        /// or a special null coalescing compound assignment, i.e. <see cref="ICoalesceAssignmentOperation"/>
        /// such as <code>a ??= b</code>.
        /// </summary>
        public static bool IsAnyCompoundAssignment(this IOperation operation)
            => operation switch
            {
                ICompoundAssignmentOperation or ICoalesceAssignmentOperation => true,
                _ => false,
            };
#endif
    }
}

#endif
