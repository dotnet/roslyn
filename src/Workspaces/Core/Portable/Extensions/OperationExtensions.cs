// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
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
        /// <param name="operation">Operation to get value usage info.</param>
        /// <param name="isInErrorContext">
        /// Indicates if the operation is in error context. For example, consider the below invocation:
        ///     `M(ref x);`
        /// If there is an overload resolution failure for the invocation, then the operation tree
        /// has an <see cref="IInvalidOperation"/> for the invocation and argument syntax nodes, and
        /// we cannot correctly determine that 'x' is used in a 'ref' context.
        /// NOTE: We can remove this out flag once https://github.com/dotnet/roslyn/issues/18722 is implemented
        ///       and we have an IInvalidInvocationOperation to handle this case.
        /// </param>
        public static ValueUsageInfo GetValueUsageInfo(this IOperation operation, out bool isInErrorContext)
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

            */

            if (operation == null)
            {
                isInErrorContext = false;
                return ValueUsageInfo.None;
            }

            isInErrorContext = operation.Kind == OperationKind.Invalid;

            switch (operation.Parent)
            {
                case IAssignmentOperation assignmentOperation:
                    if (assignmentOperation.Target == operation)
                    {
                        return operation.Parent.Kind == OperationKind.CompoundAssignment
                            ? ValueUsageInfo.ReadWrite
                            : ValueUsageInfo.Write;
                    }

                    break;

                case IIncrementOrDecrementOperation _:
                    return ValueUsageInfo.ReadWrite;

                case IInvalidOperation invalidOperation:
                    isInErrorContext = true;
                    if (invalidOperation.IsImplicit)
                    {
                        return invalidOperation.GetValueUsageInfo(out _);
                    }

                    break;

                case IConversionOperation conversionOperation:
                    var result = conversionOperation.GetValueUsageInfo(out var isParentInErrorContext) | ValueUsageInfo.Read;
                    isInErrorContext |= isParentInErrorContext;
                    return result;

                case IParenthesizedOperation parenthesizedOperation:
                    // Note: IParenthesizedOperation is specific to VB, where the parens cause a copy, so this cannot be classified as a write.
                    Debug.Assert(parenthesizedOperation.Language == LanguageNames.VisualBasic);

                    return parenthesizedOperation.GetValueUsageInfo(out isInErrorContext) &
                        ~(ValueUsageInfo.Write | ValueUsageInfo.WritableRef);

                case INameOfOperation _:
                case ITypeOfOperation _:
                case ISizeOfOperation _:
                    return ValueUsageInfo.NonReadWriteRef;

                case IArgumentOperation argumentOperation:
                    switch (argumentOperation.Parameter.RefKind)
                    {
                        case RefKind.RefReadOnly:
                            return ValueUsageInfo.ReadableRef;

                        case RefKind.Out:
                            return ValueUsageInfo.WritableRef;

                        case RefKind.Ref:
                            return ValueUsageInfo.ReadableWritableRef;

                        default:
                            return ValueUsageInfo.Read;
                    }

                default:
                    if (IsInLeftOfDeconstructionAssignment(operation))
                    {
                        return ValueUsageInfo.Write;
                    }

                    break;
            }

            return ValueUsageInfo.Read;
        }

        private static bool IsInLeftOfDeconstructionAssignment(IOperation operation)
        {
            var previousOperation = operation;
            operation = operation.Parent;

            while (operation != null)
            {
                switch (operation.Kind)
                {
                    case OperationKind.DeconstructionAssignment:
                        var deconstructionAssignment = (IDeconstructionAssignmentOperation)operation;
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
