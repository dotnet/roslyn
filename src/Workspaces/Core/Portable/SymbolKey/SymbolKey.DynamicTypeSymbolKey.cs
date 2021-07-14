// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                if (reader.Compilation.Language == LanguageNames.VisualBasic)
                {
                    // TODO: We could consider mapping 'dynamic' to 'object' when resolving these types in Visual Basic.
                    // However, this should be driven by an actual scenario that is not working that can be traced down
                    // to this check.
                    failureReason = $"({nameof(DynamicTypeSymbolKey)} is not supported in {LanguageNames.VisualBasic})";
                    return default;
                }

                failureReason = null;
                return new SymbolKeyResolution(reader.Compilation.DynamicType);
            }
        }
    }
}
