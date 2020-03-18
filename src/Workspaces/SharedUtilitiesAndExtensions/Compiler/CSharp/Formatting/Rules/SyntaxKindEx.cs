// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    /// <summary>
    /// This class allows the Code Style layer to light up support for new language features when available at runtime
    /// while compiling against an older version of the Roslyn assemblies.
    /// </summary>
    internal static class SyntaxKindEx
    {
        // The code style layer does not currently need access to any syntax defined in newer versions of Roslyn. This
        // type is included as an example should this change in future updates.
#if false
        public const SyntaxKind DotDotToken = (SyntaxKind)8222;

#if CODE_STYLE
        /// <summary>
        /// This will only compile if <see cref="DotDotToken"/> and <see cref="SyntaxKind.DotDotToken"/> have the same
        /// value.
        /// </summary>
        /// <remarks>
        /// <para>The subtraction will overflow if <see cref="SyntaxKind.DotDotToken"/> is greater, and the conversion
        /// to an unsigned value after negation will overflow if <see cref="DotDotToken"/> is greater.</para>
        /// </remarks>
        private const uint DotDotTokenValueAssertion = -(DotDotToken - SyntaxKind.DotDotToken);
#endif
#endif
    }
}
