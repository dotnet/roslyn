// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    public class CodeStyleOptions
    {
        internal const string PerLanguageCodeStyleOption = "CodeStylePerLanguage";

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in field access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyFieldAccess = new PerLanguageOption<CodeStyleOption<bool>>(PerLanguageCodeStyleOption, nameof(QualifyFieldAccess), defaultValue: CodeStyleOption<bool>.Default);

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyPropertyAccess = new PerLanguageOption<CodeStyleOption<bool>>(PerLanguageCodeStyleOption, nameof(QualifyPropertyAccess), defaultValue: CodeStyleOption<bool>.Default);

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyMethodAccess = new PerLanguageOption<CodeStyleOption<bool>>(PerLanguageCodeStyleOption, nameof(QualifyMethodAccess), defaultValue: CodeStyleOption<bool>.Default);

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> QualifyEventAccess = new PerLanguageOption<CodeStyleOption<bool>>(PerLanguageCodeStyleOption, nameof(QualifyEventAccess), defaultValue: CodeStyleOption<bool>.Default);
    }
}
