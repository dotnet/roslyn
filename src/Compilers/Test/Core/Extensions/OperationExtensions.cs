// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    internal static class OperationTestExtensions
    {
        public static bool MustHaveNullType(this IOperation operation)
        {
            switch (operation.Kind)
            {
                // TODO: Expand to cover all operations that must always have null type.
                case OperationKind.ArrayInitializer:
                case OperationKind.Argument:
                    return true;

                default:
                    return false;
            }
        }
    }
}
