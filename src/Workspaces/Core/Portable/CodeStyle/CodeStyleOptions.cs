// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    public class CodeStyleOptions
    {
        internal const string PerLanguageCodeStyleOption = "CodeStylePerLanguage";

        /// <remarks>
        /// When user preferences are not yet set for a style, we fall back to the default value.
        /// One such default(s), is that the feature is turned on, so that codegen consumes it,
        /// but with none enforcement, so that the user is not prompted about their usage.
        /// </remarks>
        private static readonly CodeStyleOption<bool> trueWithNoneEnforcement = new CodeStyleOption<bool>(value: true, notification: NotificationOption.None);

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

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Declarations
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIntrinsicPredefinedTypeKeywordInDeclaration = new PerLanguageOption<CodeStyleOption<bool>>(PerLanguageCodeStyleOption, nameof(PreferIntrinsicPredefinedTypeKeywordInDeclaration), defaultValue: trueWithNoneEnforcement);

        /// <summary>
        /// This option says if we should prefer keyword for Intrinsic Predefined Types in Member Access Expression
        /// </summary>
        public static readonly PerLanguageOption<CodeStyleOption<bool>> PreferIntrinsicPredefinedTypeKeywordInMemberAccess = new PerLanguageOption<CodeStyleOption<bool>>(PerLanguageCodeStyleOption, nameof(PreferIntrinsicPredefinedTypeKeywordInMemberAccess), defaultValue: trueWithNoneEnforcement);
    }
}
