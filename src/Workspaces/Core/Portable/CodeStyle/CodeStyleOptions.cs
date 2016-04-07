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
        public static readonly PerLanguageOption<SimpleCodeStyleOption> QualifyFieldAccess = new PerLanguageOption<SimpleCodeStyleOption>(PerLanguageCodeStyleOption, nameof(QualifyFieldAccess), defaultValue: SimpleCodeStyleOption.Default);

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in property access expressions.
        /// </summary>
        public static readonly PerLanguageOption<SimpleCodeStyleOption> QualifyPropertyAccess = new PerLanguageOption<SimpleCodeStyleOption>(PerLanguageCodeStyleOption, nameof(QualifyPropertyAccess), defaultValue: SimpleCodeStyleOption.Default);

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in method access expressions.
        /// </summary>
        public static readonly PerLanguageOption<SimpleCodeStyleOption> QualifyMethodAccess = new PerLanguageOption<SimpleCodeStyleOption>(PerLanguageCodeStyleOption, nameof(QualifyMethodAccess), defaultValue: SimpleCodeStyleOption.Default);

        /// <summary>
        /// This option says if we should simplify away the <see langword="this"/>. or <see langword="Me"/>. in event access expressions.
        /// </summary>
        public static readonly PerLanguageOption<SimpleCodeStyleOption> QualifyEventAccess = new PerLanguageOption<SimpleCodeStyleOption>(PerLanguageCodeStyleOption, nameof(QualifyEventAccess), defaultValue: SimpleCodeStyleOption.Default);
    }
}
