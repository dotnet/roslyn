﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis
{
    internal static partial class OperationExtensions
    {
        public static bool IsTargetOfObjectMemberInitializer(this IOperation operation)
            => operation.Parent is IAssignmentOperation assignmentOperation &&
               assignmentOperation.Target == operation &&
               assignmentOperation.Parent?.Kind == OperationKind.ObjectOrCollectionInitializer;

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
                while (operation.Parent is IBinaryPatternOperation ||
                       operation.Parent is INegatedPatternOperation ||
                       operation.Parent is IRelationalPatternOperation)
                {
                    operation = operation.Parent;
                }

                switch (operation.Parent)
                {
                    case IPatternCaseClauseOperation _:
                        // A declaration pattern within a pattern case clause is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      switch (obj)
                        //      {
                        //          case X x:
                        //
                        return ValueUsageInfo.Write;

                    case IRecursivePatternOperation _:
                        // A declaration pattern within a recursive pattern is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      (obj) switch
                        //      {
                        //          (X x) => ...
                        //      };
                        //
                        return ValueUsageInfo.Write;

                    case ISwitchExpressionArmOperation _:
                        // A declaration pattern within a switch expression arm is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      obj switch
                        //      {
                        //          X x => ...
                        //
                        return ValueUsageInfo.Write;

                    case IIsPatternOperation _:
                        // A declaration pattern within an is pattern is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      if (obj is X x)
                        //
                        return ValueUsageInfo.Write;

                    case IPropertySubpatternOperation _:
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
            else if (operation.Parent is INameOfOperation ||
                     operation.Parent is ITypeOfOperation ||
                     operation.Parent is ISizeOfOperation)
            {
                return ValueUsageInfo.Name;
            }
            else if (operation.Parent is IArgumentOperation argumentOperation)
            {
                switch (argumentOperation.Parameter.RefKind)
                {
                    case RefKind.RefReadOnly:
                        return ValueUsageInfo.ReadableReference;

                    case RefKind.Out:
                        return ValueUsageInfo.WritableReference;

                    case RefKind.Ref:
                        return ValueUsageInfo.ReadableWritableReference;

                    default:
                        return ValueUsageInfo.Read;
                }
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
                return (reDimClauseOperation.Parent as IReDimOperation)?.Preserve == true
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
            else if (operation.Parent is IVariableInitializerOperation variableInitializerOperation)
            {
                if (variableInitializerOperation.Parent is IVariableDeclaratorOperation variableDeclaratorOperation)
                {
                    switch (variableDeclaratorOperation.Symbol.RefKind)
                    {
                        case RefKind.Ref:
                            return ValueUsageInfo.ReadableWritableReference;

                        case RefKind.RefReadOnly:
                            return ValueUsageInfo.ReadableReference;
                    }
                }
            }

            return ValueUsageInfo.Read;
        }

        public static RefKind GetRefKind(this IReturnOperation operation, ISymbol containingSymbol)
        {
            var containingMethod = TryGetContainingAnonymousFunctionOrLocalFunction(operation) ?? (containingSymbol as IMethodSymbol);
            return containingMethod?.RefKind ?? RefKind.None;
        }

        public static IMethodSymbol TryGetContainingAnonymousFunctionOrLocalFunction(this IOperation operation)
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

        public static bool IsInLeftOfDeconstructionAssignment(this IOperation operation, out IDeconstructionAssignmentOperation deconstructionAssignment)
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

        /// <summary>
        /// Retursn true if the given operation is a regular compound assignment,
        /// i.e. <see cref="ICompoundAssignmentOperation"/> such as <code>a += b</code>,
        /// or a special null coalescing compoud assignment, i.e. <see cref="ICoalesceAssignmentOperation"/>
        /// such as <code>a ??= b</code>.
        /// </summary>
        public static bool IsAnyCompoundAssignment(this IOperation operation)
        {
            switch (operation)
            {
                case ICompoundAssignmentOperation _:
                case ICoalesceAssignmentOperation _:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsInsideCatchRegion(this IOperation operation, ControlFlowGraph cfg)
        {
            foreach (var block in cfg.Blocks)
            {
                var isCatchRegionBlock = false;
                var currentRegion = block.EnclosingRegion;
                while (currentRegion != null)
                {
                    switch (currentRegion.Kind)
                    {
                        case ControlFlowRegionKind.Catch:
                            isCatchRegionBlock = true;
                            break;
                    }

                    currentRegion = currentRegion.EnclosingRegion;
                }

                if (isCatchRegionBlock)
                {
                    foreach (var descendant in block.DescendantOperations())
                    {
                        if (operation == descendant)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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
            => operationBlock.HasAnyOperationDescendant(predicate, out _);

        public static bool HasAnyOperationDescendant(this IOperation operationBlock, Func<IOperation, bool> predicate, out IOperation foundOperation)
        {
            Debug.Assert(operationBlock != null);
            Debug.Assert(predicate != null);
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
            => operationBlocks.HasAnyOperationDescendant(predicate: operation => operation.Kind == kind);

        public static bool IsNumericLiteral(this IOperation operation)
            => operation.Kind == OperationKind.Literal && operation.Type.IsNumericType();

        public static bool IsNullLiteral(this IOperation operand)
            => operand is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } };
    }
}
