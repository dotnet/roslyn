// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    /// <summary>
    /// This class allows the Code Style layer to light up support for new language features when available at runtime
    /// while compiling against an older version of the Roslyn assemblies.
    /// </summary>
    internal static class SyntaxKindEx
    {
        public const SyntaxKind DotDotToken = (SyntaxKind)8222;
        public const SyntaxKind RangeExpression = (SyntaxKind)8658;
        public const SyntaxKind IndexExpression = (SyntaxKind)8741;
        public const SyntaxKind SuppressNullableWarningExpression = (SyntaxKind)9054;

#if !CODE_STYLE
        /// <summary>
        /// This will only compile if <see cref="DotDotToken"/> and <see cref="SyntaxKind.DotDotToken"/> have the same
        /// value.
        /// </summary>
        /// <remarks>
        /// <para>The subtraction will overflow if <see cref="SyntaxKind.DotDotToken"/> is greater, and the conversion
        /// to an unsigned value after negation will overflow if <see cref="DotDotToken"/> is greater.</para>
        /// </remarks>
        private const uint DotDotTokenValueAssertion = -(DotDotToken - SyntaxKind.DotDotToken);

        /// <summary>
        /// This will only compile if <see cref="RangeExpression"/> and <see cref="SyntaxKind.RangeExpression"/> have
        /// the same value.
        /// </summary>
        private const uint RangeExpressionValueAssertion = -(RangeExpression - SyntaxKind.RangeExpression);

        /// <summary>
        /// This will only compile if <see cref="IndexExpression"/> and <see cref="SyntaxKind.IndexExpression"/> have
        /// the same value.
        /// </summary>
        private const uint IndexExpressionValueAssertion = -(IndexExpression - SyntaxKind.IndexExpression);

        /// <summary>
        /// This will only compile if <see cref="SuppressNullableWarningExpression"/> and
        /// <see cref="SyntaxKind.SuppressNullableWarningExpression"/> have the same value.
        /// </summary>
        private const uint SuppressNullableWarningExpressionValueAssertion = -(SuppressNullableWarningExpression - SyntaxKind.SuppressNullableWarningExpression);
#endif
    }
}
