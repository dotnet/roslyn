// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

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
        public static ValueUsageInfo GetValueUsageInfo(this IOperation operation)
        {
            /*
            |    code         | Read | Write | ReadableRef | WritableRef | NonReadWriteRef |
            | x.Prop = 1      |      |  ✔️   |             |             |                 |
            | x.Prop += 1     |  ✔️  |  ✔️   |             |             |                 |
            | x.Prop++        |  ✔️  |  ✔️   |             |             |                 |
            | Foo(x.Prop)     |  ✔️  |       |             |             |                 |
            | Foo(x.Prop),    |      |       |     ✔️      |             |                 |
               where void Foo(in T v)
            | Foo(out x.Prop) |      |       |             |     ✔️      |                 |
            | Foo(ref x.Prop) |      |       |     ✔️      |     ✔️      |                 |
            | nameof(x)       |      |       |             |             |       ✔️        | ️
            | sizeof(x)       |      |       |             |             |       ✔️        | ️
            | typeof(x)       |      |       |             |             |       ✔️        | ️
            | out var x       |      |  ✔️   |             |             |                 | ️
            | case X x:       |      |  ✔️   |             |             |                 | ️
            | obj is X x      |      |  ✔️   |             |             |                 |

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
                    case IPatternCaseClauseOperation _:
                        // A declaration pattern within a pattern case clause is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      switch (obj)
                        //      {
                        //          case X x:
                        //
                        return ValueUsageInfo.Write;

                    case IIsPatternOperation _:
                        // A declaration pattern within an is pattern is a
                        // write for the declared local.
                        // For example, 'x' is defined and assigned the value from 'obj' below:
                        //      if (obj is X x)
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
                return operation.Parent.Kind == OperationKind.CompoundAssignment
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

                return parenthesizedOperation.GetValueUsageInfo() &
                    ~(ValueUsageInfo.Write | ValueUsageInfo.Reference);
            }
            else if (operation.Parent is INameOfOperation ||
                     operation.Parent is ITypeOfOperation ||
                     operation.Parent is ISizeOfOperation)
            {
                return ValueUsageInfo.NameOnly;
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
            else if (operation.Parent is IReDimClauseOperation reDimClauseOperation &&
                reDimClauseOperation.Operand == operation)
            {
                return (reDimClauseOperation.Parent as IReDimOperation)?.Preserve == true
                    ? ValueUsageInfo.ReadWrite
                    : ValueUsageInfo.Write;
            }
            else if (operation.Parent is IDeclarationExpressionOperation declarationExpression)
            {
                return declarationExpression.GetValueUsageInfo();
            }
            else if (operation.IsInLeftOfDeconstructionAssignment(out _))
            {
                return ValueUsageInfo.Write;
            }

            return ValueUsageInfo.Read;
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
    }
}
