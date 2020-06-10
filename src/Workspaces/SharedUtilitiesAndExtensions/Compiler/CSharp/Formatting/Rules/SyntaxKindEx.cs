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
        public const SyntaxKind ImplicitObjectCreationExpression = (SyntaxKind)8659;
        public const SyntaxKind RelationalPattern = (SyntaxKind)9029;
        public const SyntaxKind ParenthesizedPattern = (SyntaxKind)9028;
        public const SyntaxKind AndPattern = (SyntaxKind)9032;
        public const SyntaxKind OrPattern = (SyntaxKind)9031;
        public const SyntaxKind NotPattern = (SyntaxKind)9033;
        public const SyntaxKind AndKeyword = (SyntaxKind)8439;
        public const SyntaxKind OrKeyword = (SyntaxKind)8438;
        public const SyntaxKind FunctionPointerType = (SyntaxKind)9056;
        public const SyntaxKind NotKeyword = (SyntaxKind)8440;
        public const SyntaxKind DataKeyword = (SyntaxKind)8441;
        public const SyntaxKind RecordDeclaration = (SyntaxKind)9063;

#if !CODE_STYLE
        private const uint ImplicitObjectCreationExpressionAssertion = -(ImplicitObjectCreationExpression - SyntaxKind.ImplicitObjectCreationExpression);

        /// <summary>
        /// This will only compile if <see cref="RelationalPattern"/> and <see cref="SyntaxKind.RelationalPattern"/> have the same
        /// value.
        /// </summary>
        /// <remarks>
        /// <para>The subtraction will overflow if <see cref="SyntaxKind.RelationalPattern"/> is greater, and the conversion
        /// to an unsigned value after negation will overflow if <see cref="RelationalPattern"/> is greater.</para>
        /// </remarks>
        private const uint RelationalPatternValueAssertion = -(RelationalPattern - SyntaxKind.RelationalPattern);
        private const uint DataKeywordValueAssertion = -(DataKeyword - SyntaxKind.DataKeyword);
        private const uint ParenthesizedPatternValueAssertion = -(ParenthesizedPattern - SyntaxKind.ParenthesizedPattern);
        private const uint AndPatternValueAssertion = -(AndPattern - SyntaxKind.AndPattern);
        private const uint OrPatternValueAssertion = -(OrPattern - SyntaxKind.OrPattern);
        private const uint NotPatternValueAssertion = -(NotPattern - SyntaxKind.NotPattern);
        private const uint AndKeywordValueAssertion = -(AndKeyword - SyntaxKind.AndKeyword);
        private const uint OrKeywordValueAssertion = -(OrKeyword - SyntaxKind.OrKeyword);
        private const uint FunctionPointerTypeAssertion = -(FunctionPointerType - SyntaxKind.FunctionPointerType);
        private const uint NotKeywordValueAssertion = -(NotKeyword - SyntaxKind.NotKeyword);
        private const uint RecordDeclarationValueAssertion = -(RecordDeclaration - SyntaxKind.RecordDeclaration);
#endif
    }
}
