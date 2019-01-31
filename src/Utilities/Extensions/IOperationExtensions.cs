// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Analyzer.Utilities.Extensions
{
    internal static class IOperationExtensions
    {
        /// <summary>
        /// Gets the receiver type for an invocation expression (i.e. type of 'A' in invocation 'A.B()')
        /// If the invocation actually involves a conversion from A to some other type, say 'C', on which B is invoked,
        /// then this method returns type A if <paramref name="beforeConversion"/> is true, and C if false.
        /// </summary>
        public static INamedTypeSymbol GetReceiverType(this IInvocationOperation invocation, Compilation compilation, bool beforeConversion, CancellationToken cancellationToken)
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

        private static INamedTypeSymbol GetReceiverType(SyntaxNode receiverSyntax, Compilation compilation, CancellationToken cancellationToken)
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

        public static ITypeSymbol GetElementType(this IArrayCreationOperation arrayCreation)
        {
            return (arrayCreation?.Type as IArrayTypeSymbol)?.ElementType;
        }

        /// <summary>
        /// Filters out operations that are implicit and have no explicit descendant with a constant value or a non-null type.
        /// </summary>
        public static ImmutableArray<IOperation> WithoutFullyImplicitOperations(this ImmutableArray<IOperation> operations)
        {
            ImmutableArray<IOperation>.Builder builder = null;
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
        /// Gets all valid members of the block operation body, excluding the VB implicit label and return statements.
        /// </summary>
        public static ImmutableArray<IOperation> GetOperations(this ImmutableArray<IOperation> blockOperations)
        {
            if (blockOperations.IsDefaultOrEmpty)
            {
                return blockOperations;
            }

            if (blockOperations.Length > 1 && blockOperations[0].Language == LanguageNames.VisualBasic)
            {
                var lastOperation = blockOperations[blockOperations.Length - 1];
                var secondLastOperation = blockOperations[blockOperations.Length - 2];

                if (lastOperation.Kind == OperationKind.Return && lastOperation.IsImplicit &&
                    secondLastOperation.Kind == OperationKind.Labeled &&
                    ((ILabeledOperation)secondLastOperation).Label.Name == "exit" &&
                    secondLastOperation.IsImplicit)
                {
                    var builder = ImmutableArray.CreateBuilder<IOperation>();
                    builder.AddRange(blockOperations, blockOperations.Length - 2);
                    return builder.ToImmutable();
                }
                else
                {
                    return blockOperations;
                }
            }
            else
            {
                return blockOperations;
            }
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
        public static IBlockOperation GetTopmostParentBlock(this IOperation operation)
        {
            IOperation currentOperation = operation;
            IBlockOperation topmostBlockOperation = null;
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
        public static TOperation GetAncestor<TOperation>(this IOperation root, OperationKind ancestorKind, Func<TOperation, bool> predicateOpt = null) where TOperation : IOperation
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            IOperation ancestor = root;
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

        public static IConditionalAccessOperation GetConditionalAccess(this IConditionalAccessInstanceOperation operation)
        {
            return operation.GetAncestor(OperationKind.ConditionalAccess, (IConditionalAccessOperation c) => c.Operation.Syntax == operation.Syntax);
        }

        /// <summary>
        /// Gets the operation for the object being created that is being referenced by <paramref name="operation"/>.
        /// If the operation is referencing an implicit or an explicit this/base/Me/MyBase/MyClass instance, then we return "null".
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="isInsideObjectInitializer">Flag to indicate if the operation is a descendant of an <see cref="IObjectOrCollectionInitializerOperation"/> or an <see cref="IAnonymousObjectCreationOperation"/>.</param>
        /// <remarks>
        /// PERF: Note that the parameter <paramref name="isInsideObjectInitializer"/> is to improve performance by avoiding walking the entire IOperation parent for non-initializer cases.
        /// </remarks>
        public static IOperation GetInstance(this IInstanceReferenceOperation operation, bool isInsideObjectInitializer)
        {
            Debug.Assert(isInsideObjectInitializer ==
                (operation.GetAncestor<IObjectOrCollectionInitializerOperation>(OperationKind.ObjectOrCollectionInitializer) != null ||
                 operation.GetAncestor<IAnonymousObjectCreationOperation>(OperationKind.AnonymousObjectCreation) != null));

            if (isInsideObjectInitializer)
            {
                for (IOperation current = operation; current != null && current.Kind != OperationKind.Block; current = current.Parent)
                {
                    switch (current.Kind)
                    {
                        // VB object initializer allows accessing the members of the object being created with "." operator.
                        // The syntax of such an IInstanceReferenceOperation points to the object being created.
                        // Check for such an  IObjectCreationOperation or IAnonymousObjectCreationOperation with matching syntax.
                        // For example, instance reference for members ".Field1" and ".Field2" in "New C() With { .Field1 = 0, .Field2 = .Field1 }".
                        case OperationKind.ObjectCreation:
                        case OperationKind.AnonymousObjectCreation:
                            if (current.Syntax == operation.Syntax)
                            {
                                return current;
                            }

                            break;

                        // IInstanceReferenceOperation on left of an IMemberInitializerOperation refers to the ancestor IObjectCreationOperation/IAnonymousObjectCreationOperation/IMemberInitializerOperation.
                        // For example, implicit instance reference for member initializer "AnotherType" in "new C() { AnotherType = { IntField = 0 } };", where "AnotherType" is a member of named type kind.
                        case OperationKind.MemberInitializer:
                            var parentMemberInitializer = (IMemberInitializerOperation)current;
                            if (parentMemberInitializer.InitializedMember.DescendantsAndSelf().Contains(operation))
                            {
                                return parentMemberInitializer.GetCreation();
                            }

                            break;

                        // IInstanceReferenceOperation on left of an ISimpleAssignmentOperation with an IObjectOrCollectionInitializerOperation parent refers to the parenting IObjectCreationOperation/IAnonymousObjectCreationOperation/IMemberInitializerOperation.
                        // For example, implicit instance reference for "IntField" in "new C() { IntField = 0 };".
                        case OperationKind.SimpleAssignment:
                            var parentSimpleAssignmentInitialier = (ISimpleAssignmentOperation)current;
                            if (parentSimpleAssignmentInitialier.Parent is IObjectOrCollectionInitializerOperation &&
                                parentSimpleAssignmentInitialier.Target.DescendantsAndSelf().Contains(operation))
                            {
                                return parentSimpleAssignmentInitialier.Parent.Parent;
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
        /// Gets the object creation or anonymous object creation for the given member initializer.
        /// </summary>
        public static IOperation GetCreation(this IMemberInitializerOperation operation)
        {
            Debug.Assert(operation.Parent is IObjectOrCollectionInitializerOperation);
            return operation.Parent.Parent;
        }

        /// <summary>
        /// Workaround for https://github.com/dotnet/roslyn/issues/22736 (IPropertyReferenceExpressions in IAnonymousObjectCreationExpression are missing a receiver).
        /// Gets the instance for the anonymous object being created that is being referenced by <paramref name="operation"/>.
        /// Otherwise, returns null
        /// </summary>
        public static IAnonymousObjectCreationOperation GetAnonymousObjectCreation(this IPropertyReferenceOperation operation)
        {
            if (operation.Instance == null &&
                operation.Property.ContainingType.IsAnonymousType)
            {
                var declarationSyntax = operation.Property.ContainingType.DeclaringSyntaxReferences[0].GetSyntax();
                return operation.GetAncestor(OperationKind.AnonymousObjectCreation, (IAnonymousObjectCreationOperation a) => a.Syntax == declarationSyntax);
            }

            return null;
        }

        public static bool IsInsideCatchClause(this IOperation operation)
            => operation.GetAncestor<ICatchClauseOperation>(OperationKind.CatchClause) != null;

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
            Debug.Assert(operationBlock != null);
            Debug.Assert(predicate != null);
            foreach (var descendant in operationBlock.DescendantsAndSelf())
            {
                if (predicate(descendant))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAnyOperationDescendant(this ImmutableArray<IOperation> operationBlocks, OperationKind kind)
        {
            return operationBlocks.HasAnyOperationDescendant(predicate: operation => operation.Kind == kind);
        }

        /// <summary>
        /// Indicates if the given <paramref name="binaryOperation"/> is a ConditionalAnd/ConditionalOr operator ('&&' or '||').
        /// </summary>
        /// <param name="binaryOperation"></param>
        /// <returns></returns>
        public static bool IsConditionalOperator(this IBinaryOperation binaryOperation) => binaryOperation.IsConditionalAndOperator() || binaryOperation.IsConditionalOrOperator();

        /// <summary>
        /// Indicates if the given <paramref name="binaryOperation"/> is a ConditionalAnd operator ('&&').
        /// </summary>
        /// <param name="binaryOperation"></param>
        /// <returns></returns>
        public static bool IsConditionalAndOperator(this IBinaryOperation binaryOperation)
        {
            switch (binaryOperation.OperatorKind)
            {
                case BinaryOperatorKind.ConditionalAnd:
                    return true;

                case BinaryOperatorKind.And:
                    // Workaround for https://github.com/dotnet/roslyn/issues/23956
                    return binaryOperation.Type.SpecialType == SpecialType.System_Boolean;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Indicates if the given <paramref name="binaryOperation"/> is a ConditionalOr operator ('||').
        /// </summary>
        /// <param name="binaryOperation"></param>
        /// <returns></returns>
        public static bool IsConditionalOrOperator(this IBinaryOperation binaryOperation)
        {
            switch (binaryOperation.OperatorKind)
            {
                case BinaryOperatorKind.ConditionalOr:
                    return true;

                case BinaryOperatorKind.Or:
                    // Workaround for https://github.com/dotnet/roslyn/issues/23956
                    return binaryOperation.Type.SpecialType == SpecialType.System_Boolean;

                default:
                    return false;
            }
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

        public static ITypeSymbol GetExceptionType(this IThrowOperation throwOperation)
        {
            if (throwOperation.Exception != null)
            {
                return throwOperation.Exception.Type;
            }

            var catchOperation = throwOperation.GetAncestor<ICatchClauseOperation>(OperationKind.CatchClause);
            return catchOperation?.ExceptionType;
        }

        public static bool IsLambdaOrLocalFunctionOrDelegateInvocation(this IInvocationOperation operation)
            => operation.TargetMethod.IsLambdaOrLocalFunctionOrDelegate();

        public static bool IsLambdaOrLocalFunctionOrDelegateReference(this IMethodReferenceOperation operation)
            => operation.Method.IsLambdaOrLocalFunctionOrDelegate();

        /// <summary>
        /// Gets the <see cref="InstanceReferenceKind"/> for the given <paramref name="instance"/> operation.
        /// </summary>
        /// <remarks>
        /// NOTE: Currently, we return incorrect <see cref="InstanceReferenceKind"/> for anonymous instance property reference
        ///       due to https://github.com/dotnet/roslyn/issues/22736 (IPropertyReferenceExpressions in IAnonymousObjectCreationExpression are missing a receiver).
        ///       Use <see cref="GetAnonymousObjectCreation(IPropertyReferenceOperation)"/> as a workaround.
        /// </remarks>
        public static InstanceReferenceKind GetInstanceReferenceKind(this IOperation instance)
        {
            if (instance == null || instance.Kind != OperationKind.InstanceReference)
            {
                return InstanceReferenceKind.None;
            }

            var text = instance.Syntax.ToString();

            // Ignore case for VB by converting ToUpperInvariant.
            if (instance.Language == LanguageNames.VisualBasic)
            {
                text = text.ToUpperInvariant();
            }

            switch (text)
            {
                case "this":
                    if (instance.Language == LanguageNames.CSharp)
                    {
                        return InstanceReferenceKind.This;
                    }

                    break;

                case "ME":
                    if (instance.Language == LanguageNames.VisualBasic)
                    {
                        return InstanceReferenceKind.This;
                    }

                    break;

                case "base":
                    if (instance.Language == LanguageNames.CSharp)
                    {
                        return InstanceReferenceKind.Base;
                    }

                    break;

                case "MYBASE":
                    if (instance.Language == LanguageNames.VisualBasic)
                    {
                        return InstanceReferenceKind.Base;
                    }

                    break;

                case "MYCLASS":
                    if (instance.Language == LanguageNames.VisualBasic)
                    {
                        return InstanceReferenceKind.MyClass;
                    }

                    break;
            }

            return InstanceReferenceKind.Creation;
        }

        public static bool IsWithinLambdaOrLocalFunction(this IOperation operation)
            => operation.GetAncestor<IAnonymousFunctionOperation>(OperationKind.AnonymousFunction) != null ||
               operation.GetAncestor<ILocalFunctionOperation>(OperationKind.LocalFunction) != null;
    }
}
