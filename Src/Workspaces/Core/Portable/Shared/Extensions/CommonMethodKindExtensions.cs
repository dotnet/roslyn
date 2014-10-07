// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class CommonMethodKindExtensions
    {
        public static bool IsPropertyAccessor(this MethodKind kind)
        {
            return kind == MethodKind.PropertyGet || kind == MethodKind.PropertySet;
        }
    }
}