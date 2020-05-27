// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Shared.Lightup
{
    /// <summary>
    /// This class allows the Code Style layer to light up support for new language features when available at runtime
    /// while compiling against an older version of the Roslyn assemblies.
    /// </summary>
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Private constants are used for static assertions.")]
    internal static class SymbolKindEx
    {
        public const SymbolKind FunctionPointer = (SymbolKind)20;

#if !CODE_STYLE
        /// <summary>
        /// This will only compile if <see cref="FunctionPointer"/> and <see cref="SymbolKind.FunctionPointer"/> have the same
        /// value.
        /// </summary>
        /// <remarks>
        /// <para>The subtraction will overflow if <see cref="SymbolKind.FunctionPointer"/> is greater, and the conversion
        /// to an unsigned value after negation will overflow if <see cref="FunctionPointer"/> is greater.</para>
        /// </remarks>
        private const uint FunctionPointerValueAssertion = -(FunctionPointer - SymbolKind.FunctionPointer);
#endif
    }
}
