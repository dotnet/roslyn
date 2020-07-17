// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    /// <summary>
    /// This class allows the Code Style layer to light up support for new language features when available at runtime
    /// while compiling against an older version of the Roslyn assemblies.
    /// </summary>
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Private constants are used for static assertions.")]
    internal static class SymbolKindEx
    {
        public const SymbolKind FunctionPointerType = (SymbolKind)20;

#if !CODE_STYLE
        // This will overflow if the kinds don't match up.
        private const uint FunctionPointerValueAssertion = -(FunctionPointerType - SymbolKind.FunctionPointerType);
#endif
    }
}
