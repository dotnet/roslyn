// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
