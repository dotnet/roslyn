// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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
            | obj is { } x             |      |  ✔️   |             |             |                 |
            | obj is [] x              |      |  ✔️   |             |             |                 |
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
                while (operation.Parent is IBinaryPatternOperation or
                       INegatedPatternOperation or
                       IRelationalPatternOperation)
                {
                    operation = operation.Parent;
                }

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
            else if (operation is IRecursivePatternOperation or IListPatternOperation)
            {
                return ValueUsageInfo.Write;
            }

            if (operation.Parent is IAssignmentOperation assignmentOperation &&
                assignmentOperation.Target == operation)
            {
                return operation.Parent.IsAnyCompoundAssignment()
                    ? ValueUsageInfo.ReadWrite
                    : ValueUsageInfo.Write;
            }
            else if (operation.Parent is ISimpleAssignmentOperation simpleAssignmentOperation &&
                simpleAssignmentOperation.Value == operation &&
                simpleAssignmentOperation.IsRef)
            {
                return ValueUsageInfo.ReadableWritableReference;
            }
            else if (operation.Parent is IIncrementOrDecrementOperation || (operation.Parent is IForToLoopOperation forToLoopOperation && forToLoopOperation.LoopControlVariable.Equals(operation)))
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
                switch (argumentOperation.Parameter?.RefKind)
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

        public static RefKind GetRefKind(this IReturnOperation? operation, ISymbol containingSymbol)
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

        public static bool IsInLeftOfDeconstructionAssignment(this IOperation operation, [NotNullWhen(true)] out IDeconstructionAssignmentOperation? deconstructionAssignment)
        {
            deconstructionAssignment = null;

            var previousOperation = operation;
            var current = operation.Parent;

            while (current != null)
            {
                switch (current.Kind)
                {
                    case OperationKind.DeconstructionAssignment:
                        deconstructionAssignment = (IDeconstructionAssignmentOperation)current;
                        return deconstructionAssignment.Target == previousOperation;

                    case OperationKind.Tuple:
                    case OperationKind.Conversion:
                    case OperationKind.Parenthesized:
                        previousOperation = current;
                        current = current.Parent;
                        continue;

                    default:
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the given operation is a regular compound assignment,
        /// i.e. <see cref="ICompoundAssignmentOperation"/> such as <code>a += b</code>,
        /// or a special null coalescing compound assignment, i.e. <see cref="ICoalesceAssignmentOperation"/>
        /// such as <code>a ??= b</code>.
        /// </summary>
        public static bool IsAnyCompoundAssignment(this IOperation operation)
        {
            switch (operation)
            {
                case ICompoundAssignmentOperation:
                case ICoalesceAssignmentOperation:
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

        public static bool HasAnyOperationDescendant(this IOperation operationBlock, Func<IOperation, bool> predicate, [NotNullWhen(true)] out IOperation? foundOperation)
        {
            RoslynDebug.AssertNotNull(operationBlock);
            RoslynDebug.AssertNotNull(predicate);
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

        /// <summary>
        /// Walks down consecutive conversion operations until an operand is reached that isn't a conversion operation.
        /// </summary>
        /// <param name="operation">The starting operation.</param>
        /// <returns>The inner non conversion operation or the starting operation if it wasn't a conversion operation.</returns>
        [return: NotNullIfNotNull(nameof(operation))]
        public static IOperation? WalkDownConversion(this IOperation? operation)
        {
            while (operation is IConversionOperation conversionOperation)
            {
                operation = conversionOperation.Operand;
            }

            return operation;
        }

        public static bool IsSingleThrowNotImplementedOperation([NotNullWhen(true)] this IOperation? firstBlock)
        {
            if (firstBlock is null)
                return false;

            var compilation = firstBlock.SemanticModel!.Compilation;
            var notImplementedExceptionType = compilation.NotImplementedExceptionType();
            if (notImplementedExceptionType == null)
                return false;

            if (firstBlock is not IBlockOperation block)
                return false;

            if (block.Operations.Length == 0)
                return false;

            var firstOp = block.Operations.Length == 1
                ? block.Operations[0]
                : TryGetSingleExplicitStatement(block.Operations);
            if (firstOp == null)
                return false;

            if (firstOp is IExpressionStatementOperation expressionStatement)
            {
                // unwrap: { throw new NYI(); }
                firstOp = expressionStatement.Operation;
            }
            else if (firstOp is IReturnOperation returnOperation)
            {
                // unwrap: 'int M(int p) => throw new NYI();'
                // For this case, the throw operation is wrapped within a conversion operation to 'int',
                // which in turn is wrapped within a return operation.
                firstOp = returnOperation.ReturnedValue.WalkDownConversion();
            }

            // => throw new NotImplementedOperation(...)
            return IsThrowNotImplementedOperation(notImplementedExceptionType, firstOp);

            static IOperation? TryGetSingleExplicitStatement(ImmutableArray<IOperation> operations)
            {
                IOperation? firstOp = null;
                foreach (var operation in operations)
                {
                    if (operation.IsImplicit)
                        continue;

                    if (firstOp != null)
                        return null;

                    firstOp = operation;
                }

                return firstOp;
            }

            static bool IsThrowNotImplementedOperation(INamedTypeSymbol notImplementedExceptionType, IOperation? operation)
                => operation is IThrowOperation throwOperation &&
                   throwOperation.Exception.UnwrapImplicitConversion() is IObjectCreationOperation objectCreation &&
                   notImplementedExceptionType.Equals(objectCreation.Type);
        }

        [return: NotNullIfNotNull(nameof(value))]
        public static IOperation? UnwrapImplicitConversion(this IOperation? value)
            => value is IConversionOperation conversion && conversion.IsImplicit
                ? conversion.Operand
                : value;
    }
}
