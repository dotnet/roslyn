// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class MethodKindEx
    {
        public const MethodKind LocalFunction = (MethodKind)17;

#if HAS_IOPERATION
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable CA1823 // Remove unused private members
        /// <summary>
        /// This will only compile if <see cref="LocalFunction"/> and <see cref="MethodKind.LocalFunction"/> have the
        /// same value.
        /// </summary>
        /// <remarks>
        /// <para>The subtraction in <see cref="LocalFunctionValueAssertion1"/> will overflow if <see cref="MethodKind.LocalFunction"/> is greater, and the conversion
        /// to an unsigned value after negation in <see cref="LocalFunctionValueAssertion2"/> will overflow if <see cref="LocalFunction"/> is greater.</para>
        /// </remarks>
        private const uint LocalFunctionValueAssertion1 = LocalFunction - MethodKind.LocalFunction,
            LocalFunctionValueAssertion2 = -(LocalFunction - MethodKind.LocalFunction);
#endif
    }
}
