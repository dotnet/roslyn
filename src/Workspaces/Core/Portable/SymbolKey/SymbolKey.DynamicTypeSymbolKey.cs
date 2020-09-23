// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                failureReason = null;
                return new SymbolKeyResolution(reader.Compilation.DynamicType);
            }
        }
    }
}
