// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Test.Extensions
{
    internal static class OperationExtensions
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
