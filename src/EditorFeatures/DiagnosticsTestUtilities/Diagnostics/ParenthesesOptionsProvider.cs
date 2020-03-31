// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

#if !CODE_STYLE
using static Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.AbstractCodeActionOrUserDiagnosticTest;
#endif

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
{
    public sealed class ParenthesesOptionsProvider
    {
        private readonly string _language;
        public ParenthesesOptionsProvider(string language)
        {
            _language = language;
        }

        private static readonly CodeStyleOption2<ParenthesesPreference> IgnorePreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption2.None);

        private static readonly CodeStyleOption2<ParenthesesPreference> RequireForPrecedenceClarityPreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.AlwaysForClarity, NotificationOption2.Suggestion);

        private static readonly CodeStyleOption2<ParenthesesPreference> RemoveIfUnnecessaryPreference =
            new CodeStyleOption2<ParenthesesPreference>(ParenthesesPreference.NeverIfUnnecessary, NotificationOption2.Suggestion);

        private static IEnumerable<PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>>> GetAllExceptOtherParenthesesOptions()
        {
            yield return CodeStyleOptions2.ArithmeticBinaryParentheses;
            yield return CodeStyleOptions2.RelationalBinaryParentheses;
            yield return CodeStyleOptions2.OtherBinaryParentheses;
        }

        internal IOptionsCollection RequireArithmeticBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions2.ArithmeticBinaryParentheses);

        internal IOptionsCollection RequireRelationalBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions2.RelationalBinaryParentheses);

        internal IOptionsCollection RequireOtherBinaryParenthesesForClarity
            => GetSingleRequireOption(CodeStyleOptions2.OtherBinaryParentheses);

        private IEnumerable<PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>>> GetAllParenthesesOptions()
            => GetAllExceptOtherParenthesesOptions().Concat(CodeStyleOptions2.OtherParentheses);

        internal IOptionsCollection IgnoreAllParentheses
            => OptionsSet(GetAllParenthesesOptions().Select(
                o => SingleOption(o, IgnorePreference)).ToArray());

        internal IOptionsCollection RemoveAllUnnecessaryParentheses
            => OptionsSet(GetAllParenthesesOptions().Select(
                o => SingleOption(o, RemoveIfUnnecessaryPreference)).ToArray());

        internal IOptionsCollection RequireAllParenthesesForClarity
            => OptionsSet(GetAllExceptOtherParenthesesOptions()
                    .Select(o => SingleOption(o, RequireForPrecedenceClarityPreference))
                    .Concat(SingleOption(CodeStyleOptions2.OtherParentheses, RemoveIfUnnecessaryPreference)).ToArray());

        private IOptionsCollection GetSingleRequireOption(PerLanguageOption2<CodeStyleOption2<ParenthesesPreference>> option)
            => OptionsSet(GetAllParenthesesOptions()
                    .Where(o => o != option)
                    .Select(o => SingleOption(o, RemoveIfUnnecessaryPreference))
                    .Concat(SingleOption(option, RequireForPrecedenceClarityPreference)).ToArray());

        private (OptionKey2, object) SingleOption<T>(PerLanguageOption2<CodeStyleOption2<T>> option, CodeStyleOption2<T> codeStyle)
            => (new OptionKey2(option, _language), codeStyle);

        internal IOptionsCollection OptionsSet(params (OptionKey2 key, object value)[] options)
#if CODE_STYLE
            => new OptionsCollection(_language, options);
#else
            => new OptionsDictionary(options);
#endif
    }
}
