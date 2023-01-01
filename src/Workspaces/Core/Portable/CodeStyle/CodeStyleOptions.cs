// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable RS0030 // Do not used banned APIs: PerLanguageOption<T>

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <inheritdoc cref="CodeStyleOptions2"/>
    public class CodeStyleOptions
    {
        /// <inheritdoc cref="CodeStyleOptions2.QualifyFieldAccess"/>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyFieldAccess =
            CodeStyleOptions2.QualifyFieldAccess.ToPublicOption();

        /// <inheritdoc cref="CodeStyleOptions2.QualifyPropertyAccess"/>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyPropertyAccess =
            CodeStyleOptions2.QualifyPropertyAccess.ToPublicOption();

        /// <inheritdoc cref="CodeStyleOptions2.QualifyMethodAccess"/>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyMethodAccess =
            CodeStyleOptions2.QualifyMethodAccess.ToPublicOption();

        /// <inheritdoc cref="CodeStyleOptions2.QualifyEventAccess"/>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyEventAccess =
            CodeStyleOptions2.QualifyEventAccess.ToPublicOption();

        /// <inheritdoc cref="CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration"/>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIntrinsicPredefinedTypeKeywordInDeclaration =
            CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration.ToPublicOption();

        /// <inheritdoc cref="CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess"/>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIntrinsicPredefinedTypeKeywordInMemberAccess =
            CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess.ToPublicOption();
    }
}
