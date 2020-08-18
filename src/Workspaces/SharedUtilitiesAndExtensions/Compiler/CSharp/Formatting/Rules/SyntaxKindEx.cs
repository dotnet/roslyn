// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    /// <summary>
    /// This class allows the Code Style layer to light up support for new language features when available at runtime
    /// while compiling against an older version of the Roslyn assemblies.
    /// </summary>
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Private constants are used for static assertions.")]
    internal static class SyntaxKindEx
    {
        public const SyntaxKind ManagedKeyword = (SyntaxKind)8445;
        public const SyntaxKind UnmanagedKeyword = (SyntaxKind)8446;
        public const SyntaxKind FunctionPointerParameter = (SyntaxKind)9057;
        public const SyntaxKind FunctionPointerParameterList = (SyntaxKind)9058;
        public const SyntaxKind FunctionPointerCallingConvention = (SyntaxKind)9059;
        public const SyntaxKind FunctionPointerUnmanagedCallingConventionList = (SyntaxKind)9066;
        public const SyntaxKind FunctionPointerUnmanagedCallingConvention = (SyntaxKind)9067;

#if !CODE_STYLE
        /// <summary>
        /// This will only compile if <see cref="ManagedKeyword"/> and <see cref="SyntaxKind.ManagedKeyword"/> have the same
        /// value.
        /// </summary>
        /// <remarks>
        /// <para>The subtraction will overflow if <see cref="SyntaxKind.ManagedKeyword"/> is greater, and the conversion
        /// to an unsigned value after negation will overflow if <see cref="ManagedKeyword"/> is greater.</para>
        /// </remarks>
        private const uint ManagedKeywordValueAssertion = -(ManagedKeyword - SyntaxKind.ManagedKeyword);
        private const uint UnmanagedKeywordValueAssertion = -(UnmanagedKeyword - SyntaxKind.UnmanagedKeyword);
        private const uint FunctionPointerParameterValueAssertion = -(FunctionPointerParameter - SyntaxKind.FunctionPointerParameter);
        private const uint FunctionPointerParameterListValueAssertion = -(FunctionPointerParameterList - SyntaxKind.FunctionPointerParameterList);
        private const uint FunctionPointerCallingConventionValueAssertion = -(FunctionPointerCallingConvention - SyntaxKind.FunctionPointerCallingConvention);
        private const uint FunctionPointerUnmanagecCallingConventionListValueAssertion = -(FunctionPointerUnmanagedCallingConventionList - SyntaxKind.FunctionPointerUnmanagedCallingConventionList);
        private const uint FunctionPointerUnmanagecCallingConventionValueAssertion = -(FunctionPointerUnmanagedCallingConvention - SyntaxKind.FunctionPointerUnmanagedCallingConvention);
#endif
    }
}
