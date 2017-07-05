// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class DynamicTypeSymbolKey
        {
            private static readonly object instance = new object();

            public static void Create(SymbolKeyWriter visitor)
            {
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                return new SymbolKeyResolution(reader.Compilation.DynamicType);
            }
        }
    }
}
