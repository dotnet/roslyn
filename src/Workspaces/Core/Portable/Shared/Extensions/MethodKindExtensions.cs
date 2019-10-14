// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class MethodKindExtensions
    {
        public static bool IsPropertyAccessor(this MethodKind kind)
        {
            return kind == MethodKind.PropertyGet || kind == MethodKind.PropertySet;
        }
    }
}
