// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class OperationExtensions
    {
        public static bool IsNumericLiteral(this IOperation operation)
            => operation.Kind == OperationKind.Literal && operation.Type.IsNumericType();

        public static bool IsNullLiteral(this IOperation operand)
            => operand is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } };
    }
}
