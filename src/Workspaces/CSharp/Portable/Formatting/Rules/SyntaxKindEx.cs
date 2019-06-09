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
        public const SyntaxKind RecursivePattern = (SyntaxKind)9020;
        public const SyntaxKind PropertyPatternClause = (SyntaxKind)9021;
        public const SyntaxKind PositionalPatternClause = (SyntaxKind)9023;
        public const SyntaxKind SwitchExpression = (SyntaxKind)9025;
        public const SyntaxKind SwitchExpressionArm = (SyntaxKind)9026;
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
        private const uint RangeExpressionValueAssertion = -(RangeExpression - SyntaxKind.RangeExpression);
        private const uint IndexExpressionValueAssertion = -(IndexExpression - SyntaxKind.IndexExpression);
        private const uint RecursivePatternValueAssertion = -(RecursivePattern - SyntaxKind.RecursivePattern);
        private const uint PropertyPatternClauseValueAssertion = -(PropertyPatternClause - SyntaxKind.PropertyPatternClause);
        private const uint PositionalPatternClauseValueAssertion = -(PositionalPatternClause - SyntaxKind.PositionalPatternClause);
        private const uint SwitchExpressionValueAssertion = -(SwitchExpression - SyntaxKind.SwitchExpression);
        private const uint SwitchExpressionArmValueAssertion = -(SwitchExpressionArm - SyntaxKind.SwitchExpressionArm);
        private const uint SuppressNullableWarningExpressionValueAssertion = -(SuppressNullableWarningExpression - SyntaxKind.SuppressNullableWarningExpression);
#endif
    }
}
