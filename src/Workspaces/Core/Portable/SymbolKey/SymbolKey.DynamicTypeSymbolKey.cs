// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class DynamicTypeSymbolKey
        {
            public static void Create(SymbolKeyWriter _)
            {
                // No need to encode anything here.  There is only ever one dynamic-symbol
                // per compilation.
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
                => new SymbolKeyResolution(reader.Compilation.DynamicType);
        }
    }
}
